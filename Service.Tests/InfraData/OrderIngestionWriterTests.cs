using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Ingestion;
using Service.Infra.Data.Context;
using Service.Infra.Data.Ingestion.Writers;

namespace Service.Tests.InfraData;

public class OrderIngestionWriterTests
{
    private static (OrderIngestionWriter writer, ApplicationDbContext context) CreateSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(1);

        var writer = new OrderIngestionWriter(context, currentUser);
        return (writer, context);
    }

    private static IngestionSourceConfig BuildProductionOrderSource() => new()
    {
        View = "ProductionOrder",
        Type = "Csv",
        Path = "test.csv",
        Table = "Orders",
        Key = ["OrderNumber", "IdDestinyCenter", "IdProduct"],
        FieldMappings =
        [
            new FieldMappingConfig { Source = "OrderNumber", Target = "OrderNumber" },
            new FieldMappingConfig { Source = "Center", Target = "IdDestinyCenter" },
            new FieldMappingConfig { Source = "Product", Target = "IdProduct" },
            new FieldMappingConfig { Source = "Quantity", Target = "Quantity" },
            new FieldMappingConfig { Source = "MeasurementUnit", Target = "MeasurementUnit" },
            new FieldMappingConfig { Source = null, Target = "Type", Default = "ProductionOrder" },
            new FieldMappingConfig { Source = null, Target = "IsInbound", Default = "true" },
            new FieldMappingConfig { Source = null, Target = "IsOutbound", Default = "false" }
        ]
    };

    private static Dictionary<string, string?> BuildRow(string orderNumber, int idDestinyCenter, int idProduct, string quantity = "100") => new()
    {
        ["OrderNumber"] = orderNumber,
        ["IdDestinyCenter"] = idDestinyCenter.ToString(),
        ["IdProduct"] = idProduct.ToString(),
        ["Quantity"] = quantity,
        ["MeasurementUnit"] = "UN",
        ["CreationDate"] = DateTime.UtcNow.ToString("o"),
        ["Type"] = "ProductionOrder",
        ["IsInbound"] = "true",
        ["IsOutbound"] = "false"
    };

    [Fact]
    public void CanHandle_ReturnsTrue_WhenTableIsOrders_RegardlessOfViewName()
    {
        var (writer, _) = CreateSut();

        Assert.True(writer.CanHandle(BuildProductionOrderSource()));
    }

    [Fact]
    public void CanHandle_ReturnsFalse_WhenTableIsNotOrders()
    {
        var (writer, _) = CreateSut();
        var source = new IngestionSourceConfig { View = "Whatever", Type = "Csv", Path = "test.csv", Table = "CenterProducts" };

        Assert.False(writer.CanHandle(source));
    }

    [Fact]
    public async Task WriteAsync_InsertsNewOrder_AsNonFictional()
    {
        var (writer, context) = CreateSut();
        var rows = new List<Dictionary<string, string?>> { BuildRow("OP-001", 1, 10) };

        var result = await writer.WriteAsync(BuildProductionOrderSource(), rows);

        Assert.Equal(1, result.Inserted);
        var created = Assert.Single(context.Order);
        Assert.Equal("OP-001", created.OrderNumber);
        Assert.False(created.IsFictional);
    }

    [Fact]
    public async Task WriteAsync_UpdatesExistingOrder_ByCompositeKey()
    {
        var (writer, context) = CreateSut();
        var order = new Order
        {
            OrderNumber = "OP-001",
            IdDestinyCenter = 1,
            IdProduct = 10,
            Quantity = 1m,
            MeasurementUnit = "UN",
            CreationDate = DateTime.UtcNow,
            Type = OrderType.ProductionOrder
        };
        context.Order.Add(order);
        await context.SaveChangesAsync();

        var rows = new List<Dictionary<string, string?>> { BuildRow("OP-001", 1, 10, "500") };

        var result = await writer.WriteAsync(BuildProductionOrderSource(), rows);

        Assert.Equal(1, result.Updated);
        Assert.Equal(0, result.Inserted);
        var updated = await context.Order.FindAsync(order.Id);
        Assert.Equal(500m, updated!.Quantity);
    }

    [Fact]
    public async Task WriteAsync_NeverMatchesOrUpdates_AFictionalOrder_EvenWithTheSameKey()
    {
        var (writer, context) = CreateSut();
        var fictional = new Order
        {
            OrderNumber = "OP-001",
            IdDestinyCenter = 1,
            IdProduct = 10,
            Quantity = 999m,
            MeasurementUnit = "UN",
            CreationDate = DateTime.UtcNow,
            Type = OrderType.ProductionOrder,
            IsFictional = true
        };
        context.Order.Add(fictional);
        await context.SaveChangesAsync();

        var rows = new List<Dictionary<string, string?>> { BuildRow("OP-001", 1, 10, "500") };

        var result = await writer.WriteAsync(BuildProductionOrderSource(), rows);

        // The fictional row is left untouched; a new, real Order is inserted alongside it.
        Assert.Equal(1, result.Inserted);
        Assert.Equal(0, result.Updated);
        Assert.Equal(2, context.Order.Count());
        var untouchedFictional = await context.Order.FindAsync(fictional.Id);
        Assert.Equal(999m, untouchedFictional!.Quantity);
    }

    [Fact]
    public async Task WriteAsync_SoftDeletes_NonFictionalOrdersOfTheSameType_NotPresentInTheFile()
    {
        var (writer, context) = CreateSut();
        var notSent = new Order
        {
            OrderNumber = "OP-OLD",
            IdDestinyCenter = 1,
            IdProduct = 99,
            Quantity = 1m,
            MeasurementUnit = "UN",
            CreationDate = DateTime.UtcNow,
            Type = OrderType.ProductionOrder
        };
        context.Order.Add(notSent);
        await context.SaveChangesAsync();

        var rows = new List<Dictionary<string, string?>> { BuildRow("OP-NEW", 1, 10) };

        var result = await writer.WriteAsync(BuildProductionOrderSource(), rows);

        Assert.Equal(1, result.Deleted);
        var deleted = await context.Order.FindAsync(notSent.Id);
        Assert.NotNull(deleted!.deletedAt);
    }

    [Fact]
    public async Task WriteAsync_NeverDeletes_FictionalOrders_EvenWhenNotPresentInTheFile()
    {
        var (writer, context) = CreateSut();
        var fictional = new Order
        {
            OrderNumber = "OP-FICT",
            IdDestinyCenter = 1,
            IdProduct = 99,
            Quantity = 1m,
            MeasurementUnit = "UN",
            CreationDate = DateTime.UtcNow,
            Type = OrderType.ProductionOrder,
            IsFictional = true
        };
        context.Order.Add(fictional);
        await context.SaveChangesAsync();

        var rows = new List<Dictionary<string, string?>> { BuildRow("OP-NEW", 1, 10) };

        var result = await writer.WriteAsync(BuildProductionOrderSource(), rows);

        Assert.Equal(0, result.Deleted);
        var stillActive = await context.Order.FindAsync(fictional.Id);
        Assert.Null(stillActive!.deletedAt);
    }

    [Fact]
    public async Task WriteAsync_NeverDeletes_OrdersOfADifferentType()
    {
        var (writer, context) = CreateSut();
        var transfer = new Order
        {
            OrderNumber = "TR-001",
            IdDestinyCenter = 1,
            IdOriginCenter = 2,
            IdProduct = 10,
            Quantity = 1m,
            MeasurementUnit = "UN",
            CreationDate = DateTime.UtcNow,
            Type = OrderType.Transfer
        };
        context.Order.Add(transfer);
        await context.SaveChangesAsync();

        // Running the ProductionOrder view must never touch a Transfer-type row, even though
        // it's not present in this file's rows.
        var rows = new List<Dictionary<string, string?>> { BuildRow("OP-NEW", 1, 10) };

        var result = await writer.WriteAsync(BuildProductionOrderSource(), rows);

        Assert.Equal(0, result.Deleted);
        var stillActive = await context.Order.FindAsync(transfer.Id);
        Assert.Null(stillActive!.deletedAt);
    }
}
