using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace Common.MessageQueueClient.RabbitMQ
{
    public class RabbitmqConsumer<T> : IMQConsumer<T> where T : class, IMQData, new()
    {
        private static IConnectionFactory m_connectionFactory;
        private static IConnection m_connection;
        private static IModel m_channel;
        private static bool m_isGetMessage;

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

        public RabbitmqConsumer()
        {
            if (m_connectionFactory == null)
                m_connectionFactory = RabbitmqHelper.CreateConnectionFactory();

            if (m_connection == null)
                m_connection = m_connectionFactory.CreateConnection();

            if (m_channel == null)
                m_channel = m_connection.CreateModel();

            AppDomain.CurrentDomain.ProcessExit += (send, e) => { Dispose(); };
        }

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

                    callback(ConvertMessageToData(Encoding.UTF8.GetString(message)));
                    //返回消息确认
                    m_channel.BasicAck(args.DeliveryTag, true);
                };
                //开启监听
                m_channel.BasicConsume(queue: mQContext.MessageQueueName, autoAck: false, consumer: consumer);
            }
        }

        public void DeSubscribe()
        {
            m_isGetMessage = false;
        }

        public void Dispose()
        {
            if (m_channel != null)
                m_channel.Dispose();

            if (m_connection != null)
                m_connection.Dispose();
        }

        public void Subscribe(MQContext mQContext)
        {
            RabbitmqHelper.BindingQueues(
                   mQContext.MessageQueueName,
                   ((RabbitmqParameters)mQContext.Context).ExchangeType.Value, m_channel,
                   ((RabbitmqParameters)mQContext.Context).RoutingKey,
                   ((RabbitmqParameters)mQContext.Context).QueueNames);

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
