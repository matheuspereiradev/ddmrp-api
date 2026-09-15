using Microsoft.EntityFrameworkCore;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Utils;
using Service.Infra.Data.Context;
using Service.Infra.Data.Repositories;

namespace Service.Tests.InfraData;

public class ReportRepositoryTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetInventoryBufferManagementAsync_JoinsRelatedDataAndSumsOrdersScopedByProductAndCenter()
    {
        await using var context = CreateContext();

        var center = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var otherCenter = new Center { Id = 2, Code = "C2", Description = "Center 2" };
        var product = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        var otherProduct = new Product { Id = 2, Reference = "REF2", Description = "Product 2", UnitOfMeasure = "UN" };
        var provider = new Partner { Id = 1, Code = "P1", Description = "Provider 1" };
        var bufferProfile = new BufferProfile { Id = 1, ProfileName = "BP1" };

        var centerProduct = new CenterProduct
        {
            Id = 1,
            IdProduct = product.Id,
            IdCenter = center.Id,
            IdProvider = provider.Id,
            IdBufferProfile = bufferProfile.Id,
            PackQuantity = 10m,
            Moq = 5m,
            Stock = 100m,
            RedZoneBase = 20m,
            RedZoneSafe = 20m,
            YellowZone = 30m,
            GreenZone = 30m
        };

        context.AddRange(center, otherCenter, product, otherProduct, provider, bufferProfile, centerProduct);

        context.Order.AddRange(
            new Order { Id = 1, OrderNumber = "IN1", IdProduct = product.Id, IdDestinyCenter = center.Id, Quantity = 100, DeliveredQuantity = 40, MeasurementUnit = "UN", IsInbound = true, IsOutbound = false, IsFictional = false },
            new Order { Id = 2, OrderNumber = "IN2", IdProduct = product.Id, IdDestinyCenter = center.Id, Quantity = 30, DeliveredQuantity = 0, MeasurementUnit = "UN", IsInbound = true, IsOutbound = false, IsFictional = true },
            new Order { Id = 3, OrderNumber = "OUT1", IdProduct = product.Id, IdOriginCenter = center.Id, Quantity = 50, DeliveredQuantity = 10, MeasurementUnit = "UN", IsInbound = false, IsOutbound = true, IsFictional = false },
            new Order { Id = 4, OrderNumber = "OUT2", IdProduct = product.Id, IdOriginCenter = center.Id, Quantity = 20, DeliveredQuantity = 5, MeasurementUnit = "UN", IsInbound = false, IsOutbound = true, IsFictional = true },
            new Order { Id = 5, OrderNumber = "IN-OTHERPRODUCT", IdProduct = otherProduct.Id, IdDestinyCenter = center.Id, Quantity = 999, DeliveredQuantity = 0, MeasurementUnit = "UN", IsInbound = true, IsOutbound = false, IsFictional = false },
            new Order { Id = 6, OrderNumber = "IN-OTHERCENTER", IdProduct = product.Id, IdDestinyCenter = otherCenter.Id, Quantity = 999, DeliveredQuantity = 0, MeasurementUnit = "UN", IsInbound = true, IsOutbound = false, IsFictional = false },
            new Order { Id = 7, OrderNumber = "IN-DELETED", IdProduct = product.Id, IdDestinyCenter = center.Id, Quantity = 999, DeliveredQuantity = 0, MeasurementUnit = "UN", IsInbound = true, IsOutbound = false, IsFictional = false, deletedAt = DateTime.UtcNow });

        await context.SaveChangesAsync();

        var repository = new ReportRepository(context);

        var rows = await repository.GetInventoryBufferManagementAsync();

        var row = Assert.Single(rows);

        Assert.Equal("C1", row.CenterCode);
        Assert.Equal("Center 1", row.CenterDescription);
        Assert.Equal("REF1", row.ProductReference);
        Assert.Equal("Product 1", row.ProductDescription);
        Assert.Equal("P1", row.ProviderCode);
        Assert.Equal("BP1", row.BufferProfileName);

        Assert.Equal(60m, row.Inbounds);
        Assert.Equal(30m, row.FictionalInbounds);
        Assert.Equal(40m, row.Outbounds);
        Assert.Equal(15m, row.FictionalOutbounds);
    }

    [Fact]
    public async Task GetInventoryBufferManagementAsync_ReturnsNullOptionalRelations_WhenNotSet()
    {
        await using var context = CreateContext();

        var center = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var product = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        var centerProduct = new CenterProduct
        {
            Id = 1,
            IdProduct = product.Id,
            IdCenter = center.Id,
            PackQuantity = 10m,
            Moq = 5m,
            Stock = 100m,
            RedZoneBase = 20m,
            RedZoneSafe = 20m,
            YellowZone = 30m,
            GreenZone = 30m
        };

        context.AddRange(center, product, centerProduct);
        await context.SaveChangesAsync();

        var repository = new ReportRepository(context);

        var rows = await repository.GetInventoryBufferManagementAsync();

        var row = Assert.Single(rows);
        Assert.Null(row.ProviderCode);
        Assert.Null(row.BufferProfileName);
        Assert.Equal(0m, row.Inbounds);
        Assert.Equal(0m, row.Outbounds);
    }

    [Fact]
    public async Task GetInventoryBufferManagementAsync_RoundsDerivedZonesUp()
    {
        await using var context = CreateContext();

        var center = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var product = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        var centerProduct = new CenterProduct
        {
            Id = 1,
            IdProduct = product.Id,
            IdCenter = center.Id,
            PackQuantity = 10m,
            Moq = 5m,
            Stock = 100m,
            RedZoneBase = 10.1m,
            RedZoneSafe = 10.2m,
            YellowZone = 15.3m,
            GreenZone = 15.4m
        };

        context.AddRange(center, product, centerProduct);
        await context.SaveChangesAsync();

        var repository = new ReportRepository(context);

        var row = Assert.Single(await repository.GetInventoryBufferManagementAsync());

        Assert.Equal(21m, row.TopOfRed);
        Assert.Equal(36m, row.TopOfYellow);
        Assert.Equal(51m, row.TopOfGreen);
        Assert.Equal(11m, row.RedZoneExecution);
        Assert.Equal(11m, row.YellowZoneExecution);
        Assert.Equal(16m, row.GreenZoneExecution);
    }

    [Fact]
    public async Task GetOpenOrdersAsync_ExcludesFictionalDeletedAndFullyDeliveredOrders_ByDefault()
    {
        await using var context = CreateContext();

        var center = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var otherCenter = new Center { Id = 2, Code = "C2", Description = "Center 2" };
        var product = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };

        context.AddRange(center, otherCenter, product);

        context.Order.AddRange(
            new Order { Id = 1, OrderNumber = "OPEN", IdProduct = product.Id, IdDestinyCenter = center.Id, Quantity = 100, DeliveredQuantity = 40, MeasurementUnit = "UN", CreationDate = new DateTime(2026, 9, 10), DeliveryDate = new DateTime(2026, 9, 15), IsInbound = true, IsOutbound = false, IsFictional = false },
            new Order { Id = 2, OrderNumber = "CLOSED", IdProduct = product.Id, IdDestinyCenter = center.Id, Quantity = 50, DeliveredQuantity = 50, MeasurementUnit = "UN", IsInbound = true, IsOutbound = false, IsFictional = false },
            new Order { Id = 3, OrderNumber = "FICTIONAL", IdProduct = product.Id, IdDestinyCenter = center.Id, Quantity = 30, DeliveredQuantity = 0, MeasurementUnit = "UN", IsInbound = true, IsOutbound = false, IsFictional = true },
            new Order { Id = 4, OrderNumber = "DELETED", IdProduct = product.Id, IdDestinyCenter = center.Id, Quantity = 30, DeliveredQuantity = 0, MeasurementUnit = "UN", IsInbound = true, IsOutbound = false, IsFictional = false, deletedAt = DateTime.UtcNow },
            new Order { Id = 5, OrderNumber = "OTHERCENTER", IdProduct = product.Id, IdDestinyCenter = otherCenter.Id, Quantity = 30, DeliveredQuantity = 0, MeasurementUnit = "UN", IsInbound = true, IsOutbound = false, IsFictional = false });

        await context.SaveChangesAsync();

        var repository = new ReportRepository(context);

        var rows = await repository.GetOpenOrdersAsync(idCenter: center.Id, idProduct: null);

        var row = Assert.Single(rows);
        Assert.Equal("OPEN", row.OrderNumber);
        Assert.Equal(60m, row.PendingQuantity);
        Assert.Equal(5, row.OrderLeadtime);
        Assert.Equal(UtilsDdmrp.CalculateTimeBuffer(row.DeliveryDate!.Value, row.OrderLeadtime!.Value), row.TimeBuffer);
        Assert.Equal(UtilsDdmrp.CalculateTimeBufferColor(row.TimeBuffer!.Value), row.TimeBufferColor);
        Assert.Equal(UtilsDdmrp.CalculateDaysToReceive(row.DeliveryDate), row.DaysToReceive);
        Assert.Equal(UtilsDdmrp.CalculateDaysLate(row.DeliveryDate), row.DaysLate);
        Assert.Equal("C1", row.DestinyCenterCode);
        Assert.Equal("REF1", row.ProductReference);
    }

    [Fact]
    public async Task GetOpenOrdersAsync_OnlyReturnsInboundOrders_AndAppliesFilters()
    {
        await using var context = CreateContext();

        var center = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var product = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        var otherProduct = new Product { Id = 2, Reference = "REF2", Description = "Product 2", UnitOfMeasure = "UN" };

        context.AddRange(center, product, otherProduct);

        context.Order.AddRange(
            new Order { Id = 1, OrderNumber = "IN", IdProduct = product.Id, IdDestinyCenter = center.Id, Quantity = 100, DeliveredQuantity = 0, MeasurementUnit = "UN", IsInbound = true, IsOutbound = false, IsFictional = false },
            new Order { Id = 2, OrderNumber = "OUT", IdProduct = product.Id, IdOriginCenter = center.Id, Quantity = 100, DeliveredQuantity = 0, MeasurementUnit = "UN", IsInbound = false, IsOutbound = true, IsFictional = false },
            new Order { Id = 3, OrderNumber = "OTHERPRODUCT", IdProduct = otherProduct.Id, IdDestinyCenter = center.Id, Quantity = 100, DeliveredQuantity = 0, MeasurementUnit = "UN", IsInbound = true, IsOutbound = false, IsFictional = false },
            new Order { Id = 4, OrderNumber = "FICTIONAL", IdProduct = product.Id, IdDestinyCenter = center.Id, Quantity = 100, DeliveredQuantity = 0, MeasurementUnit = "UN", IsInbound = true, IsOutbound = false, IsFictional = true });

        await context.SaveChangesAsync();

        var repository = new ReportRepository(context);

        var byProduct = await repository.GetOpenOrdersAsync(idCenter: center.Id, idProduct: product.Id);
        Assert.Equal("IN", Assert.Single(byProduct).OrderNumber);

        var all = await repository.GetOpenOrdersAsync(idCenter: null, idProduct: null);
        Assert.DoesNotContain(all, r => r.OrderNumber == "FICTIONAL");
    }

    [Fact]
    public async Task GetOpenOrdersAsync_ComputesExecutionBuffer_FromStockAndEarlierPendingOrders()
    {
        await using var context = CreateContext();

        var center = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var product = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        var centerProduct = new CenterProduct
        {
            Id = 1,
            IdProduct = product.Id,
            IdCenter = center.Id,
            PackQuantity = 10m,
            Moq = 5m,
            Stock = 100m,
            RedZoneBase = 20m,
            RedZoneSafe = 20m,
            YellowZone = 60m,
            GreenZone = 30m
        };

        context.AddRange(center, product, centerProduct);

        context.Order.AddRange(
            new Order { Id = 1, OrderNumber = "EARLIER-INCLUDED", IdProduct = product.Id, IdDestinyCenter = center.Id, Quantity = 50, DeliveredQuantity = 10, MeasurementUnit = "UN", DeliveryDate = new DateTime(2026, 9, 10), IsInbound = true, IsOutbound = false, IsFictional = false },
            new Order { Id = 2, OrderNumber = "EARLIER-FICTIONAL-EXCLUDED", IdProduct = product.Id, IdDestinyCenter = center.Id, Quantity = 999, DeliveredQuantity = 0, MeasurementUnit = "UN", DeliveryDate = new DateTime(2026, 9, 10), IsInbound = true, IsOutbound = false, IsFictional = true },
            new Order { Id = 3, OrderNumber = "EARLIER-LATER-DATE-EXCLUDED", IdProduct = product.Id, IdDestinyCenter = center.Id, Quantity = 999, DeliveredQuantity = 0, MeasurementUnit = "UN", DeliveryDate = new DateTime(2026, 9, 20), IsInbound = true, IsOutbound = false, IsFictional = false },
            new Order { Id = 4, OrderNumber = "CURRENT", IdProduct = product.Id, IdDestinyCenter = center.Id, Quantity = 30, DeliveredQuantity = 0, MeasurementUnit = "UN", DeliveryDate = new DateTime(2026, 9, 12), IsInbound = true, IsOutbound = false, IsFictional = false });

        await context.SaveChangesAsync();

        var repository = new ReportRepository(context);

        var rows = await repository.GetOpenOrdersAsync(idCenter: center.Id, idProduct: product.Id);

        var current = rows.Single(r => r.OrderNumber == "CURRENT");
        // TopOfYellowExecution = RedZoneExecution + YellowZoneExecution = (TopOfRed/2)*2 = TopOfRed = 40
        // ExecutionBuffer = (Stock 100 + earlier pending 40) / 40 = 3.5
        Assert.Equal(3.5m, current.ExecutionBuffer);
        // ExecutionBufferColor: quantity 140 > TopOfGreenExecution (20+20+60=100) => Blue
        Assert.Equal(BufferColor.Blue, current.ExecutionBufferColor);
    }

    [Fact]
    public async Task GetOpenOrdersAsync_ExecutionBufferIsNull_WhenTopOfYellowExecutionIsZeroOrMissing()
    {
        await using var context = CreateContext();

        var center = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var product = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };

        context.AddRange(center, product);
        context.Order.Add(new Order { Id = 1, OrderNumber = "NO-CENTERPRODUCT", IdProduct = product.Id, IdDestinyCenter = center.Id, Quantity = 30, DeliveredQuantity = 0, MeasurementUnit = "UN", DeliveryDate = new DateTime(2026, 9, 12), IsInbound = true, IsOutbound = false, IsFictional = false });

        await context.SaveChangesAsync();

        var repository = new ReportRepository(context);

        var rows = await repository.GetOpenOrdersAsync(idCenter: center.Id, idProduct: product.Id);

        var row = Assert.Single(rows);
        Assert.Null(row.ExecutionBuffer);
        Assert.Null(row.ExecutionBufferColor);
    }
}
