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
        private IConnection m_connection;
        private IModel m_channel;
        private string m_routingKey;
        private ISet<string> m_queueNames;
        private EventingBasicConsumer m_consumer;

        private readonly ExChangeTypeEnum m_exChangeTypeEnum;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="exChangeTypeEnum">数据分发模式</param>
        public RabbitmqConsumer(ExChangeTypeEnum exChangeTypeEnum)
        {
            m_exChangeTypeEnum = exChangeTypeEnum;

            RabbitMqConsumerInit();

            AppDomain.CurrentDomain.ProcessExit += (send, e) => { Dispose(); };
        }

        /// <summary>
        /// 消费
        /// </summary>
        /// <param name="mQContext">MQ上下文</param>
        /// <param name="callback">消费回调</param>
        public void Consume(MQContext mQContext, Func<T, bool> callback)
        {
            if (m_queueNames.Contains(mQContext.MessageQueueName))
            {
                //接收事件
                m_consumer.Received += (eventSender, args) =>
                {
                    byte[] message = args.Body;//接收到的消息
                    T data = null;

                    try
                    {
                        data = ConvertMessageToData(Encoding.UTF8.GetString(message));
                    }
                    catch
                    {
                        //数据convert失败时，直接删除该数据
                        m_channel.BasicReject(args.DeliveryTag, false);
                        throw;
                    }

                    try
                    {
                        if (callback.Invoke(data))
                            //返回消息确认
                            m_channel.BasicAck(args.DeliveryTag, true);
                        else
                            m_channel.BasicReject(args.DeliveryTag, true);
                    }
                    catch
                    {
                        //处理逻辑失败时，该消息扔回消息队列
                        m_channel.BasicReject(args.DeliveryTag, true);
                        throw;
                    }
                };

                //开启监听
                m_channel.BasicConsume(queue: mQContext.MessageQueueName, autoAck: false, consumer: m_consumer);
            }
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public void DeSubscribe()
        {
            Dispose();
        }

        private void RabbitMqConsumerInit()
        {
            m_queueNames = new HashSet<string>();

            if (m_connectionFactory == null)
                m_connectionFactory = RabbitmqHelper.CreateConnectionFactory();

            if (m_connection == null)
                m_connection = m_connectionFactory.CreateConnection();

            if (m_channel == null)
                m_channel = m_connection.CreateModel();

            //申明是否手动确认
            m_channel.BasicQos(0, 1, false);

            m_consumer = new EventingBasicConsumer(m_channel);
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            m_channel?.Dispose();
            m_connection?.Dispose();
        }

        /// <summary>
        /// 订阅
        /// </summary>
        /// <param name="mQContext"></param>
        public void Subscribe(MQContext mQContext)
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

                    RabbitmqHelper.BindingQueues(mQContext.MessageQueueName, m_exChangeTypeEnum, m_channel, m_routingKey, m_queueNames);
                }
            }
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
                string errorMessage = $"RabbitMQConsum序列化失败：{message}。";

                Log4netCreater.CreateLog("RabbitmqConsumer").Error(errorMessage);
                throw new Exception(errorMessage);
            }
        }
    }
}