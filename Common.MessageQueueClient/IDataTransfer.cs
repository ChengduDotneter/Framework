using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.MessageQueueClient
{
    /// <summary>
    /// 生产者相关操作接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMQProducer<T> : IDisposable where T : class, IMQData, new()
    {
        /// <summary>
        /// 推送泛型对象
        /// </summary>
        /// <param name="message"></param>
        void Produce(T message);

        /// <summary>
        /// 异步推送
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task ProduceAsync(T message);
    }

    /// <summary>
    /// 消费者相关操作接口
    /// </summary>
    public interface IMQConsumer<T> : IMQConsumerBase where T : class, IMQData, new()
    {
        /// <summary>
        /// 同步消费
        /// </summary>
        /// <param name="callback"></param>
        void Consume(Func<T, bool> callback);

        /// <summary>
        /// 异步消费
        /// </summary>
        /// <param name="callback"></param>
        Task ConsumeAsync(Func<T, Task<bool>> callback);
    }

    /// <summary>
    /// 批量消费者相关操作接口
    /// </summary>
    public interface IMQBatchConsumer<T> : IMQConsumerBase where T : class, IMQData, new()
    {
        /// <summary>
        /// 同步批量消费
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="pullingTimeSpan">拉取数据时间间隔</param>
        /// <param name="pullingCount">拉取数据数据包大小分割</param>
        void Consume(Func<IEnumerable<T>, bool> callback, TimeSpan pullingTimeSpan, int pullingCount);

        /// <summary>
        /// 异步批量消费
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="pullingTimeSpan">拉取数据时间间隔</param>
        /// <param name="pullingCount">拉取数据数据包大小分割</param>
        Task ConsumeAsync(Func<IEnumerable<T>, Task<bool>> callback, TimeSpan pullingTimeSpan, int pullingCount);
    }

    /// <summary>
    /// 消费者相关操作基接口
    /// </summary>
    public interface IMQConsumerBase : IDisposable
    {
        /// <summary>
        /// 订阅
        /// </summary>
        void Subscribe();

        /// <summary>
        /// 取消订阅
        /// </summary>
        void DeSubscribe();
    }
}