using System;
using System.Collections.Generic;
using System.Linq;
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
        /// 默认超时时间
        /// </summary>
        private const int DEFAULT_TIME_OUT = 60 * 1000;

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
        private readonly static IDictionary<string, IDictionary<string, IDistributedLock>> m_lockInstances;

        /// <summary>
        /// SessionID
        /// </summary>
        private readonly static string m_sessionID;

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
                lock (m_lockInstances)
                {
                    if (!m_lockInstances.ContainsKey(identity))
                        m_lockInstances.Add(identity, new Dictionary<string, IDistributedLock>());
                }

                lock (m_lockInstances[identity])
                {
                    if (!m_lockInstances[identity].ContainsKey(table.FullName))
                    {
                        LockOptions lockOptions = new LockOptions(table.FullName)
                        {
                            LockTryOnce = true,
                            LockWaitTime = TimeSpan.FromMilliseconds(m_timeOut),
                            Value = BitConverter.GetBytes(weight),
                            Session = m_sessionID,
                            SessionTTL = TTL
                        };

                        IDistributedLock distributedLock = m_consulClient.CreateLock(lockOptions);

                        try
                        {
                            distributedLock.Acquire(CancellationToken.None).Wait();
                            m_lockInstances[identity][table.FullName] = distributedLock;

                            return true;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return m_lockInstances[identity][table.FullName].IsHeld;
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

                IDistributedLock[] distributedLocks;

                lock (m_lockInstances)
                    distributedLocks = m_lockInstances[identity].Values.ToArray();

                for (int i = 0; i < distributedLocks.Length; i++)
                    distributedLocks[i].Release().Wait();
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

            ConsulServiceEntity serviceEntity = new ConsulServiceEntity();
            ConfigManager.Configuration.Bind("ConsulService", serviceEntity);

            if (!string.IsNullOrWhiteSpace(serviceEntity.ConsulIP) && serviceEntity.ConsulPort != 0)
                m_consulClient = new ConsulClient(x => x.Address = new Uri($"http://{serviceEntity.ConsulIP}:{serviceEntity.ConsulPort}"));

            m_lockInstances = new Dictionary<string, IDictionary<string, IDistributedLock>>();

            WriteResult<string> sessionRequest = m_consulClient.Session.Create(new SessionEntry()).Result;
            m_sessionID = sessionRequest.Response;
            m_consulClient.Session.RenewPeriodic(TTL, m_sessionID, CancellationToken.None);
        }
    }
}
