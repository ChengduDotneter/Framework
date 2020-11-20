using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Common.DAL.Cache
{
    /// <summary>
    /// 键值缓存接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IKeyCache<T> where T : IEntity
    {
        /// <summary>
        /// 根据id同步查数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        T Get(long id, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 根据id异步查数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        Task<T> GetAsync(long id, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 事务中根据id同步查数据
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        T Get(ITransaction transaction, long id);

        /// <summary>
        /// 事务中根据id异步查数据
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<T> GetAsync(ITransaction transaction, long id);
    }

    /// <summary>
    /// 条件缓存接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConditionCache<T> where T : IEntity
    {
        /// <summary>
        /// 根据筛选条件同步匹配数据
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        IEnumerable<T> Get(Expression<Func<T, bool>> condition, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 根据筛选条件异步匹配数据
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> condition, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 事务中根据筛选条件同步匹配数据
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="condition"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        IEnumerable<T> Get(ITransaction transaction, Expression<Func<T, bool>> condition, int startIndex = 0, int count = int.MaxValue);

        /// <summary>
        /// 事务中根据筛选条件异步匹配数据
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="condition"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> GetAsync(ITransaction transaction, Expression<Func<T, bool>> condition, int startIndex = 0, int count = int.MaxValue);
    }

    /// <summary>
    /// 缓存提供接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICacheProvider<T> where T : class, IEntity, new()
    {
        /// <summary>
        /// 创建EditQuery缓存代理
        /// </summary>
        /// <param name="editQuery"></param>
        /// <returns></returns>
        IEditQuery<T> CreateEditQueryCache(IEditQuery<T> editQuery);

        /// <summary>
        /// 创建SearchQuery的KeyCache代理
        /// </summary>
        /// <param name="searchQuery"></param>
        /// <returns></returns>
        IKeyCache<T> CreateKeyCache(ISearchQuery<T> searchQuery);

        /// <summary>
        /// 创建SearchQuery的ConditionCache代理
        /// </summary>
        /// <param name="searchQuery"></param>
        /// <returns></returns>
        IConditionCache<T> CreateConditionCache(ISearchQuery<T> searchQuery);
    }

    /// <summary>
    /// 缓存接口
    /// </summary>
    internal interface ICache
    {
        /// <summary>
        /// 同步获取数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        bool TryGetValue<T>(object key, out T result);

        /// <summary>
        /// 异步获取数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        Task<bool> TryGetValueAsync<T>(object key, out T result);

        /// <summary>
        /// 设置数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        T Set<T>(object key, T value);

        /// <summary>
        /// 异步设置数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task<T> SetAsync<T>(object key, T value);

        /// <summary>
        /// 同步删除
        /// </summary>
        /// <param name="key"></param>
        void Remove(object key);

        /// <summary>
        /// 异步删除
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task RemoveAsync(object key);

        /// <summary>
        /// 同步清除缓存
        /// </summary>
        void Clear();

        /// <summary>
        /// 异步清除缓存
        /// </summary>
        Task ClearAsync();
    }
}