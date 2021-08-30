using System;
using System.Reflection;
using System.Threading.Tasks;
using Common.Lock;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Common.DAL.Cache
{
    /// <summary>
    /// redis缓存客户端
    /// </summary>
    internal class RedisClient
    {
        private static ConnectionMultiplexer m_connectionMultiplexer;//redis连接

        internal static IServer Server { get; }//服务

        internal static IDatabase Master { get; }//redis数据库
        /// <summary>
        /// 连接redis服务器
        /// </summary>
        static RedisClient()
        {
            ConfigurationOptions configurationOptions = new ConfigurationOptions();
            configurationOptions.EndPoints.Add(ConfigManager.Configuration["RedisEndPoint"]);
            configurationOptions.Password = ConfigManager.Configuration["RedisPassWord"];
            configurationOptions.AllowAdmin = true;
            m_connectionMultiplexer = ConnectionMultiplexer.Connect(configurationOptions);
            Server = m_connectionMultiplexer.GetServer(ConfigManager.Configuration["RedisEndPoint"]);
            Master = m_connectionMultiplexer.GetDatabase(Convert.ToInt32(ConfigManager.Configuration["RedisMasterDatabase"]));
        }
        /// <summary>
        /// 根据id获取redis数据库
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        internal static IDatabase GetDatabase(int database)
        {
            return m_connectionMultiplexer.GetDatabase(database);
        }
    }
    /// <summary>
    /// redis代理
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RedisCacheProvider<T> : ICacheProvider<T>
        where T : class, IEntity, new()
    {
        private const string TABLE_LOCK_NAME = "table_lock";
        private const string TABLE_INFO_NAME = "table_info";
        private const string TABLE_INDEX_NAME = "table_index";
        private static IDatabase m_keyCache;//键值缓存
        private static IDatabase m_conditionCache;//条件缓存

        static RedisCacheProvider()//构造函数
        {
            string tableInfokName = $"{Assembly.GetEntryAssembly().GetName().Name}_{TABLE_INFO_NAME}";//当前程序在redis服务器的表名

            ILock @lock = LockFactory.GetRedisLock();//获取redis锁
            string identity = IDGenerator.NextID().ToString();//身份

            if (!@lock.AcquireMutex(TABLE_LOCK_NAME, identity, 0, 10000))//互斥锁同步申请资源
                throw new Exception("读取redis表配置信息失败。");

            try
            {
                string keyCacheName = KeyCacheNameGenerator();//键值缓存名称
                string conditionCacheName = ConditionCacheNameGenerator();//条件缓存名字
                long keyDatabaseIndex;//数据库索引
                long conditionDatabaseIndex;//数据库索引

                if (!RedisClient.Master.HashExists(new RedisKey(tableInfokName), new RedisValue(keyCacheName)))//如果不存在则创建
                {
                    keyDatabaseIndex = RedisClient.Master.StringIncrement(new RedisKey(TABLE_INDEX_NAME), 1);//返回递增后的key值

                    if (keyDatabaseIndex > RedisClient.Server.DatabaseCount - 1)
                        throw new Exception($"请修改redis服务器databases参数设置，当前值{RedisClient.Server.DatabaseCount}，请求值{keyDatabaseIndex}。");

                    RedisClient.Master.HashSet(new RedisKey(tableInfokName), new RedisValue(keyCacheName), new RedisValue(keyDatabaseIndex.ToString()));
                }
                else
                {
                    keyDatabaseIndex = Convert.ToInt64(RedisClient.Master.HashGet(new RedisKey(tableInfokName), new RedisValue(keyCacheName)));//获取数据库的索引
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

        private static string KeyCacheNameGenerator()//获取键值缓存名字
        {
            return $"key_{typeof(T).FullName}";
        }

        private static string ConditionCacheNameGenerator()//获取条件缓存名字
        {
            return $"condition_{typeof(T).FullName}";
        }
        /// <summary>
        /// 创建条件缓存接口
        /// </summary>
        /// <param name="searchQuery"></param>
        /// <returns></returns>
        public IConditionCache<T> CreateConditionCache(ISearchQuery<T> searchQuery)
        {
            return new ConditionCache<T>(searchQuery, new RedisCache(m_conditionCache));
        }
        /// <summary>
        /// 创建修改对象代理
        /// </summary>
        /// <param name="editQuery"></param>
        /// <returns></returns>
        public IEditQuery<T> CreateEditQueryCache(IEditQuery<T> editQuery)
        {
            return new EditQueryProxy<T>(editQuery, new RedisCache(m_keyCache), new RedisCache(m_conditionCache));
        }
        /// <summary>
        /// 创建键值缓存
        /// </summary>
        /// <param name="searchQuery"></param>
        /// <returns></returns>
        public IKeyCache<T> CreateKeyCache(ISearchQuery<T> searchQuery)
        {
            return new KeyCache<T>(searchQuery, new RedisCache(m_keyCache));
        }
    }
    /// <summary>
    /// redis缓存
    /// </summary>
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
