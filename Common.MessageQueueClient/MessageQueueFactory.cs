namespace Common.MessageQueueClient
{
    /// <summary>
    /// 信息管道工厂类
    /// </summary>
    public static class MessageQueueFactory
    {
        /// <summary>
        /// 获取推送上下文
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IMQProducer<T> GetRabbitMQProducer<T>() where T : class, IMQData, new()
        {
            return RabbitmqDao.DeclarePublisherContext<T>();
        }

        /// <summary>
        /// 获取用户上下文
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IMQConsumer<T> GetRabbitMQConsumer<T>() where T : class, IMQData, new()
        {
            return RabbitmqDao.DeclareSubscriberContext<T>();
        }

        /// <summary>
        /// 获取推送上下文
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IMQProducer<T> GetKafkaProducer<T>() where T : class, IMQData, new()
        {
            return RabbitmqDao.DeclarePublisherContext<T>();
        }

        /// <summary>
        /// 获取用户上下文
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IMQConsumer<T> GetKafkaConsumer<T>() where T : class, IMQData, new()
        {
            return RabbitmqDao.DeclareSubscriberContext<T>();
        }
    }
}