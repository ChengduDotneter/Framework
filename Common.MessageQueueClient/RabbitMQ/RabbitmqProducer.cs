using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Common.MessageQueueClient.RabbitMQ
{
    /// <summary>
    /// RabbitMq生产者操作类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RabbitmqProducer<T> : IMQProducer<T> where T : class, IMQData, new()
    {
        private static IConnectionFactory m_connectionFactory;
        private IConnection m_connection;
        private IModel m_channel;
        private IBasicProperties m_properties;
        private ISet<string> m_queueNames;
        private string m_routingKey;

        private readonly ExChangeTypeEnum m_exChangeTypeEnum;

        private void RabbitMqProducerInit()
        {
            m_queueNames = new HashSet<string>();

            if (m_connectionFactory == null)
                //1:创建一个连接的工厂类 
                m_connectionFactory = RabbitmqHelper.CreateConnectionFactory();

            if (m_connection == null)
                //创建连接
                m_connection = m_connectionFactory.CreateConnection();

            if (m_channel == null)
                //创建通道
                m_channel = m_connection.CreateModel();

            if (m_properties == null)
            {
                m_properties = m_channel.CreateBasicProperties();
                m_properties.Persistent = true;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public RabbitmqProducer(ExChangeTypeEnum exChangeTypeEnum)
        {
            m_exChangeTypeEnum = exChangeTypeEnum;
            RabbitMqProducerInit();
        }

        /// <summary>
        /// 同步推送消息
        /// </summary>
        /// <param name="mQContext">消息队列上下文</param>
        /// <param name="message">需要推送的消息</param>
        public void Produce(MQContext mQContext, T message)
        {
            ProduceData(m_channel, mQContext, message);
        }

        /// <summary>
        /// 异步推送消息
        /// </summary>
        /// <param name="mQContext"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task ProduceAsync(MQContext mQContext, T message)
        {
            return Task.Factory.StartNew(() =>
            {
                Produce(mQContext, message);
            });
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

        /// <summary>
        /// 推送消息
        /// </summary>
        /// <param name="channel">通道</param>
        /// <param name="mQContext">上下文</param>
        /// <param name="message">推送的消息</param>
        private void ProduceData(IModel channel, MQContext mQContext, T message)
        {
            try
            {
                if (mQContext.Context is RabbitMqContent rabbitMqContent)
                {
                    if (string.IsNullOrWhiteSpace(m_routingKey) && !string.IsNullOrWhiteSpace(rabbitMqContent.RoutingKey))
                        m_routingKey = rabbitMqContent.RoutingKey;

                    if (string.IsNullOrWhiteSpace(m_routingKey))
                        throw new Exception("路由关键字出错。");

                    if (!m_queueNames.Contains(mQContext.MessageQueueName))
                    {
                        m_queueNames.Add(mQContext.MessageQueueName);

                        RabbitmqHelper.BindingQueues(mQContext.MessageQueueName, m_exChangeTypeEnum, channel, m_routingKey, m_queueNames);
                    }

                    channel.BasicPublish(exchange: mQContext.MessageQueueName, routingKey: m_routingKey, mandatory: false, basicProperties: m_properties, body: Encoding.UTF8.GetBytes(ConvertDataToMessage(message)));
                }
            }
            catch (Exception ex)
            {
                throw new DealException($"RabbitMq推送数据发生错误：{ex.Message}");
            }
        }
    }
}