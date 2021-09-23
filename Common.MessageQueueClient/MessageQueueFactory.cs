using Common.MessageQueueClient.Kafka;
using Common.MessageQueueClient.RabbitMQ;
using Microsoft.Extensions.Configuration;

namespace Common.MessageQueueClient
{
    public abstract class QueueConfigBase
    {
        public string QueueName { get; set; }
    }

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
        public static IMQProducer<T> GetRabbitMQProducer<T>(RabbitMQConfig rabbitMqConfig) where T : class, IMQData, new()
        {
            return new RabbitmqProducer<T>(rabbitMqConfig);
        }
        
        /// <summary>
        /// 获取RabbitMQ生产者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IMQProducer<T> GetRabbitMQProducer<T>(string queueName, string routeKey) where T : class, IMQData, new()
        {
            RabbitMQConfig rabbitMqConfig = new RabbitMQConfig();
            ConfigManager.Configuration.Bind("RabbitMQService", rabbitMqConfig);
            rabbitMqConfig.QueueName = queueName;
            rabbitMqConfig.RoutingKey = routeKey;
            
            return GetRabbitMQProducer<T>(rabbitMqConfig);
        }

        /// <summary>
        /// 获取RabbitMQ消费者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IMQConsumer<T> GetRabbitMQConsumer<T>(RabbitMQConfig rabbitMqConfig) where T : class, IMQData, new()
        {
            return new RabbitmqConsumer<T>(rabbitMqConfig);
        }
        
        /// <summary>
        /// 获取RabbitMQ消费者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IMQConsumer<T> GetRabbitMQConsumer<T>(string queueName, string routeKey) where T : class, IMQData, new()
        {
            RabbitMQConfig rabbitMqConfig = new RabbitMQConfig();
            ConfigManager.Configuration.Bind("RabbitMQService", rabbitMqConfig);
            rabbitMqConfig.QueueName = queueName;
            rabbitMqConfig.RoutingKey = routeKey;

            return GetRabbitMQConsumer<T>(rabbitMqConfig);
        }

        /// <summary>
        /// 获取RabbitMQ批量消费者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IMQBatchConsumer<T> GetRabbitMQBatchConsumer<T>(RabbitMQConfig rabbitMqConfig) where T : class, IMQData, new()
        {
            return new RabbitmqConsumer<T>(rabbitMqConfig);
        }
        
        /// <summary>
        /// 获取RabbitMQ批量消费者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IMQBatchConsumer<T> GetRabbitMQBatchConsumer<T>(string queueName, string routeKey) where T : class, IMQData, new()
        {
            RabbitMQConfig rabbitMqConfig = new RabbitMQConfig();
            ConfigManager.Configuration.Bind("RabbitMQService", rabbitMqConfig);
            rabbitMqConfig.QueueName = queueName;
            rabbitMqConfig.RoutingKey = routeKey;

            return GetRabbitMQBatchConsumer<T>(rabbitMqConfig);
        }

        /// <summary>
        /// 获取Kafka生产者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IMQProducer<T> GetKafkaProducer<T>(KafkaConfig kafkaConfig) where T : class, IMQData, new()
        {
            return new KafkaProducer<T>(kafkaConfig);
        }
        
        /// <summary>
        /// 获取Kafka生产者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IMQProducer<T> GetKafkaProducer<T>(string queueName) where T : class, IMQData, new()
        {
            KafkaConfig kafkaConfig = new KafkaConfig();
            ConfigManager.Configuration.Bind("KafkaService", kafkaConfig);
            kafkaConfig.QueueName = queueName;

            return GetKafkaProducer<T>(kafkaConfig);
        }

        /// <summary>
        /// 获取Kafka消费者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IMQConsumer<T> GetKafkaConsumer<T>(KafkaConfig kafkaConfig) where T : class, IMQData, new()
        {
            return new KafkaConsumer<T>(kafkaConfig);
        }

        /// <summary>
        /// 获取Kafka消费者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IMQConsumer<T> GetKafkaConsumer<T>(string groupId, string queueName) where T : class, IMQData, new()
        {
            KafkaConfig kafkaConfig = new KafkaConfig();
            ConfigManager.Configuration.Bind("KafkaService", kafkaConfig);
            kafkaConfig.GroupId = groupId;
            kafkaConfig.QueueName = queueName;

            return GetKafkaConsumer<T>(kafkaConfig);
        }

        /// <summary>
        /// 获取Kafka批量消费者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IMQBatchConsumer<T> GetKafkaBatchConsumer<T>(KafkaConfig kafkaConfig) where T : class, IMQData, new()
        {
            return new KafkaConsumer<T>(kafkaConfig);
        }
        
        /// <summary>
        /// 获取Kafka批量消费者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IMQBatchConsumer<T> GetKafkaBatchConsumer<T>(string groupId, string queueName) where T : class, IMQData, new()
        {
            KafkaConfig kafkaConfig = new KafkaConfig();
            ConfigManager.Configuration.Bind("KafkaService", kafkaConfig);
            kafkaConfig.GroupId = groupId;
            kafkaConfig.QueueName = queueName;

            return GetKafkaBatchConsumer<T>(kafkaConfig);
        }
    }
}