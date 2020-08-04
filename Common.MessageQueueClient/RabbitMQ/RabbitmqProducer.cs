using RabbitMQ.Client;
using System;
using System.Threading.Tasks;

namespace Common.MessageQueueClient.RabbitMQ
{
    public class RabbitmqProducer<T> : IMQProducer<T> where T : class, IMQData, new()
    {
        private static IConnectionFactory m_connectionFactory;
        private static RabbitMqClientContext m_mqClientContext;
        private static readonly MqConfigModel m_mqConfig;

        static RabbitmqProducer()
        {
            m_mqConfig = MqConfigFactory.CreateConfigDomInstance(); //获取MQ的配置
            m_mqClientContext = new RabbitMqClientContext();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public RabbitmqProducer()
        {

            if (m_mqClientContext.PublishConnection == null)
                m_mqClientContext.PublishConnection = ConnectionFactory.CreateConnection();

            if (m_mqClientContext.PublishChannel == null)
                m_mqClientContext.PublishChannel = m_mqClientContext.PublishConnection.CreateModel();

            return new RabbitmqDaoInstance<T>();
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~RabbitmqProducer()
        {
            m_kafkaProducer.Dispose();
        }

        /// <summary>
        /// 同步推送消息
        /// </summary>
        /// <param name="mQContext">消息队列上下文</param>
        /// <param name="message">需要推送的消息</param>
        public void Produce(MQContext mQContext, T message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 异步推送消息
        /// </summary>
        /// <param name="mQContext"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task ProduceAsync(MQContext mQContext, T message)
        {
            throw new NotImplementedException();
        }
    }
}
