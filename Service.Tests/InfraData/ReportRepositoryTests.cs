using Microsoft.EntityFrameworkCore;
using Service.Domain.Entities;
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
}
