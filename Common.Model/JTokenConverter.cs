using Newtonsoft.Json.Linq;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Model
{
    public interface IJTokenConverter
    {
        JToken Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);

        void Write(Utf8JsonWriter writer, JToken value, JsonSerializerOptions options);
    }

    public class JTokenConverter : JsonConverter<JToken>, IJTokenConverter
    {
        public override JToken Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
                return new JObjectConverter().Read(ref reader, typeof(JObject), options);
            else if (reader.TokenType == JsonTokenType.StartArray)
                return new JArrayConverter().Read(ref reader, typeof(JArray), options);

            throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, JToken value, JsonSerializerOptions options)
        {
            if (value.Type == JTokenType.Object)
                new JObjectConverter().Write(writer, (JObject)value, options);
            if (value.Type == JTokenType.Array)
                new JArrayConverter().Write(writer, (JArray)value, options);

            throw new NotSupportedException();
        }
    }
}