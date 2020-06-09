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
        public static IPublisher<T> GetPublisherContext<T>() where T : class, IData, new()
        {
            return RabbitmqDao.DeclarePublisherContext<T>();
        }

        /// <summary>
        /// 获取用户上下文
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ISubscriber<T> GetSubscriberContext<T>() where T : class, IData, new()
        {
            return RabbitmqDao.DeclareSubscriberContext<T>();
        }
    }
}
