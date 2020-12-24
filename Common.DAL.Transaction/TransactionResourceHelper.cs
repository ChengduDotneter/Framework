﻿using Common.Lock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        /// 申请事务资源 表锁
        /// </summary>
        /// <param name="table">所需申请的表类型</param>
        /// <param name="identity">事务线程ID</param>
        /// <param name="weight">事务权重</param>
        /// <returns></returns>
        public static bool ApplayTableResource(Type table, string identity, int weight)
        {
            return m_lock.AcquireWriteLockWithGroupKey(table.FullName, identity, weight, m_timeOut);
        }

        /// <summary>
        /// 异步申请事务资源 表锁
        /// </summary>
        /// <param name="table">所需申请的表类型</param>
        /// <param name="identity">事务线程ID</param>
        /// <param name="weight">事务权重</param>
        // <returns></returns>
        public static async Task<bool> ApplayTableResourceAsync(Type table, string identity, int weight)
        {
            return await m_lock.AcquireWriteLockWithGroupKeyAsync(table.FullName, identity, weight, m_timeOut);
        }

        /// <summary>
        /// 申请事务资源 行写锁
        /// </summary>
        /// <param name="table">所需申请的表类型</param>
        /// <param name="identity">事务线程ID</param>
        /// <param name="weight">事务权重</param>
        /// <param name="readWriteLockMode">读写锁模式</param>
        /// <param name="ids">行id</param>
        /// <returns></returns>
        public static bool ApplayRowResourceWithWrite(Type table, string identity, int weight, IEnumerable<long> ids)
        {
            if (ids == null || ids.Count() == 0)
                return false;

            return m_lock.AcquireWriteLockWithResourceKeys(table.FullName, identity, weight, m_timeOut, ids.Select(item => item.ToString()).ToArray());
        }

        /// <summary>
        /// 异步申请事务资源 行写锁
        /// </summary>
        /// <param name="table">所需申请的表类型</param>
        /// <param name="identity">事务线程ID</param>
        /// <param name="weight">事务权重</param>
        /// <param name="readWriteLockMode">读写锁模式</param>
        /// <param name="ids">行id</param>
        // <returns></returns>
        public static async Task<bool> ApplayRowResourceWithWriteAsync(Type table, string identity, int weight, IEnumerable<long> ids)
        {
            if (ids == null || ids.Count() == 0)
                return false;

            return await m_lock.AcquireWriteLockWithResourceKeysAsync(table.FullName, identity, weight, m_timeOut, ids.Select(item => item.ToString()).ToArray());
        }

        /// <summary>
        /// 申请事务资源 行读锁
        /// </summary>
        /// <param name="table">所需申请的表类型</param>
        /// <param name="identity">事务线程ID</param>
        /// <param name="weight">事务权重</param>
        /// <param name="readWriteLockMode">读写锁模式</param>
        /// <param name="ids">行id</param>
        /// <returns></returns>
        public static bool ApplayRowResourceWithRead(Type table, string identity, int weight, IEnumerable<long> ids)
        {
            if (ids == null || ids.Count() == 0)
                return false;

            return m_lock.AcquireReadLockWithResourceKeys(table.FullName, identity, weight, m_timeOut, ids.Select(item => item.ToString()).ToArray());
        }

        /// <summary>
        /// 异步申请事务资源 行读锁
        /// </summary>
        /// <param name="table">所需申请的表类型</param>
        /// <param name="identity">事务线程ID</param>
        /// <param name="weight">事务权重</param>
        /// <param name="readWriteLockMode">读写锁模式</param>
        /// <param name="ids">行id</param>
        // <returns></returns>
        public static async Task<bool> ApplayRowResourceWithReadAsync(Type table, string identity, int weight, IEnumerable<long> ids)
        {
            if (ids == null || ids.Count() == 0)
                return false;

            return await m_lock.AcquireReadLockWithResourceKeysAsync(table.FullName, identity, weight, m_timeOut, ids.Select(item => item.ToString()).ToArray());
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
        /// 释放事务资源
        /// </summary>
        /// <param name="identity">事务线程ID</param>
        public static async Task ReleaseResourceAsync(string identity)
        {
            await m_lock.ReleaseAsync(identity);
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