using Common.MessageQueueClient.Kafka;
using Common.MessageQueueClient.RabbitMQ;

namespace Common.MessageQueueClient
{
    /// <summary>
    /// 信息管道工厂类
    /// </summary>
    public static class MessageQueueFactory
    {
        /// <summary>
        /// 获取RabbitMQ生产者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IMQProducer<T> GetRabbitMQProducer<T>(ExChangeTypeEnum exChangeTypeEnum) where T : class, IMQData, new()
        {
            return new RabbitmqProducer<T>(exChangeTypeEnum);
        }

        /// <summary>
        /// 获取RabbitMQ消费者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IMQConsumer<T> GetRabbitMQConsumer<T>(ExChangeTypeEnum exChangeTypeEnum) where T : class, IMQData, new()
        {
            return new RabbitmqConsumer<T>(exChangeTypeEnum);
        }
        
        /// <summary>
        /// 获取RabbitMQ Ack消费者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IMQAckConsumer<T> GetRabbitMQAckConsumer<T>(ExChangeTypeEnum exChangeTypeEnum) where T : class, IMQData, new()
        {
            return new RabbitmqConsumer<T>(exChangeTypeEnum);
        }

        /// <summary>
        /// 获取Kafka生产者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IMQProducer<T> GetKafkaProducer<T>() where T : class, IMQData, new()
        {
            return new KafkaProducer<T>();
        }

        /// <summary>
        /// 获取Kafka消费者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="groupId">消费者组ID</param>
        /// <param name="enableAutoOffsetStore">是否自动推送Offset</param>
        /// <returns></returns>
        public static IMQConsumer<T> GetKafkaConsumer<T>(string groupId, bool enableAutoOffsetStore = true) where T : class, IMQData, new()
        {
            return new KafkaConsumer<T>(groupId, enableAutoOffsetStore);
        }
        
        /// <summary>
        /// 获取Kafka Ack消费者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="groupId">消费者组ID</param>
        /// <returns></returns>
        public static IMQAckConsumer<T> GetKafkaAckConsumer<T>(string groupId) where T : class, IMQData, new()
        {
            return new KafkaConsumer<T>(groupId, false);
        }
    }
}