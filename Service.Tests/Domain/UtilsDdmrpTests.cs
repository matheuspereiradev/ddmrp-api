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
    [InlineData(50, 100, 200, 100, 40, 160)]
    [InlineData(100, 100, 200, 10, 10, 0)]
    public void CalculateOptimizedOrderQuantity_RoundsUpToPackMultiple_UnlessBelowMoq(
        decimal netflow, decimal topOfYellow, decimal topOfGreen, decimal moq, decimal packQuantity, decimal expected)
    {
        var result = UtilsDdmrp.CalculateOptimizedOrderQuantity(netflow, topOfYellow, topOfGreen, moq, packQuantity);

        Assert.Equal(expected, result);
    }
}
