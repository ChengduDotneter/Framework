using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using Microsoft.Extensions.Configuration;

namespace Common.DAL.Transaction
{
    /// <summary>
    /// 事务资源帮助类
    /// </summary>
    public static class TransactionResourceHelper
    {
        /// <summary>
        /// 锁对象
        /// </summary>
        private class LockInstance : IDisposable
        {
            /// <summary>
            /// ConsulClient
            /// </summary>
            private IConsulClient m_consulClient;

            /// <summary>
            /// SessionID
            /// </summary>
            public string SessionID { get; set; }

            /// <summary>
            /// 锁集合
            /// </summary>
            public IDictionary<string, IDistributedLock> Locks { get; }

            /// <summary>
            /// CancellationTokenSource
            /// </summary>
            public CancellationTokenSource CancellationTokenSource { get; }

            public LockInstance(IConsulClient consulClient)
            {
                m_consulClient = consulClient;
                Locks = new Dictionary<string, IDistributedLock>();
                WriteResult<string> sessionRequest = m_consulClient.Session.Create(new SessionEntry() { TTL = TTL, LockDelay = TimeSpan.FromMilliseconds(1) }).Result;
                SessionID = sessionRequest.Response;
                CancellationTokenSource = new CancellationTokenSource();
                m_consulClient.Session.RenewPeriodic(TTL, SessionID, CancellationTokenSource.Token);
            }

            public void Dispose()
            {
                m_consulClient.Session.Destroy(SessionID);
                CancellationTokenSource.Cancel();
                CancellationTokenSource.Dispose();
            }
        }

        /// <summary>
        /// 默认超时时间
        /// </summary>
        private const int DEFAULT_TIME_OUT = 60 * 1000;

        /// <summary>
        /// LOCK_PREFIX
        /// </summary>
        private const string LOCK_PREFIX = "lock";

        /// <summary>
        /// TTL
        /// </summary>
        private readonly static TimeSpan TTL = TimeSpan.FromMilliseconds(10 * 1000);

        /// <summary>
        /// 超时时间
        /// </summary>
        private readonly static int m_timeOut;

        /// <summary>
        /// Consul
        /// </summary>
        private readonly static IConsulClient m_consulClient;

        /// <summary>
        /// SessionID集合
        /// </summary>
        private readonly static IDictionary<string, LockInstance> m_lockInstances;

        /// <summary>
        /// 申请事务资源
        /// </summary>
        /// <param name="table">所需申请的表类型</param>
        /// <param name="identity">事务线程ID</param>
        /// <param name="weight">事务权重</param>
        /// <returns></returns>
        public static bool ApplayResource(Type table, string identity, int weight)
        {
            if (m_consulClient != null)
            {
                string lockKey = $"{LOCK_PREFIX}/{table.Namespace}/{table.Name}";
                LockInstance lockInstance;

                lock (m_lockInstances)
                {
                    if (!m_lockInstances.ContainsKey(identity))
                    {
                        lockInstance = new LockInstance(m_consulClient);
                        m_lockInstances.Add(identity, lockInstance);
                    }
                    else
                    {
                        lockInstance = m_lockInstances[identity];
                    }
                }

                lock (lockInstance)
                {
                    if (!lockInstance.Locks.ContainsKey(lockKey))
                    {
                        LockOptions lockOptions = new LockOptions(lockKey)
                        {
                            LockTryOnce = true,
                            LockWaitTime = TimeSpan.FromMilliseconds(m_timeOut),
                            Value = Encoding.UTF8.GetBytes(weight.ToString()),
                            Session = lockInstance.SessionID,
                            SessionTTL = TTL
                        };

                        IDistributedLock distributedLock = m_consulClient.CreateLock(lockOptions);

                        try
                        {
                            distributedLock.Acquire(CancellationToken.None).Wait();
                            m_lockInstances[identity].Locks[lockKey] = distributedLock;

                            return true;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return m_lockInstances[identity].Locks[lockKey].IsHeld;
                    }
                }
            }

            return true;
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
            if (m_consulClient != null)
            {
                if (!m_lockInstances.ContainsKey(identity))
                    return;

                LockInstance lockInstance;
                IDistributedLock[] distributedLocks;

                lock (m_lockInstances)
                {
                    lockInstance = m_lockInstances[identity];
                    m_lockInstances.Remove(identity);
                }

                lock (lockInstance)
                    distributedLocks = lockInstance.Locks.Values.ToArray();

                for (int i = 0; i < distributedLocks.Length; i++)
                    distributedLocks[i].Release().Wait();

                lock (lockInstance)
                    lockInstance.Dispose();
            }
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
            m_lockInstances = new Dictionary<string, LockInstance>();

            ConsulServiceEntity serviceEntity = new ConsulServiceEntity();
            ConfigManager.Configuration.Bind("ConsulService", serviceEntity);

            if (!string.IsNullOrWhiteSpace(serviceEntity.ConsulIP) && serviceEntity.ConsulPort != 0)
                m_consulClient = new ConsulClient(x => x.Address = new Uri($"http://{serviceEntity.ConsulIP}:{serviceEntity.ConsulPort}"));
        }
    }
}
