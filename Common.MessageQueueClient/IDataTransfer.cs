using System;
using System.Threading.Tasks;

namespace Common.MessageQueueClient
{
    /// <summary>
    /// 数据推送相关接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPublisher<T> where T : class, IData, new()
    {
        /// <summary>
        /// Dispose
        /// </summary>
        void Dispose();

        /// <summary>
        /// 推送泛型对象
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exchangeName"></param>
        /// <param name="routingKey"></param>
        void Publish(T message, string exchangeName, string routingKey);

        /// <summary>
        /// 推送字符串消息（Json）
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exchangeName"></param>
        /// <param name="routingKey"></param>
        void Publish(string message, string exchangeName, string routingKey);

        /// <summary>
        /// 绑定推送队列
        /// </summary>
        /// <param name="exchangeName"></param>
        /// <param name="exchangeType"></param>
        /// <param name="queuesName"></param>
        void BindingPublishQueues(string exchangeName, ExChangeTypeEnum exchangeType, params string[] queuesName);

        /// <summary>
        /// 异步推送
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task PublishAsync(T message);
    }

    /// <summary>
    /// 消息订阅接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISubscriber<T> where T : class, IData, new()
    {
        /// <summary>
        /// Dispose
        /// </summary>
        void Dispose();

        /// <summary>
        /// 消息订阅
        /// </summary>
        /// <param name="exchangeName"></param>
        /// <param name="callback"></param>
        void Subscribe(string exchangeName, Action<string> callback);

        /// <summary>
        /// 绑定订阅队列
        /// </summary>
        /// <param name="exchangeName"></param>
        /// <param name="exchangeType"></param>
        /// <param name="routingKey"></param>
        /// <param name="listenqueue"></param>

        void BindingSubscribeQueue(string exchangeName, ExChangeTypeEnum exchangeType, string routingKey, string listenqueue);

        /// <summary>
        /// 异步订阅消息
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns></returns>
        Task<T> SubscribeAsync(string queueName);
    }
}