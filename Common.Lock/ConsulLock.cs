using Consul;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Lock
{
    internal class ConsulLock : ILock
    {
        /// <summary>
        /// 锁对象
        /// </summary>
        private class LockInstance : IDisposable
        {
            /// <summary>
            /// ConsulClient
            /// </summary>
            private IConsulClient m_instanceConsulClient;

            /// <summary>
            /// SessionID
            /// </summary>
            public string SessionID { get; set; }

            /// <summary>
            /// 锁集合
            /// </summary>
            public ConcurrentDictionary<string, IDistributedLock> Locks { get; }

            /// <summary>
            /// CancellationTokenSource
            /// </summary>
            public CancellationTokenSource CancellationTokenSource { get; }

            public LockInstance(IConsulClient consulClient)
            {
                m_instanceConsulClient = consulClient;
                Locks = new ConcurrentDictionary<string, IDistributedLock>();
                WriteResult<string> sessionRequest = m_instanceConsulClient.Session.Create(new SessionEntry() { TTL = TTL, LockDelay = TimeSpan.FromMilliseconds(1) }).Result;
                SessionID = sessionRequest.Response;
                CancellationTokenSource = new CancellationTokenSource();
                m_instanceConsulClient.Session.RenewPeriodic(TTL, SessionID, CancellationTokenSource.Token);
            }

            public void Dispose()
            {
                m_instanceConsulClient.Session.Destroy(SessionID);
                CancellationTokenSource.Cancel();
                CancellationTokenSource.Dispose();
            }
        }

        /// <summary>
        /// LOCK_PREFIX
        /// </summary>
        private const string LOCK_PREFIX = "lock";

        /// <summary>
        /// TTL
        /// </summary>
        private readonly static TimeSpan TTL = TimeSpan.FromMilliseconds(10 * 1000);

        /// <summary>
        /// Lock集合
        /// </summary>
        private readonly static ConcurrentDictionary<string, LockInstance> m_lockInstances;

        /// <summary>
        /// Consul
        /// </summary>
        private readonly static IConsulClient m_consulClient;

        static ConsulLock()
        {
            m_lockInstances = new ConcurrentDictionary<string, LockInstance>();
            ConsulServiceEntity serviceEntity = new ConsulServiceEntity();//consul服务实体
            ConfigManager.Configuration.Bind("ConsulService", serviceEntity);//绑定

            if (string.IsNullOrWhiteSpace(serviceEntity.ConsulIP) || serviceEntity.ConsulPort == 0)
                throw new Exception("ConsulIP和ConsulPort不能为空。");

            m_consulClient = new ConsulClient(x => x.Address = new Uri($"http://{serviceEntity.ConsulIP}:{serviceEntity.ConsulPort}"));//consul客户端
        }
        /// <summary>
        /// 互斥锁同步申请资源
        /// </summary>
        /// <param name="key">锁的唯一key</param>
        /// <param name="identity">所对象唯一身份</param>
        /// <param name="weight"></param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        bool ILock.AcquireMutex(string key, string identity, int weight, int timeOut)
        {
            string lockKey = $"{LOCK_PREFIX}/{key}";

            if (!m_lockInstances.ContainsKey(identity))
                m_lockInstances.TryAdd(identity, new LockInstance(m_consulClient));

            LockInstance lockInstance = m_lockInstances[identity];

            if (!lockInstance.Locks.ContainsKey(lockKey))
            {
                LockOptions lockOptions = new LockOptions(lockKey)
                {
                    LockTryOnce = true,
                    LockWaitTime = TimeSpan.FromMilliseconds(timeOut),
                    Value = Encoding.UTF8.GetBytes(weight.ToString()),
                    Session = lockInstance.SessionID,
                    SessionTTL = TTL
                };

                IDistributedLock distributedLock = m_consulClient.CreateLock(lockOptions);//服务发现加锁

                try
                {
                    distributedLock.Acquire(CancellationToken.None).Wait();

                    lock (lockInstance.Locks)
                        m_lockInstances[identity].Locks.TryAdd(lockKey, distributedLock);

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
        /// <summary>
        /// 互斥锁异步申请
        /// </summary>
        /// <param name="key"></param>
        /// <param name="identity"></param>
        /// <param name="weight"></param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        async Task<bool> ILock.AcquireMutexAsync(string key, string identity, int weight, int timeOut)
        {
            string lockKey = $"{LOCK_PREFIX}/{key}";

            if (!m_lockInstances.ContainsKey(identity))
                m_lockInstances.TryAdd(identity, new LockInstance(m_consulClient));

            LockInstance lockInstance = m_lockInstances[identity];

            if (!lockInstance.Locks.ContainsKey(lockKey))
            {
                LockOptions lockOptions = new LockOptions(lockKey)
                {
                    LockTryOnce = true,
                    LockWaitTime = TimeSpan.FromMilliseconds(timeOut),
                    Value = Encoding.UTF8.GetBytes(weight.ToString()),
                    Session = lockInstance.SessionID,
                    SessionTTL = TTL
                };

                IDistributedLock distributedLock = m_consulClient.CreateLock(lockOptions);

                try
                {
                    await distributedLock.Acquire(CancellationToken.None);

                    lock (lockInstance.Locks)
                        m_lockInstances[identity].Locks.TryAdd(lockKey, distributedLock);

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
        /// <summary>
        /// 同步释放锁资源
        /// </summary>
        /// <param name="identity"></param>
        void ILock.Release(string identity)
        {
            if (!m_lockInstances.TryGetValue(identity, out LockInstance lockInstance))
                return;

            IDistributedLock[] distributedLocks;

            lock (lockInstance.Locks)
                distributedLocks = lockInstance.Locks.Values.ToArray();

            for (int i = 0; i < distributedLocks.Length; i++)
                distributedLocks[i].Release().Wait();

            lock (lockInstance)
                lockInstance.Dispose();
        }
        /// <summary>
        /// 异步释放锁资源
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        async Task ILock.ReleaseAsync(string identity)
        {
            if (!m_lockInstances.TryGetValue(identity, out LockInstance lockInstance))
                return;

            IDistributedLock[] distributedLocks;

            lock (lockInstance.Locks)
                distributedLocks = lockInstance.Locks.Values.ToArray();

            IList<Task> tasks = new List<Task>();

            for (int i = 0; i < distributedLocks.Length; i++)
                tasks.Add(distributedLocks[i].Release());

            await Task.WhenAll(tasks);

            lock (lockInstance)
                lockInstance.Dispose();
        }

        bool ILock.AcquireReadLockWithGroupKey(string groupKey, string identity, int weight, int timeOut)
        {
            throw new NotImplementedException();
        }

        Task<bool> ILock.AcquireReadLockWithGroupKeyAsync(string groupKey, string identity, int weight, int timeOut)
        {
            throw new NotImplementedException();
        }

        bool ILock.AcquireWriteLockWithGroupKey(string groupKey, string identity, int weight, int timeOut)
        {
            throw new NotImplementedException();
        }

        Task<bool> ILock.AcquireWriteLockWithGroupKeyAsync(string groupKey, string identity, int weight, int timeOut)
        {
            throw new NotImplementedException();
        }

        bool ILock.AcquireReadLockWithResourceKeys(string groupKey, string identity, int weight, int timeOut, params string[] resourceKeys)
        {
            throw new NotImplementedException();
        }

        Task<bool> ILock.AcquireReadLockWithResourceKeysAsync(string groupKey, string identity, int weight, int timeOut, params string[] resourceKeys)
        {
            throw new NotImplementedException();
        }

        bool ILock.AcquireWriteLockWithResourceKeys(string groupKey, string identity, int weight, int timeOut, params string[] resourceKeys)
        {
            throw new NotImplementedException();
        }

        Task<bool> ILock.AcquireWriteLockWithResourceKeysAsync(string groupKey, string identity, int weight, int timeOut, params string[] resourceKeys)
        {
            throw new NotImplementedException();
        }
    }
}