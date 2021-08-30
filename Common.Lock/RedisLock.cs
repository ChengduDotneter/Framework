using Common.Log;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Lock
{
    /// <summary>
    /// redis锁
    /// </summary>
    internal class RedisLock : ILock
    {
        private class LockInstance
        {
            private CancellationTokenSource m_cancellationTokenSource;
            public RedisValue Token { get; }//token
            public ISet<RedisKey> MutexLocks { get; }
            public ISet<RedisKey> ReadWriteLocks { get; }
            public bool Running { get; private set; }

            public LockInstance(string identity, IDatabase m_saveRedisClient)
            {
                m_cancellationTokenSource = new CancellationTokenSource();
                Token = new RedisValue(identity);
                MutexLocks = new HashSet<RedisKey>();//互斥锁
                ReadWriteLocks = new HashSet<RedisKey>();//读写锁
                Running = true;

                Task.Factory.StartNew(async () =>
                {
                    while (Running)
                    {
                        try
                        {
                            IList<Task> tasks = new List<Task>();

                            Task<RedisResult> readWriteLockTask = null;

                            lock (MutexLocks)//上互斥锁 线程独享
                            {
                                foreach (RedisKey item in MutexLocks)
                                    tasks.Add(m_saveRedisClient.LockExtendAsync(item, Token, TTL));//使用item锁住
                            }

                            lock (ReadWriteLocks)//读写锁锁住
                            {
                                if (ReadWriteLocks.Count() > 0)
                                {
                                    IList<RedisKey> evaluatParameters = new List<RedisKey>();

                                    evaluatParameters.Add((TTL.TotalMilliseconds / 1.5).ToString());
                                    evaluatParameters.Add(TTL.TotalMilliseconds.ToString());
                                    evaluatParameters.Add(ReadWriteLocks.Count().ToString());
                                    evaluatParameters.AddRange(ReadWriteLocks);

                                    readWriteLockTask = m_saveRedisClient.ScriptEvaluateAsync(SAVE_DB_LOCK_HASH, evaluatParameters.ToArray());//加锁

                                    tasks.Add(readWriteLockTask);//把task任务添加进集合
                                }
                            }

                            await Task.WhenAll(tasks);//执行

                            if (readWriteLockTask != null && readWriteLockTask.Result.ToString() != "1")
                                throw new ResourceException("读写锁续锁失败。");

                            await Task.Delay((int)TTL.TotalMilliseconds / 3, m_cancellationTokenSource.Token);
                        }
                        catch (ResourceException)
                        {
                            Close(true);
                            await LogHelperFactory.GetDefaultLogHelper().Error("LockSave", "锁未能维持成功。");
                            throw;
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }, TaskCreationOptions.LongRunning);
            }

            public void Close(bool isCancelClose)
            {
                Running = false;
                m_cancellationTokenSource.Cancel(isCancelClose);
            }
        }

        /// <summary>
        /// Lock集合
        /// </summary>
        private readonly static ConcurrentDictionary<string, LockInstance> m_lockInstances;

        /// <summary>
        /// TTL
        /// </summary>
        private readonly static TimeSpan TTL = TimeSpan.FromMilliseconds(6 * 1000);

        /// <summary>
        /// 连接工具
        /// </summary>
        private readonly static ConcurrentQueue<ConnectionMultiplexer> m_connectionMultiplexerQueue;

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

        private static int m_maxLockCount;

        static RedisLock()
        {
            m_lockInstances = new ConcurrentDictionary<string, LockInstance>();

            ConfigurationOptions configurationOptions = new ConfigurationOptions();
            configurationOptions.EndPoints.Add(ConfigManager.Configuration["RedisEndPoint"]);
            configurationOptions.Password = ConfigManager.Configuration["RedisPassWord"];

            ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(configurationOptions);

            IServer server = connectionMultiplexer.GetServer(ConfigManager.Configuration["RedisEndPoint"]);

            ACQUIRE_NREAD_NWRITE_LOCK_HASH = server.ScriptLoad(RedisLockLuaScript.ACQUIRE_NREAD_NWRITE_LOCK);
            ACQUIRE_NREAD_ONEWRITE_LOCK_HASH = server.ScriptLoad(RedisLockLuaScript.ACQUIRE_NREAD_ONEWRITE_LOCK);
            SAVE_DB_LOCK_HASH = server.ScriptLoad(RedisLockLuaScript.SAVE_DB_LOCK);
            Release_DB_LOCK_HASH = server.ScriptLoad(RedisLockLuaScript.Release_DB_LOCK);

            m_connectionMultiplexerQueue = new ConcurrentQueue<ConnectionMultiplexer>();
            m_connectionMultiplexerQueue.Enqueue(connectionMultiplexer);

            for (int i = 0; i < 10; i++)
            {
                m_connectionMultiplexerQueue.Enqueue(ConnectionMultiplexer.Connect(configurationOptions));
            }

            m_maxLockCount = Convert.ToInt32(ConfigManager.Configuration["MaxLockCount"]) == 0 ? 1000 : Convert.ToInt32(ConfigManager.Configuration["MaxLockCount"]);
        }

        private LockInstance GetOrAddLockInstance(string identity, IDatabase database)
        {
            if (!m_lockInstances.ContainsKey(identity))
            {
                lock (m_lockInstances)
                {
                    if (m_lockInstances.Count() < m_maxLockCount)
                        m_lockInstances.TryAdd(identity, new LockInstance(identity, database));
                    else
                        throw new ResourceException("锁资源已满。");
                }
            }

            return m_lockInstances[identity];
        }

        private LockInstance GetLockInstance(string identity)
        {
            if (!m_lockInstances.ContainsKey(identity))
                return null;

            return m_lockInstances[identity];
        }

        private IDatabase GetDatabase()
        {
            m_connectionMultiplexerQueue.TryDequeue(out ConnectionMultiplexer connectionMultiplexer);
            m_connectionMultiplexerQueue.Enqueue(connectionMultiplexer);
            return connectionMultiplexer.GetDatabase();
        }

        /// <summary>
        /// 互斥锁同步申请
        /// </summary>
        /// <param name="key">锁键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">权重</param>
        /// <param name="timeOut">超时时间</param>
        /// <returns></returns>
        bool ILock.AcquireMutex(string key, string identity, int weight, int timeOut)
        {
            IDatabase database = GetDatabase();
            LockInstance lockInstance = GetOrAddLockInstance(identity, database);

            try
            {
                RedisKey redisKey = new RedisKey(key);
                int time = Environment.TickCount;

                while (!database.LockTake(redisKey, lockInstance.Token, TTL))
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
        async Task<bool> ILock.AcquireMutexAsync(string key, string identity, int weight, int timeOut)
        {
            IDatabase database = GetDatabase();
            LockInstance lockInstance = GetOrAddLockInstance(identity, database);

            try
            {
                RedisKey redisKey = new RedisKey(key);
                int time = Environment.TickCount;

                while (!await database.LockTakeAsync(redisKey, lockInstance.Token, TTL))
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
        void ILock.Release(string identity)
        {
            IDatabase database = GetDatabase();
            LockInstance lockInstance = GetLockInstance(identity);

            if (lockInstance == null)
                return;

            lockInstance.Close(false);

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
                    database.LockRelease(locks[i], lockInstance.Token);
                }
                catch
                {
                    // ignored
                }
            }

            if (lockInstance.ReadWriteLocks.Count > 0)
            {
                IList<RedisKey> evaluatParameters = new List<RedisKey>();
                evaluatParameters.Add(identity);
                evaluatParameters.Add(lockInstance.ReadWriteLocks.Count().ToString());
                evaluatParameters.AddRange(lockInstance.ReadWriteLocks);

                database.ScriptEvaluate(Release_DB_LOCK_HASH, evaluatParameters.ToArray());
            }
        }

        /// <summary>
        /// 锁资源异步释放
        /// </summary>
        /// <param name="identity">锁ID</param>
        /// <returns></returns>
        async Task ILock.ReleaseAsync(string identity)
        {
            IDatabase database = GetDatabase();
            LockInstance lockInstance = GetLockInstance(identity);

            if (lockInstance == null)
                return;

            lockInstance.Close(false);

            RedisKey[] locks = lockInstance.MutexLocks.ToArray();

            IList<Task> tasks = new List<Task>();

            if (lockInstance.ReadWriteLocks.Count > 0)
            {
                IList<RedisKey> evaluatParameters = new List<RedisKey>();
                evaluatParameters.Add(identity);
                evaluatParameters.Add(lockInstance.ReadWriteLocks.Count().ToString());
                evaluatParameters.AddRange(lockInstance.ReadWriteLocks);

                tasks.Add(database.ScriptEvaluateAsync(Release_DB_LOCK_HASH, evaluatParameters.ToArray()));
            }

            for (int i = 0; i < locks.Length; i++)
                tasks.Add(database.LockReleaseAsync(locks[i], lockInstance.Token));

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
        bool ILock.AcquireReadLockWithGroupKey(string groupKey, string identity, int weight, int timeOut)
        {
            return AcquireReadWriteLockWithGroupKey(groupKey, identity, timeOut, ReadWriteLockMode.ReadLock).Result;
        }

        /// <summary>
        /// 多写多读读写锁写锁同步申请
        /// </summary>
        /// <param name="groupKey">与一读多写读写锁互斥的唯一键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <returns></returns>
        bool ILock.AcquireWriteLockWithGroupKey(string groupKey, string identity, int weight, int timeOut)
        {
            return AcquireReadWriteLockWithGroupKey(groupKey, identity, timeOut, ReadWriteLockMode.WriteLock).Result;
        }

        /// <summary>
        /// 多写多读读写锁读锁异步申请
        /// </summary>
        /// <param name="groupKey">与一读多写读写锁互斥的唯一键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <returns></returns>
        Task<bool> ILock.AcquireReadLockWithGroupKeyAsync(string groupKey, string identity, int weight, int timeOut)
        {
            return AcquireReadWriteLockWithGroupKey(groupKey, identity, timeOut, ReadWriteLockMode.ReadLock);
        }

        /// <summary>
        /// 多写多读读写锁写锁异步申请
        /// </summary>
        /// <param name="groupKey">与一读多写读写锁互斥的唯一键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <returns></returns>
        Task<bool> ILock.AcquireWriteLockWithGroupKeyAsync(string groupKey, string identity, int weight, int timeOut)
        {
            return AcquireReadWriteLockWithGroupKey(groupKey, identity, timeOut, ReadWriteLockMode.WriteLock);
        }

        private async Task<bool> AcquireReadWriteLockWithGroupKey(string groupKey, string identity, int timeOut, ReadWriteLockMode lockMode)
        {
            IDatabase database = GetDatabase();
            LockInstance lockInstance = GetOrAddLockInstance(identity, database);

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

                while ((await database.ScriptEvaluateAsync(ACQUIRE_NREAD_NWRITE_LOCK_HASH, evaluatParameters.ToArray())).ToString() != "1")
                {
                    if (Environment.TickCount - time > timeOut)
                        return false;
                    else
                        Thread.Sleep(THREAD_TIME_SPAN);
                }

                lock (lockInstance.ReadWriteLocks)
                {
                    if (lockMode == ReadWriteLockMode.WriteLock)
                        lockInstance.ReadWriteLocks.Add(writeGroupKey);
                    else if (lockMode == ReadWriteLockMode.ReadLock)
                        lockInstance.ReadWriteLocks.Add(readGroupKey);
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
        bool ILock.AcquireReadLockWithResourceKeys(string groupKey, string identity, int weight, int timeOut, params string[] resourceKeys)
        {
            return AcquireReadWriteLockWithResourceKeys(groupKey, identity, timeOut, ReadWriteLockMode.ReadLock, resourceKeys).Result;
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
        bool ILock.AcquireWriteLockWithResourceKeys(string groupKey, string identity, int weight, int timeOut, params string[] resourceKeys)
        {
            return AcquireReadWriteLockWithResourceKeys(groupKey, identity, timeOut, ReadWriteLockMode.WriteLock, resourceKeys).Result;
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
        Task<bool> ILock.AcquireReadLockWithResourceKeysAsync(string groupKey, string identity, int weight, int timeOut, params string[] resourceKeys)
        {
            return AcquireReadWriteLockWithResourceKeys(groupKey, identity, timeOut, ReadWriteLockMode.ReadLock, resourceKeys);
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
        Task<bool> ILock.AcquireWriteLockWithResourceKeysAsync(string groupKey, string identity, int weight, int timeOut, params string[] resourceKeys)
        {
            return AcquireReadWriteLockWithResourceKeys(groupKey, identity, timeOut, ReadWriteLockMode.WriteLock, resourceKeys);
        }

        private async Task<bool> AcquireReadWriteLockWithResourceKeys(string groupKey, string identity, int timeOut, ReadWriteLockMode lockMode, params string[] resourceKeys)
        {
            bool isLocked = true;

            IDatabase database = GetDatabase();
            LockInstance lockInstance = GetOrAddLockInstance(identity, database);
            IList<RedisKey> needLockedResourceKeys = new List<RedisKey>();

            string readGroupKey = GetReadReasouceKey(groupKey);
            string writeGroupKey = GetWriteReasouceKey(groupKey);

            try
            {
                int time = Environment.TickCount;

                IList<RedisKey> evaluatParameters = new List<RedisKey>();
                evaluatParameters.Add(identity);
                evaluatParameters.Add(TTL.TotalMilliseconds.ToString());
                evaluatParameters.Add(((int)lockMode).ToString());
                evaluatParameters.Add(readGroupKey);
                evaluatParameters.Add(writeGroupKey);

                if (resourceKeys != null && resourceKeys.Count() > 0)
                {
                    needLockedResourceKeys.Add(readGroupKey);
                    evaluatParameters.Add(resourceKeys.Length.ToString());

                    foreach (string item in resourceKeys)
                    {
                        if (lockMode == ReadWriteLockMode.ReadLock)
                            needLockedResourceKeys.Add(GetReadReasouceKey(groupKey, item));

                        evaluatParameters.Add(GetReadReasouceKey(groupKey, item));
                    }

                    foreach (string item in resourceKeys)
                    {
                        if (lockMode == ReadWriteLockMode.WriteLock)
                            needLockedResourceKeys.Add(GetWriteReasouceKey(groupKey, item));

                        evaluatParameters.Add(GetWriteReasouceKey(groupKey, item));
                    }
                }
                else
                {
                    evaluatParameters.Add("0");
                }

                while ((await database.ScriptEvaluateAsync(ACQUIRE_NREAD_ONEWRITE_LOCK_HASH, evaluatParameters.ToArray())).ToString() != "1")
                {
                    if (Environment.TickCount - time > timeOut)
                    {
                        isLocked = false;
                        break;
                    }
                    else
                        Thread.Sleep(THREAD_TIME_SPAN);
                }
            }
            catch
            {
                isLocked = false;
            }
            finally
            {
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
            }
            return isLocked;
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