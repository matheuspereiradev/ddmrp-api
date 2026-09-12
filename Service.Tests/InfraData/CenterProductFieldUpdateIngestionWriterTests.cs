using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Ingestion;
using Service.Infra.Data.Context;
using Service.Infra.Data.Ingestion.Writers;

namespace Service.Tests.InfraData;

public class CenterProductFieldUpdateIngestionWriterTests
{
    private static (CenterProductFieldUpdateIngestionWriter writer, ApplicationDbContext context) CreateSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(1);

        var writer = new CenterProductFieldUpdateIngestionWriter(context, currentUser);
        return (writer, context);
    }

    private static IngestionSourceConfig BuildSource(string view, string target) => new()
    {
        View = view,
        Type = "Csv",
        Path = "test.csv",
        Table = "CenterProducts",
        Key = ["IdProduct", "IdCenter"],
        FieldMappings =
        [
            new FieldMappingConfig { Source = "MaterialCode", Target = "IdProduct" },
            new FieldMappingConfig { Source = "CenterCode", Target = "IdCenter" },
            new FieldMappingConfig { Source = "Quantity", Target = target }
        ]
    };

    [Fact]
    public void CanHandle_ReturnsTrue_WhenTableIsCenterProducts_RegardlessOfViewName()
    {
        var (writer, _) = CreateSut();
        var source = BuildSource("Stock", "Stock");

        Assert.True(writer.CanHandle(source));
    }

    [Fact]
    public void CanHandle_ReturnsFalse_WhenTableIsNotSet()
    {
        var (writer, _) = CreateSut();
        var source = new IngestionSourceConfig { View = "CenterProducts", Type = "Csv", Path = "test.csv" };

        Assert.False(writer.CanHandle(source));
    }

    [Fact]
    public async Task WriteAsync_UpdatesOnlyTheMappedField_OnExistingCenterProduct()
    {
        var (writer, context) = CreateSut();
        var centerProduct = new CenterProduct { IdProduct = 1, IdCenter = 2, PackQuantity = 5, Stock = 0 };
        context.CenterProduct.Add(centerProduct);
        await context.SaveChangesAsync();

        var source = BuildSource("Stock", "Stock");
        var rows = new List<Dictionary<string, string?>>
        {
            new() { ["IdProduct"] = "1", ["IdCenter"] = "2", ["Stock"] = "42.5" }
        };

        var result = await writer.WriteAsync(source, rows);

        Assert.Equal(1, result.Updated);
        Assert.Empty(result.Errors);
        var updated = await context.CenterProduct.FindAsync(centerProduct.Id);
        Assert.Equal(42.5m, updated!.Stock);
        Assert.Equal(5, updated.PackQuantity);
    }

    [Fact]
    public async Task WriteAsync_NeverInserts_WhenNoMatchingCenterProductExists()
    {
        var (writer, context) = CreateSut();
        var source = BuildSource("Stock", "Stock");
        var rows = new List<Dictionary<string, string?>>
        {
            new() { ["IdProduct"] = "1", ["IdCenter"] = "2", ["Stock"] = "10" }
        };

        var result = await writer.WriteAsync(source, rows);

        Assert.Equal(0, result.Updated);
        Assert.Single(result.Errors);
        Assert.Contains("no CenterProduct found", result.Errors[0]);
        Assert.Empty(context.CenterProduct);
    }

    [Fact]
    public async Task WriteAsync_Throws_WhenMappedFieldIsNotAnUpdatableCenterProductProperty()
    {
        var (writer, _) = CreateSut();
        var source = BuildSource("Stock", "NotARealField");
        var rows = new List<Dictionary<string, string?>>
        {
            new() { ["IdProduct"] = "1", ["IdCenter"] = "2", ["NotARealField"] = "10" }
        };

        await Assert.ThrowsAsync<NotSupportedException>(() => writer.WriteAsync(source, rows));
    }

    [Fact]
    public async Task WriteAsync_Throws_WhenMappedFieldIsAProtectedField()
    {
        var (writer, _) = CreateSut();
        var source = BuildSource("Stock", "Adu");
        var rows = new List<Dictionary<string, string?>>
        {
            new() { ["IdProduct"] = "1", ["IdCenter"] = "2", ["Adu"] = "10" }
        };

        await Assert.ThrowsAsync<NotSupportedException>(() => writer.WriteAsync(source, rows));
    }
}
