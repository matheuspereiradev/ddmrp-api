namespace Service.Domain.Ingestion
{
    public class IngestionWriteResult
    {
        public int Inserted { get; set; }
        public int Updated { get; set; }
        public int Deleted { get; set; }
        public List<string> Errors { get; set; } = [];
    }
}
