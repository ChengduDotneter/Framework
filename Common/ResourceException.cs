using System;

namespace Common
{
    /// <summary>
    /// 资源错误
    /// </summary>
    public class ResourceException : Exception
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message"></param>
        public ResourceException(string message) : base(message) { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ResourceException(string message, Exception innerException) : base(message, innerException) { }
    }
}
