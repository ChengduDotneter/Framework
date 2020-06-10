using System;

namespace Common.MessageQueueClient
{
    /// <summary>
    /// 对接推送/接收消息实体
    /// </summary>
    public class MessageBody : IData
    {
        /// <summary>
        /// 当前推送/接收的数据类型（表名）
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        /// 当前推送/接收的数据
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 公司名称
        /// </summary>
        public string Company { get; set; }

        /// <summary>
        /// 当前消息创建的时间
        /// </summary>
        public DateTime CreateTime { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public MessageBody()
        {
            CreateTime = DateTime.Now;
        }
    }
}