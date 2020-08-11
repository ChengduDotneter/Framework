using System.Collections.Generic;

namespace Common.MessageQueueClient.RabbitMQ
{
    /// <summary>
    /// RabbitMQ 参数类
    /// </summary>
    public class RabbitmqParameters
    {
        /// <summary>
        /// 路由关键字
        /// </summary>
        public string RoutingKey { get; set; }

        /// <summary>
        /// 交还器类型
        /// </summary>
        public ExChangeTypeEnum? ExchangeType { get; set; }

        /// <summary>
        /// 需要绑定的队列名称
        /// </summary>
        public IEnumerable<string> QueueNames { get; set; }
    }
}
