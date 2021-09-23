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
        private readonly KafkaConfig m_kafkaConfig;
        private readonly IProducer<string, string> m_kafkaProducer;

        /// <summary>
        /// 构造函数
        /// </summary>
        public KafkaProducer(KafkaConfig kafkaConfig)
        {
            m_kafkaConfig = kafkaConfig;

            ProducerConfig producerConfig = new ProducerConfig
            {
                //Kafka的服务地址，多个用逗号隔开
                BootstrapServers = m_kafkaConfig.HostName,
                //是否开启生产者幂等性
                EnableIdempotence = true,
                //生产者提交消息延时时间 单位 毫秒
                LingerMs = 10
            };

            m_kafkaProducer = new ProducerBuilder<string, string>(producerConfig).Build();
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
        /// <param name="message">需要推送的消息</param>
        public void Produce(T message)
        {
            m_kafkaProducer.Produce(m_kafkaConfig.QueueName, ConvertDataToMessage(message));
        }

        /// <summary>
        /// 异步推送消息
        /// </summary>
        /// <param name="message">需要推送的消息</param>
        /// <returns></returns>
        public Task ProduceAsync(T message)
        {
            m_kafkaProducer.ProduceAsync(m_kafkaConfig.QueueName, ConvertDataToMessage(message));
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