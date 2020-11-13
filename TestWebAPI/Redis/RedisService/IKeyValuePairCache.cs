using System.Threading.Tasks;

namespace TestRedis.RedisService
{
    public interface IKeyValuePairCache
    {
        const int SAVE_MILLISECONDS = 1000 * 60 * 5;

        bool KeyExists(string key);

        T GetValueByKey<T>(string key);

        void SetValueByKey<T>(string key, T Value, int saveMilliseconds = SAVE_MILLISECONDS);

        void ClearCacheByKey(string key);

        void DeleteCacheByKey(params string[] keys);

        Task<T> GetValueByKeyAsync<T>(string key);

        Task SetValueByKeyAsync<T>(string key, T Value, int saveMilliseconds = SAVE_MILLISECONDS);

        Task ClearCacheByKeyAsync(string key);

        Task DeleteCacheByKeyAsync(params string[] keys);
    }
}
