using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Model
{
    public interface IStringToBufferConverter
    {
        byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);
        void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options);
        string GetStringFromByte(byte[] value);
    }

    public class StringToBufferConverter : JsonConverter<byte[]>, IStringToBufferConverter
    {
        public string GetStringFromByte(byte[] value)
        {
            return Encoding.UTF8.GetString(value);
        }

        public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringValue = JsonSerializer.Deserialize<object>(ref reader, options).ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
                return null;

            return Encoding.UTF8.GetBytes(stringValue);
        }

        public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(GetStringFromByte(value));
        }
    }
}
