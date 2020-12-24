using System;
using System.Reflection;
using System.Threading.Tasks;
using Common.Lock;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Common.DAL.Cache
{
    internal class RedisClient
    {
        private static ConnectionMultiplexer m_connectionMultiplexer;

        internal static IServer Server { get; }

        internal static IDatabase Master { get; }

        static RedisClient()
        {
            ConfigurationOptions configurationOptions = new ConfigurationOptions();
            configurationOptions.EndPoints.Add(ConfigManager.Configuration["RedisEndPoint"]);
            configurationOptions.Password = ConfigManager.Configuration["RedisPassWord"];
            configurationOptions.AllowAdmin = true;
            m_connectionMultiplexer = ConnectionMultiplexer.Connect(configurationOptions);
            Server = m_connectionMultiplexer.GetServer(ConfigManager.Configuration["RedisEndPoint"]);
            Master = m_connectionMultiplexer.GetDatabase(Convert.ToInt32(ConfigManager.Configuration["RedisMasterDatabase"].ToString()));
        }

        internal static IDatabase GetDatabase(int database)
        {
            return m_connectionMultiplexer.GetDatabase(database);
        }
    }

    public class RedisCacheProvider<T> : ICacheProvider<T>
        where T : class, IEntity, new()
    {
        private const string TABLE_LOCK_NAME = "table_lock";
        private const string TABLE_INFO_NAME = "table_info";
        private const string TABLE_INDEX_NAME = "table_index";
        private static IDatabase m_keyCache;
        private static IDatabase m_conditionCache;

        static RedisCacheProvider()
        {
            string tableInfokName = $"{Assembly.GetEntryAssembly().GetName().Name}_{TABLE_INFO_NAME}";

            ILock @lock = LockFactory.GetRedisLock();
            string identity = IDGenerator.NextID().ToString();

            if (!@lock.AcquireMutex(TABLE_LOCK_NAME, identity, 0, 10000))
                throw new Exception("读取redis表配置信息失败。");

            try
            {
                string keyCacheName = KeyCacheNameGenerator();
                string conditionCacheName = ConditionCacheNameGenerator();
                long keyDatabaseIndex;
                long conditionDatabaseIndex;

                if (!RedisClient.Master.HashExists(new RedisKey(tableInfokName), new RedisValue(keyCacheName)))
                {
                    keyDatabaseIndex = RedisClient.Master.StringIncrement(new RedisKey(TABLE_INDEX_NAME), 1);

                    if (keyDatabaseIndex > RedisClient.Server.DatabaseCount - 1)
                        throw new Exception($"请修改redis服务器databases参数设置，当前值{RedisClient.Server.DatabaseCount}，请求值{keyDatabaseIndex}。");

                    RedisClient.Master.HashSet(new RedisKey(tableInfokName), new RedisValue(keyCacheName), new RedisValue(keyDatabaseIndex.ToString()));
                }
                else
                {
                    keyDatabaseIndex = Convert.ToInt64(RedisClient.Master.HashGet(new RedisKey(tableInfokName), new RedisValue(keyCacheName)));
                }

                if (!RedisClient.Master.HashExists(new RedisKey(tableInfokName), new RedisValue(conditionCacheName)))
                {
                    conditionDatabaseIndex = RedisClient.Master.StringIncrement(new RedisKey(TABLE_INDEX_NAME), 1);

                    if (conditionDatabaseIndex > RedisClient.Server.DatabaseCount - 1)
                        throw new Exception($"请修改redis服务器databases参数设置，当前值{RedisClient.Server.DatabaseCount}，请求值{conditionDatabaseIndex}。");

                    RedisClient.Master.HashSet(new RedisKey(tableInfokName), new RedisValue(conditionCacheName), new RedisValue(conditionDatabaseIndex.ToString()));
                }
                else
                {
                    conditionDatabaseIndex = Convert.ToInt64(RedisClient.Master.HashGet(new RedisKey(tableInfokName), new RedisValue(conditionCacheName)));
                }

                m_keyCache = RedisClient.GetDatabase((int)keyDatabaseIndex);
                m_conditionCache = RedisClient.GetDatabase((int)conditionDatabaseIndex);
            }
            finally
            {
                @lock.Release(identity);
            }
        }

        private static string KeyCacheNameGenerator()
        {
            return $"key_{typeof(T).FullName}";
        }

        private static string ConditionCacheNameGenerator()
        {
            return $"condition_{typeof(T).FullName}";
        }

        public IConditionCache<T> CreateConditionCache(ISearchQuery<T> searchQuery)
        {
            return new ConditionCache<T>(searchQuery, new RedisCache(m_conditionCache));
        }

        public IEditQuery<T> CreateEditQueryCache(IEditQuery<T> editQuery)
        {
            return new EditQueryProxy<T>(editQuery, new RedisCache(m_keyCache), new RedisCache(m_conditionCache));
        }

        public IKeyCache<T> CreateKeyCache(ISearchQuery<T> searchQuery)
        {
            return new KeyCache<T>(searchQuery, new RedisCache(m_keyCache));
        }
    }

    internal class RedisCache : ICache
    {
        private const int CACHE_EXPIRATION = 60 * 2;
        private IDatabase m_database;

        public RedisCache(IDatabase database)
        {
            m_database = database;
        }

        public void Clear()
        {
            RedisClient.Server.FlushDatabase(m_database.Database);
        }

        public Task ClearAsync()
        {
            return RedisClient.Server.FlushDatabaseAsync(m_database.Database);
        }

        public void Remove(object key)
        {
            m_database.KeyDelete(new RedisKey(key.ToString()));
        }

        public Task RemoveAsync(object key)
        {
            return m_database.KeyDeleteAsync(new RedisKey(key.ToString()));
        }

        public T Set<T>(object key, T value)
        {
            string valueString = JsonConvert.SerializeObject(value);
            m_database.StringSet(new RedisKey(key.ToString()), new RedisValue(valueString), TimeSpan.FromSeconds(CACHE_EXPIRATION));

            return value;
        }

        public async Task<T> SetAsync<T>(object key, T value)
        {
            string valueString = JsonConvert.SerializeObject(value);
            await m_database.StringSetAsync(new RedisKey(key.ToString()), new RedisValue(valueString), TimeSpan.FromSeconds(CACHE_EXPIRATION));

            return value;
        }

        public Tuple<bool, T> TryGetValue<T>(object key)
        {
            T value = default(T);

            RedisValue redisValue = m_database.StringGet(new RedisKey(key.ToString()));

            if (!redisValue.IsNullOrEmpty)
                value = JsonConvert.DeserializeObject<T>(redisValue.ToString());

            return Tuple.Create(!redisValue.IsNullOrEmpty, value);
        }

        public async Task<Tuple<bool, T>> TryGetValueAsync<T>(object key)
        {
            T value = default(T);

            RedisValue redisValue = await m_database.StringGetAsync(new RedisKey(key.ToString()));

            if (!redisValue.IsNullOrEmpty)
                value = JsonConvert.DeserializeObject<T>(redisValue.ToString());

            return Tuple.Create(!redisValue.IsNullOrEmpty, value);
        }
    }
}
