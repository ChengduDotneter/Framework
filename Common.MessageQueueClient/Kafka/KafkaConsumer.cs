using Confluent.Kafka;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace Common.MessageQueueClient.Kafka
{
    /// <summary>
    /// Kafka消费者操作类
    /// </summary>
    public class KafkaConsumer<T> : IMQConsumer<T>, IMQBatchConsumer<T> where T : class, IMQData, new()
    {
        private readonly static ILog m_log;
        private readonly IConsumer<string, string> m_consumer;
        private readonly KafkaConfig m_kafkaConfig;

        private class BatchData
        {
            public TopicPartitionOffset TopicPartitionOffset { get; }
            public T Data { get; }

            public BatchData(TopicPartitionOffset topicPartitionOffset, T data)
            {
                TopicPartitionOffset = topicPartitionOffset;
                Data = data;
            }
        }

        static KafkaConsumer()
        {
            m_log = Log4netCreater.CreateLog("KafkaConsumer");
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public KafkaConsumer(KafkaConfig kafkaConfig)
        {
            m_kafkaConfig = kafkaConfig;

            ConsumerConfig consumerConfig = new ConsumerConfig
            {
                //Kafka的服务地址，多个用逗号隔开
                BootstrapServers = m_kafkaConfig.HostName,
                //消费者组ID
                GroupId = m_kafkaConfig.GroupId,
                //消费者对Offset消费数据的决策 此为若没有Offset 则从最开始消费数据
                AutoOffsetReset = AutoOffsetReset.Earliest,
                StatisticsIntervalMs = 60000,
                SessionTimeoutMs = 6000,
                //是否自动提交offset
                EnableAutoOffsetStore = m_kafkaConfig.EnableAutoOffsetStore,
                AllowAutoCreateTopics = true
            };

            m_consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        }

        /// <summary>
        /// 订阅
        /// </summary>
        public void Subscribe()
        {
            m_consumer.Subscribe(m_kafkaConfig.QueueName);
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public void DeSubscribe()
        {
            m_consumer.Unsubscribe();
        }

        /// <summary>
        /// 消费
        /// </summary>
        /// <param name="callback">回调</param>
        public void Consume(Func<T, bool> callback)
        {
            try
            {
                ConsumeResult<string, string> consumeResult = m_consumer.Consume();

                if ((callback?.Invoke(ConvertMessageToData(consumeResult.Message)) ?? false) && !m_kafkaConfig.EnableAutoOffsetStore)
                    m_consumer.Commit(new[] { consumeResult.TopicPartitionOffset });
            }
            catch (Exception ex)
            {
                throw new Exception($"数据消费失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 异步消费
        /// </summary>
        /// <param name="callback">回调</param>
        public async Task ConsumeAsync(Func<T, Task<bool>> callback)
        {
            try
            {
                ConsumeResult<string, string> consumeResult = m_consumer.Consume();

                bool success;

                if (callback == null)
                    success = false;
                else
                    success = await callback.Invoke(ConvertMessageToData(consumeResult.Message));

                if (success && !m_kafkaConfig.EnableAutoOffsetStore)
                    m_consumer.Commit(new[] { consumeResult.TopicPartitionOffset });
            }
            catch (Exception ex)
            {
                throw new Exception($"数据消费失败：{ex.Message}");
            }
        }

        public void Consume(Func<IEnumerable<T>, bool> callback, TimeSpan pullingTimeSpan, int pullingCount)
        {
            IList<BatchData> batchDatas = new List<BatchData>();

            while (true)
            {
                while (true)
                {
                    try
                    {
                        ConsumeResult<string, string> consumeResult = m_consumer.Consume();
                        batchDatas.Add(new BatchData(consumeResult.TopicPartitionOffset, ConvertMessageToData(consumeResult.Message)));

                        if (batchDatas.Count >= pullingCount)
                        {
                            try
                            {
                                bool result = false;

                                if (callback != null)
                                    result = callback.Invoke(batchDatas.Select(item => item.Data));

                                if (result)
                                    m_consumer.Commit(new[] { batchDatas.Last().TopicPartitionOffset });

                                batchDatas.Clear();
                            }
                            finally
                            {
                                batchDatas.Clear();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        m_log.Error($"数据消费失败：{ex.Message}");
                    }

                    Thread.Sleep(pullingTimeSpan);
                }
            }
        }

        public async Task ConsumeAsync(Func<IEnumerable<T>, Task<bool>> callback, TimeSpan pullingTimeSpan, int pullingCount)
        {
            IList<BatchData> batchDatas = new List<BatchData>();

            while (true)
            {
                while (true)
                {
                    try
                    {
                        ConsumeResult<string, string> consumeResult = m_consumer.Consume();
                        batchDatas.Add(new BatchData(consumeResult.TopicPartitionOffset, ConvertMessageToData(consumeResult.Message)));

                        if (batchDatas.Count >= pullingCount)
                        {
                            try
                            {
                                bool result = false;

                                if (callback != null)
                                    result = await callback.Invoke(batchDatas.Select(item => item.Data));

                                if (result)
                                    m_consumer.Commit(new[] { batchDatas.Last().TopicPartitionOffset });

                                batchDatas.Clear();
                            }
                            finally
                            {
                                batchDatas.Clear();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        m_log.Error($"数据消费失败：{ex.Message}");
                    }

                    await Task.Delay(pullingTimeSpan);
                }
            }
        }

        /// <summary>
        /// 根据泛型将Message反序列化为相应对象
        /// </summary>
        /// <param name="message">数据类型</param>
        /// <returns></returns>
        private T ConvertMessageToData(Message<string, string> message)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(message.Value);
            }
            catch
            {
                m_log.Error($"反序列化失败: {message.Value}");
                throw new Exception("反序列化失败。");
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            m_consumer?.Dispose();
        }
    }
}