using System;
using System.Threading.Tasks;

namespace Common.MessageQueueClient
{
    public interface IPublisher<T> where T : class, IData, new()
    {
        void Dispose();

        void Publish(T message, string exchangeName, string routingKey);

        void Publish(string message, string exchangeName, string routingKey);

        void BindingPublishQueues(string exchangeName, ExChangeTypeEnum exchangeType, params string[] queuesName);

        Task PublishAsync(T message);
    }

    public interface ISubscriber<T> where T : class, IData, new()
    {
        void Dispose();

        void Subscribe(string exchangeName, Action<string> callback);

        void BindingSubscribeQueue(string exchangeName, ExChangeTypeEnum exchangeType, string routingKey, string listenqueue);

        Task<T> SubscribeAsync(string queueName);
    }
}