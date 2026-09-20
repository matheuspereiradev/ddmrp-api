using Service.Domain.AllocationGroups;
using Service.Domain.Enums;

namespace Service.Tests.Domain;

public class PriorizedAllocationEngineTests
{
    private static PriorizedAllocationItem Item(
        int id, decimal approvedQuantity, decimal packQuantity, decimal tog = 10, decimal netflow = 0, decimal moq = 0,
        decimal? weight = null, decimal? volume = null, decimal? value = null, decimal? pallet = null) =>
        new()
        {
            Id = id,
            ApprovedQuantity = approvedQuantity,
            PackQuantity = packQuantity,
            Tog = tog,
            Netflow = netflow,
            Moq = moq,
            ProductWeight = weight,
            ProductVolume = volume,
            ProductValue = value,
            ProductPallet = pallet
        };

    [Fact]
    public void Run_IncreasesLowestPercentageItemFirst_UntilGroupTotalReachesLimit()
    {
        var a = Item(1, approvedQuantity: 0, packQuantity: 2);
        var b = Item(2, approvedQuantity: 6, packQuantity: 2);
        var items = new List<PriorizedAllocationItem> { a, b };

        PriorizedAllocationEngine.Run(items, limit: 8, PriorizedAllocationStopCondition.Zero, PriorizedAllocationAdjustmentType.Unit);

        Assert.Equal(2, a.ApprovedQuantity);
        Assert.Equal(6, b.ApprovedQuantity);
        Assert.True(a.Finished);
        Assert.True(b.Finished);
        Assert.Equal(8, items.Sum(i => i.ApprovedQuantity));
    }

    [Fact]
    public void Run_DecreasesHighestPercentageItemFirst_UntilGroupTotalReachesLimit()
    {
        var a = Item(1, approvedQuantity: 8, packQuantity: 2, moq: 4);
        var b = Item(2, approvedQuantity: 2, packQuantity: 2, moq: 2);
        var items = new List<PriorizedAllocationItem> { a, b };

        PriorizedAllocationEngine.Run(items, limit: 6, PriorizedAllocationStopCondition.Zero, PriorizedAllocationAdjustmentType.Unit);

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
        var items = new List<PriorizedAllocationItem> { a };

        PriorizedAllocationEngine.Run(items, limit: 0, PriorizedAllocationStopCondition.Moq, PriorizedAllocationAdjustmentType.Unit);

        Assert.Equal(3, a.ApprovedQuantity);
        Assert.True(a.Finished);
    }

    [Fact]
    public void Run_Down_SnapsDownToMoq_WhenPackQuantityOvershootsPastIt()
    {
        var a = Item(1, approvedQuantity: 8, packQuantity: 3, moq: 4);
        var items = new List<PriorizedAllocationItem> { a };

        PriorizedAllocationEngine.Run(items, limit: 0, PriorizedAllocationStopCondition.Moq, PriorizedAllocationAdjustmentType.Unit);

        Assert.Equal(4, a.ApprovedQuantity);
        Assert.True(a.Finished);
    }

    [Fact]
    public void Run_Down_StopsAtOnePackQuantityFloor()
    {
        var a = Item(1, approvedQuantity: 10, packQuantity: 4, moq: 6);
        var items = new List<PriorizedAllocationItem> { a };

        PriorizedAllocationEngine.Run(items, limit: 0, PriorizedAllocationStopCondition.OnePackQuantity, PriorizedAllocationAdjustmentType.Unit);

        Assert.Equal(6, a.ApprovedQuantity);
        Assert.True(a.Finished);
    }

    [Fact]
    public void Run_Down_StopsAtZeroFloor()
    {
        var a = Item(1, approvedQuantity: 5, packQuantity: 2, moq: 1);
        var items = new List<PriorizedAllocationItem> { a };

        PriorizedAllocationEngine.Run(items, limit: 0, PriorizedAllocationStopCondition.Zero, PriorizedAllocationAdjustmentType.Unit);

        Assert.Equal(1, a.ApprovedQuantity);
        Assert.True(a.Finished);
    }

