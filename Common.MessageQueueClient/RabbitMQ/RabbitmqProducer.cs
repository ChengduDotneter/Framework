using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace Common.MessageQueueClient.RabbitMQ
{
    /// <summary>
    /// RabbitMq生产者操作类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RabbitmqProducer<T> : IMQProducer<T> where T : class, IMQData, new()
    {
        private readonly IConnection m_connection;
        private readonly IModel m_channel;
        private readonly IBasicProperties m_properties;
        private readonly RabbitMQConfig m_rabbitMqConfig;
        private readonly AsyncLock m_mutex;
        private const int MAX_RETRY_COUNT = 10;

        /// <summary>
        /// 构造函数
        /// </summary>
        public RabbitmqProducer(RabbitMQConfig rabbitMqConfig)
        {
            m_rabbitMqConfig = rabbitMqConfig;
            m_mutex = new AsyncLock();

            ConnectionFactory connectionFactory = new ConnectionFactory()
            {
                HostName = m_rabbitMqConfig.HostName,
                Port = m_rabbitMqConfig.Port,
                UserName = m_rabbitMqConfig.UserName,
                Password = m_rabbitMqConfig.Password,
                RequestedHeartbeat = m_rabbitMqConfig.RequestedHeartbeat,
                AutomaticRecoveryEnabled = true //自动重连
            };

            m_connection = connectionFactory.CreateConnection();
            m_channel = m_connection.CreateModel();
            m_properties = m_channel.CreateBasicProperties();
            m_properties.Persistent = true;

            m_channel.ExchangeDeclare(m_rabbitMqConfig.QueueName, type: m_rabbitMqConfig.ExChangeType.ToString().ToLower(), durable: true, autoDelete: false, null); //设置交换器类型
            m_channel.QueueDeclare(m_rabbitMqConfig.QueueName, true, false, false, null);
            m_channel.QueueBind(m_rabbitMqConfig.QueueName, m_rabbitMqConfig.QueueName, m_rabbitMqConfig.RoutingKey); // 设置路由关键字即为队列的名称
            m_channel.ConfirmSelect();
        }

        /// <summary>
        /// 同步推送消息
        /// </summary>
        /// <param name="message">需要推送的消息</param>
        public void Produce(T message)
        {
            ProduceAsync(message).Wait();
        }

        /// <summary>
        /// 异步推送消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task ProduceAsync(T message)
        {
            using (await m_mutex.LockAsync())
            {
                try
                {
                    int retryCount = 0;

                    while (true)
                    {
                        m_channel.BasicPublish(exchange: m_rabbitMqConfig.QueueName, routingKey: m_rabbitMqConfig.RoutingKey, mandatory: false, basicProperties: m_properties,
                                               body: Encoding.UTF8.GetBytes(ConvertDataToMessage(message)));

                        // if (m_channel.WaitForConfirms())
                        //     break;

                        retryCount++;

                        if (retryCount >= MAX_RETRY_COUNT)
                            throw new DealException($"RabbitMq推送数据发生错误");
                    }
                }
                catch (Exception ex)
                {
                    throw new DealException($"RabbitMq推送数据发生错误：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            m_channel?.Dispose();
            m_connection?.Dispose();
        }

        /// <summary>
        /// 把对象转换为JSON字符串
        /// </summary>
        /// <param name="message">对象</param>
        /// <returns>JSON字符串</returns>
        private string ConvertDataToMessage(T message)
        {
            try
            {
                if (message == null)
                    return null;

                return JsonConvert.SerializeObject(message);
            }
            catch
            {
                throw new Exception($"序列化失败。");
            }
        }
    }
}