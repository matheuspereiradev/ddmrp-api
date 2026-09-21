using Service.Domain.Enums;

namespace Service.Domain.Utils
{
    public static class UtilsDdmrp
    {
        public static decimal CalculateNetflow(decimal stock, decimal qualifiedDemand, decimal inbounds)
        {
            return stock + inbounds - qualifiedDemand;
        }

        public static decimal CalculateSimulatedNetflow(decimal netflow, bool approved, decimal workspaceOptimizedQuantity)
        {
            return netflow + (approved ? workspaceOptimizedQuantity : 0);
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

            return quantity < moq ? 0 : Math.Floor(quantity / packQuantity) * packQuantity;
        }

        public static decimal CalculateBufferPercentage(decimal topOfGreen, decimal delta)
        {
            return topOfGreen == 0 ? 0 : delta / topOfGreen;
        }

        public static BufferColor CalculateBufferColor(decimal quantity, decimal topOfRed, decimal topOfYellow, decimal topOfGreen)
        {
            if (topOfGreen == 0)
                return BufferColor.NoColor;

            if (quantity <= 0)
                return BufferColor.Black;

            if (quantity > topOfGreen)
                return BufferColor.Blue;

            if (quantity <= topOfRed)
                return BufferColor.Red;

            if (quantity <= topOfYellow)
                return BufferColor.Yellow;

            return BufferColor.Green;
        }
        
        
        public static AnalyticalBufferColor CalculateAnalyticalBufferColor(decimal stock, decimal topOfRedSafeAnalytical, decimal topOfYellowSafeAnalytical, decimal topOfGreenAnalytical, decimal topOfYellowExcessAnalytical, decimal topOfRedExcessAnalytical)
        {
            if (topOfRedExcessAnalytical == 0)
                return AnalyticalBufferColor.NoColor;

            if (stock <= 0)
                return AnalyticalBufferColor.NoColor;

            if (stock <= topOfRedSafeAnalytical)
                return AnalyticalBufferColor.RedSafe;

            if (stock <= topOfYellowSafeAnalytical)
                return AnalyticalBufferColor.YellowSafe;

            if (stock <= topOfGreenAnalytical)
                return AnalyticalBufferColor.Green;

            if (stock <= topOfYellowExcessAnalytical)
                return AnalyticalBufferColor.YellowExcess;
            
            if (stock <= topOfRedExcessAnalytical)
                return AnalyticalBufferColor.RedExcess;

            return AnalyticalBufferColor.Blue;
        }

        public static decimal CalculateCoverageDays(decimal availableStock, decimal adu)
        {
            return adu > 0 ? availableStock / adu : 0;
        }

        public static decimal CalculateTimeBuffer(DateTime deliveryDate, int orderLeadtime)
        {
            var referenceDate = deliveryDate.AddDays(1 - orderLeadtime);
            var daysDiff = (DateTime.Now.Date - referenceDate.Date).Days;

            return (decimal)daysDiff / Math.Max(1, orderLeadtime);
        }

        public static int CalculateDaysToReceive(DateTime? deliveryDate)
        {
            if (!deliveryDate.HasValue)
                return 0;

            var today = DateTime.Now.Date;

            return deliveryDate.Value.Date < today ? 0 : (deliveryDate.Value.Date - today).Days;
        }

        public static BufferColor CalculateTimeBufferColor(decimal timeBufferPercentage)
        {
            if (timeBufferPercentage > 1)
                return BufferColor.Black;

            if (timeBufferPercentage > 0.66m)
                return BufferColor.Red;

            if (timeBufferPercentage > 0.33m)
                return BufferColor.Yellow;

            if (timeBufferPercentage > 0)
                return BufferColor.Green;

            return BufferColor.NoColor;
        }

        public static int CalculateDaysLate(DateTime? deliveryDate)
        {
            if (!deliveryDate.HasValue)
                return 0;

            var today = DateTime.Now.Date;

            return deliveryDate.Value.Date < today ? (today - deliveryDate.Value.Date).Days : 0;
        }

        public static (decimal executionRedZone,decimal executionYellowZone,  decimal executionGreenZone ) CalculateExecutionZone(decimal redZoneNetflow, decimal yellowZoneNetflow)
        {
            var executionRedZone = redZoneNetflow / 2;
            var executionYellowZone = redZoneNetflow / 2;
            var executionGreenZone = yellowZoneNetflow;
            return (executionRedZone, executionYellowZone, executionGreenZone);
        }
        
        public static (decimal executionTopOfRed,decimal executionTopOfYellow,  decimal executionTopOfGreen ) CalculateExecutionTops(decimal redZone, decimal yellowZone, decimal greenZone)
        {
            var executionTopOfRed = redZone;
            var executionTopOfYellow = executionTopOfRed + yellowZone;
            var executionTopOfGreen = executionTopOfYellow +  greenZone;
            return (executionTopOfRed, executionTopOfYellow, executionTopOfGreen);
        }
        
        public static (decimal netflowTopOfRed,decimal netflowTopOfYellow,  decimal netflowTopOfGreen ) CalculateNetflowTops(decimal redBaseZone, decimal redSafeZone, decimal yellowZone, decimal greenZone)
        {
            var netflowTopOfRed = redBaseZone + redSafeZone;
            var netflowTopOfYellow = netflowTopOfRed + yellowZone;
            var netflowTopOfGreen = netflowTopOfYellow +  greenZone;
            return (netflowTopOfRed, netflowTopOfYellow, netflowTopOfGreen);
        }

        public static (decimal redSafeAnalytical, decimal yellowSafeAnalytical, decimal greenAnalytical, decimal yellowExcessAnalytical, decimal redExcessAnalytical) CalculateAnaliticalZone(decimal redZone, decimal yellowZone, decimal greenZone)
        {
            var redSafeAnalytical = redZone / 2;
            var yellowSafeAnalytical = redZone / 2;
            var greenAnalytical = greenZone;
            var yellowExcessAnalytical = greenZone >= yellowZone
                ? 0
                : yellowZone - greenZone;
            var redExcessAnalytical = (redZone + yellowZone + greenZone) <= 0
                ? 0
                : (redZone + yellowZone + greenZone) - (redZone + greenZone + yellowExcessAnalytical);
            return (redSafeAnalytical, yellowSafeAnalytical, greenAnalytical, yellowExcessAnalytical,
                redExcessAnalytical);

        }
        
        public static (decimal topOfRedSafeAnalytical, decimal topOfYellowSafeAnalytical, decimal topOfGreenAnalytical, decimal topOfYellowExcessAnalytical, decimal topOfRedExcessAnalytical) CalculateAnalyticalTops(decimal redSafeAnalytical, decimal yellowSafeAnalytical, decimal greenAnalytical, decimal yellowExcessAnalytical, decimal redExcessAnalytical)
        {
            
            var topOfRedSafeAnalytical = redSafeAnalytical;
            var topOfYellowSafeAnalytical = topOfRedSafeAnalytical + yellowSafeAnalytical;
            var topOfGreenAnalytical = topOfYellowSafeAnalytical + greenAnalytical;
            var topOfYellowExcessAnalytical = topOfGreenAnalytical + yellowExcessAnalytical;
            var topOfRedExcessAnalytical = topOfYellowExcessAnalytical + redExcessAnalytical;
            
            return (topOfRedSafeAnalytical, topOfYellowSafeAnalytical, topOfGreenAnalytical, topOfYellowExcessAnalytical,
                topOfRedExcessAnalytical);

        }
    }
}
