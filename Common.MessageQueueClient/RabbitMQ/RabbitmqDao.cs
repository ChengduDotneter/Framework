using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Common.MessageQueueClient
{
    internal static class RabbitmqDao
    {
        private static IConnectionFactory m_connectionFactory;
        private static RabbitMqClientContext m_mqClientContext;
        private static readonly MqConfigModel m_mqConfig;

        private static IConnectionFactory ConnectionFactory
        {
            get
            {
                if (m_connectionFactory != null)
                    return m_connectionFactory;

                m_connectionFactory = new ConnectionFactory()
                {
                    HostName = m_mqConfig.MqHost,
                    UserName = m_mqConfig.MqUserName,
                    Password = m_mqConfig.MqPassword,
                    RequestedHeartbeat = m_mqConfig.RequestedHeartbeat, //心跳超时时间
                    AutomaticRecoveryEnabled = true //自动重连
                };

                return m_connectionFactory;
            }
        }

        static RabbitmqDao()
        {
            m_mqConfig = MqConfigFactory.CreateConfigDomInstance(); //获取MQ的配置
            m_mqClientContext = new RabbitMqClientContext();
        }

        private class RabbitmqDaoInstance<T> : IMQProducer<T>, IMQConsumer<T> where T : class, IMQData, new()
        {
            public void Dispose()
            {
                if (m_mqClientContext.PublishChannel != null)
                {
                    if (m_mqClientContext.PublishChannel.IsOpen)
                        m_mqClientContext.PublishChannel.Close();

                    m_mqClientContext.PublishChannel.Abort();
                    m_mqClientContext.PublishChannel.Dispose();
                }

                if (m_mqClientContext.PublishConnection != null)
                {
                    if (m_mqClientContext.PublishConnection.IsOpen)
                        m_mqClientContext.PublishConnection.Close();
                }

                if (m_mqClientContext.SubscribeChannel != null)
                {
                    if (m_mqClientContext.SubscribeChannel.IsOpen)
                        m_mqClientContext.SubscribeChannel.Close();

                    m_mqClientContext.SubscribeChannel.Abort();
                    m_mqClientContext.SubscribeChannel.Dispose();
                }

                if (m_mqClientContext.SubscribeConnection != null)
                {
                    if (m_mqClientContext.SubscribeConnection.IsOpen)
                        m_mqClientContext.SubscribeConnection.Close();
                }
            }

            public void Publish(T message, string exchangeName, string routingKey = "")
            {
                var properties = m_mqClientContext.PublishChannel.CreateBasicProperties();
                properties.Persistent = true;

                m_mqClientContext.PublishChannel.BasicPublish
                (
                    exchange: exchangeName,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message))
                );
            }

            public void Publish(string message, string exchangeName, string routingKey = "")
            {
                var properties = m_mqClientContext.PublishChannel.CreateBasicProperties();
                properties.Persistent = true;

                m_mqClientContext.PublishChannel.BasicPublish
                (
                    exchange: exchangeName,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: Encoding.UTF8.GetBytes(message)
                );
            }

            public void BindingPublishQueues(string exchangeName, ExChangeTypeEnum exchangeType, params string[] queuesName)
            {
                IDictionary<string, object> argument = new Dictionary<string, object>
                {
                    { "x-expires", 6000 }  //防止生成多余的缓存队列
                };

                m_mqClientContext.PublishChannel.ExchangeDeclare(exchange: exchangeName, type: exchangeType.ToString(), durable: true, autoDelete: false, argument);

                foreach (var queueName in queuesName)
                {
                    m_mqClientContext.PublishChannel.QueueDeclare(queueName, true, false, false, null);
                    m_mqClientContext.PublishChannel.QueueBind(queueName, exchangeName, queueName); // 设置路由关键字即为队列的名称
                }
            }

            public void BindingSubscribeQueue(string exchangeName, ExChangeTypeEnum exchangeType, string routingKey, string listenqueue)
            {
                IDictionary<string, object> argument = new Dictionary<string, object>
                {
                    { "x-expires", 6000 }
                };

                m_mqClientContext.PublishChannel.ExchangeDeclare(exchange: exchangeName, type: exchangeType.ToString(), durable: true, autoDelete: false, argument);
                m_mqClientContext.PublishChannel.QueueDeclare(listenqueue, true, false, false, null);
                m_mqClientContext.PublishChannel.QueueBind(listenqueue, exchangeName, routingKey);
            }

            public Task ProduceAsync(T message)
            {
                throw new NotImplementedException();
            }

            public void Consume(string exchangeName, Action<string> callback)
            {
                //声明为手动确认，每次只消费1条消息。
                m_mqClientContext.SubscribeChannel.BasicQos(0, 1, false);
                //定义消费者
                var consumer = new EventingBasicConsumer(m_mqClientContext.SubscribeChannel);
                //接收事件
                consumer.Received += (eventSender, args) =>
                {
                    var message = args.Body;//接收到的消息

                    callback(Encoding.UTF8.GetString(message));
                    //返回消息确认
                    m_mqClientContext.SubscribeChannel.BasicAck(args.DeliveryTag, true);
                };
                //开启监听
                m_mqClientContext.SubscribeChannel.BasicConsume(queue: exchangeName, autoAck: false, consumer: consumer);
            }

            public Task<T> Consume(string channelName)
            {
                throw new NotImplementedException();
            }
        }

        internal static IMQProducer<T> DeclarePublisherContext<T>() where T : class, IMQData, new()
        {
            if (m_mqClientContext.PublishConnection == null)
                m_mqClientContext.PublishConnection = ConnectionFactory.CreateConnection();

            if (m_mqClientContext.PublishChannel == null)
                m_mqClientContext.PublishChannel = m_mqClientContext.PublishConnection.CreateModel();

            return new RabbitmqDaoInstance<T>();
        }

        internal static IMQConsumer<T> DeclareSubscriberContext<T>() where T : class, IMQData, new()
        {
            if (m_mqClientContext.SubscribeConnection == null)
                m_mqClientContext.SubscribeConnection = ConnectionFactory.CreateConnection();

            if (m_mqClientContext.SubscribeChannel == null)
                m_mqClientContext.SubscribeChannel = m_mqClientContext.SubscribeConnection.CreateModel();

            return new RabbitmqDaoInstance<T>();
        }
    }
}