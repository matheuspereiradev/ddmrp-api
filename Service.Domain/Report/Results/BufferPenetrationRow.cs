namespace Service.Domain.Report.Results
{
    public class BufferPenetrationRow
    {
        public int IdCenter { get; set; }
        public string CenterCode { get; set; }
        public int IdProduct { get; set; }
        public string ReferenceProduct { get; set; }
        public string DescriptionProduct { get; set; }
        public int DaysBlack { get; set; }
        public int DaysRed { get; set; }
        public int DaysYellow { get; set; }
        public int DaysGreen { get; set; }
        public int DaysBlue { get; set; }
        public int DaysNoColor { get; set; }
        public int DaysRedAndBlack { get; set; }
        public int QuantityDays { get; set; }
        public decimal DaysBlackPercentage { get; set; }
        public decimal DaysRedPercentage { get; set; }
        public decimal DaysYellowPercentage { get; set; }
        public decimal DaysGreenPercentage { get; set; }
        public decimal DaysBluePercentage { get; set; }
        public decimal DaysNoColorPercentage { get; set; }
        public decimal DaysRedAndBlackPercentage { get; set; }
    }
}
