using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Service.Domain.Account;
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

    private static ReportRepository CreateRepository(ApplicationDbContext context, int currentUserId = 1)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(currentUserId);
        return new ReportRepository(context, currentUser);
    }

    [Fact]
    public async Task GetInventoryBufferManagementQueryable_JoinsRelatedDataAndSumsOrdersScopedByProductAndCenter()
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

        var repository = CreateRepository(context);

        var rows = await repository.GetInventoryBufferManagementQueryable().ToListAsync();

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
    public async Task GetInventoryBufferManagementQueryable_OptimizedOrderQuantity_UsesWorkspaceOverride_ForCurrentUserOnly()
    {
        await using var context = CreateContext();

        var center = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var product = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        var user1 = new User { Id = 1, Name = "User 1", Email = "user1@test.com", Password = "hash", IdRole = 1 };
        var user2 = new User { Id = 2, Name = "User 2", Email = "user2@test.com", Password = "hash", IdRole = 1 };
        var centerProduct = new CenterProduct
        {
            Id = 1,
            IdProduct = product.Id,
            IdCenter = center.Id,
            PackQuantity = 10m,
            Moq = 5m,
            Stock = 0m,
            RedZoneBase = 20m,
            RedZoneSafe = 20m,
            YellowZone = 30m,
            GreenZone = 30m
        };

        context.AddRange(center, product, user1, user2, centerProduct);
        context.Workspace.Add(new Workspace { IdCenter = center.Id, IdProduct = product.Id, IdUser = user1.Id, OptimizedQuantity = 42m, Approved = true });
        context.Workspace.Add(new Workspace { IdCenter = center.Id, IdProduct = product.Id, IdUser = user2.Id, OptimizedQuantity = 999m, Approved = true });
        await context.SaveChangesAsync();

        var rowForUser1 = Assert.Single(await CreateRepository(context, user1.Id).GetInventoryBufferManagementQueryable().ToListAsync());
        Assert.True(rowForUser1.SystemOptimizedOrderQuantity > 0);
        Assert.True(rowForUser1.HasSuggestion);
        Assert.Equal(42m, rowForUser1.OptimizedOrderQuantity);
        Assert.True(rowForUser1.Approved);
        Assert.Equal(
            UtilsDdmrp.CalculateBufferPercentage(rowForUser1.TopOfGreen ?? 0, rowForUser1.Netflow + 42m),
            rowForUser1.SimulatedNetflowBufferPercentage);
        // Netflow (0) is Red on its own, but simulated (0 + 42 approved) crosses into Yellow.
        Assert.Equal(BufferColor.Red, rowForUser1.NetflowBufferColor);
        Assert.Equal(
            UtilsDdmrp.CalculateBufferColor(rowForUser1.Netflow + 42m, rowForUser1.TopOfRed ?? 0, rowForUser1.TopOfYellow ?? 0, rowForUser1.TopOfGreen ?? 0),
            rowForUser1.SimulatedNetflowBufferColor);
        Assert.Equal(BufferColor.Yellow, rowForUser1.SimulatedNetflowBufferColor);

        var rowForOtherUser = Assert.Single(await CreateRepository(context, 999).GetInventoryBufferManagementQueryable().ToListAsync());
        Assert.Equal(rowForOtherUser.SystemOptimizedOrderQuantity, rowForOtherUser.OptimizedOrderQuantity);
        Assert.False(rowForOtherUser.Approved);
        // No Workspace row for this user, so the simulated color falls back to the plain NetflowBufferColor.
        Assert.Equal(rowForOtherUser.NetflowBufferColor, rowForOtherUser.SimulatedNetflowBufferColor);
        // No Workspace row for this user, so the simulated buffer falls back to the plain NetflowBufferPercentage.
        Assert.Equal(rowForOtherUser.NetflowBufferPercentage, rowForOtherUser.SimulatedNetflowBufferPercentage);
    }

    [Fact]
    public async Task GetInventoryBufferManagementQueryable_ReturnsNullOptionalRelations_WhenNotSet()
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

        var repository = CreateRepository(context);

        var rows = await repository.GetInventoryBufferManagementQueryable().ToListAsync();

        var row = Assert.Single(rows);
        Assert.Null(row.ProviderCode);
        Assert.Null(row.BufferProfileName);
        Assert.Equal(0m, row.Inbounds);
        Assert.Equal(0m, row.Outbounds);
    }

    [Fact]
    public async Task GetInventoryBufferManagementQueryable_RoundsDerivedZonesUp()
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

        var repository = CreateRepository(context);

        var row = Assert.Single(await repository.GetInventoryBufferManagementQueryable().ToListAsync());

        Assert.Equal(21m, row.TopOfRed);
        Assert.Equal(36m, row.TopOfYellow);
        Assert.Equal(51m, row.TopOfGreen);
        Assert.Equal(11m, row.RedZoneExecution);
        Assert.Equal(11m, row.YellowZoneExecution);
        Assert.Equal(16m, row.GreenZoneExecution);
    }

    // Parity coverage: GetInventoryBufferManagementQueryable now computes Netflow/OrderQuantity/
    // OptimizedOrderQuantity/NetflowBufferPercentage/NetflowBufferColor/CoverageDays/ExecutionBufferPercentage/
    // ExecutionBufferColor as inline, SQL-translatable expressions (so OData's $filter/$orderby can run in SQL)
    // instead of calling UtilsDdmrp in a post-materialization loop. These theories seed each buffer-zone branch
    // and assert the repository's result equals UtilsDdmrp called with the row's own inputs — catching any drift
    // between the two implementations of the same formulas (see the comment above GetInventoryBufferManagementQueryable
    // and Formulas.md).
    [Theory]
    [InlineData("Green", 100, 0, 20, 20, 30, 30, 5, 10, 10)]
    [InlineData("Yellow", 50, 0, 20, 20, 30, 30, 5, 10, 10)]
    [InlineData("Red", 10, 0, 20, 20, 30, 30, 5, 10, 10)]
    [InlineData("Black-Stockout", 10, 50, 20, 20, 30, 30, 5, 10, 10)]
    [InlineData("Blue-Overstock", 150, 0, 20, 20, 30, 30, 5, 10, 10)]
    [InlineData("Red-PackQuantityZero", 10, 0, 20, 20, 30, 30, 5, 0, 10)]
    public async Task GetInventoryBufferManagementQueryable_DerivedMetrics_MatchUtilsDdmrp(
        string scenario, int stock, int qualifiedDemand, int redZoneBase, int redZoneSafe,
        int yellowZone, int greenZone, int moq, int packQuantity, int adu)
    {
        await using var context = CreateContext();

        var center = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var product = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        var centerProduct = new CenterProduct
        {
            Id = 1,
            IdProduct = product.Id,
            IdCenter = center.Id,
            PackQuantity = packQuantity,
            Moq = moq,
            Stock = stock,
            QualifiedDemand = qualifiedDemand,
            Adu = adu,
            RedZoneBase = redZoneBase,
            RedZoneSafe = redZoneSafe,
            YellowZone = yellowZone,
            GreenZone = greenZone
        };

        context.AddRange(center, product, centerProduct);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var row = Assert.Single(await repository.GetInventoryBufferManagementQueryable().ToListAsync());

        Assert.Equal(UtilsDdmrp.CalculateNetflow(row.Stock, row.QualifiedDemand ?? 0, row.Inbounds), row.Netflow);
        Assert.Equal(UtilsDdmrp.CalculateOrderQuantity(row.Netflow, row.TopOfYellow ?? 0, row.TopOfGreen ?? 0), row.OrderQuantity);
        Assert.Equal(
            UtilsDdmrp.CalculateOptimizedOrderQuantity(row.Netflow, row.TopOfYellow ?? 0, row.TopOfGreen ?? 0, row.Moq, row.PackQuantity),
            row.SystemOptimizedOrderQuantity);
        // No Workspace row exists for this CenterProduct, so OptimizedOrderQuantity falls back to the system value.
        Assert.Equal(row.SystemOptimizedOrderQuantity, row.OptimizedOrderQuantity);
        Assert.Equal(row.SystemOptimizedOrderQuantity > 0, row.HasSuggestion);
        Assert.False(row.Approved);
        Assert.Equal(UtilsDdmrp.CalculateBufferPercentage(row.TopOfGreen ?? 0, row.Netflow), row.NetflowBufferPercentage);
        // No Workspace row exists for this CenterProduct, so the simulated netflow equals the plain Netflow.
        Assert.Equal(
            UtilsDdmrp.CalculateBufferPercentage(row.TopOfGreen ?? 0, UtilsDdmrp.CalculateSimulatedNetflow(row.Netflow, row.Approved, 0)),
            row.SimulatedNetflowBufferPercentage);
        Assert.Equal(
            UtilsDdmrp.CalculateBufferColor(row.Netflow, row.TopOfRed ?? 0, row.TopOfYellow ?? 0, row.TopOfGreen ?? 0),
            row.NetflowBufferColor);
        // No Workspace row exists for this CenterProduct, so the simulated color equals the plain NetflowBufferColor.
        Assert.Equal(
            UtilsDdmrp.CalculateBufferColor(
                UtilsDdmrp.CalculateSimulatedNetflow(row.Netflow, row.Approved, 0),
                row.TopOfRed ?? 0, row.TopOfYellow ?? 0, row.TopOfGreen ?? 0),
            row.SimulatedNetflowBufferColor);
        Assert.Equal(row.NetflowBufferColor, row.SimulatedNetflowBufferColor);
        Assert.Equal(UtilsDdmrp.CalculateCoverageDays(row.Stock, row.Adu ?? 0), row.CoverageDays);
        Assert.Equal(UtilsDdmrp.CalculateBufferPercentage(row.GreenZoneExecution ?? 0, row.Stock), row.ExecutionBufferPercentage);
        Assert.Equal(
            UtilsDdmrp.CalculateBufferColor(row.Stock, row.RedZoneExecution ?? 0, row.YellowZoneExecution ?? 0, row.GreenZoneExecution ?? 0),
            row.ExecutionBufferColor);

        var expectedColor = scenario switch
        {
            "Green" => BufferColor.Green,
            "Yellow" => BufferColor.Yellow,
            "Red" or "Red-PackQuantityZero" => BufferColor.Red,
            "Black-Stockout" => BufferColor.Black,
            "Blue-Overstock" => BufferColor.Blue,
            _ => throw new InvalidOperationException($"Unmapped scenario {scenario}")
        };
        Assert.Equal(expectedColor, row.NetflowBufferColor);

        if (scenario == "Red-PackQuantityZero")
            Assert.Equal(0m, row.OptimizedOrderQuantity);
    }

    // Regression coverage for the 2026-09-15 OData bug: [EnableQuery] composes an extra `.Where(...)` on top of
    // GetInventoryBufferManagementQueryable()'s result (exactly what `$filter=netflowBufferColor eq 'Red'` does).
    // Before the fix, EF Core threw "Translation of member 'TopOfGreen' ... failed" trying to re-derive
    // CenterProduct's Ignore()'d computed properties a second time for the WHERE clause — reproduced here by
    // composing .Where() directly instead of going through the OData pipeline (InMemory throws the same
    // "could not be translated" exception the real SQL Server provider did). Covers both a zone-top field
    // (topOfGreen) and a field built on top of it (netflowBufferColor) since both shared the same root cause.
    [Fact]
    public async Task GetInventoryBufferManagementQueryable_ComposesWithAnExternalWhere_LikeODataDoes()
    {
        await using var context = CreateContext();

        var center = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var product = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        // Red on both axes: low stock relative to a big buffer (netflow inside the red zone) and a big TopOfGreen.
        var redCenterProduct = new CenterProduct
        {
            Id = 1,
            IdProduct = product.Id,
            IdCenter = center.Id,
            PackQuantity = 10m,
            Moq = 5m,
            Stock = 10m,
            RedZoneBase = 20m,
            RedZoneSafe = 20m,
            YellowZone = 30m,
            GreenZone = 30m
        };
        // Excluded by both filters for different reasons: overstocked relative to a tiny buffer (Blue, not Red)
        // and a small TopOfGreen (not > 90).
        var blueCenterProduct = new CenterProduct
        {
            Id = 2,
            IdProduct = product.Id,
            IdCenter = center.Id,
            PackQuantity = 10m,
            Moq = 5m,
            Stock = 150m,
            RedZoneBase = 5m,
            RedZoneSafe = 5m,
            YellowZone = 5m,
            GreenZone = 5m
        };

        context.AddRange(center, product, redCenterProduct, blueCenterProduct);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var byColor = await repository.GetInventoryBufferManagementQueryable()
            .Where(r => r.NetflowBufferColor == BufferColor.Red)
            .ToListAsync();
        Assert.Equal(10m, Assert.Single(byColor).Stock);

        var byZoneTop = await repository.GetInventoryBufferManagementQueryable()
            .Where(r => r.TopOfGreen > 90)
            .ToListAsync();
        Assert.Equal(10m, Assert.Single(byZoneTop).Stock);
    }

    [Fact]
    public async Task GetInventoryBufferManagementQueryable_DerivedMetrics_MatchUtilsDdmrp_WhenBufferNotYetComputed()
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
            Stock = 100m
        };

        context.AddRange(center, product, centerProduct);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var row = Assert.Single(await repository.GetInventoryBufferManagementQueryable().ToListAsync());

        Assert.Equal(UtilsDdmrp.CalculateNetflow(row.Stock, row.QualifiedDemand ?? 0, row.Inbounds), row.Netflow);
        Assert.Equal(
            UtilsDdmrp.CalculateBufferColor(row.Netflow, row.TopOfRed ?? 0, row.TopOfYellow ?? 0, row.TopOfGreen ?? 0),
            row.NetflowBufferColor);
        Assert.Equal(UtilsDdmrp.CalculateCoverageDays(row.Stock, row.Adu ?? 0), row.CoverageDays);
        Assert.Equal(
            UtilsDdmrp.CalculateBufferColor(row.Stock, row.RedZoneExecution ?? 0, row.YellowZoneExecution ?? 0, row.GreenZoneExecution ?? 0),
            row.ExecutionBufferColor);

        Assert.Equal(BufferColor.NoColor, row.NetflowBufferColor);
        Assert.Equal(BufferColor.NoColor, row.SimulatedNetflowBufferColor);
        Assert.Equal(BufferColor.NoColor, row.ExecutionBufferColor);
        Assert.Equal(0m, row.NetflowBufferPercentage);
        Assert.Equal(0m, row.SimulatedNetflowBufferPercentage);
        Assert.Equal(0m, row.ExecutionBufferPercentage);
        Assert.Equal(0m, row.CoverageDays);
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

        var repository = CreateRepository(context);

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

        var repository = CreateRepository(context);

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

        var repository = CreateRepository(context);

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

        var repository = CreateRepository(context);

        var rows = await repository.GetOpenOrdersAsync(idCenter: center.Id, idProduct: product.Id);

        var row = Assert.Single(rows);
        Assert.Null(row.ExecutionBuffer);
        Assert.Null(row.ExecutionBufferColor);
    }

    [Fact]
    public async Task GetProjectedStockAlertAsync_SimulatesDailyStock_CarryingClosingStockForwardAsNextOpeningStock()
    {
        await using var context = CreateContext();

        var center = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var product = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        var centerProduct = new CenterProduct
        {
            Id = 1,
            IdProduct = product.Id,
            IdCenter = center.Id,
            Adu = 5m,
            Stock = 100m,
            RedZoneBase = 10m,
            RedZoneSafe = 10m,
            YellowZone = 20m,
            GreenZone = 20m
        };

        context.AddRange(center, product, centerProduct);

        context.Calendar.AddRange(
            new Calendar { Date = new DateTime(2026, 9, 1), DayOfWeekNumber = 2, IsWorkingDay = true },
            new Calendar { Date = new DateTime(2026, 9, 2), DayOfWeekNumber = 3, IsWorkingDay = true },
            new Calendar { Date = new DateTime(2026, 9, 3), DayOfWeekNumber = 4, IsWorkingDay = true });

        context.Forecast.Add(new Forecast { Id = 1, IdProduct = product.Id, IdCenter = center.Id, Value = 30m, StartDate = new DateTime(2026, 9, 1), EndDate = new DateTime(2026, 9, 3) });

        context.Order.AddRange(
            new Order { Id = 1, OrderNumber = "IN1", IdProduct = product.Id, IdDestinyCenter = center.Id, Quantity = 50, DeliveredQuantity = 0, MeasurementUnit = "UN", DeliveryDate = new DateTime(2026, 9, 2), IsInbound = true, IsOutbound = false, IsFictional = false },
            new Order { Id = 2, OrderNumber = "OUT1", IdProduct = product.Id, IdOriginCenter = center.Id, Quantity = 8, DeliveredQuantity = 0, MeasurementUnit = "UN", DeliveryDate = new DateTime(2026, 9, 1), IsInbound = false, IsOutbound = true, IsFictional = false },
            new Order { Id = 3, OrderNumber = "FICTIONAL-OUT-EXCLUDED-VIA-FLAG", IdProduct = product.Id, IdOriginCenter = center.Id, Quantity = 999, DeliveredQuantity = 0, MeasurementUnit = "UN", DeliveryDate = new DateTime(2026, 9, 1), IsInbound = false, IsOutbound = true, IsFictional = true });

        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var rows = await repository.GetProjectedStockAlertAsync(center.Id, product.Id, new DateTime(2026, 9, 1), new DateTime(2026, 9, 3), useFictionalOrders: false);

        Assert.Equal(3, rows.Count);

        var day1 = rows[0];
        Assert.Equal(new DateTime(2026, 9, 1), day1.Date);
        Assert.Equal("REF1", day1.ProductReference);
        Assert.Equal("C1", day1.CenterCode);
        Assert.Equal(5m, day1.Adu);
        Assert.Equal(10m, day1.RedZoneExecution);
        Assert.Equal(10m, day1.YellowZoneExecution);
        Assert.Equal(20m, day1.GreenZoneExecution);
        Assert.Equal(10m, day1.ProjectedConsumption);
        Assert.Equal(0m, day1.Inbound);
        Assert.Equal(8m, day1.OutboundOrders);
        Assert.Equal(10m, day1.Outbound); // MAX(Adu 5, OutboundOrders 8, ProjectedConsumption 10)
        Assert.Equal(100m, day1.OpeningStock);
        Assert.Equal(90m, day1.ClosingStock);

        var day2 = rows[1];
        Assert.Equal(day1.ClosingStock, day2.OpeningStock);
        Assert.Equal(10m, day2.ProjectedConsumption);
        Assert.Equal(50m, day2.Inbound);
        Assert.Equal(0m, day2.OutboundOrders);
        Assert.Equal(10m, day2.Outbound);
        Assert.Equal(130m, day2.ClosingStock);

        var day3 = rows[2];
        Assert.Equal(day2.ClosingStock, day3.OpeningStock);
        Assert.Equal(0m, day3.Inbound);
        Assert.Equal(0m, day3.OutboundOrders);
        Assert.Equal(10m, day3.Outbound);
        Assert.Equal(120m, day3.ClosingStock);

        // TopOfRedExecution/TopOfYellowExecution/TopOfGreenExecution = 10/20/40, from RedZoneBase 10 + RedZoneSafe 10 + YellowZone 20
        foreach (var row in rows)
            Assert.Equal(UtilsDdmrp.CalculateBufferColor(row.ClosingStock, 10m, 20m, 40m), row.ExecutionBufferColor);
    }

    [Fact]
    public async Task GetProjectedStockAlertAsync_UseFictionalOrdersTogglesWhetherFictionalOrdersCount()
    {
        await using var context = CreateContext();

        var center = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var product = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        var centerProduct = new CenterProduct { Id = 1, IdProduct = product.Id, IdCenter = center.Id, Stock = 100m };

        context.AddRange(center, product, centerProduct);
        context.Calendar.Add(new Calendar { Date = new DateTime(2026, 9, 1), DayOfWeekNumber = 2, IsWorkingDay = true });
        context.Order.AddRange(
            new Order { Id = 1, OrderNumber = "IN-REAL", IdProduct = product.Id, IdDestinyCenter = center.Id, Quantity = 20, DeliveredQuantity = 0, MeasurementUnit = "UN", DeliveryDate = new DateTime(2026, 9, 1), IsInbound = true, IsOutbound = false, IsFictional = false },
            new Order { Id = 2, OrderNumber = "IN-FICTIONAL", IdProduct = product.Id, IdDestinyCenter = center.Id, Quantity = 30, DeliveredQuantity = 0, MeasurementUnit = "UN", DeliveryDate = new DateTime(2026, 9, 1), IsInbound = true, IsOutbound = false, IsFictional = true },
            new Order { Id = 3, OrderNumber = "OUT-REAL", IdProduct = product.Id, IdOriginCenter = center.Id, Quantity = 5, DeliveredQuantity = 0, MeasurementUnit = "UN", DeliveryDate = new DateTime(2026, 9, 1), IsInbound = false, IsOutbound = true, IsFictional = false },
            new Order { Id = 4, OrderNumber = "OUT-FICTIONAL", IdProduct = product.Id, IdOriginCenter = center.Id, Quantity = 7, DeliveredQuantity = 0, MeasurementUnit = "UN", DeliveryDate = new DateTime(2026, 9, 1), IsInbound = false, IsOutbound = true, IsFictional = true });

        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var withFictional = Assert.Single(await repository.GetProjectedStockAlertAsync(center.Id, product.Id, new DateTime(2026, 9, 1), new DateTime(2026, 9, 1)));
        Assert.Equal(50m, withFictional.Inbound); // 20 + 30, useFictionalOrders defaults to true
        Assert.Equal(12m, withFictional.OutboundOrders); // 5 + 7

        var withoutFictional = Assert.Single(await repository.GetProjectedStockAlertAsync(center.Id, product.Id, new DateTime(2026, 9, 1), new DateTime(2026, 9, 1), useFictionalOrders: false));
        Assert.Equal(20m, withoutFictional.Inbound);
        Assert.Equal(5m, withoutFictional.OutboundOrders);
    }

    [Fact]
    public async Task GetProjectedStockAlertAsync_ReturnsEmptyList_WhenCenterProductDoesNotExist()
    {
        await using var context = CreateContext();

        context.Calendar.Add(new Calendar { Date = new DateTime(2026, 9, 1), DayOfWeekNumber = 2, IsWorkingDay = true });
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var rows = await repository.GetProjectedStockAlertAsync(idCenter: 1, idProduct: 1, new DateTime(2026, 9, 1), new DateTime(2026, 9, 1));

        Assert.Empty(rows);
    }

    [Fact]
    public async Task GetProjectedStockAlertAsync_OutboundCandidateTogglesControlTheDailyMax_AndUseInboundsControlsClosingStock()
    {
        await using var context = CreateContext();

        var center = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var product = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        var centerProduct = new CenterProduct { Id = 1, IdProduct = product.Id, IdCenter = center.Id, Adu = 5m, Stock = 100m };

        context.AddRange(center, product, centerProduct);
        context.Calendar.Add(new Calendar { Date = new DateTime(2026, 9, 1), DayOfWeekNumber = 2, IsWorkingDay = true });
        context.Forecast.Add(new Forecast { Id = 1, IdProduct = product.Id, IdCenter = center.Id, Value = 20m, StartDate = new DateTime(2026, 9, 1), EndDate = new DateTime(2026, 9, 1) });
        context.Order.AddRange(
            new Order { Id = 1, OrderNumber = "IN1", IdProduct = product.Id, IdDestinyCenter = center.Id, Quantity = 50, DeliveredQuantity = 0, MeasurementUnit = "UN", DeliveryDate = new DateTime(2026, 9, 1), IsInbound = true, IsOutbound = false, IsFictional = false },
            new Order { Id = 2, OrderNumber = "OUT1", IdProduct = product.Id, IdOriginCenter = center.Id, Quantity = 8, DeliveredQuantity = 0, MeasurementUnit = "UN", DeliveryDate = new DateTime(2026, 9, 1), IsInbound = false, IsOutbound = true, IsFictional = false });

        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var defaultRow = Assert.Single(await repository.GetProjectedStockAlertAsync(center.Id, product.Id, new DateTime(2026, 9, 1), new DateTime(2026, 9, 1)));
        Assert.Equal(20m, defaultRow.Outbound); // MAX(Adu 5, OutboundOrders 8, ProjectedConsumption 20)
        Assert.Equal(130m, defaultRow.ClosingStock); // 100 - 20 + 50

        var noCandidatesRow = Assert.Single(await repository.GetProjectedStockAlertAsync(center.Id, product.Id, new DateTime(2026, 9, 1), new DateTime(2026, 9, 1),
            useAdu: false, useForecast: false, useOutbounds: false));
        Assert.Equal(0m, noCandidatesRow.Outbound);
        Assert.Equal(150m, noCandidatesRow.ClosingStock); // 100 - 0 + 50

        var onlyAduRow = Assert.Single(await repository.GetProjectedStockAlertAsync(center.Id, product.Id, new DateTime(2026, 9, 1), new DateTime(2026, 9, 1),
            useForecast: false, useOutbounds: false));
        Assert.Equal(5m, onlyAduRow.Outbound);

        var noInboundsRow = Assert.Single(await repository.GetProjectedStockAlertAsync(center.Id, product.Id, new DateTime(2026, 9, 1), new DateTime(2026, 9, 1),
            useInbounds: false));
        Assert.Equal(50m, noInboundsRow.Inbound); // still reported on the row, just not added to closingStock
        Assert.Equal(80m, noInboundsRow.ClosingStock); // 100 - 20 + 0
    }

    [Fact]
    public async Task GetProjectedStockAlertAsync_AccumulateTodayToggles_MoveOverdueOrdersOntoToday()
    {
        await using var context = CreateContext();

        var today = DateTime.Today;
        var overdue = today.AddDays(-2);

        var center = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var product = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        var centerProduct = new CenterProduct { Id = 1, IdProduct = product.Id, IdCenter = center.Id, Stock = 100m };

        context.AddRange(center, product, centerProduct);
        context.Calendar.AddRange(
            new Calendar { Date = overdue, DayOfWeekNumber = (int)overdue.DayOfWeek, IsWorkingDay = true },
            new Calendar { Date = today, DayOfWeekNumber = (int)today.DayOfWeek, IsWorkingDay = true });
        context.Order.AddRange(
            new Order { Id = 1, OrderNumber = "IN-OVERDUE", IdProduct = product.Id, IdDestinyCenter = center.Id, Quantity = 30, DeliveredQuantity = 0, MeasurementUnit = "UN", DeliveryDate = overdue, IsInbound = true, IsOutbound = false, IsFictional = false },
            new Order { Id = 2, OrderNumber = "OUT-OVERDUE", IdProduct = product.Id, IdOriginCenter = center.Id, Quantity = 12, DeliveredQuantity = 0, MeasurementUnit = "UN", DeliveryDate = overdue, IsInbound = false, IsOutbound = true, IsFictional = false });

        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var withoutAccumulation = await repository.GetProjectedStockAlertAsync(center.Id, product.Id, overdue, today);
        Assert.Equal(30m, withoutAccumulation.Single(r => r.Date == overdue).Inbound);
        Assert.Equal(0m, withoutAccumulation.Single(r => r.Date == today).Inbound);
        Assert.Equal(12m, withoutAccumulation.Single(r => r.Date == overdue).OutboundOrders);
        Assert.Equal(0m, withoutAccumulation.Single(r => r.Date == today).OutboundOrders);

        var withAccumulation = await repository.GetProjectedStockAlertAsync(center.Id, product.Id, overdue, today,
            accumulateInboundsToday: true, accumulateOutboundsToday: true);
        Assert.Equal(0m, withAccumulation.Single(r => r.Date == overdue).Inbound);
        Assert.Equal(30m, withAccumulation.Single(r => r.Date == today).Inbound);
        Assert.Equal(0m, withAccumulation.Single(r => r.Date == overdue).OutboundOrders);
        Assert.Equal(12m, withAccumulation.Single(r => r.Date == today).OutboundOrders);
    }
}
