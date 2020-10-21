using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Common.Lock
{
    internal class RedisLock : ILock
    {
        private class LockInstance
        {
            private CancellationTokenSource m_cancellationTokenSource;
            public RedisValue Token { get; }
            public ISet<RedisKey> Locks { get; }
            public bool Running { get; private set; }

            public LockInstance(string identity)
            {
                m_cancellationTokenSource = new CancellationTokenSource();
                Token = new RedisValue(identity);
                Locks = new HashSet<RedisKey>();
                Running = true;

                Task.Factory.StartNew(async () =>
                {
                    while (Running)
                    {
                        try
                        {
                            RedisKey[] redisKeys = Locks.ToArray();
                            Task[] tasks = new Task[redisKeys.Length];

                            for (int i = 0; i < redisKeys.Length; i++)
                                tasks[i] = m_redisClient.LockExtendAsync(redisKeys[i], Token, TTL);

                            await Task.WhenAll(tasks);
                            await Task.Delay((int)TTL.TotalMilliseconds / 2, m_cancellationTokenSource.Token);
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }, TaskCreationOptions.LongRunning);
            }

            public void Close()
            {
                Running = false;
                m_cancellationTokenSource.Cancel(false);
            }
        }

        /// <summary>
        /// Lock集合
        /// </summary>
        private readonly static ConcurrentDictionary<string, LockInstance> m_lockInstances;

        /// <summary>
        /// TTL
        /// </summary>
        private readonly static TimeSpan TTL = TimeSpan.FromMilliseconds(4 * 1000);

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
            m_lockInstances = new ConcurrentDictionary<string, LockInstance>();

            ConfigurationOptions configurationOptions = new ConfigurationOptions();
            configurationOptions.EndPoints.Add(ConfigManager.Configuration["RedisEndPoint"]);
            configurationOptions.Password = ConfigManager.Configuration["RedisPassWord"];

            m_connectionMultiplexer = ConnectionMultiplexer.Connect(configurationOptions);

            m_redisClient = m_connectionMultiplexer.GetDatabase();
        }

        bool ILock.Acquire(string key, string identity, int weight, int timeOut)
        {
            if (!m_lockInstances.ContainsKey(identity))
                m_lockInstances.TryAdd(identity, new LockInstance(identity));

            LockInstance lockInstance = m_lockInstances[identity];

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

                lock (lockInstance.Locks)
                    lockInstance.Locks.Add(redisKey);

                return true;
            }
            catch
            {
                return false;
            }
        }

        async Task<bool> ILock.AcquireAsync(string key, string identity, int weight, int timeOut)
        {
            if (!m_lockInstances.ContainsKey(identity))
                m_lockInstances.TryAdd(identity, new LockInstance(identity));

            LockInstance lockInstance = m_lockInstances[identity];

            try
            {
                RedisKey redisKey = new RedisKey(key);
                int time = Environment.TickCount;

                while (!await m_redisClient.LockTakeAsync(redisKey, lockInstance.Token, TTL))
                {
                    if (Environment.TickCount - time > timeOut)
                        return false;
                    else
                        await Task.Delay(THREAD_TIME_SPAN);
                }

                lockInstance.Locks.Add(redisKey);
                return true;
            }
            catch
            {
                return false;
            }
        }

        void ILock.Release(string identity)
        {
            if (!m_lockInstances.TryGetValue(identity, out LockInstance lockInstance))
                return;

            lockInstance.Close();

            Task.Factory.StartNew(() =>
            {
                while (!m_lockInstances.TryRemove(identity, out _))
                    Thread.Sleep(THREAD_TIME_SPAN);
            });

            RedisKey[] locks = lockInstance.Locks.ToArray();

            for (int i = 0; i < locks.Length; i++)
                m_redisClient.LockRelease(locks[i], lockInstance.Token);
        }

        async Task ILock.ReleaseAsync(string identity)
        {
            if (!m_lockInstances.TryGetValue(identity, out LockInstance lockInstance))
                return;

            lockInstance.Close();

            RedisKey[] locks = lockInstance.Locks.ToArray();

            IList<Task> tasks = new List<Task>();

            for (int i = 0; i < locks.Length; i++)
                tasks.Add(m_redisClient.LockReleaseAsync(locks[i], lockInstance.Token));

            await Task.WhenAll(tasks);

            while (!m_lockInstances.TryRemove(identity, out _))
                await Task.Delay(THREAD_TIME_SPAN);
        }
    }
}
