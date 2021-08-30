using System;

namespace Common
{
    /// <summary>
    /// 业务错误类
    /// </summary>
    public class DealException : Exception
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误消息</param>
        public DealException(string message) : base(message) { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">错误详细信息</param>
        public DealException(string message, Exception innerException) : base(message, innerException) { }
    }
}