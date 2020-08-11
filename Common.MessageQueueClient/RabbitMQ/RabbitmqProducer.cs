using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Common.MessageQueueClient.RabbitMQ
{
    public class RabbitmqProducer<T> : IMQProducer<T> where T : class, IMQData, new()
    {
        private static IConnectionFactory m_connectionFactory;
        private static IConnection m_connection;
        private static IModel m_channel;

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
        public RabbitmqProducer()
        {
            if (m_connectionFactory == null)
                m_connectionFactory = RabbitmqHelper.CreateConnectionFactory();

            if (m_connection == null)
                m_connection = m_connectionFactory.CreateConnection();

            if (m_channel == null)
                m_channel = m_connection.CreateModel();

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
        public async Task ProduceAsync(MQContext mQContext, T message)
        {
            await Task.Factory.StartNew(() =>
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
            if (message == null)
                return null;

            return JsonConvert.SerializeObject(message);
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

                RabbitmqHelper.BindingQueues(
                    mQContext.MessageQueueName,
                    ((RabbitmqParameters)mQContext.Context).ExchangeType.Value, m_channel,
                    ((RabbitmqParameters)mQContext.Context).RoutingKey,
                    ((RabbitmqParameters)mQContext.Context).QueueNames);

                m_channel.BasicPublish(
                    exchange: mQContext.MessageQueueName,
                    routingKey: ((RabbitmqParameters)mQContext.Context).RoutingKey,
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
