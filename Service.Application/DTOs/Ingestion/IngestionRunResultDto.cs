namespace Service.Application.DTOs.Ingestion
{
    public class IngestionRunResultDto
    {
        public string View { get; set; } = string.Empty;
        public int RowsRead { get; set; }
        public int RowsInserted { get; set; }
        public int RowsUpdated { get; set; }
        public int RowsDeleted { get; set; }
        public int RowsFailed { get; set; }
        public List<string> Errors { get; set; } = [];
    }
}
