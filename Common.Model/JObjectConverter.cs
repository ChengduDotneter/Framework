using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Model
{
    public interface IJObjectConverter
    {
        JObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);

        void Write(Utf8JsonWriter writer, JObject value, JsonSerializerOptions options);
    }

    public class JObjectConverter : JsonConverter<JObject>, IJObjectConverter
    {
        public override JObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JObject jObject = new JObject();
            string currentPropertyName = string.Empty;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName: currentPropertyName = reader.GetString(); break;
                    case JsonTokenType.String: jObject[currentPropertyName] = GetString(reader.GetString()); break;
                    case JsonTokenType.True: jObject[currentPropertyName] = true; break;
                    case JsonTokenType.False: jObject[currentPropertyName] = false; break;
                    case JsonTokenType.Null: jObject[currentPropertyName] = null; break;
                    case JsonTokenType.None: jObject[currentPropertyName] = string.Empty; break;
                    case JsonTokenType.Number: jObject[currentPropertyName] = GetNumber(reader); break;
                    case JsonTokenType.StartArray: jObject[currentPropertyName] = new JArrayConverter().Read(ref reader, typeof(JArray), options, true); break;
                    case JsonTokenType.StartObject: if (!string.IsNullOrWhiteSpace(currentPropertyName)) jObject[currentPropertyName] = Read(ref reader, typeof(JObject), options); break;
                    case JsonTokenType.EndObject: return jObject;
                    case JsonTokenType.Comment: break;
                }
            }

            throw new NotSupportedException();
        }

        internal static JToken GetNumber(Utf8JsonReader reader)
        {
            if (reader.TryGetSingle(out float number))
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (number == (int)number)
                    return (int)number;
                else
                    return number;
            }
            else if (reader.TryGetInt64(out long longNumber))
                return longNumber.ToString();

            throw new NotSupportedException();
        }

        internal static JToken GetString(string @string)
        {
            return @string;
        }

        public override void Write(Utf8JsonWriter writer, JObject value, JsonSerializerOptions options)
        {
            Write(writer, value, options);
        }

        private void Write(Utf8JsonWriter writer, JObject @object, JsonSerializerOptions options, string propertyName = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                writer.WriteStartObject();
            else
                writer.WriteStartObject(propertyName);

            foreach (KeyValuePair<string, JToken> item in @object)
            {
                switch (item.Value.Type)
                {
                    case JTokenType.Integer: WriteNumber(writer, item); break;
                    case JTokenType.Float: writer.WriteNumber(item.Key, item.Value.ToObject<float>()); break;
                    case JTokenType.Boolean: writer.WriteBoolean(item.Key, item.Value.ToObject<bool>()); break;
                    case JTokenType.String: writer.WriteString(item.Key, item.Value.ToString()); break;
                    case JTokenType.Date: writer.WriteString(item.Key, item.Value.ToObject<DateTime>().ToString("yyyy-MM-dd HH:mm:ss")); break;
                    case JTokenType.Null: writer.WriteNull(item.Key); break;
                    case JTokenType.Object: Write(writer, item.Value.ToObject<JObject>(), options, item.Key); break;
                    case JTokenType.Array: new JArrayConverter().Write(writer, item.Value.ToObject<JArray>(), options, item.Key); break;
                    default: throw new NotSupportedException();
                }
            }

            writer.WriteEndObject();
        }

        private void WriteNumber(Utf8JsonWriter writer, KeyValuePair<string, JToken> item)
        {
            if (int.TryParse(item.Value.ToString(), out int oldNumber))
                writer.WriteNumber(item.Key, oldNumber);
            else
                writer.WriteNumber(item.Key, item.Value.ToObject<long>());
        }
    }
}