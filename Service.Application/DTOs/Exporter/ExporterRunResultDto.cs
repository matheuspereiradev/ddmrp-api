namespace Service.Application.DTOs.Exporter
{
    public class ExporterRunResultDto
    {
        public string DownloadUrl { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
