using Service.Domain.Enums;
using Service.Domain.Utils;

namespace Service.Tests.Domain;

public class UtilsDdmrpTests
{
    [Theory]
    [InlineData(100, 30, 20, 90)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(50, 80, 0, -30)]
    public void CalculateNetflow_ReturnsStockPlusInboundsMinusQualifiedDemand(decimal stock, decimal qualifiedDemand, decimal inbounds, decimal expected)
    {
        var result = UtilsDdmrp.CalculateNetflow(stock, qualifiedDemand, inbounds);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(50, 100, 200, 150)]
    [InlineData(100, 100, 200, 0)]
    [InlineData(150, 100, 200, 0)]
    [InlineData(-10, 0, 50, 60)]
    public void CalculateOrderQuantity_ReturnsTopOfGreenMinusNetflow_OnlyWhenNetflowIsBelowTopOfYellow(decimal netflow, decimal topOfYellow, decimal topOfGreen, decimal expected)
    {
        var result = UtilsDdmrp.CalculateOrderQuantity(netflow, topOfYellow, topOfGreen);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(50, 100, 200, 200, 10, 0)]
    [InlineData(50, 100, 200, 100, 10, 150)]
    [InlineData(50, 100, 200, 100, 40, 120)]
    [InlineData(100, 100, 200, 10, 10, 0)]
    public void CalculateOptimizedOrderQuantity_RoundsDownToPackMultiple_UnlessBelowMoq(
        decimal netflow, decimal topOfYellow, decimal topOfGreen, decimal moq, decimal packQuantity, decimal expected)
    {
        var result = UtilsDdmrp.CalculateOptimizedOrderQuantity(netflow, topOfYellow, topOfGreen, moq, packQuantity);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculateOptimizedOrderQuantity_ReturnsZero_WhenPackQuantityIsZero()
    {
        var result = UtilsDdmrp.CalculateOptimizedOrderQuantity(50, 100, 200, 10, 0);

        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData(200, 100, 0.5)]
    [InlineData(200, 0, 0)]
    [InlineData(200, -50, -0.25)]
    public void CalculateBufferPercentage_ReturnsNetflowDividedByTopOfGreen(decimal topOfGreen, decimal netflow, decimal expected)
    {
        var result = UtilsDdmrp.CalculateBufferPercentage(topOfGreen, netflow);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculateBufferPercentage_ReturnsZero_WhenThereIsNoBuffer()
    {
        var result = UtilsDdmrp.CalculateBufferPercentage(0, 50);

        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData(-1, 50, 100, 200, BufferColor.Black)]
    [InlineData(201, 50, 100, 200, BufferColor.Blue)]
    [InlineData(0, 50, 100, 200, BufferColor.Red)]
    [InlineData(50, 50, 100, 200, BufferColor.Red)]
    [InlineData(51, 50, 100, 200, BufferColor.Yellow)]
    [InlineData(100, 50, 100, 200, BufferColor.Yellow)]
    [InlineData(101, 50, 100, 200, BufferColor.Green)]
    [InlineData(200, 50, 100, 200, BufferColor.Green)]
    public void CalculateBufferColor_ReturnsExpectedColor(decimal quantity, decimal topOfRed, decimal topOfYellow, decimal topOfGreen, BufferColor expected)
    {
        var result = UtilsDdmrp.CalculateBufferColor(quantity, topOfRed, topOfYellow, topOfGreen);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculateBufferColor_ReturnsNoColor_WhenThereIsNoBuffer()
    {
        var result = UtilsDdmrp.CalculateBufferColor(50, 0, 0, 0);

        Assert.Equal(BufferColor.NoColor, result);
    }

    [Theory]
    [InlineData(100, 10, 10)]
    [InlineData(0, 10, 0)]
    [InlineData(100, 0, 0)]
    [InlineData(-50, 10, -5)]
    public void CalculateCoverageDays_ReturnsAvailableStockDividedByAdu_UnlessAduIsZeroOrLess(decimal availableStock, decimal adu, decimal expected)
    {
        var result = UtilsDdmrp.CalculateCoverageDays(availableStock, adu);

        Assert.Equal(expected, result);
    }
}
