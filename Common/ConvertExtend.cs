using System;

namespace Common
{
    /// <summary>
    /// 转换扩展类
    /// </summary>
    public class ConvertExtend
    {
        /// <summary>
        /// object转换为字节数组
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] ObjectToByteArray(object data)
        {
            return (byte[])data;
        }

        /// <summary>
        /// 字符串转Bool值
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool StringToBoolean(string data)
        {
            return Convert.ToBoolean(Convert.ToInt32(data));
        }
    }
}