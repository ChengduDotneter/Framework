using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Common.DAL.Cache
{
    /// <summary>
    /// 键值缓存接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IKeyCache<T> where T : IEntity
    {
        /// <summary>
        /// 根据id查数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        T Get(long id, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 事务中根据id查数据
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        T Get(ITransaction transaction, long id);
    }

    /// <summary>
    /// 条件缓存接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConditionCache<T> where T : IEntity
    {
        /// <summary>
        /// 根据筛选条件匹配数据
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        IEnumerable<T> Get(Expression<Func<T, bool>> condition, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 事务中根据筛选条件匹配数据
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="condition"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        IEnumerable<T> Get(ITransaction transaction, Expression<Func<T, bool>> condition, int startIndex = 0, int count = int.MaxValue);
    }
}