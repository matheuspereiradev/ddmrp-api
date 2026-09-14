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
            var quantity = CalculateOrderQuantity(netflow, topOfYellow, topOfGreen);

            return quantity < moq ? 0 : Math.Ceiling(quantity / packQuantity) * packQuantity;
        }
    }
}
