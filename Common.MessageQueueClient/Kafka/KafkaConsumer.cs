﻿using Confluent.Kafka;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Common.MessageQueueClient.Kafka
{
    /// <summary>
    /// Kafka消费者操作类
    /// </summary>
    public class KafkaConsumer<T> : IMQConsumer<T>, IMQBatchConsumer<T> where T : class, IMQData, new()
    {
        private readonly IConsumer<string, string> m_consumer;
        private readonly bool m_enableAutoOffsetStore;
        private string m_subscribeMessageQueueName;

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

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="enableAutoOffsetStore"></param>
        public KafkaConsumer(string groupId, bool enableAutoOffsetStore = true)
        {
            m_enableAutoOffsetStore = enableAutoOffsetStore;
            m_consumer = new ConsumerBuilder<string, string>(KafkaConfigBuilder.GetConsumerConfig(groupId, m_enableAutoOffsetStore)).Build();
        }

        /// <summary>
        /// 订阅
        /// </summary>
        /// <param name="mQContext">消息队列上下文</param>
        public void Subscribe(MQContext mQContext)
        {
            m_subscribeMessageQueueName = mQContext.MessageQueueName;
            m_consumer.Subscribe(mQContext.MessageQueueName);
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public void DeSubscribe()
        {
            m_subscribeMessageQueueName = string.Empty;
            m_consumer.Unsubscribe();
        }

        /// <summary>
        /// 消费
        /// </summary>
        /// <param name="mQContext">消息队列上下文</param>
        /// <param name="callback">回调</param>
        public void Consume(MQContext mQContext, Func<T, bool> callback)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(m_subscribeMessageQueueName))
                    throw new Exception("该消费者未订阅任何队列，请先订阅队列");

                ConsumeResult<string, string> consumeResult = m_consumer.Consume();

                if ((callback?.Invoke(ConvertMessageToData(consumeResult.Message)) ?? false) && !m_enableAutoOffsetStore)
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
        /// <param name="mQContext">消息队列上下文</param>
        /// <param name="callback">回调</param>
        public async Task ConsumeAsync(MQContext mQContext, Func<T, Task<bool>> callback)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(m_subscribeMessageQueueName))
                    throw new Exception("该消费者未订阅任何队列，请先订阅队列");

                ConsumeResult<string, string> consumeResult = m_consumer.Consume();

                bool success;

                if (callback == null)
                    success = false;
                else
                    success = await callback.Invoke(ConvertMessageToData(consumeResult.Message));

                if (success && !m_enableAutoOffsetStore)
                    m_consumer.Commit(new[] { consumeResult.TopicPartitionOffset });
            }
            catch (Exception ex)
            {
                throw new Exception($"数据消费失败：{ex.Message}");
            }
        }

        public void Consume(MQContext mQContext, Func<IEnumerable<T>, bool> callback, TimeSpan pullingTimeSpan, int pullingCount)
        {
            if (string.IsNullOrWhiteSpace(m_subscribeMessageQueueName))
                throw new Exception("该消费者未订阅任何队列，请先订阅队列");

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
                        throw new Exception($"数据消费失败：{ex.Message}");
                    }

                    Thread.Sleep(pullingTimeSpan);
                }
            }
        }

        public async Task ConsumeAsync(MQContext mQContext, Func<IEnumerable<T>, Task<bool>> callback, TimeSpan pullingTimeSpan, int pullingCount)
        {
            if (string.IsNullOrWhiteSpace(m_subscribeMessageQueueName))
                throw new Exception("该消费者未订阅任何队列，请先订阅队列");

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
                        throw new Exception($"数据消费失败：{ex.Message}");
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
            return JsonConvert.DeserializeObject<T>(message.Value);
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