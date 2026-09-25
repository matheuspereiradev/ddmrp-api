using System.Text.Json.Serialization;
using Service.Domain.Report.Results;

namespace Service.API.Models
{
    public class ODataResult<T>
    {
        public List<T> Value { get; init; } = [];

        [JsonPropertyName("@odata.count")]
        public long? Count { get; init; }

        public Dictionary<string, ColumnSummaryResult>? Summary { get; init; }
    }
}
