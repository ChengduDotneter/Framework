using AspectCore.Extensions.Reflection;
using Common;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestRedis.RedisService
{
    public class RedisCache : IKeyValuePairCache
    {
        private static readonly ConnectionMultiplexer m_connectionMultiplexer;

        private const int SAVE_MILLISECONDS = 1000 * 60 * 5;

        static RedisCache()
        {
            ConfigurationOptions configurationOptions = new ConfigurationOptions();
            configurationOptions.EndPoints.Add(ConfigManager.Configuration["RedisEndPoint"]);
            configurationOptions.Password = ConfigManager.Configuration["RedisPassWord"];
            configurationOptions.ConnectTimeout = 1500000;
            configurationOptions.SyncTimeout = 15000;
            m_connectionMultiplexer = ConnectionMultiplexer.Connect(configurationOptions);
        }

        public void ClearCacheByKey(string key)
        {
            Console.WriteLine($"clear conditoinche:{key}");

            IServer server = m_connectionMultiplexer.GetServer(ConfigManager.Configuration["RedisEndPoint"]);

            IEnumerable<RedisKey> redisKeys = server.Keys(pattern: key + "*");

            if (redisKeys != null && redisKeys.Count() > 0)
                DeleteCacheByRedisKey(redisKeys.ToArray());
        }

        public Task ClearCacheByKeyAsync(string key)
        {
            return Task.Factory.StartNew(() => ClearCacheByKey(key));
        }


        public void DeleteCacheByKey(params string[] keys)
        {
            DeleteCacheByRedisKey(keys.Select(item => new RedisKey(item)).ToArray());
        }

        private void DeleteCacheByRedisKey(params RedisKey[] redisKeys)
        {
            IDatabase database = m_connectionMultiplexer.GetDatabase();

            if (redisKeys != null && redisKeys.Count() > 0)
            {
                database.KeyDelete(redisKeys);
            }
        }

        public Task DeleteCacheByKeyAsync(params string[] keys)
        {
            return Task.Factory.StartNew(() => DeleteCacheByKey(keys));
        }


        public T GetValueByKey<T>(string key)
        {
            IDatabase database = m_connectionMultiplexer.GetDatabase();

            string valueString = database.StringGet(key);

            if (!string.IsNullOrWhiteSpace(valueString))
                Console.WriteLine($"GET redis key:{key}");

            if (!string.IsNullOrWhiteSpace(valueString))
                return JsonConvert.DeserializeObject<T>(valueString);
            else return (T)typeof(T).GetDefaultValue();
        }

        public IEnumerable<T> GetValuesByKey<T>(string key)
        {
            IDatabase database = m_connectionMultiplexer.GetDatabase();

            string valueString = database.StringGet(key);

            if (!string.IsNullOrWhiteSpace(valueString))
                Console.WriteLine($"GET redis key:{key} redis value:{valueString}");

            return JsonConvert.DeserializeObject<IEnumerable<T>>(valueString);
        }

        public Task<T> GetValueByKeyAsync<T>(string key)
        {
            return Task.Factory.StartNew(() => GetValueByKey<T>(key));
        }

        public void SetValueByKey<T>(string key, T Value, int saveMilliseconds = SAVE_MILLISECONDS)
        {
            IDatabase database = m_connectionMultiplexer.GetDatabase();

            Console.WriteLine($"SET redis key:{key} redis overTime:{saveMilliseconds}");

            database.StringSet(key, JsonConvert.SerializeObject(Value), TimeSpan.FromMilliseconds(saveMilliseconds > 0 ? saveMilliseconds : SAVE_MILLISECONDS));
        }

        public Task SetValueByKeyAsync<T>(string key, T Value, int saveMilliseconds = SAVE_MILLISECONDS)
        {
            return Task.Factory.StartNew(() => 
            {
                SetValueByKey(key, Value, saveMilliseconds);
            });
        }

        public bool KeyExists(string key)
        {
            IDatabase database = m_connectionMultiplexer.GetDatabase();

            return database.KeyExists(key);
        }
    }
}
