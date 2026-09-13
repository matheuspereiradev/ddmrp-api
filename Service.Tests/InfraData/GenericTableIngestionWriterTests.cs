using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Ingestion;
using Service.Infra.Data.Context;
using Service.Infra.Data.Ingestion.Writers;

namespace Service.Tests.InfraData;

public class GenericTableIngestionWriterTests
{
    private static (GenericTableIngestionWriter writer, ApplicationDbContext context) CreateSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(1);

        var writer = new GenericTableIngestionWriter(context, currentUser);
        return (writer, context);
    }

    private static IngestionSourceConfig BuildStockSource() => new()
    {
        View = "Stock",
        Type = "Csv",
        Path = "test.csv",
        Table = "CenterProducts",
        AllowInsert = false,
        Key = ["IdProduct", "IdCenter"],
        FieldMappings =
        [
            new FieldMappingConfig { Source = "MaterialCode", Target = "IdProduct" },
            new FieldMappingConfig { Source = "CenterCode", Target = "IdCenter" },
            new FieldMappingConfig { Source = "Quantity", Target = "Stock" }
        ]
    };

    [Fact]
    public void CanHandle_ReturnsTrue_ForAnAllowedResolvableTable()
    {
        var (writer, _) = CreateSut();

        Assert.True(writer.CanHandle(BuildStockSource()));
    }

    [Fact]
    public void CanHandle_ReturnsFalse_ForOrders_EvenThoughItsAValidTable()
    {
        // Orders has its own dedicated OrderIngestionWriter (Type-scoped, fictional-excluding
        // delete-non-sent business logic) — it must never be picked up by the generic engine too.
        var (writer, _) = CreateSut();
        var source = new IngestionSourceConfig { View = "Whatever", Type = "Csv", Path = "test.csv", Table = "Orders" };

        Assert.False(writer.CanHandle(source));
    }

    [Fact]
    public void CanHandle_ReturnsFalse_WhenTableIsNotOnTheAllowList()
    {
        var (writer, _) = CreateSut();
        var source = new IngestionSourceConfig { View = "Whatever", Type = "Csv", Path = "test.csv", Table = "Users" };

        Assert.False(writer.CanHandle(source));
    }

    [Fact]
    public void CanHandle_ReturnsFalse_WhenTableIsNotSet()
    {
        var (writer, _) = CreateSut();
        var source = new IngestionSourceConfig { View = "Whatever", Type = "Csv", Path = "test.csv" };

        Assert.False(writer.CanHandle(source));
    }

    [Fact]
    public async Task WriteAsync_UpdatesOnlyTheMappedField_WhenExistingRowMatches()
    {
        var (writer, context) = CreateSut();
        var centerProduct = new CenterProduct { IdProduct = 1, IdCenter = 2, PackQuantity = 5, Stock = 0 };
        context.CenterProduct.Add(centerProduct);
        await context.SaveChangesAsync();

        var rows = new List<Dictionary<string, string?>>
        {
            new() { ["IdProduct"] = "1", ["IdCenter"] = "2", ["Stock"] = "42.5" }
        };

        var result = await writer.WriteAsync(BuildStockSource(), rows);

        Assert.Equal(1, result.Updated);
        Assert.Empty(result.Errors);
        var updated = await context.CenterProduct.FindAsync(centerProduct.Id);
        Assert.Equal(42.5m, updated!.Stock);
        Assert.Equal(5, updated.PackQuantity);
    }

    [Fact]
    public async Task WriteAsync_NeverInserts_WhenAllowInsertIsFalse_AndNoMatchingRowExists()
    {
        var (writer, context) = CreateSut();
        var rows = new List<Dictionary<string, string?>>
        {
            new() { ["IdProduct"] = "1", ["IdCenter"] = "2", ["Stock"] = "10" }
        };

        var result = await writer.WriteAsync(BuildStockSource(), rows);

        Assert.Equal(0, result.Updated);
        Assert.Single(result.Errors);
        Assert.Contains("doesn't create new rows", result.Errors[0]);
        Assert.Empty(context.CenterProduct);
    }

    [Fact]
    public async Task WriteAsync_Throws_WhenMappedFieldIsNotRecognizedForTheTable()
    {
        var (writer, _) = CreateSut();
        var source = BuildStockSource();
        source.FieldMappings.Add(new FieldMappingConfig { Source = "Nope", Target = "NotARealField" });
        var rows = new List<Dictionary<string, string?>>
        {
            new() { ["IdProduct"] = "1", ["IdCenter"] = "2", ["Stock"] = "10", ["NotARealField"] = "x" }
        };

        await Assert.ThrowsAsync<NotSupportedException>(() => writer.WriteAsync(source, rows));
    }

    [Fact]
    public async Task WriteAsync_Throws_WhenDeclaredKeyFieldIsNotRecognizedForTheTable()
    {
        var (writer, _) = CreateSut();
        var source = BuildStockSource();
        source.Key = ["NotARealField"];
        var rows = new List<Dictionary<string, string?>>
        {
            new() { ["IdProduct"] = "1", ["IdCenter"] = "2", ["Stock"] = "10" }
        };

        await Assert.ThrowsAsync<NotSupportedException>(() => writer.WriteAsync(source, rows));
    }
}
