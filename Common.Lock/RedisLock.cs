using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using StackExchange.Redis;

namespace Common.Lock
{
    internal class RedisLock : ILock
    {
        private class LockInstance
        {
            public RedisValue Token { get; }
            public ISet<RedisKey> Locks { get; }

            public LockInstance(string identity)
            {
                Token = new RedisValue(identity);
                Locks = new HashSet<RedisKey>();
            }
        }

        /// <summary>
        /// Lock集合
        /// </summary>
        private readonly static IDictionary<string, LockInstance> m_lockInstances;

        /// <summary>
        /// TTL
        /// </summary>
        private readonly static TimeSpan TTL = TimeSpan.FromMilliseconds(2 * 1000);

        /// <summary>
        /// 连接工具
        /// </summary>
        private readonly static ConnectionMultiplexer m_connectionMultiplexer;

        /// <summary>
        /// Redis实例
        /// </summary>
        public readonly static IDatabase m_redisClient;

        /// <summary>
        /// 线程等待
        /// </summary>
        private const int THREAD_TIME_SPAN = 1;

        static RedisLock()
        {
            m_lockInstances = new Dictionary<string, LockInstance>();

            ConfigurationOptions configurationOptions = new ConfigurationOptions();
            configurationOptions.EndPoints.Add(ConfigManager.Configuration["RedisEndPoint"]);
            configurationOptions.Password = ConfigManager.Configuration["RedisPassWord"];

            m_connectionMultiplexer = ConnectionMultiplexer.Connect(configurationOptions);

            m_redisClient = m_connectionMultiplexer.GetDatabase();
        }

        bool ILock.Acquire(string key, string identity, int weight, int timeOut)
        {
            LockInstance lockInstance;

            lock (m_lockInstances)
            {
                if (!m_lockInstances.ContainsKey(identity))
                    m_lockInstances.Add(identity, new LockInstance(identity));

                lockInstance = m_lockInstances[identity];
            }

            lock (lockInstance)
            {
                if (!lockInstance.Locks.Contains(key))
                {
                    try
                    {
                        RedisKey redisKey = new RedisKey(key);
                        int time = Environment.TickCount;

                        while (!m_redisClient.LockTake(redisKey, lockInstance.Token, TTL))
                        {
                            if (Environment.TickCount - time > timeOut)
                                return false;
                            else
                                Thread.Sleep(THREAD_TIME_SPAN);
                        }

                        lockInstance.Locks.Add(redisKey);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
        }

        void ILock.Release(string identity)
        {
            if (!m_lockInstances.ContainsKey(identity))
                return;

            try
            {
                LockInstance lockInstance;
                RedisKey[] locks;

                lock (m_lockInstances)
                {
                    lockInstance = m_lockInstances[identity];
                    m_lockInstances.Remove(identity);
                }

                lock (lockInstance)
                    locks = lockInstance.Locks.ToArray();

                for (int i = 0; i < locks.Length; i++)
                    m_redisClient.LockRelease(locks[i], lockInstance.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
