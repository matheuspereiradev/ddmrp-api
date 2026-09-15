using System.Text.Json;
using System.Text.Json.Serialization;

namespace Service.API.Json
{
    public class DecimalRoundingJsonConverter : JsonConverter<decimal>
    {
        private const int DecimalPlaces = 4;

        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetDecimal();

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
            => writer.WriteNumberValue(Math.Round(value, DecimalPlaces, MidpointRounding.AwayFromZero));
    }
}
