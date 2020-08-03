using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Consul;
using Microsoft.Extensions.Configuration;

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
        private readonly static IDictionary<string, LockInstance> m_lockInstances;

        /// <summary>
        /// Consul
        /// </summary>
        private readonly static IConsulClient m_consulClient;

        static ConsulLock()
        {
            m_lockInstances = new Dictionary<string, LockInstance>();
            ConsulServiceEntity serviceEntity = new ConsulServiceEntity();
            ConfigManager.Configuration.Bind("ConsulService", serviceEntity);

            if (string.IsNullOrWhiteSpace(serviceEntity.ConsulIP) || serviceEntity.ConsulPort == 0)
                throw new DealException("ConsulIP和ConsulPort不能为空。");

            m_consulClient = new ConsulClient(x => x.Address = new Uri($"http://{serviceEntity.ConsulIP}:{serviceEntity.ConsulPort}"));
        }

        bool ILock.Acquire(string key, string identity, int weight, int timeOut)
        {
            string lockKey = $"{LOCK_PREFIX}/{key}";
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
                        LockWaitTime = TimeSpan.FromMilliseconds(timeOut),
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

        void ILock.Release(string identity)
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
}
