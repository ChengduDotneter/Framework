using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Common.Model
{
    public class ObjectIdConverter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringValue = JsonSerializer.Deserialize<object>(ref reader, options).ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
                return 0;
            else
                return Convert.ToInt64(stringValue);
        }

        public override void Write(
            Utf8JsonWriter writer, long value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    /// <summary>
    /// 可为空的id转换类
    /// </summary>
    public class ObjectIdNullableConverter : JsonConverter<long?>
    {
        public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringValue = JsonSerializer.Deserialize<object>(ref reader, options).ToString();

            return !string.IsNullOrWhiteSpace(stringValue) ? Convert.ToInt64(stringValue) : (long?)null;
        }

        public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.ToString());
            else writer.WriteNullValue();
        }
    }

    /// <summary>
    /// Int转换类
    /// </summary>
    public class IntConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringValue = JsonSerializer.Deserialize<object>(ref reader, options).ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
                return 0;
            else
                return Convert.ToInt32(stringValue);
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    /// <summary>
    /// 可为空的Int转换类
    /// </summary>
    public class IntNullableConverter : JsonConverter<int?>
    {
        public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringValue = JsonSerializer.Deserialize<object>(ref reader, options).ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
                return null;

            return Convert.ToInt32(stringValue);
        }

        public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteNumberValue(value.Value);
            else writer.WriteNullValue();
        }
    }

    /// <summary>
    /// Decimal转换类
    /// </summary>
    public class DecimalConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringValue = JsonSerializer.Deserialize<object>(ref reader, options).ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
                return 0;
            else
                return Convert.ToDecimal(stringValue);
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    /// <summary>
    /// 可为空的Decimal转换类
    /// </summary>
    public class DecimalNullableConverter : JsonConverter<decimal?>
    {
        public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringValue = JsonSerializer.Deserialize<object>(ref reader, options).ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
                return null;

            return Convert.ToDecimal(stringValue);
        }

        public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteNumberValue(value.Value);
            else writer.WriteNullValue();
        }
    }

    /// <summary>
    /// float转换类
    /// </summary>
    public class FloatConverter : JsonConverter<float>
    {
        public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringValue = JsonSerializer.Deserialize<object>(ref reader, options).ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
                return 0;
            else
                return Convert.ToSingle(stringValue);
        }

        public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    /// <summary>
    /// 可为空的Decimal转换类
    /// </summary>
    public class FloatNallableConverter : JsonConverter<float?>
    {
        public override float? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringValue = JsonSerializer.Deserialize<object>(ref reader, options).ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
                return null;

            return Convert.ToSingle(stringValue);
        }

        public override void Write(Utf8JsonWriter writer, float? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteNumberValue(value.Value);
            else writer.WriteNullValue();
        }
    }

    /// <summary>
    /// Bool转换类
    /// </summary>
    public class BoolConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringValue = JsonSerializer.Deserialize<object>(ref reader, options).ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
                return false;
            else
                return Convert.ToBoolean(stringValue);
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    /// <summary>
    /// 可为空的Bool转换类
    /// </summary>
    public class BoolNullableConverter : JsonConverter<bool?>
    {
        public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringValue = JsonSerializer.Deserialize<object>(ref reader, options).ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
                return null;

            return Convert.ToBoolean(stringValue);
        }

        public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteBooleanValue(value.Value);
            else writer.WriteNullValue();
        }
    }

    /// <summary>
    /// 只保留字符串中的字母、数字和汉字
    /// </summary>
    public class StringSpecialSymbolsConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringValue = JsonSerializer.Deserialize<object>(ref reader, options).ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
                return null;

            return Regex.Replace(stringValue, @"[^a-zA-Z0-9\u4e00-\u9fa5\s]", "");
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// 可为空的DateTime转换类
    /// </summary>
    public class DateTimeNullableConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringValue = JsonSerializer.Deserialize<object>(ref reader, options).ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
                return null;

            return Convert.ToDateTime(stringValue);
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd HH:mm:ss"));
            else writer.WriteNullValue();
        }
    }

    /// <summary>
    /// 可为空的DateTime转换类
    /// </summary>
    public class DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringValue = JsonSerializer.Deserialize<object>(ref reader, options).ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
                return DateTime.MinValue;
            return Convert.ToDateTime(stringValue);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }

    /// <summary>
    /// 可为空的本地时间转换
    /// </summary>
    public class DateTimeToLocalConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringValue = JsonSerializer.Deserialize<object>(ref reader, options).ToString();

            if (string.IsNullOrWhiteSpace(stringValue))
                return null;

            DateTime dateTime = Convert.ToDateTime(stringValue).ToLocalTime();

            return dateTime;
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value);
            else writer.WriteNullValue();
        }
    }

    /// <summary>
    /// 数组long类型转换
    /// </summary>
    public class ObjectIdEnumerableConverter : JsonConverter<IEnumerable<long>>
    {
        public override IEnumerable<long> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JArray array = new JArrayConverter().Read(ref reader, typeToConvert, options);
            return array.Select(item => item.ToObject<long>());
        }

        public override void Write(Utf8JsonWriter writer, IEnumerable<long> value, JsonSerializerOptions options)
        {
            new JArrayConverter().Write(writer, JArray.FromObject(value.Select(item => item.ToString())), options);
        }
    }
}