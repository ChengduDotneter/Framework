using MongoDB.Driver;
using System.Collections.Generic;

namespace Common.DAL
{
    /// <summary>
    /// MongoDBDao的相关扩张
    /// </summary>
    internal static class MongoDBDaoExtend
    {
        /// <summary>
        /// 根据排序数组的需要获取排序后的数据集
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="findFluent"></param>
        /// <param name="queryOrderBies"></param>
        /// <returns></returns>
        internal static IFindFluent<T, T> GetSortFluent<T>(this IFindFluent<T, T> findFluent, IEnumerable<QueryOrderBy<T>> queryOrderBies = null) where T : class, IEntity, new()
        {
            if (queryOrderBies == null)
                return findFluent;

            IList<SortDefinition<T>> sortDefinitions = new List<SortDefinition<T>>();

            foreach (QueryOrderBy<T> queryOrderBy in queryOrderBies)
            {
                if (queryOrderBy.OrderByType == OrderByType.Asc)
                    sortDefinitions.Add(Builders<T>.Sort.Ascending(queryOrderBy.Expression));
                else
                    sortDefinitions.Add(Builders<T>.Sort.Descending(queryOrderBy.Expression));
            }

            return findFluent.Sort(Builders<T>.Sort.Combine(sortDefinitions));
        }

        /// <summary>
        /// 获取ID的映射数据集
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="findFluent"></param>
        /// <returns></returns>
        internal static IFindFluent<T, T> GetIDProjectFluent<T>(this IFindFluent<T, T> findFluent) where T : class, IEntity, new()
        {
            return findFluent.Project<T>(Builders<T>.Projection.Include(nameof(IEntity.ID)));
        }
    }
}
