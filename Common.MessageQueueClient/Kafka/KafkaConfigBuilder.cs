using Confluent.Kafka;

namespace KafcaTestPro
{
    /// <summary>
    /// Kafka配置构建接口
    /// </summary>
    public interface IKafkaConfigBuilder
    {
        /// <summary>
        /// 获取客户端配置
        /// </summary>
        /// <returns></returns>
        ClientConfig GetClientConfig();

        /// <summary>
        /// 获取消费者配置
        /// </summary>
        /// <returns></returns>
        ConsumerConfig GetConsumerConfig(string groupId, bool enableAutoOffsetStore = true);

        /// <summary>
        /// 获取生产者配置
        /// </summary>
        /// <returns></returns>
        ProducerConfig GetProducerConfig();
    }

    /// <summary>
    /// Kafka配置构建类
    /// </summary>
    public class KafkaConfigBuilder : IKafkaConfigBuilder
    {
        /// <summary>
        /// 获取客户端配置
        /// </summary>
        /// <returns></returns>
        public ClientConfig GetClientConfig()
        {
            ClientConfig clientConfig = new ClientConfig
            {
                //Kafka的服务地址，多个用逗号隔开
                BootstrapServers = "192.168.10.211:9092"
            };

            return clientConfig;
        }

        /// <summary>
        /// 获取消费者配置
        /// </summary>
        /// <returns></returns>
        public ConsumerConfig GetConsumerConfig(string groupId, bool enableAutoOffsetStore = true)
        {
            ConsumerConfig consumerConfig = new ConsumerConfig
            {
                //Kafka的服务地址，多个用逗号隔开
                BootstrapServers = "192.168.10.211:9092",
                //消费者组ID
                GroupId = groupId,
                //消费者对Offset消费数据的决策 此为若没有Offset 则从最开始消费数据
                AutoOffsetReset = AutoOffsetReset.Earliest,
                StatisticsIntervalMs = 60000,
                SessionTimeoutMs = 6000,
                //是否自动提交offset
                EnableAutoOffsetStore = enableAutoOffsetStore,
            };

            return consumerConfig;
        }

        /// <summary>
        /// 获取生产者配置
        /// </summary>
        /// <returns></returns>
        public ProducerConfig GetProducerConfig()
        {
            ProducerConfig producerConfig = new ProducerConfig
            {
                //Kafka的服务地址，多个用逗号隔开
                BootstrapServers = "192.168.10.211:9092",
                //是否开启生产者幂等性
                EnableIdempotence = true,
                //生产者提交消息延时时间 单位 毫秒
                LingerMs = 10,
            };

            return producerConfig;
        }
    }
}
