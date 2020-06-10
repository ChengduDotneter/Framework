using System;

namespace Common.MessageQueueClient
{
    /// <summary>
    ///
    /// </summary>
    public interface IData
    {
        /// <summary>
        /// 创建时间
        /// </summary>
        DateTime CreateTime { get; }
    }
}