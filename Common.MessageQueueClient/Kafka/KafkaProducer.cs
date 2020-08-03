using Confluent.Kafka;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace KafcaTestPro
{
    /// <summary>
    /// Kafka生产者操作接口
    /// </summary>
    public interface IKafkaProducer
    {
        /// <summary>
        /// 同步推动消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="topic">主题名称</param>
        /// <param name="data">所需推送的数据</param>
        void Produce<T>(string topic, T data);

        /// <summary>
        /// 异步推送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="topic">主题名称</param>
        /// <param name="data">所需推送的数据</param>
        /// <returns></returns>
        Task ProduceAsync<T>(string topic, T data);
    }

    /// <summary>
    /// Kafka生产者操作类
    /// </summary>
    public class KafkaProducer : IKafkaProducer
    {
        private readonly IProducer<string, string> m_kafkaProducer;

        public KafkaProducer(IKafkaConfigBuilder kafkaConfigBuilder)
        {
            m_kafkaProducer = new ProducerBuilder<string, string>(kafkaConfigBuilder.GetProducerConfig()).Build();
        }

        ~KafkaProducer()
        {
            m_kafkaProducer.Dispose();
        }

        /// <summary>
        /// 同步推送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="topic">主题名称</param>
        /// <param name="data">所需推送的数据</param>
        /// <returns></returns>
        public void Produce<T>(string topic, T data)
        {
            m_kafkaProducer.Produce(topic, ConvertDataToMessage(data));
        }

        /// <summary>
        /// 异步推送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="topic">主题名称</param>
        /// <param name="data">所需推送的数据</param>
        /// <returns></returns>
        public async Task ProduceAsync<T>(string topic, T data)
        {
            await m_kafkaProducer.ProduceAsync(topic, ConvertDataToMessage(data));
        }

        /// <summary>
        /// 根据传入对象转换为Kafka所需传输对象Message
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        private Message<string, string> ConvertDataToMessage<T>(T data)
        {
            return new Message<string, string> { Key = Guid.NewGuid().ToString(), Value = JsonConvert.SerializeObject(data) };
        }
    }
}
