using System.Text.Json.Serialization;

namespace Service.API.Models
{
    public class ODataResult<T>
    {
        public List<T> Value { get; init; } = [];

        [JsonPropertyName("@odata.count")]
        public long? Count { get; init; }
    }
}
