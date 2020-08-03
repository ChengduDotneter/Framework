using Confluent.Kafka;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace KafcaTestPro
{
    /// <summary>
    /// Kafka消费者操作接口
    /// </summary>
    public interface IKafkaConsumer
    {

        /// <summary>
        /// 初始化Kafka消费者
        /// </summary>
        /// <param name="groupId">消费者组ID</param>
        /// <param name="enableAutoOffsetStore">是否自动推送Offset</param>
        void InitKafkaConsumer(string groupId, bool enableAutoOffsetStore = true);

        /// <summary>
        /// 订阅
        /// </summary>
        /// <param name="topic">主题名称</param>
        void Subscribe(string topic);

        /// <summary>
        /// 取消订阅
        /// </summary>
        void DeSubscribe();

        /// <summary>
        /// 订阅分区
        /// </summary>
        /// <param name="topic">主题名称</param>
        /// <param name="partitionId">分区id</param>
        void Assign(string topic, int partitionId);

        /// <summary>
        /// 取消订阅分区
        /// </summary>
        void Unassign();

        /// <summary>
        /// 消费
        /// </summary>
        /// <typeparam name="T">需要消费的数据类型</typeparam>
        /// <param name="function">所需消费数据的逻辑处理方法</param>
        void Consume<T>(Func<T, bool> function);
    }

    /// <summary>
    /// Kafka消费者操作类
    /// </summary>
    public class KafkaConsumer : IKafkaConsumer
    {
        private readonly IKafkaConfigBuilder m_kafkaConfigBuilder;
        private readonly IKafkaAdminClient m_kafkaAdminClient;

        private IConsumer<string, string> m_consumer;
        private bool m_enableAutoOffsetStore;
        public KafkaConsumer(IKafkaConfigBuilder kafkaConfigBuilder, IKafkaAdminClient kafkaAdminClient)
        {
            m_kafkaAdminClient = kafkaAdminClient;
            m_kafkaConfigBuilder = kafkaConfigBuilder;
        }

        ~KafkaConsumer()
        {
            m_consumer?.Dispose();
        }

        /// <summary>
        /// 初始化Kafka消费者
        /// </summary>
        /// <param name="groupId">消费者组ID</param>
        /// <param name="enableAutoOffsetStore">是否自动推送Offset</param>
        public void InitKafkaConsumer(string groupId, bool enableAutoOffsetStore = true)
        {
            if (m_consumer == null)
            {
                m_enableAutoOffsetStore = enableAutoOffsetStore;
                m_consumer = new ConsumerBuilder<string, string>(m_kafkaConfigBuilder.GetConsumerConfig(groupId, m_enableAutoOffsetStore)).Build();
            }
        }

        /// <summary>
        /// 订阅
        /// </summary>
        /// <param name="topic">主题名称</param>
        public void Subscribe(string topic)
        {
            CheckConosumer();

            if (!m_kafkaAdminClient.IsTopicExisted(topic, out _))
                throw new Exception("不存在Topic主题");

            m_consumer.Subscribe(topic);
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public void DeSubscribe()
        {
            CheckConosumer();

            m_consumer.Unsubscribe();
        }

        /// <summary>
        /// 订阅分区
        /// </summary>
        /// <param name="topic">主题名称</param>
        /// <param name="partitionId">分区id</param>
        public void Assign(string topic, int partitionId)
        {
            CheckConosumer();

            if (!m_kafkaAdminClient.IsTopicExisted(topic, out Metadata metadata))
                throw new Exception("不存在Topic主题");

            if (metadata.Topics.Count(item => item.Topic == topic && item.Partitions.Count(par => par.PartitionId == partitionId) > 0) <= 0)
                throw new Exception("不存在分区ID");

            m_consumer.Assign(new TopicPartition(topic, new Partition(partitionId)));
        }

        /// <summary>
        /// 取消订阅分区
        /// </summary>
        public void Unassign()
        {
            CheckConosumer();

            m_consumer.Unassign();
        }

        /// <summary>
        /// 消费
        /// </summary>
        /// <typeparam name="T">需要消费的数据类型</typeparam>
        /// <param name="function">所需消费数据的逻辑处理方法</param>
        public void Consume<T>(Func<T, bool> function)
        {
            CheckConosumer();

            try
            {
                ConsumeResult<string, string> consumeResult = m_consumer.Consume();

                if (function?.Invoke(ConvertMessageToData<T>(consumeResult.Message)) ?? false && !m_enableAutoOffsetStore)
                    m_consumer.Commit(new[] { consumeResult.TopicPartitionOffset });
            }
            catch (Exception ex)
            {
                throw new Exception($"数据消费失败：{ex.Message}");
            }
        }

        private void CheckConosumer()
        {
            if (m_consumer == null)
                throw new Exception("未初始化消费者。");
        }

        /// <summary>
        /// 根据泛型将Message反序列化为相应对象
        /// </summary>
        /// <typeparam name="T">需要反序列化的类型</typeparam>
        /// <param name="message">数据类型</param>
        /// <returns></returns>
        private T ConvertMessageToData<T>(Message<string, string> message)
        {
            return JsonConvert.DeserializeObject<T>(message.Value);
        }
    }
}
