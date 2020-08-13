namespace Common.MessageQueueClient
{
    /// <summary>
    /// MQ消息队列上下文
    /// </summary>
    public class MQContext
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public MQContext(string messageQueueName, object context)
        {
            MessageQueueName = messageQueueName;
            Context = context;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public MQContext(string messageQueueName)
        {
            MessageQueueName = messageQueueName;
        }

        /// <summary>
        /// 管道名（队列名）
        /// </summary>
        public string MessageQueueName { get; set; }

        /// <summary>
        /// 通讯内容
        /// </summary>
        public object Context { get; internal set; }
    }
}
