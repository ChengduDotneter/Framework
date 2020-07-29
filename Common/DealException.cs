﻿using System;

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
        /// <param name="message"></param>
        public DealException(string message) : base(message) { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public DealException(string message, Exception innerException) : base(message, innerException) { }
    }
}