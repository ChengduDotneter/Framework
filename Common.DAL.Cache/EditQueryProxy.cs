using System;
using System.Linq.Expressions;
using Microsoft.Extensions.Caching.Memory;

namespace Common.DAL.Cache
{
    public class EditQueryProxy<T> : IEditQuery<T>
         where T : class, IEntity, new()
    {
        private IEditQuery<T> m_editQuery;
        private MemoryCache m_keyMemoryCache;

        public EditQueryProxy(IEditQuery<T> editQuery)
        {
            m_editQuery = editQuery;
            m_keyMemoryCache = CacheFactory<T>.GetKeyMemoryCache();
        }

        public ITransaction BeginTransaction()
        {
            return new TransactionProxy(m_editQuery.BeginTransaction());
        }

        public void Delete(params long[] ids)
        {
            m_editQuery.Delete(ids);

            DoAction(() =>
            {
                for (int i = 0; i < ids.Length; i++)
                    m_keyMemoryCache.Remove(ids[i]);

                CacheFactory<T>.ClearConditionMemoryCache();
            });
        }

        public void Insert(params T[] datas)
        {
            m_editQuery.Insert(datas);
            DoAction(CacheFactory<T>.ClearConditionMemoryCache);
        }

        public void Merge(params T[] datas)
        {
            m_editQuery.Merge(datas);

            DoAction(() =>
            {
                for (int i = 0; i < datas.Length; i++)
                    if (m_keyMemoryCache.TryGetValue(datas[i], out T _))
                        m_keyMemoryCache.Set(datas[i].ID, datas[i]);

                CacheFactory<T>.ClearConditionMemoryCache();
            });
        }

        public void Update(T data, params string[] IgnoreColumns)
        {
            m_editQuery.Update(data, IgnoreColumns);

            DoAction(() =>
            {
                if (m_keyMemoryCache.TryGetValue(data.ID, out T _))
                    m_keyMemoryCache.Set(data.ID, data);

                CacheFactory<T>.ClearConditionMemoryCache();
            });
        }

        public void Update(Expression<Func<T, bool>> predicate, Expression<Func<T, bool>> updateExpression)
        {
            m_editQuery.Update(predicate, updateExpression);

            DoAction(() =>
            {
                CacheFactory<T>.ClearKeyMemoryCache();
                CacheFactory<T>.ClearConditionMemoryCache();
            });
        }

        private static void DoAction(Action action)
        {
            if (TransactionProxy.InTransaction)
                TransactionProxy.Instance.AddAction(action);
            else
                action.Invoke();
        }
    }
}
