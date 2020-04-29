namespace Common.MessageQueueClient
{
    public static class MessageQueueFactory
    {
        public static IPublisher<T> GetPublisherContext<T>() where T : class, IData, new()
        {
            return RabbitmqDao.DeclarePublisherContext<T>();
        }

        public static ISubscriber<T> GetSubscriberContext<T>() where T : class, IData, new()
        {
            return RabbitmqDao.DeclareSubscriberContext<T>();
        }
    }
}
