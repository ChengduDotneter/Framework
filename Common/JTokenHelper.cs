using Newtonsoft.Json.Linq;
using System;

namespace Common
{
    /// <summary>
    /// JToken相关Helper类
    /// </summary>
    public class JTokenHelper
    {
        /// <summary>
        /// 获取整形数据
        /// </summary>
        /// <param name="jToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 获取时间数据
        /// </summary>
        /// <param name="jToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 获取可为空的时间数据
        /// </summary>
        /// <param name="jToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <param name="jToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 获取字符串
        /// </summary>
        /// <param name="jToken"></param>
        /// <returns></returns>
        public static string GetStringValue(JToken jToken)
        {
            try
            {
                return jToken.ToObject<string>().Trim();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取decimal
        /// </summary>
        /// <param name="jToken"></param>
        /// <returns></returns>
        public static decimal GetDecimalValue(JToken jToken)
        {
            try
            {
                string jTokenValue = GetStringValue(jToken);

                if (!string.IsNullOrWhiteSpace(jTokenValue))
                    return decimal.Parse(jTokenValue);
                else
                    return 0;
            }
            catch { return 0; }
        }

        /// <summary>
        /// 获取可为空的dececimal
        /// </summary>
        /// <param name="jToken"></param>
        /// <returns></returns>
        public static decimal? GetDecimalNullabelValue(JToken jToken)
        {
            try
            {
                string jTokenValue = GetStringValue(jToken);

                if (!string.IsNullOrWhiteSpace(jTokenValue))
                    return decimal.Parse(jTokenValue);
                else
                    return null;
            }
            catch { return null; }
        }

        /// <summary>
        /// 获取bool
        /// </summary>
        /// <param name="jToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 获取长整形
        /// </summary>
        /// <param name="jToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 获取枚举值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jToken"></param>
        /// <returns></returns>
        public static T GetEnumValue<T>(JToken jToken) where T : Enum
        {
            try
            {
                if (Enum.IsDefined(typeof(T), jToken.ToObject<int>()))
                    return (T)Enum.ToObject(typeof(T), jToken.ToObject<int>());
                else
                    throw new DealException("不受支持的枚举类型");
            }
            catch
            {
                throw new DealException("不受支持的枚举类型");
            }
        }
    }
}