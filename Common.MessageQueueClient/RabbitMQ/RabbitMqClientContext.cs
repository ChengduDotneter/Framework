using RabbitMQ.Client;

namespace Common.MessageQueueClient
{
    /// <summary>
    /// RabbitMqClient上下文。
    /// </summary>
    public class RabbitMqClientContext
    {
        /// <summary>
        /// 用于发送消息的Connection。
        /// </summary>
        public IConnection PublishConnection { get; internal set; }

        /// <summary>
        /// 用于发送消息的Channel。
        /// </summary>
        public IModel PublishChannel { get; internal set; }

        /// <summary>
        /// 用户侦听的Connection。
        /// </summary>
        public IConnection SubscribeConnection { get; internal set; }

        /// <summary>
        /// 用户侦听的Channel。
        /// </summary>
        public IModel SubscribeChannel { get; internal set; }
    }
}