    [Fact]
    public void Run_DoesNothing_WhenTotalAlreadyEqualsLimit()
    {
        var a = Item(1, approvedQuantity: 5, packQuantity: 1);
        var items = new List<PriorizedAllocationItem> { a };

        PriorizedAllocationEngine.Run(items, limit: 5, PriorizedAllocationStopCondition.Zero, PriorizedAllocationAdjustmentType.Unit);

        Assert.Equal(5, a.ApprovedQuantity);
        Assert.False(a.Finished);
    }

    [Fact]
    public void Run_PreMarksZeroOrNegativePackQuantityItemsAsFinished_AndNeverAdjustsThem()
    {
        var a = Item(1, approvedQuantity: 3, packQuantity: 0);
        var b = Item(2, approvedQuantity: 1, packQuantity: 1);
        var items = new List<PriorizedAllocationItem> { a, b };

        PriorizedAllocationEngine.Run(items, limit: 5, PriorizedAllocationStopCondition.Zero, PriorizedAllocationAdjustmentType.Unit);

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

    [Fact]
    public void Run_Weight_ComparesLimitAgainstQuantityTimesProductWeight_ButStoresRawUnits()
    {
        var a = Item(1, approvedQuantity: 0, packQuantity: 2, weight: 2);
        var b = Item(2, approvedQuantity: 6, packQuantity: 2, weight: 1);
        var items = new List<PriorizedAllocationItem> { a, b };

        PriorizedAllocationEngine.Run(items, limit: 10, PriorizedAllocationStopCondition.Zero, PriorizedAllocationAdjustmentType.Weight);

        Assert.Equal(2, a.ApprovedQuantity);
        Assert.Equal(6, b.ApprovedQuantity);
        Assert.Equal(10, a.ApprovedQuantity * 2 + b.ApprovedQuantity * 1);
    }

    [Fact]
    public void Run_Value_ComparesLimitAgainstQuantityTimesProductValue()
    {
        var a = Item(1, approvedQuantity: 0, packQuantity: 1, value: 4);
        var items = new List<PriorizedAllocationItem> { a };

        PriorizedAllocationEngine.Run(items, limit: 10, PriorizedAllocationStopCondition.Zero, PriorizedAllocationAdjustmentType.Value);

        Assert.Equal(2, a.ApprovedQuantity);
        Assert.True(a.Finished);
    }

    [Fact]
    public void Run_Pallet_ComparesLimitAgainstQuantityDividedByProductPallet()
    {
        var a = Item(1, approvedQuantity: 0, packQuantity: 10, pallet: 5);
        var items = new List<PriorizedAllocationItem> { a };

        PriorizedAllocationEngine.Run(items, limit: 3, PriorizedAllocationStopCondition.Zero, PriorizedAllocationAdjustmentType.Pallet);

        Assert.Equal(10, a.ApprovedQuantity);
        Assert.Equal(2, a.ApprovedQuantity / 5);
        Assert.True(a.Finished);
    }

    [Fact]
    public void Run_LeavesItemUntouched_WhenProductIsMissingThePropertyTheAdjustmentTypeNeeds()
    {
        var missingWeight = Item(1, approvedQuantity: 3, packQuantity: 1, weight: null);
        var withWeight = Item(2, approvedQuantity: 0, packQuantity: 1, weight: 2);
        var items = new List<PriorizedAllocationItem> { missingWeight, withWeight };

        PriorizedAllocationEngine.Run(items, limit: 4, PriorizedAllocationStopCondition.Zero, PriorizedAllocationAdjustmentType.Weight);

        Assert.True(missingWeight.Finished);
        Assert.Equal(3, missingWeight.ApprovedQuantity);
        Assert.Equal(2, withWeight.ApprovedQuantity);
    }

    [Fact]
    public void Run_LeavesItemUntouched_WhenPalletIsZero()
    {
        var zeroPallet = Item(1, approvedQuantity: 3, packQuantity: 1, pallet: 0);
        var items = new List<PriorizedAllocationItem> { zeroPallet };

        PriorizedAllocationEngine.Run(items, limit: 10, PriorizedAllocationStopCondition.Zero, PriorizedAllocationAdjustmentType.Pallet);

        Assert.True(zeroPallet.Finished);
        Assert.Equal(3, zeroPallet.ApprovedQuantity);
    }
}
