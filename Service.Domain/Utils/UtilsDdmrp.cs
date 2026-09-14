using Service.Domain.Enums;

namespace Service.Domain.Utils
{
    public static class UtilsDdmrp
    {
        public static decimal CalculateNetflow(decimal stock, decimal qualifiedDemand, decimal inbounds)
        {
            return stock + inbounds - qualifiedDemand;
        }

        public static decimal CalculateOrderQuantity(decimal netflow, decimal topOfYellow, decimal topOfGreen)
        {
            return netflow < topOfYellow ? topOfGreen - netflow : 0;
        }

        public static decimal CalculateOptimizedOrderQuantity(decimal netflow, decimal topOfYellow, decimal topOfGreen, decimal moq, decimal packQuantity)
        {
            if (packQuantity == 0)
                return 0;

            var quantity = CalculateOrderQuantity(netflow, topOfYellow, topOfGreen);

            return quantity < moq ? 0 : Math.Ceiling(quantity / packQuantity) * packQuantity;
        }

        public static decimal CalculateBufferPercentage(decimal topOfGreen, decimal delta)
        {
            return topOfGreen == 0 ? 0 : delta / topOfGreen;
        }

        public static BufferColor CalculateBufferColor(decimal quantity, decimal topOfRed, decimal topOfYellow, decimal topOfGreen)
        {
            if (topOfGreen == 0)
                return BufferColor.NoColor;

            if (quantity < 0)
                return BufferColor.Black;

            if (quantity > topOfGreen)
                return BufferColor.Blue;

            if (quantity <= topOfRed)
                return BufferColor.Red;

            if (quantity <= topOfYellow)
                return BufferColor.Yellow;

            return BufferColor.Green;
        }

        public static decimal CalculateCoverageDays(decimal availableStock, decimal adu)
        {
            return adu > 0 ? availableStock / adu : 0;
        }
    }
}
