using System.Collections.Generic;

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
        /// <returns></returns>
        T Get(long id);
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
        /// <returns></returns>
        IEnumerable<T> Get(string condition);
    }
}