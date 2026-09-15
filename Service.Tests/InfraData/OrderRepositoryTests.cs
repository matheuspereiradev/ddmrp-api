using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Infra.Data.Context;
using Service.Infra.Data.Repositories;

namespace Service.Tests.InfraData;

public class OrderRepositoryTests
{
    private static OrderRepository CreateSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(1);

        context.Product.Add(new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" });
        context.Center.AddRange(
            new Center { Id = 1, Code = "C1", Description = "Center 1" },
            new Center { Id = 2, Code = "C2", Description = "Center 2" });

        context.Order.AddRange(
            new Order { Id = 1, OrderNumber = "DEST1-IN-REAL", IdProduct = 1, IdDestinyCenter = 1, Quantity = 10, DeliveredQuantity = 0, MeasurementUnit = "UN", IsInbound = true, IsOutbound = false, IsFictional = false },
            new Order { Id = 2, OrderNumber = "DEST1-IN-FICTIONAL", IdProduct = 1, IdDestinyCenter = 1, Quantity = 10, DeliveredQuantity = 0, MeasurementUnit = "UN", IsInbound = true, IsOutbound = false, IsFictional = true },
            new Order { Id = 3, OrderNumber = "ORIGIN1-OUT-REAL", IdProduct = 1, IdOriginCenter = 1, Quantity = 10, DeliveredQuantity = 0, MeasurementUnit = "UN", IsInbound = false, IsOutbound = true, IsFictional = false },
            new Order { Id = 4, OrderNumber = "DEST2-IN-REAL", IdProduct = 1, IdDestinyCenter = 2, Quantity = 10, DeliveredQuantity = 0, MeasurementUnit = "UN", IsInbound = true, IsOutbound = false, IsFictional = false },
            new Order { Id = 5, OrderNumber = "DELETED", IdProduct = 1, IdDestinyCenter = 1, Quantity = 10, DeliveredQuantity = 0, MeasurementUnit = "UN", IsInbound = true, IsOutbound = false, IsFictional = false, deletedAt = DateTime.UtcNow });
        context.SaveChanges();

        return new OrderRepository(context, currentUser);
    }

    [Fact]
    public async Task GetFilteredAsync_ReturnsEverythingNonDeleted_WhenNoFilterIsGiven()
    {
        var repository = CreateSut();

        var result = await repository.GetFilteredAsync(null, null, null, null, null, 1, 10);

        Assert.Equal(4, result.TotalCount);
        Assert.DoesNotContain(result, o => o.OrderNumber == "DELETED");
    }

    [Fact]
    public async Task GetFilteredAsync_FiltersByIdDestinyCenter()
    {
        var repository = CreateSut();

        var result = await repository.GetFilteredAsync(1, null, null, null, null, 1, 10);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result, o => Assert.Equal(1, o.IdDestinyCenter));
    }

    [Fact]
    public async Task GetFilteredAsync_FiltersByIdOriginCenter()
    {
        var repository = CreateSut();

        var result = await repository.GetFilteredAsync(null, 1, null, null, null, 1, 10);

        Assert.Equal("ORIGIN1-OUT-REAL", Assert.Single(result).OrderNumber);
    }

    [Fact]
    public async Task GetFilteredAsync_FiltersByFictionalIsInboundIsOutbound()
    {
        var repository = CreateSut();

        var fictionalOnly = await repository.GetFilteredAsync(null, null, true, null, null, 1, 10);
        Assert.Equal("DEST1-IN-FICTIONAL", Assert.Single(fictionalOnly).OrderNumber);

        var outboundOnly = await repository.GetFilteredAsync(null, null, null, null, true, 1, 10);
        Assert.Equal("ORIGIN1-OUT-REAL", Assert.Single(outboundOnly).OrderNumber);

        var inboundNonFictional = await repository.GetFilteredAsync(null, null, false, true, null, 1, 10);
        Assert.Equal(2, inboundNonFictional.TotalCount);
        Assert.All(inboundNonFictional, o => Assert.True(o.IsInbound && !o.IsFictional));
    }
}
