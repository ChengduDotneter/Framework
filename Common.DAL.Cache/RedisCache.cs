using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace Common.DAL.Cache
{
    public class RedisCache : ICache
    {
        private static ConnectionMultiplexer m_connectionMultiplexer;
        private static IDatabase m_redisClient;
        private string m_hashKey;

        static RedisCache()
        {
            ConfigurationOptions configurationOptions = new ConfigurationOptions();
            configurationOptions.EndPoints.Add(ConfigManager.Configuration["RedisEndPoint"]);
            configurationOptions.Password = ConfigManager.Configuration["RedisPassWord"];
            m_connectionMultiplexer = ConnectionMultiplexer.Connect(configurationOptions);
            m_redisClient = m_connectionMultiplexer.GetDatabase();
        }

        public RedisCache(string hashKey)
        {
            m_hashKey = hashKey;
        }

        public void Clear()
        {
            m_redisClient.KeyDelete(new RedisKey(m_hashKey));
        }

        public Task ClearAsync()
        {
            return m_redisClient.KeyDeleteAsync(new RedisKey(m_hashKey));
        }

        public void Remove(object key)
        {
            m_redisClient.HashDelete(m_hashKey, new RedisValue(key.ToString()));
        }

        public Task RemoveAsync(object key)
        {
            return m_redisClient.HashDeleteAsync(m_hashKey, new RedisValue(key.ToString()));
        }

        public T Set<T>(object key, T value)
        {
            JObject jObject = JObject.FromObject(value);
            m_redisClient.HashSet(new RedisKey(m_hashKey), new RedisValue(key.ToString()), new RedisValue(jObject.ToString()));

            return value;
        }

        public async Task<T> SetAsync<T>(object key, T value)
        {
            JObject jObject = JObject.FromObject(value);
            await m_redisClient.HashSetAsync(new RedisKey(m_hashKey), new RedisValue(key.ToString()), new RedisValue(jObject.ToString()));

            return value;
        }

        public Tuple<bool, T> TryGetValue<T>(object key)
        {
            T value = default(T);

            RedisValue redisValue = m_redisClient.HashGet(new RedisKey(m_hashKey), new RedisValue(key.ToString()));

            if (!redisValue.IsNullOrEmpty)
                value = JObject.Parse(redisValue.ToString()).ToObject<T>();

            return Tuple.Create(!redisValue.IsNullOrEmpty, value);
        }

        public async Task<Tuple<bool, T>> TryGetValueAsync<T>(object key)
        {
            T value = default(T);

            RedisValue redisValue = await m_redisClient.HashGetAsync(new RedisKey(m_hashKey), new RedisValue(key.ToString()));

            if (!redisValue.IsNullOrEmpty)
                value = JObject.Parse(redisValue.ToString()).ToObject<T>();

            return Tuple.Create(!redisValue.IsNullOrEmpty, value);
        }
    }
}
