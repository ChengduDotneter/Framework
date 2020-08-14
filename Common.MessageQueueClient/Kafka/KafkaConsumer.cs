using Confluent.Kafka;
using System;
using System.Text.Json;

namespace Common.MessageQueueClient.Kafka
{
    /// <summary>
    /// Kafka消费者操作类
    /// </summary>
    public class KafkaConsumer<T> : IMQConsumer<T> where T : class, IMQData, new()
    {
        private readonly IConsumer<string, string> m_consumer;
        private readonly bool m_enableAutoOffsetStore;
        private string m_subscribeMessageQueueName;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="enableAutoOffsetStore"></param>
        public KafkaConsumer(string groupId, bool enableAutoOffsetStore = true)
        {
            m_enableAutoOffsetStore = enableAutoOffsetStore;
            m_consumer = new ConsumerBuilder<string, string>(KafkaConfigBuilder.GetConsumerConfig(groupId, m_enableAutoOffsetStore)).Build();

            AppDomain.CurrentDomain.ProcessExit += (send, e) => { Dispose(); };
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

                if (callback?.Invoke(ConvertMessageToData(consumeResult.Message)) ?? false && !m_enableAutoOffsetStore)
                    m_consumer.Commit(new[] { consumeResult.TopicPartitionOffset });
            }
            catch (Exception ex)
            {
                throw new Exception($"数据消费失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 根据泛型将Message反序列化为相应对象
        /// </summary>
        /// <param name="message">数据类型</param>
        /// <returns></returns>
        private T ConvertMessageToData(Message<string, string> message)
        {
            return JsonSerializer.Deserialize<T>(message.Value);
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
