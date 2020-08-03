using System;

namespace Common.MessageQueueClient
{
    /// <summary>
    /// RBMQ数据结构接口
    /// </summary>
    public interface IMQData
    {
        /// <summary>
        /// 创建时间
        /// </summary>
        DateTime CreateTime { get; }
    }
}