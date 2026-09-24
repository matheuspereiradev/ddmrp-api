namespace Service.Domain.Report.Results
{
    public class QualifiedDemandOrderRow
    {
        public string OrderNumber { get; set; } = string.Empty;
        public decimal PendingQuantity { get; set; }
        public bool IsOverdue { get; set; }
        public bool IsFictional { get; set; }
    }
}
