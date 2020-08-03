using System;
using System.Threading.Tasks;
using Common.Lock;

namespace Common.DAL.Transaction
{
    /// <summary>
    /// 事务资源帮助类
    /// </summary>
    public static class TransactionResourceHelper
    {
        /// <summary>
        /// 默认超时时间
        /// </summary>
        private const int DEFAULT_TIME_OUT = 60 * 1000;

        /// <summary>
        /// 超时时间
        /// </summary>
        private readonly static int m_timeOut;

        /// <summary>
        /// 锁对象
        /// </summary>
        private readonly static ILock m_lock;

        /// <summary>
        /// 申请事务资源
        /// </summary>
        /// <param name="table">所需申请的表类型</param>
        /// <param name="identity">事务线程ID</param>
        /// <param name="weight">事务权重</param>
        /// <returns></returns>
        public static bool ApplayResource(Type table, string identity, int weight)
        {
            return m_lock.Acquire(table.FullName, identity, weight, m_timeOut);
        }

        /// <summary>
        /// 申请事务资源，异步
        /// </summary>
        /// <param name="table">所需申请的表类型</param>
        /// <param name="identity">事务线程ID</param>
        /// <param name="weight">事务权重</param>
        // <returns></returns>
        public static Task<bool> ApplayResourceAsync(Type table, string identity, int weight)
        {
            return Task.Factory.StartNew(() => { return ApplayResource(table, identity, weight); });
        }

        /// <summary>
        /// 释放事务资源
        /// </summary>
        /// <param name="identity">事务线程ID</param>
        public static void ReleaseResource(string identity)
        {
            m_lock.Release(identity);
        }

        /// <summary>
        /// 释放事务资源，异步
        /// </summary>
        /// <param name="identity">事务线程ID</param>
        public static Task ReleaseResourceAsync(string identity)
        {
            return Task.Factory.StartNew(() => { ReleaseResource(identity); });
        }

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static TransactionResourceHelper()
        {
            string timeOutString = ConfigManager.Configuration["TransactionTimeout"];
            m_timeOut = string.IsNullOrWhiteSpace(timeOutString) ? DEFAULT_TIME_OUT : Convert.ToInt32(timeOutString);
            m_lock = LockFactory.GetRedisLock();
        }
    }
}
