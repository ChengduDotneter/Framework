using Newtonsoft.Json.Linq;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Model
{
    /// <summary>
    /// json数组转换类
    /// </summary>
    public interface IJArrayConverter
    {
        /// <summary>
        /// 读
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        JArray Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options);
        /// <summary>
        /// 写
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        void Write(Utf8JsonWriter writer, JArray value, JsonSerializerOptions options);
    }
    /// <summary>
    /// json数组转换具体实现
    /// </summary>
    public class JArrayConverter : JsonConverter<JArray>, IJArrayConverter
    {
        /// <summary>
        /// 读取数组
        /// </summary>
        /// <param name="reader">读取编码格式</param>
        /// <param name="typeToConvert">转换类型</param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override JArray Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Read(ref reader, typeToConvert, options);
        }

        public JArray Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, bool inArray = false)
        {
            JArray jArray = new JArray();

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String: jArray.Add(JObjectConverter.GetString(reader.GetString())); break;
                    case JsonTokenType.True: jArray.Add(true); break;
                    case JsonTokenType.False: jArray.Add(false); break;
                    case JsonTokenType.Null: jArray.Add(null); break;
                    case JsonTokenType.None: jArray.Add(string.Empty); break;
                    case JsonTokenType.Number: jArray.Add(JObjectConverter.GetNumber(reader)); break;
                    case JsonTokenType.StartObject: jArray.Add(new JObjectConverter().Read(ref reader, typeof(JArray), options)); break;
                    case JsonTokenType.StartArray: if (inArray) jArray.Add(Read(ref reader, typeof(JArray), options)); inArray = true; break;
                    case JsonTokenType.EndArray: return jArray;
                    case JsonTokenType.Comment: break;
                    case JsonTokenType.PropertyName: throw new NotSupportedException();
                }
            }

            throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, JArray value, JsonSerializerOptions options)
        {
            Write(writer, value, options);
        }

        public void Write(Utf8JsonWriter writer, JArray array, JsonSerializerOptions options, string propertyName = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                writer.WriteStartArray();
            else
                writer.WriteStartArray(propertyName);

            foreach (JToken item in array)
            {
                switch (item.Type)
                {
                    case JTokenType.Integer: WriteNumberValue(writer, item); break;
                    case JTokenType.Float: writer.WriteNumberValue(item.ToObject<float>()); break;
                    case JTokenType.Boolean: writer.WriteBooleanValue(item.ToObject<bool>()); break;
                    case JTokenType.String: writer.WriteStringValue(item.ToString()); break;
                    case JTokenType.Date: writer.WriteStringValue(item.ToObject<DateTime>().ToString("yyyy-MM-dd HH:mm:ss")); break;
                    case JTokenType.Null: writer.WriteNullValue(); break;
                    case JTokenType.Object: new JObjectConverter().Write(writer, item.ToObject<JObject>(), options); break;
                    case JTokenType.Array: Write(writer, item.ToObject<JArray>(), options); break;
                    default: throw new NotSupportedException();
                }
            }

            writer.WriteEndArray();
        }

        private void WriteNumberValue(Utf8JsonWriter writer, JToken item)
        {
            int oldNumber;

            if (int.TryParse(item.ToString(), out oldNumber))
                writer.WriteNumberValue(oldNumber);
            else
                writer.WriteNumberValue(item.ToObject<long>());
        }
    }
}