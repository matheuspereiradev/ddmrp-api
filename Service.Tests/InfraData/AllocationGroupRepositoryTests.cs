using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Infra.Data.Context;
using Service.Infra.Data.Repositories;

namespace Service.Tests.InfraData;

public class AllocationGroupRepositoryTests
{
    private static (ApplicationDbContext Context, AllocationGroupRepository Repository) CreateSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(1);

        context.Center.Add(new Center { Id = 1, Code = "C1", Description = "Center 1" });
        context.AllocationGroup.Add(new AllocationGroup { Id = 1, Name = "Grupo A" });
        context.SaveChanges();

        return (context, new AllocationGroupRepository(context, currentUser));
    }

    [Fact]
    public async Task GetPriorizedAllocationAsync_SumsApprovedQuantityAndWeightedMetrics_PerGroup()
    {
        var (context, repository) = CreateSut();
        context.Product.AddRange(
            new Product { Id = 1, Reference = "REF1", Description = "P1", UnitOfMeasure = "UN", Weight = 2, Volume = 3, Value = 10, Pallet = 5 },
            new Product { Id = 2, Reference = "REF2", Description = "P2", UnitOfMeasure = "UN", Weight = 1, Volume = 1, Value = 4, Pallet = 2 });
        context.CenterProduct.AddRange(
            new CenterProduct { Id = 1, IdProduct = 1, IdCenter = 1, IdAllocationGroup = 1 },
            new CenterProduct { Id = 2, IdProduct = 2, IdCenter = 1, IdAllocationGroup = 1 });
        context.Workspace.AddRange(
            new Workspace { IdCenter = 1, IdProduct = 1, IdUser = 1, OptimizedQuantity = 10, Approved = true },
            new Workspace { IdCenter = 1, IdProduct = 2, IdUser = 1, OptimizedQuantity = 4, Approved = true });
        context.SaveChanges();

        var result = await repository.GetPriorizedAllocationAsync(1);

        var group = Assert.Single(result);
        Assert.Equal(1, group.Id);
        Assert.Equal("Grupo A", group.Name);
        Assert.Equal(14, group.ApprovedQuantityUnit);
        Assert.Equal(10 * 2 + 4 * 1, group.ApprovedQuantityWeight);
        Assert.Equal(10 * 3 + 4 * 1, group.ApprovedQuantityVolume);
        Assert.Equal(10 * 10 + 4 * 4, group.ApprovedQuantityValue);
        Assert.Equal(10m / 5 + 4m / 2, group.ApprovedQuantityPallet);
    }

    [Fact]
    public async Task GetPriorizedAllocationAsync_NullsOutMetric_WhenAnyItemInGroupIsMissingThatProperty()
    {
        var (context, repository) = CreateSut();
        context.Product.AddRange(
            new Product { Id = 1, Reference = "REF1", Description = "P1", UnitOfMeasure = "UN", Weight = 2, Volume = 3, Value = 10, Pallet = 5 },
            new Product { Id = 2, Reference = "REF2", Description = "P2", UnitOfMeasure = "UN", Weight = null, Volume = 1, Value = 4, Pallet = 0 });
        context.CenterProduct.AddRange(
            new CenterProduct { Id = 1, IdProduct = 1, IdCenter = 1, IdAllocationGroup = 1 },
            new CenterProduct { Id = 2, IdProduct = 2, IdCenter = 1, IdAllocationGroup = 1 });
        context.Workspace.AddRange(
            new Workspace { IdCenter = 1, IdProduct = 1, IdUser = 1, OptimizedQuantity = 10, Approved = true },
            new Workspace { IdCenter = 1, IdProduct = 2, IdUser = 1, OptimizedQuantity = 4, Approved = true });
        context.SaveChanges();

        var result = await repository.GetPriorizedAllocationAsync(1);

        var group = Assert.Single(result);
        Assert.Null(group.ApprovedQuantityWeight);
        Assert.Equal(10 * 3 + 4 * 1, group.ApprovedQuantityVolume);
        Assert.Null(group.ApprovedQuantityPallet);
    }

    [Fact]
    public async Task GetPriorizedAllocationAsync_IgnoresNonApprovedWorkspacesAndUngroupedCenterProducts()
    {
        var (context, repository) = CreateSut();
        context.Product.Add(new Product { Id = 1, Reference = "REF1", Description = "P1", UnitOfMeasure = "UN" });
        context.Product.Add(new Product { Id = 2, Reference = "REF2", Description = "P2", UnitOfMeasure = "UN" });
        context.CenterProduct.Add(new CenterProduct { Id = 1, IdProduct = 1, IdCenter = 1, IdAllocationGroup = 1 });
        context.CenterProduct.Add(new CenterProduct { Id = 2, IdProduct = 2, IdCenter = 1, IdAllocationGroup = null });
        context.Workspace.Add(new Workspace { IdCenter = 1, IdProduct = 1, IdUser = 1, OptimizedQuantity = 10, Approved = false });
        context.Workspace.Add(new Workspace { IdCenter = 1, IdProduct = 2, IdUser = 1, OptimizedQuantity = 20, Approved = true });
        context.SaveChanges();

        var result = await repository.GetPriorizedAllocationAsync(1);

        Assert.Empty(result);
    }
}
