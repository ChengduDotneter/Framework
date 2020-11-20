using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Common.DAL.Cache
{
    internal class ConditionCache<T> : IConditionCache<T>
        where T : class, IEntity, new()
    {
        private ISearchQuery<T> m_searchQuery;
        private ICache m_cache;

        public ConditionCache(ISearchQuery<T> searchQuery, ICache cache)
        {
            m_searchQuery = searchQuery;
            m_cache = cache;
        }

        public IEnumerable<T> Get(Expression<Func<T, bool>> condition, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            string conditionKey = $"{condition.ToString<T>()}_{startIndex}_{count}";

            if (!m_cache.TryGetValue(conditionKey, out IEnumerable<T> result))
            {
                result = m_searchQuery.Search(condition, startIndex: startIndex, count: count, dbResourceContent: dbResourceContent);
                m_cache.Set(conditionKey, result);
            }

            return result;
        }

        public IEnumerable<T> Get(ITransaction transaction, Expression<Func<T, bool>> condition, int startIndex = 0, int count = int.MaxValue)
        {
            return m_searchQuery.Search(transaction, condition, startIndex: startIndex, count: count);
        }

        public async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> condition, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            string conditionKey = $"{condition.ToString<T>()}_{startIndex}_{count}";

            if (!await m_cache.TryGetValueAsync(conditionKey, out IEnumerable<T> result))
            {
                result = await m_searchQuery.SearchAsync(condition, startIndex: startIndex, count: count, dbResourceContent: dbResourceContent);
                await m_cache.SetAsync(conditionKey, result);
            }

            return result;
        }

        public Task<IEnumerable<T>> GetAsync(ITransaction transaction, Expression<Func<T, bool>> condition, int startIndex = 0, int count = int.MaxValue)
        {
            return m_searchQuery.SearchAsync(transaction, condition, startIndex: startIndex, count: count);
        }
    }
}