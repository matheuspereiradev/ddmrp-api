using Service.Domain.AllocationGroups;
using Service.Domain.Enums;

namespace Service.Tests.Domain;

public class EfficientDistributionEngineTests
{
    private static EfficientDistributionAllocationItem Item(int id, decimal approvedQuantity, decimal packQuantity, decimal tog = 10, decimal netflow = 0, decimal moq = 0) =>
        new() { Id = id, ApprovedQuantity = approvedQuantity, PackQuantity = packQuantity, Tog = tog, Netflow = netflow, Moq = moq };

    [Fact]
    public void Run_IncreasesLowestPercentageItemFirst_UntilGroupTotalReachesLimit()
    {
        var a = Item(1, approvedQuantity: 0, packQuantity: 2);
        var b = Item(2, approvedQuantity: 6, packQuantity: 2);
        var items = new List<EfficientDistributionAllocationItem> { a, b };

        EfficientDistributionEngine.Run(items, limit: 8, EfficientDistributionStopCondition.Zero);

        Assert.Equal(2, a.ApprovedQuantity);
        Assert.Equal(6, b.ApprovedQuantity);
        Assert.True(a.Finished);
        Assert.True(b.Finished);
        Assert.Equal(8, items.Sum(i => i.ApprovedQuantity));
    }

    [Fact]
    public void Run_DecreasesHighestPercentageItemFirst_UntilGroupTotalReachesLimit()
    {
        var a = Item(1, approvedQuantity: 8, packQuantity: 2);
        var b = Item(2, approvedQuantity: 2, packQuantity: 2);
        var items = new List<EfficientDistributionAllocationItem> { a, b };

        EfficientDistributionEngine.Run(items, limit: 6, EfficientDistributionStopCondition.Zero);

        Assert.Equal(4, a.ApprovedQuantity);
        Assert.Equal(2, b.ApprovedQuantity);
        Assert.True(a.Finished);
        Assert.True(b.Finished);
        Assert.Equal(6, items.Sum(i => i.ApprovedQuantity));
    }

    [Fact]
    public void Run_Down_StopsAtMoqFloor_EvenWhenGroupLimitIsLower()
    {
        var a = Item(1, approvedQuantity: 5, packQuantity: 1, moq: 3);
        var items = new List<EfficientDistributionAllocationItem> { a };

        EfficientDistributionEngine.Run(items, limit: 0, EfficientDistributionStopCondition.Moq);

        Assert.Equal(3, a.ApprovedQuantity);
        Assert.True(a.Finished);
    }

    [Fact]
    public void Run_Down_StopsAtOnePackQuantityFloor()
    {
        var a = Item(1, approvedQuantity: 10, packQuantity: 4);
        var items = new List<EfficientDistributionAllocationItem> { a };

        EfficientDistributionEngine.Run(items, limit: 0, EfficientDistributionStopCondition.OnePackQuantity);

        Assert.Equal(6, a.ApprovedQuantity);
        Assert.True(a.Finished);
    }

    [Fact]
    public void Run_Down_StopsAtZeroFloor()
    {
        var a = Item(1, approvedQuantity: 5, packQuantity: 2);
        var items = new List<EfficientDistributionAllocationItem> { a };

        EfficientDistributionEngine.Run(items, limit: 0, EfficientDistributionStopCondition.Zero);

        Assert.Equal(1, a.ApprovedQuantity);
        Assert.True(a.Finished);
    }

    [Fact]
    public void Run_DoesNothing_WhenTotalAlreadyEqualsLimit()
    {
        var a = Item(1, approvedQuantity: 5, packQuantity: 1);
        var items = new List<EfficientDistributionAllocationItem> { a };

        EfficientDistributionEngine.Run(items, limit: 5, EfficientDistributionStopCondition.Zero);

        Assert.Equal(5, a.ApprovedQuantity);
        Assert.False(a.Finished);
    }

    [Fact]
    public void Run_PreMarksZeroOrNegativePackQuantityItemsAsFinished_AndNeverAdjustsThem()
    {
        var a = Item(1, approvedQuantity: 3, packQuantity: 0);
        var b = Item(2, approvedQuantity: 1, packQuantity: 1);
        var items = new List<EfficientDistributionAllocationItem> { a, b };

        EfficientDistributionEngine.Run(items, limit: 5, EfficientDistributionStopCondition.Zero);

        Assert.True(a.Finished);
        Assert.Equal(3, a.ApprovedQuantity);
        Assert.Equal(2, b.ApprovedQuantity);
        Assert.Equal(5, items.Sum(i => i.ApprovedQuantity));
    }

    [Fact]
    public void Percentage_ReturnsZero_WhenTogIsZero()
    {
        var a = Item(1, approvedQuantity: 5, packQuantity: 1, tog: 0);

        Assert.Equal(0, a.Percentage);
    }
}
