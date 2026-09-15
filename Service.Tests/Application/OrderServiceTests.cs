using NSubstitute;
using Service.Application.DTOs.Order;
using Service.Application.Exceptions;
using Service.Application.Services;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;
using Service.Domain.Utils;

namespace Service.Tests.Application;

public class OrderServiceTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IPartnerRepository _partnerRepository = Substitute.For<IPartnerRepository>();
    private readonly ICenterRepository _centerRepository = Substitute.For<ICenterRepository>();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _productRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _partnerRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _centerRepository.Exists(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(true);
        _sut = new OrderService(_orderRepository, _productRepository, _partnerRepository, _centerRepository);
    }

    private static OrderPostDto BuildPostDto() => new()
    {
        OrderNumber = "OP-001",
        IdPartner = 1,
        IdDestinyCenter = 2,
        IdOriginCenter = 3,
        IdProduct = 1,
        Quantity = 100m,
        DeliveredQuantity = 0m,
        MeasurementUnit = "UN",
        Position = 1,
        CreationDate = new DateTime(2026, 9, 13),
        DeliveryDate = null,
        Notes = "Test order",
        Type = OrderType.PurchaseOrder,
        IsInbound = true,
        IsOutbound = false,
        IsFictional = false
    };

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundException_WhenOrderDoesNotExist()
    {
        _orderRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Order)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(1));
    }

    [Fact]
    public async Task GetByIdAsync_IncludesNestedEntities_WhenLoaded()
    {
        var order = new Order
        {
            Id = 1,
            OrderNumber = "OP-001",
            IdProduct = 1,
            Quantity = 10m,
            DeliveredQuantity = 0m,
            MeasurementUnit = "UN",
            CreationDate = DateTime.UtcNow,
            Type = OrderType.Transfer,
            Product = new Product { Id = 1, Reference = "REF001", Description = "Produto Teste", UnitOfMeasure = "UN" },
            Partner = new Partner { Id = 2, Code = "P001", Description = "Fornecedor Teste" },
            DestinyCenter = new Center { Id = 3, Code = "C001", Description = "Centro Destino" },
            OriginCenter = new Center { Id = 4, Code = "C002", Description = "Centro Origem" }
        };
        _orderRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.GetByIdAsync(1);

        Assert.NotNull(result.Product);
        Assert.Equal("REF001", result.Product!.Reference);
        Assert.NotNull(result.Partner);
        Assert.Equal("P001", result.Partner!.Code);
        Assert.NotNull(result.DestinyCenter);
        Assert.Equal("C001", result.DestinyCenter!.Code);
        Assert.NotNull(result.OriginCenter);
        Assert.Equal("C002", result.OriginCenter!.Code);
        Assert.Null(result.OrderLeadtime);
    }

    [Fact]
    public async Task AddAsync_PersistsMappedEntity_WhenAllForeignKeysExist()
    {
        var postDto = BuildPostDto();
        _orderRepository.AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Order>());

        var result = await _sut.AddAsync(postDto);

        Assert.Equal(postDto.OrderNumber, result.OrderNumber);
        Assert.Equal(postDto.Type, result.Type);
        Assert.Equal(postDto.IsInbound, result.IsInbound);
        await _orderRepository.Received(1).AddAsync(
            Arg.Is<Order>(o => o.OrderNumber == postDto.OrderNumber && o.IdProduct == postDto.IdProduct),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenProductDoesNotExist()
    {
        var postDto = BuildPostDto();
        _productRepository.Exists(postDto.IdProduct, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));

        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenPartnerDoesNotExist()
    {
        var postDto = BuildPostDto();
        _partnerRepository.Exists(postDto.IdPartner!.Value, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenDestinyCenterDoesNotExist()
    {
        var postDto = BuildPostDto();
        _centerRepository.Exists(postDto.IdDestinyCenter!.Value, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));
    }

    [Fact]
    public async Task AddAsync_ThrowsBadRequestException_WhenOriginCenterDoesNotExist()
    {
        var postDto = BuildPostDto();
        _centerRepository.Exists(postDto.IdOriginCenter!.Value, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddAsync(postDto));
    }

    [Fact]
    public async Task AddAsync_Succeeds_WhenOptionalForeignKeysAreNull()
    {
        var postDto = BuildPostDto();
        postDto.IdPartner = null;
        postDto.IdDestinyCenter = null;
        postDto.IdOriginCenter = null;
        _orderRepository.AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Order>());

        var result = await _sut.AddAsync(postDto);

        Assert.Null(result.IdPartner);
        Assert.Null(result.IdDestinyCenter);
        Assert.Null(result.IdOriginCenter);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOnlyEditableFields_AndLeavesCreationFieldsUntouched()
    {
        var existing = new Order
        {
            Id = 1,
            OrderNumber = "OP-001",
            IdProduct = 1,
            IdDestinyCenter = 2,
            IdOriginCenter = 3,
            IdPartner = 4,
            Quantity = 10m,
            DeliveredQuantity = 0m,
            MeasurementUnit = "UN",
            CreationDate = new DateTime(2026, 9, 13),
            Type = OrderType.SaleOrder,
            IsInbound = false,
            IsOutbound = true,
            IsFictional = false
        };
        var putDto = new OrderPutDto
        {
            IdPartner = 5,
            Quantity = 20m,
            DeliveredQuantity = 15m,
            MeasurementUnit = "KG",
            Position = 2,
            CreationDate = new DateTime(2026, 9, 14),
            DeliveryDate = new DateTime(2026, 9, 20),
            Notes = "Updated"
        };
        _orderRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _orderRepository.UpdateAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Order>());

        var result = await _sut.UpdateAsync(1, putDto);

        Assert.Equal(5, result.IdPartner);
        Assert.Equal(20m, result.Quantity);
        Assert.Equal(15m, result.DeliveredQuantity);
        Assert.Equal(5m, result.PendingQuantity);
        Assert.Equal("KG", result.MeasurementUnit);
        Assert.Equal(2, result.Position);
        Assert.Equal(new DateTime(2026, 9, 14), result.CreationDate);
        Assert.Equal(new DateTime(2026, 9, 20), result.DeliveryDate);
        Assert.Equal(6, result.OrderLeadtime);
        Assert.Equal(0, result.DaysLate);
        Assert.Equal(UtilsDdmrp.CalculateDaysToReceive(result.DeliveryDate), result.DaysToReceive);
        Assert.Equal("Updated", result.Notes);
        Assert.Equal("OP-001", result.OrderNumber);
        Assert.Equal(1, result.IdProduct);
        Assert.Equal(2, result.IdDestinyCenter);
        Assert.Equal(3, result.IdOriginCenter);
        Assert.Equal(OrderType.SaleOrder, result.Type);
        Assert.False(result.IsInbound);
        Assert.True(result.IsOutbound);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsBadRequestException_WhenNewPartnerDoesNotExist()
    {
        var existing = new Order { Id = 1, OrderNumber = "OP-001", IdProduct = 1, MeasurementUnit = "UN", CreationDate = DateTime.UtcNow, Type = OrderType.Transfer };
        var putDto = new OrderPutDto { IdPartner = 99, Quantity = 1m, DeliveredQuantity = 0m, MeasurementUnit = "UN", CreationDate = DateTime.UtcNow };
        _orderRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _partnerRepository.Exists(99, Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<BadRequestException>(() => _sut.UpdateAsync(1, putDto));

        await _orderRepository.DidNotReceive().UpdateAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFoundException_WhenOrderDoesNotExist()
    {
        _orderRepository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns((Order)null!);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }

    [Fact]
    public async Task GetFilteredAsync_ReturnsMappedPagedList()
    {
        var orders = new List<Order>
        {
            new() { Id = 1, OrderNumber = "OP-001", IdProduct = 1, MeasurementUnit = "UN", CreationDate = DateTime.UtcNow, Type = OrderType.SaleOrder }
        };
        _orderRepository.GetFilteredAsync(2, null, false, true, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns(new PagedList<Order>(orders, 1, 10, 1));

        var result = await _sut.GetFilteredAsync(idDestinyCenter: 2, idOriginCenter: null, fictional: false, isInbound: true, isOutbound: null, pageNumber: 1, pageSize: 10);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("OP-001", Assert.Single(result).OrderNumber);
    }
}
