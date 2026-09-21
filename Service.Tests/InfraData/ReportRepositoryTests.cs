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
    public async Task GetInventoryBufferManagementQueryable_SelectedCenters_RestrictsToThoseCentersOnly()
    {
        await using var context = CreateContext();

        var center1 = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var center2 = new Center { Id = 2, Code = "C2", Description = "Center 2" };
        var center3 = new Center { Id = 3, Code = "C3", Description = "Center 3" };
        var product = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };

        context.AddRange(
            center1, center2, center3, product,
            new CenterProduct { Id = 1, IdProduct = product.Id, IdCenter = center1.Id, PackQuantity = 1m, Moq = 1m, Stock = 10m },
            new CenterProduct { Id = 2, IdProduct = product.Id, IdCenter = center2.Id, PackQuantity = 1m, Moq = 1m, Stock = 20m },
            new CenterProduct { Id = 3, IdProduct = product.Id, IdCenter = center3.Id, PackQuantity = 1m, Moq = 1m, Stock = 30m });

        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var restricted = await repository.GetInventoryBufferManagementQueryable(selectedCenters: [1, 2]).ToListAsync();
        Assert.Equal(2, restricted.Count);
        Assert.All(restricted, r => Assert.Contains(r.IdCenter, new[] { 1, 2 }));

        var restrictedFilteredForExcludedCenter = await repository.GetInventoryBufferManagementQueryable(selectedCenters: [1, 2])
            .Where(r => r.IdCenter == 3)
            .ToListAsync();
        Assert.Empty(restrictedFilteredForExcludedCenter);

        var unrestricted = await repository.GetInventoryBufferManagementQueryable().ToListAsync();
        Assert.Equal(3, unrestricted.Count);

        var emptySelection = await repository.GetInventoryBufferManagementQueryable(selectedCenters: []).ToListAsync();
        Assert.Equal(3, emptySelection.Count);
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
        // Netflow (0) is Black on its own (quantity <= 0), but simulated (0 + 42 approved) crosses into Yellow.
        Assert.Equal(BufferColor.Black, rowForUser1.NetflowBufferColor);
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

        // 2026-09-20: every field here is rounded exactly once, from ITS OWN raw formula (never by summing
        // other already-rounded fields) — see the fix below (GetInventoryBufferManagementQueryable_RoundingIsAppliedOnceAtTheEnd_NeverCompounded).
        // RedZone/TopOfRed raw = 10.1 + 10.2 = 20.3 (ceiled to 21 only in the final projection); TopOfGreen raw = 51.0 exact.
        Assert.Equal(11m, row.RedSafeAnalytical); // Ceiling(20.3 / 2) = Ceiling(10.15)
        Assert.Equal(11m, row.YellowSafeAnalytical); // Ceiling(20.3 / 2) = Ceiling(10.15)
        Assert.Equal(16m, row.GreenAnalytical); // Ceiling(15.4)
        Assert.Equal(0m, row.YellowExcessAnalytical); // GreenZone 15.4 >= YellowZone 15.3
        // Raw: TopOfGreen 51.0 - (RedZone 20.3 + GreenZone 15.4 + YellowExcessAnalytical 0) = 15.3, Ceiling = 16.
        // NOT Ceiling(51 - (21+15.4+0)) = 15 — that would reuse the already-ceiled RedZone (21) mid-formula,
        // the same double-rounding bug this test guards against.
        Assert.Equal(16m, row.RedExcessAnalytical);

        // TopOfRedExecution/TopOfYellowExecution/TopOfGreenExecution must be derived from the RAW RedZoneExecution/
        // YellowZoneExecution/GreenZoneExecution (10.15 / 10.15 / 15.3), then ceiled once — not from the already-
        // ceiled displayed RedZoneExecution/YellowZoneExecution (11 + 11 = 22, which would be wrong).
        Assert.Equal(11m, row.TopOfRedExecution); // Ceiling(10.15)
        Assert.Equal(21m, row.TopOfYellowExecution); // Ceiling(10.15 + 10.15) = Ceiling(20.3), matches TopOfRed (21) exactly
        Assert.Equal(36m, row.TopOfGreenExecution); // Ceiling(10.15 + 10.15 + 15.3) = Ceiling(35.6)

        // TopOf*Analytical: same cumulative-sum-of-raw-values-then-ceil-once rule, from RedSafeAnalytical
        // (raw 10.15) / YellowSafeAnalytical (raw 10.15) / GreenAnalytical (raw 15.4) / YellowExcessAnalytical
        // (raw 0) / RedExcessAnalytical (raw 15.3) — never from the already-ceiled displayed values above.
        Assert.Equal(11m, row.TopOfRedSafeAnalytical); // Ceiling(10.15)
        Assert.Equal(21m, row.TopOfYellowSafeAnalytical); // Ceiling(10.15 + 10.15) = Ceiling(20.3)
        Assert.Equal(36m, row.TopOfGreenAnalytical); // Ceiling(20.3 + 15.4) = Ceiling(35.7)
        Assert.Equal(36m, row.TopOfYellowExcessAnalytical); // Ceiling(35.7 + 0)
        // The 5 analytical zones always sum to the raw TopOfGreen (51.0 exact) by construction, so this must
        // equal row.TopOfGreen (both 51) even though each was ceiled from a different raw path.
        Assert.Equal(51m, row.TopOfRedExcessAnalytical); // Ceiling(35.7 + 15.3) = Ceiling(51.0)
        Assert.Equal(row.TopOfGreen, row.TopOfRedExcessAnalytical);
    }

    [Fact]
    public async Task GetInventoryBufferManagementQueryable_RoundingIsAppliedOnceAtTheEnd_NeverCompounded()
    {
        // Regression test for the exact scenario reported 2026-09-20: RedZoneBase 111 + RedZoneSafe 28 = RedZone/
        // TopOfRed 139 (a whole number, no rounding needed for RedZone itself). The old, buggy code rounded
        // RedZoneExecution and YellowZoneExecution independently (each Ceiling(139 / 2) = Ceiling(69.5) = 70) and
        // then summed the two already-rounded halves for TopOfYellowExecution (70 + 70 = 140) — wrong, since
        // TopOfYellowExecution must algebraically equal TopOfRed (139). Rounding must happen once, at the very
        // end, from the raw (unrounded) sum (69.5 + 69.5 = 139), never from summing pre-rounded parts.
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
            RedZoneBase = 111m,
            RedZoneSafe = 28m,
            YellowZone = 0m,
            GreenZone = 0m
        };

        context.AddRange(center, product, centerProduct);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var row = Assert.Single(await repository.GetInventoryBufferManagementQueryable().ToListAsync());

        Assert.Equal(139m, row.RedZone);
        Assert.Equal(139m, row.TopOfRed);
        Assert.Equal(70m, row.RedZoneExecution); // Ceiling(139 / 2) = Ceiling(69.5), display-only
        Assert.Equal(70m, row.YellowZoneExecution); // Ceiling(139 / 2) = Ceiling(69.5), display-only
        Assert.Equal(139m, row.TopOfYellowExecution); // Ceiling(69.5 + 69.5) = Ceiling(139) — NOT 70 + 70 = 140
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
    // Boundary case: stock = qualifiedDemand = 0 makes both Netflow and Stock exactly 0 — must be Black
    // (quantity <= 0), not Red. Catches drift between UtilsDdmrp.CalculateBufferColor's quantity <= 0 check
    // and this query's inline duplicate (which historically used quantity < 0 and missed the exact-zero case).
    [InlineData("Black-Zero", 0, 0, 20, 20, 30, 30, 5, 10, 10)]
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

        // ReservedStock defaults to 0 in this theory's fixtures, so AvailableStock == Stock here — the
        // ReservedStock-specific behavior (Netflow/CoverageDays using AvailableStock while ExecutionBuffer*
        // keeps using raw Stock) is covered separately below by AvailableStock_IsStockMinusReservedStock_AndOnlyFeedsNetflowAndCoverageDays.
        Assert.Equal(row.Stock - row.ReservedStock, row.AvailableStock);
        Assert.Equal(UtilsDdmrp.CalculateNetflow(row.AvailableStock, row.QualifiedDemand ?? 0, row.Inbounds), row.Netflow);
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
        Assert.Equal(UtilsDdmrp.CalculateCoverageDays(row.AvailableStock, row.Adu ?? 0), row.CoverageDays);
        Assert.Equal(UtilsDdmrp.CalculateBufferPercentage(row.TopOfYellowExecution ?? 0, row.Stock), row.ExecutionBufferPercentage);
        Assert.Equal(
            UtilsDdmrp.CalculateBufferColor(row.Stock, row.TopOfRedExecution ?? 0, row.TopOfYellowExecution ?? 0, row.TopOfGreenExecution ?? 0),
            row.ExecutionBufferColor);
        Assert.Equal(
            UtilsDdmrp.CalculateAnalyticalBufferColor(row.Stock, row.TopOfRedSafeAnalytical ?? 0,
                row.TopOfYellowSafeAnalytical ?? 0, row.TopOfGreenAnalytical ?? 0,
                row.TopOfYellowExcessAnalytical ?? 0, row.TopOfRedExcessAnalytical ?? 0),
            row.AnalyticalBufferColor);

        var expectedColor = scenario switch
        {
            "Green" => BufferColor.Green,
            "Yellow" => BufferColor.Yellow,
            "Red" or "Red-PackQuantityZero" => BufferColor.Red,
            "Black-Stockout" or "Black-Zero" => BufferColor.Black,
            "Blue-Overstock" => BufferColor.Blue,
            _ => throw new InvalidOperationException($"Unmapped scenario {scenario}")
        };
        Assert.Equal(expectedColor, row.NetflowBufferColor);
        Assert.Equal(expectedColor, row.SimulatedNetflowBufferColor);

        if (scenario == "Black-Zero")
        {
            Assert.Equal(BufferColor.Black, row.ExecutionBufferColor); // Stock = 0 too, same quantity <= 0 boundary
            // AnalyticalBufferColor: added 2026-09-20, same "stock <= 0" boundary as ExecutionBufferColor —
            // buffer IS computed here (TopOfRedExcessAnalytical > 0), so this is Black, not the NoColor branch.
            Assert.Equal(AnalyticalBufferColor.Black, row.AnalyticalBufferColor);
        }

        if (scenario == "Red-PackQuantityZero")
            Assert.Equal(0m, row.OptimizedOrderQuantity);
    }

    // ReservedStock (added 2026-09-19): AvailableStock = Stock - ReservedStock feeds Netflow/CoverageDays,
    // but ExecutionBufferPercentage/ExecutionBufferColor deliberately keep reading raw Stock (per explicit
    // decision — only Netflow and CoverageDays were asked to move to AvailableStock).
    [Fact]
    public async Task AvailableStock_IsStockMinusReservedStock_AndOnlyFeedsNetflowAndCoverageDays()
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
            ReservedStock = 30m,
            Adu = 10m,
            RedZoneBase = 5m,
            RedZoneSafe = 5m,
            YellowZone = 20m,
            GreenZone = 20m
        };

        context.AddRange(center, product, centerProduct);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var row = Assert.Single(await repository.GetInventoryBufferManagementQueryable().ToListAsync());

        Assert.Equal(100m, row.Stock);
        Assert.Equal(30m, row.ReservedStock);
        Assert.Equal(70m, row.AvailableStock);
        Assert.Equal(UtilsDdmrp.CalculateNetflow(70m, row.QualifiedDemand ?? 0, row.Inbounds), row.Netflow);
        Assert.Equal(UtilsDdmrp.CalculateCoverageDays(70m, row.Adu ?? 0), row.CoverageDays);
        // ExecutionBufferPercentage/Color stay on raw Stock (100), not AvailableStock (70).
        Assert.Equal(UtilsDdmrp.CalculateBufferPercentage(row.TopOfYellowExecution ?? 0, row.Stock), row.ExecutionBufferPercentage);
        Assert.Equal(
            UtilsDdmrp.CalculateBufferColor(row.Stock, row.TopOfRedExecution ?? 0, row.TopOfYellowExecution ?? 0, row.TopOfGreenExecution ?? 0),
            row.ExecutionBufferColor);
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

        // Same composition check for the analytical fields added 2026-09-20 — TopOfRedExcessAnalytical is
        // built the same "raw physical columns, threaded through the .Select() chain" way as TopOfGreen, so
        // it must survive an external .Where() the same way (it doesn't reference any Ignore()'d CenterProduct
        // computed property, but a regression here would mean a future edit reintroduced that mistake).
        var byAnalyticalColor = await repository.GetInventoryBufferManagementQueryable()
            .Where(r => r.AnalyticalBufferColor == AnalyticalBufferColor.RedSafe)
            .ToListAsync();
        Assert.Equal(10m, Assert.Single(byAnalyticalColor).Stock);
    }

    // Snapshot counts by color for the dashboard — covers the Analytical grouping added 2026-09-20 alongside
    // the pre-existing Netflow/Execution ones.
    [Fact]
    public async Task SummarizeInventoryBufferManagementByColorAsync_GroupsByEachColorIncludingAnalytical()
    {
        await using var context = CreateContext();

        var center = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var product = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        var otherProduct = new Product { Id = 2, Reference = "REF2", Description = "Product 2", UnitOfMeasure = "UN" };
        var thirdProduct = new Product { Id = 3, Reference = "REF3", Description = "Product 3", UnitOfMeasure = "UN" };
        // Red on every axis: low stock relative to a big buffer.
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
        // No buffer computed yet — NoColor on every axis.
        var noColorCenterProduct = new CenterProduct
        {
            Id = 2,
            IdProduct = otherProduct.Id,
            IdCenter = center.Id,
            PackQuantity = 10m,
            Moq = 5m,
            Stock = 10m
        };
        // Stockout (Stock = 0) with a computed buffer — Black on every axis (added 2026-09-20 for AnalyticalBufferColor).
        var blackCenterProduct = new CenterProduct
        {
            Id = 3,
            IdProduct = thirdProduct.Id,
            IdCenter = center.Id,
            PackQuantity = 10m,
            Moq = 5m,
            Stock = 0m,
            RedZoneBase = 20m,
            RedZoneSafe = 20m,
            YellowZone = 30m,
            GreenZone = 30m
        };

        context.AddRange(center, product, otherProduct, thirdProduct, redCenterProduct, noColorCenterProduct,
            blackCenterProduct);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var result = await repository.SummarizeInventoryBufferManagementByColorAsync(
            repository.GetInventoryBufferManagementQueryable());

        Assert.Equal(3, result.Netflow.Sum(r => r.Count));
        Assert.Equal(3, result.Execution.Sum(r => r.Count));
        Assert.Equal(3, result.Analytical.Sum(r => r.Count));
        Assert.Contains(result.Analytical, r => r.Color == AnalyticalBufferColor.RedSafe && r.Count == 1);
        Assert.Contains(result.Analytical, r => r.Color == AnalyticalBufferColor.NoColor && r.Count == 1);
        Assert.Contains(result.Analytical, r => r.Color == AnalyticalBufferColor.Black && r.Count == 1);
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

        Assert.Equal(UtilsDdmrp.CalculateNetflow(row.AvailableStock, row.QualifiedDemand ?? 0, row.Inbounds), row.Netflow);
        Assert.Equal(
            UtilsDdmrp.CalculateBufferColor(row.Netflow, row.TopOfRed ?? 0, row.TopOfYellow ?? 0, row.TopOfGreen ?? 0),
            row.NetflowBufferColor);
        Assert.Equal(UtilsDdmrp.CalculateCoverageDays(row.AvailableStock, row.Adu ?? 0), row.CoverageDays);
        Assert.Equal(
            UtilsDdmrp.CalculateBufferColor(row.Stock, row.TopOfRedExecution ?? 0, row.TopOfYellowExecution ?? 0, row.TopOfGreenExecution ?? 0),
            row.ExecutionBufferColor);

        Assert.Equal(BufferColor.NoColor, row.NetflowBufferColor);
        Assert.Equal(BufferColor.NoColor, row.SimulatedNetflowBufferColor);
        Assert.Equal(BufferColor.NoColor, row.ExecutionBufferColor);
        Assert.Equal(AnalyticalBufferColor.NoColor, row.AnalyticalBufferColor);
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

    [Fact]
    public async Task GetBufferPenetrationAsync_CountsDaysPerColor_AndComputesPercentages()
    {
        await using var context = CreateContext();

        var center = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var product = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        context.AddRange(center, product);

        var start = new DateTime(2026, 1, 1);

        // RedZone (RedBaseZone+RedSafeZone) = 20, TopOfYellow = 40, TopOfGreen = 60 (OpenInbounds/QualifiedDemand
        // are 0 on every row, so Netflow == Stock and the expected color is easy to reason about per day).
        context.History.AddRange(
            new History { Id = 1, IdProduct = product.Id, IdCenter = center.Id, Date = start, Consumption = 0, Stock = 50, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20 }, // Green
            new History { Id = 2, IdProduct = product.Id, IdCenter = center.Id, Date = start.AddDays(1), Consumption = 0, Stock = 30, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20 }, // Yellow
            new History { Id = 3, IdProduct = product.Id, IdCenter = center.Id, Date = start.AddDays(2), Consumption = 0, Stock = 10, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20 }, // Red
            new History { Id = 4, IdProduct = product.Id, IdCenter = center.Id, Date = start.AddDays(3), Consumption = 0, Stock = 0, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20 }, // Black
            new History { Id = 5, IdProduct = product.Id, IdCenter = center.Id, Date = start.AddDays(4), Consumption = 0, Stock = 70, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20 }, // Blue
            new History { Id = 6, IdProduct = product.Id, IdCenter = center.Id, Date = start.AddDays(5), Consumption = 0, Stock = 5 } // no zones -> NoColor
        );

        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var rows = await repository.GetBufferPenetrationAsync(start, start.AddDays(5), null, null);

        var row = Assert.Single(rows);

        Assert.Equal(center.Id, row.IdCenter);
        Assert.Equal("C1", row.CenterCode);
        Assert.Equal(product.Id, row.IdProduct);
        Assert.Equal("REF1", row.ReferenceProduct);
        Assert.Equal("Product 1", row.DescriptionProduct);

        Assert.Equal(6, row.QuantityDays);
        Assert.Equal(1, row.DaysGreen);
        Assert.Equal(1, row.DaysYellow);
        Assert.Equal(1, row.DaysRed);
        Assert.Equal(1, row.DaysBlack);
        Assert.Equal(1, row.DaysBlue);
        Assert.Equal(1, row.DaysNoColor);
        Assert.Equal(2, row.DaysRedAndBlack);

        Assert.Equal(1m / 6m, row.DaysGreenPercentage);
        Assert.Equal(1m / 6m, row.DaysYellowPercentage);
        Assert.Equal(1m / 6m, row.DaysRedPercentage);
        Assert.Equal(1m / 6m, row.DaysBlackPercentage);
        Assert.Equal(1m / 6m, row.DaysBluePercentage);
        Assert.Equal(1m / 6m, row.DaysNoColorPercentage);
        Assert.Equal(2m / 6m, row.DaysRedAndBlackPercentage);
    }

    [Fact]
    public async Task GetBufferPenetrationAsync_ExecutionMode_ClassifiesByStockAndExecutionZones_IgnoringQualifiedDemandAndInbounds()
    {
        await using var context = CreateContext();

        var center = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var product = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        context.AddRange(center, product);

        var start = new DateTime(2026, 1, 1);

        // TopOfRed (RedBaseZone+RedSafeZone) = 20 -> RedZoneExecution = YellowZoneExecution = Ceiling(20/2) = 10,
        // GreenZoneExecution = YellowZone = 20 -> TopOfRedExecution = 10, TopOfYellowExecution = 20, TopOfGreenExecution = 40.
        // QualifiedDemand/OpenInbounds are set to large, mismatched values on purpose: Execution mode must ignore
        // them entirely (only Stock feeds the color), unlike Netflow mode which would combine them.
        context.History.AddRange(
            new History { Id = 1, IdProduct = product.Id, IdCenter = center.Id, Date = start, Consumption = 0, Stock = 30, QualifiedDemand = 1000, OpenInbounds = 1000, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20 }, // Green
            new History { Id = 2, IdProduct = product.Id, IdCenter = center.Id, Date = start.AddDays(1), Consumption = 0, Stock = 15, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20 }, // Yellow
            new History { Id = 3, IdProduct = product.Id, IdCenter = center.Id, Date = start.AddDays(2), Consumption = 0, Stock = 5, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20 }, // Red
            new History { Id = 4, IdProduct = product.Id, IdCenter = center.Id, Date = start.AddDays(3), Consumption = 0, Stock = 0, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20 }, // Black
            new History { Id = 5, IdProduct = product.Id, IdCenter = center.Id, Date = start.AddDays(4), Consumption = 0, Stock = 50, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20 }, // Blue
            new History { Id = 6, IdProduct = product.Id, IdCenter = center.Id, Date = start.AddDays(5), Consumption = 0, Stock = 5 } // no zones -> NoColor
        );

        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var rows = await repository.GetBufferPenetrationAsync(start, start.AddDays(5), null, null, BufferPenetrationMode.Execution);

        var row = Assert.Single(rows);

        Assert.Equal(6, row.QuantityDays);
        Assert.Equal(1, row.DaysGreen);
        Assert.Equal(1, row.DaysYellow);
        Assert.Equal(1, row.DaysRed);
        Assert.Equal(1, row.DaysBlack);
        Assert.Equal(1, row.DaysBlue);
        Assert.Equal(1, row.DaysNoColor);
    }

    [Fact]
    public async Task GetBufferPenetrationAsync_GroupsPerCenterProductPair_AndRespectsFiltersAndDateRange()
    {
        await using var context = CreateContext();

        var center1 = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var center2 = new Center { Id = 2, Code = "C2", Description = "Center 2" };
        var product1 = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        var product2 = new Product { Id = 2, Reference = "REF2", Description = "Product 2", UnitOfMeasure = "UN" };
        var deletedProduct = new Product { Id = 3, Reference = "REF3", Description = "Product 3", UnitOfMeasure = "UN", deletedAt = DateTime.UtcNow };
        context.AddRange(center1, center2, product1, product2, deletedProduct);

        var start = new DateTime(2026, 1, 1);
        var end = start.AddDays(2);

        // Zones set so TopOfGreen > 0 and Stock = 0 lands on Black (quantity <= 0), not NoColor.
        context.History.AddRange(
            new History { Id = 1, IdProduct = product1.Id, IdCenter = center1.Id, Date = start, Consumption = 0, Stock = 0, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20 }, // Black, in range
            new History { Id = 2, IdProduct = product1.Id, IdCenter = center1.Id, Date = start.AddDays(1), Consumption = 0, Stock = 0, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20 }, // Black, in range
            new History { Id = 3, IdProduct = product1.Id, IdCenter = center1.Id, Date = start.AddDays(-1), Consumption = 0, Stock = 0, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20 }, // before range, excluded
            new History { Id = 4, IdProduct = product1.Id, IdCenter = center1.Id, Date = start, Consumption = 0, Stock = 0, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20, deletedAt = DateTime.UtcNow }, // soft-deleted, excluded
            new History { Id = 5, IdProduct = product2.Id, IdCenter = center2.Id, Date = start, Consumption = 0, Stock = 0, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20 }, // different pair, in range
            new History { Id = 6, IdProduct = deletedProduct.Id, IdCenter = center1.Id, Date = start, Consumption = 0, Stock = 0, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20 } // deleted product, excluded
        );

        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var allRows = await repository.GetBufferPenetrationAsync(start, end, null, null);
        Assert.Equal(2, allRows.Count);

        var pair1 = allRows.Single(r => r.IdCenter == center1.Id && r.IdProduct == product1.Id);
        Assert.Equal(2, pair1.QuantityDays);
        Assert.Equal(2, pair1.DaysBlack);

        var pair2 = allRows.Single(r => r.IdCenter == center2.Id && r.IdProduct == product2.Id);
        Assert.Equal(1, pair2.QuantityDays);

        var filteredByCenter = await repository.GetBufferPenetrationAsync(start, end, new[] { center2.Id }, null);
        Assert.Equal(pair2.IdProduct, Assert.Single(filteredByCenter).IdProduct);

        var filteredByMultipleCenters = await repository.GetBufferPenetrationAsync(start, end, new[] { center1.Id, center2.Id }, null);
        Assert.Equal(2, filteredByMultipleCenters.Count);

        var filteredByProduct = await repository.GetBufferPenetrationAsync(start, end, null, product1.Id);
        Assert.Equal(pair1.IdCenter, Assert.Single(filteredByProduct).IdCenter);
    }

    [Fact]
    public async Task GetItemsByBufferColorHistoryAsync_CountsItemsPerColor_ForBothPerspectives_AsADenseGrid()
    {
        await using var context = CreateContext();

        var center = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var productA = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        var productB = new Product { Id = 2, Reference = "REF2", Description = "Product 2", UnitOfMeasure = "UN" };
        context.AddRange(center, productA, productB);

        var date = new DateTime(2026, 1, 1);

        // Zones: TopOfRed = 20, TopOfYellow = 40, TopOfGreen = 60 (Netflow perspective).
        // Execution: RedZoneExecution = YellowZoneExecution = Ceiling(20/2) = 10, GreenZoneExecution = YellowZone = 20
        // -> TopOfRedExecution = 10, TopOfYellowExecution = 20, TopOfGreenExecution = 40.
        context.History.AddRange(
            // Item A: Netflow = 50 -> Green. Execution: Stock = 50 > 40 -> Blue. Perspectives disagree on purpose.
            new History { Id = 1, IdProduct = productA.Id, IdCenter = center.Id, Date = date, Consumption = 0, Stock = 50, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20 },
            // Item B: Netflow = 10 -> Red. Execution: Stock = 10 <= 10 -> Red. Perspectives agree.
            new History { Id = 2, IdProduct = productB.Id, IdCenter = center.Id, Date = date, Consumption = 0, Stock = 10, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20 }
        );

        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var result = await repository.GetItemsByBufferColorHistoryAsync(date, date, null, null);

        // One row per day, per perspective — all 6 colors as columns on that single row.
        var netflowDay = Assert.Single(result.Netflow);
        var executionDay = Assert.Single(result.Execution);
        Assert.Equal(date, netflowDay.Date);
        Assert.Equal(date, executionDay.Date);

        Assert.Equal(1, netflowDay.Green);
        Assert.Equal(0, executionDay.Green);

        Assert.Equal(0, netflowDay.Blue);
        Assert.Equal(1, executionDay.Blue);

        Assert.Equal(1, netflowDay.Red);
        Assert.Equal(1, executionDay.Red);

        Assert.Equal(0, netflowDay.Yellow);
        Assert.Equal(0, netflowDay.Black);
        Assert.Equal(0, netflowDay.NoColor);
    }

    [Fact]
    public async Task GetItemsByBufferColorHistoryAsync_OneDensGridPerDay_AndRespectsFiltersAndDateRange()
    {
        await using var context = CreateContext();

        var center1 = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var center2 = new Center { Id = 2, Code = "C2", Description = "Center 2" };
        var product1 = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        var product2 = new Product { Id = 2, Reference = "REF2", Description = "Product 2", UnitOfMeasure = "UN" };
        context.AddRange(center1, center2, product1, product2);

        var day1 = new DateTime(2026, 1, 1);
        var day2 = day1.AddDays(1);

        context.History.AddRange(
            new History { Id = 1, IdProduct = product1.Id, IdCenter = center1.Id, Date = day1, Consumption = 0, Stock = 0, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20 }, // Black
            new History { Id = 2, IdProduct = product1.Id, IdCenter = center1.Id, Date = day2, Consumption = 0, Stock = 0, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20 }, // Black
            new History { Id = 3, IdProduct = product1.Id, IdCenter = center1.Id, Date = day1.AddDays(-1), Consumption = 0, Stock = 0, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20 }, // before range
            new History { Id = 4, IdProduct = product2.Id, IdCenter = center2.Id, Date = day1, Consumption = 0, Stock = 0, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 20, GreenZone = 20 } // different pair
        );

        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var allResult = await repository.GetItemsByBufferColorHistoryAsync(day1, day2, null, null);
        // 2 days in range with data (day1, day2) -> 2 rows per perspective.
        Assert.Equal(2, allResult.Netflow.Count);
        Assert.Equal(2, allResult.Execution.Count);
        // day1 has 2 items (product1@center1 + product2@center2), both Black.
        Assert.Equal(2, allResult.Netflow.Single(r => r.Date == day1).Black);
        // day2 has 1 item (product1@center1 only), Black.
        Assert.Equal(1, allResult.Netflow.Single(r => r.Date == day2).Black);

        var filteredByCenter = await repository.GetItemsByBufferColorHistoryAsync(day1, day2, new[] { center2.Id }, null);
        // Only center2 has data on day1, none on day2 -> only day1's row.
        Assert.Single(filteredByCenter.Netflow);
        Assert.Equal(1, filteredByCenter.Netflow.Single().Black);

        var filteredByMultipleCenters = await repository.GetItemsByBufferColorHistoryAsync(day1, day2, new[] { center1.Id, center2.Id }, null);
        Assert.Equal(2, filteredByMultipleCenters.Netflow.Count);
        Assert.Equal(2, filteredByMultipleCenters.Netflow.Single(r => r.Date == day1).Black);

        var filteredByProduct = await repository.GetItemsByBufferColorHistoryAsync(day1, day2, null, product1.Id);
        Assert.Equal(2, filteredByProduct.Netflow.Count);
        Assert.Equal(1, filteredByProduct.Netflow.Single(r => r.Date == day1).Black);
    }

    [Fact]
    public async Task GetAccumulatedBufferHistoryAsync_SumsMetricsPerDate_ExcludingItemsWithNoRedZone_AndCentersOutsideTheFilter()
    {
        await using var context = CreateContext();

        var center1 = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var center2 = new Center { Id = 2, Code = "C2", Description = "Center 2" };
        var otherCenter = new Center { Id = 3, Code = "C3", Description = "Center 3" };
        var product1 = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        var product2 = new Product { Id = 2, Reference = "REF2", Description = "Product 2", UnitOfMeasure = "UN" };
        context.AddRange(center1, center2, otherCenter, product1, product2);

        var day1 = new DateTime(2026, 1, 1);
        var day2 = day1.AddDays(1);

        context.History.AddRange(
            // Day 1, product1@center1: RedZone 20, Yellow 30, Green 40, Stock 100, QualifiedDemand 20, OpenInbounds 5.
            new History { Id = 1, IdProduct = product1.Id, IdCenter = center1.Id, Date = day1, Consumption = 0, Stock = 100, QualifiedDemand = 20, OpenInbounds = 5, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 30, GreenZone = 40 },
            // Day 1, product2@center2: RedZone 10, Yellow 10, Green 10, Stock 20.
            new History { Id = 2, IdProduct = product2.Id, IdCenter = center2.Id, Date = day1, Consumption = 0, Stock = 20, RedBaseZone = 5, RedSafeZone = 5, YellowZone = 10, GreenZone = 10 },
            // Day 1, no red zone (RedBaseZone + RedSafeZone == 0) -> excluded from the report entirely.
            new History { Id = 3, IdProduct = product1.Id, IdCenter = center2.Id, Date = day1, Consumption = 0, Stock = 999, RedBaseZone = 0, RedSafeZone = 0, YellowZone = 999, GreenZone = 999 },
            // Day 1, center outside the idCenters filter -> excluded.
            new History { Id = 4, IdProduct = product1.Id, IdCenter = otherCenter.Id, Date = day1, Consumption = 0, Stock = 999, RedBaseZone = 999, RedSafeZone = 999, YellowZone = 999, GreenZone = 999 },
            // Day 2, single item, its own independent sum (verifies grouping doesn't leak across dates).
            new History { Id = 5, IdProduct = product1.Id, IdCenter = center1.Id, Date = day2, Consumption = 0, Stock = 50, RedBaseZone = 10, RedSafeZone = 10, YellowZone = 10, GreenZone = 10 }
        );

        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var rows = await repository.GetAccumulatedBufferHistoryAsync(day1, day2, new[] { center1.Id, center2.Id });

        Assert.Equal(2, rows.Count);

        var row1 = rows.Single(r => r.Date == day1);
        // product1@center1: ExecutionRed/Yellow = Ceiling(20/2) = 10, ExecutionGreen = YellowZone = 30.
        // product2@center2: ExecutionRed/Yellow = Ceiling(10/2) = 5, ExecutionGreen = YellowZone = 10.
        Assert.Equal(15m, row1.ExecutionRedZone); // 10 + 5
        Assert.Equal(15m, row1.ExecutionYellowZone); // 10 + 5
        Assert.Equal(40m, row1.ExecutionGreenZone); // 30 + 10
        Assert.Equal(30m, row1.NetflowRedZone); // 20 + 10
        Assert.Equal(40m, row1.NetflowYellowZone); // 30 + 10
        Assert.Equal(50m, row1.NetflowGreenZone); // 40 + 10
        // product1@center1: RedSafeAnalytical=20/2=10, YellowSafeAnalytical=20/2=10,
        // GreenAnalytical=40 (GreenZone alone, matches CenterProduct.GreenAnalytical),
        // YellowExcessAnalytical=0 (green 40 >= yellow 30), TopOfGreenNetflow=20+30+40=90,
        // RedExcessAnalytical=90-(20+40+0)=30.
        // product2@center2: RedSafeAnalytical=5, YellowSafeAnalytical=5, GreenAnalytical=10, YellowExcessAnalytical=0 (green 10 >= yellow 10),
        // TopOfGreenNetflow=10+10+10=30, RedExcessAnalytical=30-(10+10+0)=10.
        Assert.Equal(15m, row1.RedSafeAnalytical); // 10 + 5
        Assert.Equal(15m, row1.YellowSafeAnalytical); // 10 + 5
        Assert.Equal(50m, row1.GreenAnalytical); // 40 + 10
        Assert.Equal(0m, row1.YellowExcessAnalytical); // 0 + 0
        Assert.Equal(40m, row1.RedExcessAnalytical); // 30 + 10
        // AverageProjectedInventory: (20 + 40/2) + (10 + 10/2) = 40 + 15
        Assert.Equal(55m, row1.AverageProjectedInventory);
        Assert.Equal(120m, row1.AvailableStock); // 100 + 20
        // Netflow: (100 + 5 - 20) + (20 + 0 - 0) = 85 + 20
        Assert.Equal(105m, row1.Netflow);
        // ExcessStock: product1 (100 - (20+30+40)=100-90=10, >0) + product2 (20 - (10+10+10)=20-30=-10, floored to 0)
        Assert.Equal(10m, row1.ExcessStock);
        // ExcessStockAnalytical (no yellow zone): product1 (100 - (20+40)=40, >0) + product2 (20 - (10+10)=0, not >0)
        Assert.Equal(40m, row1.ExcessStockAnalytical);
        Assert.Equal(30m, row1.MinimumOscillationRange); // 20 + 10
        Assert.Equal(80m, row1.MaximumOscillationRange); // (20+40) + (10+10)

        var row2 = rows.Single(r => r.Date == day2);
        Assert.Equal(10m, row2.ExecutionRedZone); // Ceiling(20/2)
        Assert.Equal(10m, row2.ExecutionYellowZone);
        Assert.Equal(10m, row2.ExecutionGreenZone); // YellowZone
        Assert.Equal(20m, row2.NetflowRedZone);
        Assert.Equal(10m, row2.NetflowYellowZone);
        Assert.Equal(10m, row2.NetflowGreenZone);
        Assert.Equal(10m, row2.RedSafeAnalytical); // 20/2
        Assert.Equal(10m, row2.YellowSafeAnalytical); // 20/2
        Assert.Equal(10m, row2.GreenAnalytical); // GreenZone alone
        Assert.Equal(0m, row2.YellowExcessAnalytical); // green 10 >= yellow 10
        Assert.Equal(10m, row2.RedExcessAnalytical); // TopOfGreenNetflow 40 - (20+10+0)
        Assert.Equal(25m, row2.AverageProjectedInventory); // 20 + 10/2
        Assert.Equal(50m, row2.AvailableStock);
        Assert.Equal(50m, row2.Netflow); // no QualifiedDemand/OpenInbounds
        Assert.Equal(10m, row2.ExcessStock); // 50 - (20+10+10)=10
        Assert.Equal(20m, row2.ExcessStockAnalytical); // 50 - (20+10)=20
        Assert.Equal(20m, row2.MinimumOscillationRange);
        Assert.Equal(30m, row2.MaximumOscillationRange); // 20 + 10
    }

    [Fact]
    public async Task GetAccumulatedBufferHistoryAsync_YellowExcessAnalytical_IsPositive_WhenGreenZoneIsSmallerThanYellowZone()
    {
        await using var context = CreateContext();

        var center = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var product = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        context.AddRange(center, product);

        var day = new DateTime(2026, 1, 1);

        // RedZone 10, Yellow 20, Green 5 -> GreenZone < YellowZone, so YellowExcessAnalytical is non-zero.
        context.History.Add(new History
        {
            Id = 1,
            IdProduct = product.Id,
            IdCenter = center.Id,
            Date = day,
            Consumption = 0,
            Stock = 0,
            RedBaseZone = 5,
            RedSafeZone = 5,
            YellowZone = 20,
            GreenZone = 5
        });

        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var row = Assert.Single(await repository.GetAccumulatedBufferHistoryAsync(day, day, new[] { center.Id }));

        Assert.Equal(15m, row.YellowExcessAnalytical); // 20 - 5 = 15 (yellow - green)
        Assert.Equal(5m, row.RedExcessAnalytical); // topOfGreenNetflow 35 - (10+5+15) = 5
    }

    [Fact]
    public async Task GetAccumulatedBufferHistoryAsync_ReturnsEmptyList_WhenNoHistoryMatches()
    {
        await using var context = CreateContext();

        var center = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var product = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        context.AddRange(center, product);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var rows = await repository.GetAccumulatedBufferHistoryAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), new[] { center.Id });

        Assert.Empty(rows);
    }

    [Fact]
    public async Task GetAccumulatedBufferHistoryAsync_UsesAvailableStock_StockMinusReservedStock_NotRawStock()
    {
        await using var context = CreateContext();

        var center = new Center { Id = 1, Code = "C1", Description = "Center 1" };
        var product = new Product { Id = 1, Reference = "REF1", Description = "Product 1", UnitOfMeasure = "UN" };
        context.AddRange(center, product);

        var day = new DateTime(2026, 1, 1);

        // Stock 100, ReservedStock 30 -> AvailableStock 70. RedZone 20, Yellow 10, Green 10 -> TopOfGreenNetflow 40.
        context.History.Add(new History
        {
            Id = 1,
            IdProduct = product.Id,
            IdCenter = center.Id,
            Date = day,
            Consumption = 0,
            Stock = 100,
            ReservedStock = 30,
            QualifiedDemand = 5,
            OpenInbounds = 0,
            RedBaseZone = 10,
            RedSafeZone = 10,
            YellowZone = 10,
            GreenZone = 10
        });

        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var row = Assert.Single(await repository.GetAccumulatedBufferHistoryAsync(day, day, new[] { center.Id }));

        Assert.Equal(70m, row.AvailableStock); // 100 - 30, not the raw Stock of 100
        Assert.Equal(65m, row.Netflow); // CalculateNetflow(70, 5, 0) = 70 + 0 - 5
        Assert.Equal(30m, row.ExcessStock); // 70 - (20+10+10) = 30
    }
}
