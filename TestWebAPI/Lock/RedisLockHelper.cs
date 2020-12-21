using Common;
using Common.Log;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TestWebAPI.Lock
{
    public enum LockMode
    {
        ReadLock,
        WriteLock
    }

    public class RedisLockHelper : ILockHelper
    {
        private class LockInstance
        {
            private CancellationTokenSource m_cancellationTokenSource;
            public RedisValue Token { get; }
            public ISet<RedisKey> MutexLocks { get; }
            public ISet<RedisKey> ReadWriteLocks { get; }
            public bool Running { get; private set; }

            public LockInstance(string identity)
            {
                m_cancellationTokenSource = new CancellationTokenSource();
                Token = new RedisValue(identity);
                MutexLocks = new HashSet<RedisKey>();
                ReadWriteLocks = new HashSet<RedisKey>();
                Running = true;

                Task.Factory.StartNew(async () =>
                {
                    while (Running)
                    {
                        try
                        {
                            IList<Task> tasks = new List<Task>();

                            lock (MutexLocks)
                            {
                                foreach (RedisKey item in MutexLocks)
                                    tasks.Add(m_redisClient.LockExtendAsync(item, Token, TTL));
                            }


                            await Task.WhenAll(tasks);
                            await Task.Delay((int)TTL.TotalMilliseconds / 2, m_cancellationTokenSource.Token);
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }, TaskCreationOptions.LongRunning);

                Task.Factory.StartNew(async () =>
                {
                    while (Running)
                    {
                        try
                        {
                            IList<RedisKey> evaluatParameters = new List<RedisKey>();

                            evaluatParameters.Add(identity);
                            evaluatParameters.Add((TTL.TotalMilliseconds / 1.5).ToString());
                            evaluatParameters.Add(TTL.TotalMilliseconds.ToString());

                            lock (ReadWriteLocks)
                            {
                                if (ReadWriteLocks.Count() == 0)
                                    continue;

                                evaluatParameters.Add(ReadWriteLocks.Count().ToString());

                                evaluatParameters.AddRange(ReadWriteLocks);
                            }

                            await m_redisClient.ScriptEvaluateAsync(SAVE_DB_LOCK_HASH, evaluatParameters.ToArray());

                            await Task.Delay((int)TTL.TotalMilliseconds / 2, m_cancellationTokenSource.Token);
                        }
                        catch (Exception exception)
                        {
                            LogHelperFactory.GetLog4netLogHelper().Error("RedisLockInstance", $"续锁失败，{exception.Message} {exception.StackTrace}");
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
        private readonly static TimeSpan TTL = TimeSpan.FromMilliseconds(10 * 1000);

        /// <summary>
        /// 连接工具
        /// </summary>
        private readonly static ConnectionMultiplexer m_connectionMultiplexer;

        /// <summary>
        /// Redis实例
        /// </summary>
        private readonly static IDatabase m_redisClient;

        /// <summary>
        /// 读写锁读锁建前缀
        /// </summary>
        private const string READ_LOCK_PREFIX = "read";

        /// <summary>
        /// 读写锁写锁前缀
        /// </summary>
        private const string WRITE_LOCK_PREFIX = "write";

        /// <summary>
        /// 线程等待
        /// </summary>
        private const int THREAD_TIME_SPAN = 1;

        private static byte[] ACQUIRE_NREAD_NWRITE_LOCK_HASH;

        private static byte[] ACQUIRE_NREAD_ONEWRITE_LOCK_HASH;

        private static byte[] SAVE_DB_LOCK_HASH;

        private static byte[] Release_DB_LOCK_HASH;

        static RedisLockHelper()
        {
            m_lockInstances = new ConcurrentDictionary<string, LockInstance>();

            ConfigurationOptions configurationOptions = new ConfigurationOptions();
            configurationOptions.EndPoints.Add(ConfigManager.Configuration["RedisEndPoint"]);
            configurationOptions.Password = ConfigManager.Configuration["RedisPassWord"];
            m_connectionMultiplexer = ConnectionMultiplexer.Connect(configurationOptions);
            m_redisClient = m_connectionMultiplexer.GetDatabase();

            IServer server = m_connectionMultiplexer.GetServer(ConfigManager.Configuration["RedisEndPoint"]);

            ACQUIRE_NREAD_NWRITE_LOCK_HASH = server.ScriptLoad(RedisLockLuaScript.ACQUIRE_NREAD_NWRITE_LOCK);
            ACQUIRE_NREAD_ONEWRITE_LOCK_HASH = server.ScriptLoad(RedisLockLuaScript.ACQUIRE_NREAD_ONEWRITE_LOCK);
            SAVE_DB_LOCK_HASH = server.ScriptLoad(RedisLockLuaScript.SAVE_DB_LOCK);
            Release_DB_LOCK_HASH = server.ScriptLoad(RedisLockLuaScript.Release_DB_LOCK);
        }

        /// <summary>
        /// 互斥锁同步申请
        /// </summary>
        /// <param name="key">锁键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">权重</param>
        /// <param name="timeOut">超时时间</param>
        /// <returns></returns>
        bool ILockHelper.AcquireMutex(string key, string identity, int weight, int timeOut)
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

                lock (lockInstance.MutexLocks)
                    lockInstance.MutexLocks.Add(redisKey);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 互斥锁异步申请
        /// </summary>
        /// <param name="key">锁键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">权重</param>
        /// <param name="timeOut">超时时间</param>
        /// <returns></returns>
        async Task<bool> ILockHelper.AcquireMutexAsync(string key, string identity, int weight, int timeOut)
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

                lockInstance.MutexLocks.Add(redisKey);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 锁资源同步释放
        /// </summary>
        /// <param name="identity">锁ID</param>
        void ILockHelper.Release(string identity)
        {
            if (!m_lockInstances.TryGetValue(identity, out LockInstance lockInstance))
                return;

            lockInstance.Close();

            Task.Factory.StartNew(() =>
            {
                while (!m_lockInstances.TryRemove(identity, out _))
                    Thread.Sleep(THREAD_TIME_SPAN);
            });

            RedisKey[] locks = lockInstance.MutexLocks.ToArray();

            for (int i = 0; i < locks.Length; i++)
            {
                try
                {
                    m_redisClient.LockRelease(locks[i], lockInstance.Token);
                }
                catch
                {
                    continue;
                }
            }

            if (lockInstance.ReadWriteLocks.Count > 0)
            {
                IList<RedisKey> evaluatParameters = new List<RedisKey>();
                evaluatParameters.Add(identity);
                evaluatParameters.Add(lockInstance.ReadWriteLocks.Count().ToString());
                evaluatParameters.AddRange(lockInstance.ReadWriteLocks);

                m_redisClient.ScriptEvaluate(Release_DB_LOCK_HASH, evaluatParameters.ToArray());
            }
        }

        /// <summary>
        /// 锁资源异步释放
        /// </summary>
        /// <param name="identity">锁ID</param>
        /// <returns></returns>
        async Task ILockHelper.ReleaseAsync(string identity)
        {
            if (!m_lockInstances.TryGetValue(identity, out LockInstance lockInstance))
                return;

            lockInstance.Close();

            RedisKey[] locks = lockInstance.MutexLocks.ToArray();

            IList<Task> tasks = new List<Task>();

            if (lockInstance.ReadWriteLocks.Count > 0)
            {
                IList<RedisKey> evaluatParameters = new List<RedisKey>();
                evaluatParameters.Add(identity);
                evaluatParameters.Add(lockInstance.ReadWriteLocks.Count().ToString());
                evaluatParameters.AddRange(lockInstance.ReadWriteLocks);

                tasks.Add(m_redisClient.ScriptEvaluateAsync(Release_DB_LOCK_HASH, evaluatParameters.ToArray()));
            }

            for (int i = 0; i < locks.Length; i++)
                tasks.Add(m_redisClient.LockReleaseAsync(locks[i], lockInstance.Token));

            try
            {
                await Task.WhenAll(tasks);
            }
            finally
            {
                while (!m_lockInstances.TryRemove(identity, out _))
                    await Task.Delay(THREAD_TIME_SPAN);
            }
        }

        /// <summary>
        /// 多写多读读写锁读锁同步申请
        /// </summary>
        /// <param name="groupKey">与一读多写读写锁互斥的唯一键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <returns></returns>
        bool ILockHelper.AcquireReadLockWithGroupKey(string groupKey, string identity, int weight, int timeOut)
        {
            return AcquireReadWriteLockWithGroupKey(groupKey, identity, weight, timeOut, LockMode.ReadLock).Result;
        }

        /// <summary>
        /// 多写多读读写锁写锁同步申请
        /// </summary>
        /// <param name="groupKey">与一读多写读写锁互斥的唯一键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <returns></returns>
        bool ILockHelper.AcquireWriteLockWithGroupKey(string groupKey, string identity, int weight, int timeOut)
        {
            return AcquireReadWriteLockWithGroupKey(groupKey, identity, weight, timeOut, LockMode.WriteLock).Result;
        }

        /// <summary>
        /// 多写多读读写锁读锁异步申请
        /// </summary>
        /// <param name="groupKey">与一读多写读写锁互斥的唯一键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <returns></returns>
        Task<bool> ILockHelper.AcquireReadLockWithGroupKeyAsync(string groupKey, string identity, int weight, int timeOut)
        {
            return AcquireReadWriteLockWithGroupKey(groupKey, identity, weight, timeOut, LockMode.ReadLock);
        }

        /// <summary>
        /// 多写多读读写锁写锁异步申请
        /// </summary>
        /// <param name="groupKey">与一读多写读写锁互斥的唯一键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <returns></returns>
        Task<bool> ILockHelper.AcquireWriteLockWithGroupKeyAsync(string groupKey, string identity, int weight, int timeOut)
        {
            return AcquireReadWriteLockWithGroupKey(groupKey, identity, weight, timeOut, LockMode.WriteLock);
        }

        private async Task<bool> AcquireReadWriteLockWithGroupKey(string groupKey, string identity, int weight, int timeOut, LockMode lockMode)
        {
            if (!m_lockInstances.ContainsKey(identity))
                m_lockInstances.TryAdd(identity, new LockInstance(identity));

            LockInstance lockInstance = m_lockInstances[identity];

            try
            {
                int time = Environment.TickCount;

                string readGroupKey = GetReadReasouceKey(groupKey);
                string writeGroupKey = GetWriteReasouceKey(groupKey);

                IList<RedisKey> evaluatParameters = new List<RedisKey>();
                evaluatParameters.Add(identity);
                evaluatParameters.Add(TTL.TotalMilliseconds.ToString());
                evaluatParameters.Add(readGroupKey);
                evaluatParameters.Add(writeGroupKey);
                evaluatParameters.Add(((int)lockMode).ToString());

                lock (lockInstance.ReadWriteLocks)
                {
                    if (lockMode == LockMode.WriteLock)
                        lockInstance.ReadWriteLocks.Add(writeGroupKey);
                    else if (lockMode == LockMode.ReadLock)
                        lockInstance.ReadWriteLocks.Add(readGroupKey);
                }

                while ((await m_redisClient.ScriptEvaluateAsync(ACQUIRE_NREAD_NWRITE_LOCK_HASH, evaluatParameters.ToArray())).ToString() != "1")
                {
                    if (Environment.TickCount - time > timeOut)
                        return false;
                    else
                        Thread.Sleep(THREAD_TIME_SPAN);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 一写多读读写锁读锁同步申请
        /// </summary>
        /// <param name="groupKey">与多读多写读写锁互斥的唯一键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <param name="resourceKeys">所需锁的资源</param>
        /// <returns></returns>
        bool ILockHelper.AcquireReadLockWithResourceKeys(string groupKey, string identity, int weight, int timeOut, params string[] resourceKeys)
        {
            return AcquireReadWriteLockWithResourceKeys(groupKey, identity, weight, timeOut, LockMode.ReadLock, resourceKeys).Result;
        }


        /// <summary>
        /// 一写多读读写锁写锁同步申请
        /// </summary>
        /// <param name="groupKey">与多读多写读写锁互斥的唯一键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <param name="resourceKeys">所需锁的资源</param>
        /// <returns></returns>
        bool ILockHelper.AcquireWriteLockWithResourceKeys(string groupKey, string identity, int weight, int timeOut, params string[] resourceKeys)
        {
            return AcquireReadWriteLockWithResourceKeys(groupKey, identity, weight, timeOut, LockMode.WriteLock, resourceKeys).Result;
        }

        /// <summary>
        /// 一写多读读写锁读锁异步申请
        /// </summary>
        /// <param name="groupKey">与多读多写读写锁互斥的唯一键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <param name="resourceKeys">所需锁的资源</param>
        /// <returns></returns>
        Task<bool> ILockHelper.AcquireReadLockWithResourceKeysAsync(string groupKey, string identity, int weight, int timeOut, params string[] resourceKeys)
        {
            return AcquireReadWriteLockWithResourceKeys(groupKey, identity, weight, timeOut, LockMode.ReadLock, resourceKeys);
        }

        /// <summary>
        /// 一写多读读写锁写锁异步申请
        /// </summary>
        /// <param name="groupKey">与多读多写读写锁互斥的唯一键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <param name="resourceKeys">所需锁的资源</param>
        /// <returns></returns>
        Task<bool> ILockHelper.AcquireWriteLockWithResourceKeysAsync(string groupKey, string identity, int weight, int timeOut, params string[] resourceKeys)
        {
            return AcquireReadWriteLockWithResourceKeys(groupKey, identity, weight, timeOut, LockMode.WriteLock, resourceKeys);
        }

        private async Task<bool> AcquireReadWriteLockWithResourceKeys(string groupKey, string identity, int weight, int timeOut, LockMode lockMode, params string[] resourceKeys)
        {
            if (!m_lockInstances.ContainsKey(identity))
                m_lockInstances.TryAdd(identity, new LockInstance(identity));

            LockInstance lockInstance = m_lockInstances[identity];

            try
            {
                int time = Environment.TickCount;

                string readGroupKey = GetReadReasouceKey(groupKey);
                string writeGroupKey = GetWriteReasouceKey(groupKey);

                IList<RedisKey> needLockedResourceKeys = new List<RedisKey>();
                IList<RedisKey> evaluatParameters = new List<RedisKey>();
                evaluatParameters.Add(identity);
                evaluatParameters.Add(TTL.TotalMilliseconds.ToString());
                evaluatParameters.Add(((int)lockMode).ToString());
                evaluatParameters.Add(readGroupKey);
                evaluatParameters.Add(writeGroupKey);
                evaluatParameters.Add(resourceKeys.Length.ToString());

                foreach (string item in resourceKeys)
                {
                    if (lockMode == LockMode.ReadLock)
                        needLockedResourceKeys.Add(GetReadReasouceKey(groupKey, item));

                    evaluatParameters.Add(GetReadReasouceKey(groupKey, item));
                }

                foreach (string item in resourceKeys)
                {
                    if (lockMode == LockMode.WriteLock)
                        needLockedResourceKeys.Add(GetWriteReasouceKey(groupKey, item));

                    evaluatParameters.Add(GetWriteReasouceKey(groupKey, item));
                }

                lock (lockInstance.ReadWriteLocks)
                {
                    lockInstance.ReadWriteLocks.Add(readGroupKey);

                    if (needLockedResourceKeys.Count() > 0)
                    {
                        foreach (RedisKey needLockedResourceKey in needLockedResourceKeys)
                        {
                            lockInstance.ReadWriteLocks.Add(needLockedResourceKey);
                        }
                    }
                }

                while ((await m_redisClient.ScriptEvaluateAsync(ACQUIRE_NREAD_ONEWRITE_LOCK_HASH, evaluatParameters.ToArray())).ToString() != "1")
                {
                    if (Environment.TickCount - time > timeOut)
                        return false;
                    else
                        Thread.Sleep(THREAD_TIME_SPAN);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GetReadReasouceKey(string groupKey)
        {
            return $"{READ_LOCK_PREFIX}:{groupKey}";
        }

        private string GetWriteReasouceKey(string groupKey)
        {
            return $"{WRITE_LOCK_PREFIX}:{groupKey}";
        }

        private string GetReadReasouceKey(string groupKey, string resouceName)
        {
            return $"{GetReadReasouceKey(groupKey)}:{resouceName}";
        }

        private string GetWriteReasouceKey(string groupKey, string resouceName)
        {
            return $"{GetWriteReasouceKey(groupKey)}:{resouceName}";
        }
    }
}
