using Common.MessageQueueClient;
using System;

namespace Common.Log
{
    /// <summary>
    /// 日志接口基类
    /// </summary>
    public abstract class LogViewModelBase : IMQData
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        public long ID { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 节点ID
        /// </summary>
        public int Node { get; set; }

        /// <summary>
        /// 节点类型
        /// </summary>
        public int NodeType { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public LogViewModelBase()
        {
            ID = IDGenerator.NextID();
            CreateTime = DateTime.Now;
        }
    }
}
