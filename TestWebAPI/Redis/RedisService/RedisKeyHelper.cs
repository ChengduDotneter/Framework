using Common;
using System;
using System.ComponentModel.DataAnnotations;

namespace TestRedis.RedisService
{
    /// <summary>
    /// 缓存存储形式
    /// </summary>
    public enum CacheSaveType
    {
        /// <summary>
        /// 键值对的缓存形式
        /// </summary>
        [Display(Name = "KY")]
        KeyCache,

        /// <summary>
        /// 查询条件的缓存形式
        /// </summary>
        [Display(Name = "CD")]
        ConditionCache
    }

    public static class RedisKeyHelper
    {
        //NodrType配置项Key值
        private const string NODE_TYPE = "NodeType";

        //RedisKey的NodeType前缀
        private const string NODE_PROFIX = "NT";

        //业务分隔符
        private const string BUSINESS_SEPARATOR = ":";

        //Key与value的分隔符
        private const string KEY_VALUE_SEPARATOR = ".";

        /// <summary>
        /// 获取RedisKey的前缀
        /// </summary>
        /// <param name="tableName">资源名</param>
        /// <param name="cacheSaveType">缓存类型</param>
        /// <returns></returns>
        public static string GetRedisKeyProfix(string tableName, CacheSaveType cacheSaveType)
        {
            string cacheSaveTypeKey = EnumHelper.GetEnumDisplayName(typeof(CacheSaveType), cacheSaveType);

            if (string.IsNullOrWhiteSpace(cacheSaveTypeKey))
                throw new NotSupportedException("不支持的缓存存取类型。");

            return $"{NODE_PROFIX}{KEY_VALUE_SEPARATOR}{ConfigManager.Configuration[NODE_TYPE]}{BUSINESS_SEPARATOR}{tableName}{BUSINESS_SEPARATOR}{cacheSaveTypeKey}";
        }

        /// <summary>
        /// 获取RedisKey
        /// </summary>
        /// <param name="tableName">资源名</param>
        /// <param name="key">唯一键</param>
        /// <param name="cacheSaveType">缓存类型</param>
        /// <param name="hasEncryption">是否加密</param>
        /// <returns></returns>
        private static string GetRedisKey(string tableName, object key, CacheSaveType cacheSaveType, bool hasEncryption = false)
        {
            string keyString = key.ToString();

            if (hasEncryption)
                keyString = MD5Encryption.GetMD5(keyString);

            return $"{GetRedisKeyProfix(tableName, cacheSaveType)}{KEY_VALUE_SEPARATOR}{keyString}";
        }

        /// <summary>
        /// 获取Redis 键值对类型的缓存Key
        /// </summary>
        /// <param name="tableName">资源名</param>
        /// <param name="key">唯一键</param>
        /// <returns></returns>
        public static string GetKeyCacheKey(string tableName, object key)
        {
            return GetRedisKey(tableName, key, CacheSaveType.KeyCache);
        }

        /// <summary>
        /// 获取Redis 查询条件类型的缓存Key
        /// </summary>
        /// <param name="tableName">资源名</param>
        /// <param name="key">唯一键</param>
        /// <returns></returns>
        public static string GetConditionCacheKey(string tableName, object key)
        {
            return GetRedisKey(tableName, key, CacheSaveType.ConditionCache, true);
        }
    }
}