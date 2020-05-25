using Newtonsoft.Json.Linq;
using System;

namespace Common
{
   public class JTokenHelper
    {
        public static int GetIntValue(JToken jToken)
        {
            try
            {
                return jToken.ToObject<int>();
            }
            catch
            {

                return 0;
            }
        }

        public static DateTime GetDateTimeValue(JToken jToken)
        {
            try
            {
                if (DateTime.TryParse(jToken.ToString(), out DateTime dateTime))
                {
                    return dateTime;
                }
                else if (long.TryParse(jToken.ToString(), out long longValue))
                {
                    DateTime startTime = new DateTime(1970, 1, 1, 8, 0, 0); // 当地时区
                    DateTime dt = startTime.AddSeconds(longValue);
                    return dt;
                }

                return DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        public static DateTime? GetNullAbleDateTimeValue(JToken jToken)
        {
            try
            {
                if (DateTime.TryParse(jToken.ToString(), out DateTime dateTime))
                {
                    return dateTime;
                }
                else if (long.TryParse(jToken.ToString(), out long longValue))
                {
                    DateTime startTime = new DateTime(1970, 1, 1, 8, 0, 0); // 当地时区
                    DateTime dt = startTime.AddSeconds(longValue);
                    return dt;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public static TimeSpan GetDateTimeSpanValue(JToken jToken)
        {
            try
            {
                if (TimeSpan.TryParse(jToken.ToString(), out TimeSpan dateTime))
                {
                    return dateTime;
                }

                return new TimeSpan(0, 0, 0);
            }
            catch
            {
                return new TimeSpan(0, 0, 0);
            }
        }
        public static string GetStringValue(JToken jToken)
        {
            try
            {
                return jToken.ToObject<string>();
            }
            catch
            {

                return null;
            }
        }
        public static decimal GetDecimalValue(JToken jToken)
        {
            try
            {
                string jTokenValue = GetStringValue(jToken);
                if (jTokenValue != null)
                {
                    if (jTokenValue.Contains("."))
                    {
                        jTokenValue = jTokenValue.TrimEnd('0');

                        jTokenValue = jTokenValue == "0." ? "0" : jTokenValue;
                        jTokenValue = jTokenValue.TrimEnd('.');
                    }
                    return decimal.Parse(jTokenValue);
                }
                else
                {
                    return 0;
                }
            }
            catch
            {

                return 0;
            }
        }

        public static bool GetBoolValue(JToken jToken)
        {
            try
            {
                return jToken.ToObject<bool>();
            }
            catch
            {

                return false;
            }
        }

        public static long GetLongValue(JToken jToken)
        {
            try
            {
                return jToken.ToObject<long>();
            }
            catch
            {

                return 0;
            }
        }

        public static T GetEnumValue<T>(JToken jToken) where T : Enum
        {
            try
            {
                if (Enum.IsDefined(typeof(T), jToken.ToObject<int>()))
                {
                    return (T)Enum.ToObject(typeof(T), jToken.ToObject<int>());
                }
                return default;
            }
            catch
            {
                return default;
            }
        }
    }
}
