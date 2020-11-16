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
        private static IConnection m_connection;
        private static IModel m_channel;
        private IEnumerable<string> m_queueNames;
        private string m_routingKey;
        private ExChangeTypeEnum m_exChangeTypeEnum;

        /// <summary>
        /// 静态构造函数 新建RabbitMQ服务连接器
        /// </summary>
        static RabbitmqProducer()
        {
            try
            {
                m_connectionFactory = RabbitmqHelper.CreateConnectionFactory();
                m_connection = m_connectionFactory.CreateConnection();
                m_channel = m_connection.CreateModel();
            }
            catch (Exception ex)
            {
                throw new Exception($"RabbitMQ参数配置初始化错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public RabbitmqProducer(IEnumerable<string> queueNames, string routingKey, ExChangeTypeEnum exChangeTypeEnum)
        {
            if (m_connectionFactory == null)
                m_connectionFactory = RabbitmqHelper.CreateConnectionFactory();

            if (m_connection == null)
                m_connection = m_connectionFactory.CreateConnection();

            if (m_channel == null)
                m_channel = m_connection.CreateModel();

            m_queueNames = queueNames;
            m_routingKey = routingKey;
            m_exChangeTypeEnum = exChangeTypeEnum;

            AppDomain.CurrentDomain.ProcessExit += (send, e) => { Dispose(); };
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
                ProduceData(m_channel, mQContext, message);
            });
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (m_channel != null)
                m_channel.Dispose();

            if (m_connection != null)
                m_connection.Dispose();
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
                return message.ToString();
            }
        }

        /// <summary>
        /// 推送消息
        /// </summary>
        /// <param name="m_channel">通道</param>
        /// <param name="mQContext">上下文</param>
        /// <param name="message">推送的消息</param>
        private void ProduceData(IModel m_channel, MQContext mQContext, T message)
        {
            try
            {
                IBasicProperties properties = m_channel.CreateBasicProperties();

                properties.Persistent = true;

                RabbitmqHelper.BindingQueues(mQContext.MessageQueueName, m_exChangeTypeEnum, m_channel, m_routingKey, m_queueNames);

                m_channel.BasicPublish(
                    exchange: mQContext.MessageQueueName,
                    routingKey: m_routingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: Encoding.UTF8.GetBytes(ConvertDataToMessage(message)));
            }
            catch (Exception ex)
            {
                throw new DealException($"RabbitMq推送数据发生错误：{ex.Message}");
            }
        }
    }
}