using MongoDB.Driver;
using System.Collections.Generic;

namespace Common.DAL
{
    /// <summary>
    /// MongoDBDao的相关扩张
    /// </summary>
    public static class MongoDBDaoExtend
    {
        public static IFindFluent<T, T> GetSortFluent<T>(this IFindFluent<T, T> findFluent, IEnumerable<QueryOrderBy<T>> queryOrderBies = null) where T : class, IEntity, new()
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

        public static IFindFluent<T, T> GetIDProjectFluent<T>(this IFindFluent<T, T> findFluent) where T : class, IEntity, new()
        {
            return findFluent.Project<T>(Builders<T>.Projection.Include(nameof(IEntity.ID)));
        }
    }
}
