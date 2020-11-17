using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.MessageQueueClient.RabbitMQ
{
    /// <summary>
    /// RabbitMq消费者操作类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RabbitmqConsumer<T> : IMQConsumer<T> where T : class, IMQData, new()
    {
        private static IConnectionFactory m_connectionFactory;
        private static IConnection m_connection;
        private static IModel m_channel;
        private static bool m_isGetMessage;
        private string m_queueName;
        private string m_routingKey;
        private ExChangeTypeEnum m_exChangeTypeEnum;

        static RabbitmqConsumer()
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
        /// <param name="queueName">队列名</param>
        /// <param name="routingKey">路由关键字</param>
        /// <param name="exChangeTypeEnum">数据分发模式</param>
        public RabbitmqConsumer(string queueName, string routingKey, ExChangeTypeEnum exChangeTypeEnum)
        {
            if (m_connectionFactory == null)
                m_connectionFactory = RabbitmqHelper.CreateConnectionFactory();

            if (m_connection == null)
                m_connection = m_connectionFactory.CreateConnection();

            if (m_channel == null)
                m_channel = m_connection.CreateModel();

            m_queueName = queueName;
            m_routingKey = routingKey;
            m_exChangeTypeEnum = exChangeTypeEnum;

            AppDomain.CurrentDomain.ProcessExit += (send, e) => { Dispose(); };
        }

        /// <summary>
        /// 消费
        /// </summary>
        /// <param name="mQContext">MQ上下文</param>
        /// <param name="callback">消费回调</param>
        public void Consume(MQContext mQContext, Func<T, bool> callback)
        {
            if (m_isGetMessage)
            {
                //声明为手动确认，每次只消费1条消息。
                m_channel.BasicQos(0, 1, false);
                //定义消费者
                var consumer = new EventingBasicConsumer(m_channel);
                //接收事件
                consumer.Received += (eventSender, args) =>
                {
                    var message = args.Body;//接收到的消息

                    if (callback.Invoke(ConvertMessageToData(Encoding.UTF8.GetString(message))))
                        //返回消息确认
                        m_channel.BasicAck(args.DeliveryTag, true);
                };
                //开启监听
                m_channel.BasicConsume(queue: mQContext.MessageQueueName, autoAck: false, consumer: consumer);
            }
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public void DeSubscribe()
        {
            m_isGetMessage = false;
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            if (m_channel != null)
                m_channel.Dispose();

            if (m_connection != null)
                m_connection.Dispose();
        }

        /// <summary>
        /// 订阅
        /// </summary>
        /// <param name="mQContext"></param>
        public void Subscribe(MQContext mQContext)
        {
            RabbitmqHelper.BindingQueues(
                   mQContext.MessageQueueName,
                   m_exChangeTypeEnum, m_channel,
                   m_routingKey,
                   new List<string>() { m_queueName });

            m_isGetMessage = true;
        }

        /// <summary>
        /// 根据泛型将Message反序列化为相应对象
        /// </summary>
        /// <param name="message">数据</param>
        /// <returns></returns>
        private T ConvertMessageToData(string message)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(message);
            }
            catch
            {
                return default;
            }
        }
    }
}