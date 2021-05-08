using System;

namespace Common.MessageQueueClient
{
    /// <summary>
    /// MQ数据结构接口
    /// </summary>
    public interface IMQData
    {
        /// <summary>
        /// 创建时间
        /// </summary>
        DateTime CreateTime { get; }
    }

    /// <summary>
    /// 带回执的QM数据
    /// </summary>
    /// <typeparam name="T">MQ数据类型</typeparam>
    public interface IAckData<T> where T : IMQData
    {
        /// <summary>
        /// MQ数据
        /// </summary>
        public T Data { get; }

        /// <summary>
        /// 确认回执
        /// </summary>
        void Commit();

        /// <summary>
        /// 取消回执
        /// </summary>
        void Cancel();
    }
}