using Confluent.Kafka;
using System;
using System.Text.Json;
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

            AppDomain.CurrentDomain.ProcessExit += (send, e) => { Dispose(); };
        }

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
        /// <param name="mQContext"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task ProduceAsync(MQContext mQContext, T message)
        {
            await m_kafkaProducer.ProduceAsync(mQContext.MessageQueueName, ConvertDataToMessage(message));
        }

        /// <summary>
        /// 根据传入对象转换为Kafka所需传输对象Message
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        private Message<string, string> ConvertDataToMessage(T data)
        {
            return new Message<string, string> { Key = IDGenerator.NextID().ToString(), Value = JsonSerializer.Serialize(data) };
        }
    }
}
