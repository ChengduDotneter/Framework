using System;
using System.Threading.Tasks;

namespace Common.MessageQueueClient
{
    /// <summary>
    /// 生产者相关操作接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMQProducer<T> where T : class, IMQData, new()
    {
        /// <summary>
        /// 推送泛型对象
        /// </summary>
        /// <param name="mQContext">队列上下文</param>
        /// <param name="message"></param>
        void Produce(MQContext mQContext, T message);

        /// <summary>
        /// 异步推送
        /// </summary>
        /// <param name="mQContext">队列上下文</param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task ProduceAsync(MQContext mQContext, T message);
    }

    /// <summary>
    /// 消费者相关操作接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMQConsumer<T> where T : class, IMQData, new()
    {
        /// <summary>
        /// 消费
        /// </summary>
        /// <param name="mQContext">队列上下文</param>
        /// <param name="callback"></param>
        void Consume(MQContext mQContext, Func<T, bool> callback);

        /// <summary>
        /// 订阅
        /// </summary>
        /// <param name="mQContext">队列上下文</param>
        void Subscribe(MQContext mQContext);

        /// <summary>
        /// 取消订阅
        /// </summary>
        void DeSubscribe();
    }
}