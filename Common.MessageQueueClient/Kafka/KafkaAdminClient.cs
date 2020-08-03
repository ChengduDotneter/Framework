using Confluent.Kafka;
using Confluent.Kafka.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KafcaTestPro
{
    /// <summary>
    /// Kafka管理操作接口
    /// </summary>
    public interface IKafkaAdminClient
    {
        /// <summary>
        /// 同步创建分区
        /// </summary>
        /// <param name="topic">主题名称</param>
        /// <param name="createCount">分区创建至指定数量</param>
        /// <returns></returns>
        void CreatePartitions(string topic, int createCount);

        /// <summary>
        /// 异步创建分区
        /// </summary>
        /// <param name="topic">主题名称</param>
        /// <param name="createCount">分区创建至指定数量</param>
        /// <returns></returns>
        Task CreatePartitionsAsync(string topic, int createCount);

        /// <summary>
        /// 同步创建新的Topic主题
        /// </summary>
        /// <param name="name">Topic主题名</param>
        /// <param name="numPartitions">新主题的分区数</param>
        /// <param name="replicationFactor">新主题的分区复制数</param>
        /// <returns></returns>
        void CreateTopic(string name, int numPartitions = -1, short replicationFactor = -1);

        /// <summary>
        /// 异步创建新的Topic主题
        /// </summary>
        /// <param name="name">Topic主题名</param>
        /// <param name="numPartitions">新主题的分区数</param>
        /// <param name="replicationFactor">新主题的分区复制数</param>
        /// <returns></returns>
        Task CreateTopicAsync(string name, int numPartitions = -1, short replicationFactor = -1);

        /// <summary>
        /// 根据Topic名称判断该topic是否存在
        /// </summary>
        /// <param name="topic">topic名称</param>
        /// <param name="metaData">相关元数据信息</param>
        /// <returns></returns>
        bool IsTopicExisted(string topic, out Metadata metaData);

        /// <summary>
        /// 根据Topic名称获取该Topic下的分区数
        /// </summary>
        /// <param name="topic">主题名称</param>
        /// <returns></returns>
        int GetTopicPartitionCount(string topic);
    }

    /// <summary>
    /// Kafka管理操作类
    /// </summary>
    public class KafkaAdminClient : IKafkaAdminClient
    {
        private readonly IAdminClient m_adminClient;
        private readonly IKafkaConfigBuilder m_kafkaConfigBuilder;

        public KafkaAdminClient(IKafkaConfigBuilder kafkaConfigBuilder)
        {
            if (m_kafkaConfigBuilder == null)
                m_kafkaConfigBuilder = kafkaConfigBuilder;

            if (m_adminClient == null)
                m_adminClient = new AdminClientBuilder(m_kafkaConfigBuilder.GetClientConfig()).Build();

            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                m_adminClient.Dispose();
            };
        }

        /// <summary>
        /// 同步创建分区
        /// </summary>
        /// <param name="topic">主题名称</param>
        /// <param name="createCount">分区增加数量</param>
        /// <returns></returns>
        public void CreatePartitions(string topic, int createCount)
        {
            CreatePartitionsAsync(topic, createCount).Wait();
        }

        /// <summary>
        /// 异步创建分区
        /// </summary>
        /// <param name="topic">主题名称</param>
        /// <param name="createCount">分区增加数量</param>
        /// <returns></returns>
        public async Task CreatePartitionsAsync(string topic, int createCount)
        {
            try
            {
                await m_adminClient.CreatePartitionsAsync(new PartitionsSpecification[] {
                        new PartitionsSpecification { Topic = topic, IncreaseTo = GetTopicPartitionCount(topic) + createCount } });
            }
            catch (Exception ex)
            {
                throw new Exception($"Kafka创建Partitions失败: {ex.Message}");
            }

        }

        /// <summary>
        /// 同步创建新的Topic主题
        /// </summary>
        /// <param name="name">Topic主题名</param>
        /// <param name="numPartitions">新主题的分区数</param>
        /// <param name="replicationFactor">新主题的分区复制数</param>
        /// <returns></returns>
        public void CreateTopic(string name, int numPartitions = -1, short replicationFactor = -1)
        {
            CreateTopicAsync(name, numPartitions, replicationFactor).Wait();
        }

        /// <summary>
        /// 异步创建新的Topic主题
        /// </summary>
        /// <param name="name">Topic主题名</param>
        /// <param name="numPartitions">新主题的分区数</param>
        /// <param name="replicationFactor">新主题的分区复制数</param>
        /// <returns></returns>
        public async Task CreateTopicAsync(string name, int numPartitions = -1, short replicationFactor = -1)
        {
            try
            {
                if (IsTopicExisted(name, out _))
                    throw new Exception("已存在该Topic主题");

                await m_adminClient.CreateTopicsAsync(new TopicSpecification[] {
                        new TopicSpecification { Name = name, NumPartitions = numPartitions, ReplicationFactor = replicationFactor } });

            }
            catch (Exception ex)
            {
                throw new Exception($"Kafka创建Topic失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据Topic名称判断该topic是否存在
        /// </summary>
        /// <param name="topic">topic名称</param>
        /// <param name="metaData">相关元数据信息</param>
        /// <returns></returns>
        public bool IsTopicExisted(string topic, out Metadata metaData)
        {
            metaData = GetMetaDataByTopic(topic);

            return metaData.Topics != null && metaData.Topics.Count() > 0;
        }

        /// <summary>
        /// 根据Topic名称获取该Topic下的分区数
        /// </summary>
        /// <param name="topic">主题名称</param>
        /// <returns></returns>
        public int GetTopicPartitionCount(string topic)
        {
            if (!IsTopicExisted(topic, out Metadata metaData))
                throw new Exception("未找到该Topic");

            return metaData.Topics.Sum(item => item.Partitions.Count);
        }

        /// <summary>
        /// 根据Topic名称获取Kafka相关元数据
        /// </summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        private Metadata GetMetaDataByTopic(string topic)
        {
            return m_adminClient.GetMetadata(topic, TimeSpan.FromSeconds(10));
        }
    }
}
