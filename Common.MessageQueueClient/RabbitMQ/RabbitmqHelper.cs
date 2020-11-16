using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace Common.MessageQueueClient.RabbitMQ
{
    /// <summary>
    /// RabbitMq相关操作类
    /// </summary>
    public static class RabbitmqHelper
    {
        /// <summary>
        /// 设置交换器类型和绑定消息队列
        /// </summary>
        /// <param name="exchangeName">交换器名称</param>
        /// <param name="exchangeType">交换器类型</param>
        /// <param name="channel">通道名称</param>
        /// <param name="routingKey">路由关键字</param>
        /// <param name="queuesName">队列名称</param>
        public static void BindingQueues(string exchangeName, ExChangeTypeEnum exchangeType, IModel channel, string routingKey, IEnumerable<string> queuesName)
        {
            channel.ExchangeDeclare(exchange: exchangeName, type: exchangeType.ToString(), durable: true, autoDelete: false, null);//设置交换器类型

            foreach (var queueName in queuesName)
            {
                channel.QueueDeclare(queueName, true, false, false, null);
                channel.QueueBind(queueName, exchangeName, routingKey); // 设置路由关键字即为队列的名称
            }
        }

        /// <summary>
        /// 创建连接工厂
        /// </summary>
        /// <returns></returns>
        public static ConnectionFactory CreateConnectionFactory()
        {
            return new ConnectionFactory()
            {
                HostName = ConfigManager.Configuration["RabbitServer:Host"],
                UserName = ConfigManager.Configuration["RabbitServer:UserName"],
                Password = ConfigManager.Configuration["RabbitServer:Password"],
                RequestedHeartbeat = (ushort)Convert.ToInt32(ConfigManager.Configuration["RabbitServer:RequestedHeartbeat"]), //心跳超时时间
                AutomaticRecoveryEnabled = true //自动重连
            };
        }
    }
}