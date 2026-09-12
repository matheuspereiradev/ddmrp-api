namespace Service.Domain.Ingestion
{
    public class FieldMappingConfig
    {
        public string? Source { get; set; }
        public string Target { get; set; } = string.Empty;
        public string? Default { get; set; }
        public LookupConfig? Lookup { get; set; }
    }
}
