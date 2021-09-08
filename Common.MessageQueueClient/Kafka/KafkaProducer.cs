using Confluent.Kafka;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Common.MessageQueueClient.Kafka
{
    /// <summary>
    /// Kafka生产者操作类
    /// </summary>
    public class KafkaProducer<T> : IMQProducer<T> where T : class, IMQData, new()
    {
        private readonly IProducer<string, string> m_kafkaProducer;

        /// <summary>
        /// 构造函数
        /// </summary>
        public KafkaProducer()
        {
            m_kafkaProducer = new ProducerBuilder<string, string>(KafkaConfigBuilder.GetProducerConfig()).Build();
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            m_kafkaProducer?.Dispose();
        }

        /// <summary>
        /// 同步推送消息
        /// </summary>
        /// <param name="mQContext">消息队列上下文</param>
        /// <param name="message">需要推送的消息</param>
        public void Produce(MQContext mQContext, T message)
        {
            m_kafkaProducer.Produce(mQContext.MessageQueueName, ConvertDataToMessage(message));
        }

        /// <summary>
        /// 异步推送消息
        /// </summary>
        /// <param name="mQContext">消息队列上下文</param>
        /// <param name="message">需要推送的消息</param>
        /// <returns></returns>
        public Task ProduceAsync(MQContext mQContext, T message)
        {
            m_kafkaProducer.ProduceAsync(mQContext.MessageQueueName, ConvertDataToMessage(message));
            return Task.CompletedTask;
        }

        /// <summary>
        /// 根据传入对象转换为Kafka所需传输对象Message
        /// </summary>
        /// <param name="data">需要转换的数据</param>
        /// <returns></returns>
        private Message<string, string> ConvertDataToMessage(T data)
        {
            return new Message<string, string> { Key = IDGenerator.NextID().ToString(), Value = JsonConvert.SerializeObject(data) };
        }
    }
}