using Confluent.Kafka;
using Confluent.Kafka.Admin;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Common.MessageQueueClient.Kafka
{
    /// <summary>
    /// Kafka管理操作类
    /// </summary>
    public static class KafkaAdminClient
    {
        /// <summary>
        /// 同步创建分区
        /// </summary>
        /// <param name="topic">主题名称</param>
        /// <param name="createCount">分区增加数量</param>
        /// <returns></returns>
        public static void CreatePartitions(string topic, int createCount)
        {
            CreatePartitionsAsync(topic, createCount).Wait();
        }

        /// <summary>
        /// 异步创建分区
        /// </summary>
        /// <param name="topic">主题名称</param>
        /// <param name="createCount">分区增加数量</param>
        /// <returns></returns>
        public static async Task CreatePartitionsAsync(string topic, int createCount)
        {
            try
            {
                ClientConfig clientConfig = new ClientConfig
                {
                    //Kafka的服务地址，多个用逗号隔开
                    BootstrapServers = ConfigManager.Configuration["KafkaService:Host"]
                };

                using IAdminClient adminClient = new AdminClientBuilder(clientConfig).Build(); //根据配置创建客户端

                await adminClient.CreatePartitionsAsync(new PartitionsSpecification[]
                {
                    new PartitionsSpecification { Topic = topic, IncreaseTo = GetTopicPartitionCount(adminClient, topic) + createCount }
                });
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
        public static void CreateTopic(string name, int numPartitions = -1, short replicationFactor = -1)
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
        public static async Task CreateTopicAsync(string name, int numPartitions = -1, short replicationFactor = -1)
        {
            try
            {
                ClientConfig clientConfig = new ClientConfig
                {
                    //Kafka的服务地址，多个用逗号隔开
                    BootstrapServers = ConfigManager.Configuration["KafkaService:Host"]
                };

                using IAdminClient adminClient = new AdminClientBuilder(clientConfig).Build(); //根据配置创建客户端

                if (IsTopicExisted(adminClient, name, out _))
                    throw new Exception("已存在该Topic主题");

                await adminClient.CreateTopicsAsync(new TopicSpecification[]
                {
                    new TopicSpecification { Name = name, NumPartitions = numPartitions, ReplicationFactor = replicationFactor }
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Kafka创建Topic失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据Topic名称判断该topic是否存在
        /// </summary>
        /// <param name="adminClient"></param>
        /// <param name="topic">topic名称</param>
        /// <param name="metaData">相关元数据信息</param>
        /// <returns></returns>
        public static bool IsTopicExisted(IAdminClient adminClient, string topic, out Metadata metaData)
        {
            metaData = GetMetaDataByTopic(adminClient, topic);
            return metaData.Topics != null && metaData.Topics.Count() > 0;
        }

        /// <summary>
        /// 根据Topic名称获取该Topic下的分区数
        /// </summary>
        /// <param name="adminClient"></param>
        /// <param name="topic">主题名称</param>
        /// <returns></returns>
        public static int GetTopicPartitionCount(IAdminClient adminClient, string topic)
        {
            if (!IsTopicExisted(adminClient, topic, out Metadata metaData))
                throw new Exception("未找到该Topic");

            return metaData.Topics.Sum(item => item.Partitions.Count);
        }

        /// <summary>
        /// 根据Topic名称获取Kafka相关元数据
        /// </summary>
        /// <param name="adminClient"></param>
        /// <param name="topic">主题名称</param>
        /// <returns></returns>
        private static Metadata GetMetaDataByTopic(IAdminClient adminClient, string topic)
        {
            return adminClient.GetMetadata(topic, TimeSpan.FromSeconds(10));
        }
    }
}