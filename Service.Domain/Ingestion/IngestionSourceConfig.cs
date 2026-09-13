namespace Service.Domain.Ingestion
{
    public class IngestionSourceConfig
    {
        public string View { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? Table { get; set; }
        public bool AllowInsert { get; set; } = true;
        public bool DeleteNonSent { get; set; } = false;
        public List<string> Key { get; set; } = [];
        public List<FieldMappingConfig> FieldMappings { get; set; } = [];
    }
}
