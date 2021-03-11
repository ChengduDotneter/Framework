using Common.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Common.ServiceCommon
{
    /// <summary>
    /// ISearchQuery过滤装饰者基类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class FilterSearchQueryProxyBase<T> : ISearchQuery<T> where T : class, IEntity, new()
    {
        private readonly ISearchQuery<T> m_searchQuery;

        public FilterSearchQueryProxyBase(ISearchQuery<T> searchQuery)
        {
            m_searchQuery = searchQuery;
        }

        private T ModelFilter(T data)
        {
            Expression<Func<T, bool>> predicate = GetExpressionProxy();

            if (predicate != null && data != null)
                return new T[] { data }.Where(predicate.Compile()).FirstOrDefault();
            else
                return data;
        }

        /// <summary>
        /// 获取装饰者需要过滤的表达式
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        protected abstract Expression<Func<T, bool>> GetExpressionProxy(Expression<Func<T, bool>> predicate = null);

        /// <summary>
        /// 为queryable添加装饰者的过滤条件
        /// </summary>
        /// <param name="queryable"></param>
        /// <returns></returns>
        private ISearchQueryable<T> GetSearchQueryableProxy(ISearchQueryable<T> queryable)
        {
            return new SearchQueryableProxy<T>(queryable.Where(GetExpressionProxy()), queryable);
        }

        public int Count(ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false)
        {
            return m_searchQuery.Count(transaction, GetExpressionProxy(predicate), forUpdate);
        }

        public int Count<TResult>(ITransaction transaction, IQueryable<TResult> query)
        {
            return m_searchQuery.Count(transaction, query);
        }

        public Task<int> CountAsync(ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false)
        {
            return m_searchQuery.CountAsync(transaction, GetExpressionProxy(predicate), forUpdate);
        }

        public Task<int> CountAsync<TResult>(ITransaction transaction, IQueryable<TResult> query)
        {
            return m_searchQuery.CountAsync(transaction, query);
        }

        public T Get(long id, ITransaction transaction, bool forUpdate = false)
        {
            return ModelFilter(m_searchQuery.Get(id, transaction, forUpdate));
        }

        public async Task<T> GetAsync(long id, ITransaction transaction, bool forUpdate = false)
        {
            return ModelFilter(await m_searchQuery.GetAsync(id, transaction, forUpdate));
        }

        public ISearchQueryable<T> GetQueryable(ITransaction transaction)
        {
            return GetSearchQueryableProxy(m_searchQuery.GetQueryable(transaction));
        }

        public async Task<ISearchQueryable<T>> GetQueryableAsync(ITransaction transaction)
        {
            return GetSearchQueryableProxy(await m_searchQuery.GetQueryableAsync(transaction));
        }

        public IEnumerable<T> Search(ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, bool forUpdate = false)
        {
            return m_searchQuery.Search(transaction, GetExpressionProxy(predicate), queryOrderBies, startIndex, count, forUpdate);
        }

        public IEnumerable<TResult> Search<TResult>(ITransaction transaction, IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
        {
            return m_searchQuery.Search(transaction, query, startIndex, count);
        }

        public Task<IEnumerable<T>> SearchAsync(ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, bool forUpdate = false)
        {
            return m_searchQuery.SearchAsync(transaction, GetExpressionProxy(predicate), queryOrderBies, startIndex, count, forUpdate);
        }

        public Task<IEnumerable<TResult>> SearchAsync<TResult>(ITransaction transaction, IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
        {
            return m_searchQuery.SearchAsync(transaction, query, startIndex, count);
        }

        public T Get(long id, IDBResourceContent dbResourceContent = null)
        {
            return ModelFilter(m_searchQuery.Get(id, dbResourceContent));
        }

        public async Task<T> GetAsync(long id, IDBResourceContent dbResourceContent = null)
        {
            return ModelFilter(await m_searchQuery.GetAsync(id, dbResourceContent));
        }

        public int Count(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.Count(GetExpressionProxy(predicate), dbResourceContent);
        }

        public Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.CountAsync(GetExpressionProxy(predicate), dbResourceContent);
        }

        public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.Search(GetExpressionProxy(predicate), queryOrderBies, startIndex, count, dbResourceContent);
        }

        public Task<IEnumerable<T>> SearchAsync(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.SearchAsync(GetExpressionProxy(predicate), queryOrderBies, startIndex, count, dbResourceContent);
        }

        public int Count<TResult>(IQueryable<TResult> query)
        {
            return m_searchQuery.Count(query);
        }

        public Task<int> CountAsync<TResult>(IQueryable<TResult> query)
        {
            return m_searchQuery.CountAsync(query);
        }

        public IEnumerable<TResult> Search<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
        {
            return m_searchQuery.Search(query, startIndex, count);
        }

        public Task<IEnumerable<TResult>> SearchAsync<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
        {
            return m_searchQuery.SearchAsync(query, startIndex, count);
        }

        public ISearchQueryable<T> GetQueryable(IDBResourceContent dbResourceContent = null)
        {
            return GetSearchQueryableProxy(m_searchQuery.GetQueryable(dbResourceContent));
        }

        public async Task<ISearchQueryable<T>> GetQueryableAsync(IDBResourceContent dbResourceContent = null)
        {
            return GetSearchQueryableProxy(await m_searchQuery.GetQueryableAsync(dbResourceContent));
        }
    }

    /// <summary>
    /// IEditQuery过滤装饰者基类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class FilterEditQueryProxyBase<T> : IEditQuery<T> where T : class, IEntity, new()
    {
        private readonly IEditQuery<T> m_editQuery;

        public FilterEditQueryProxyBase(IEditQuery<T> editQuery)
        {
            m_editQuery = editQuery;
        }

        /// <summary>
        /// 根据过滤表达式来过滤当个实体
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private T ModelFilter(T data)
        {
            Expression<Func<T, bool>> predicate = GetFilterExpressionProxy();

            if (predicate != null && data != null)
                return new T[] { data }.Where(predicate.Compile()).FirstOrDefault();
            else
                return data;
        }

        /// <summary>
        /// 根据表达式来过滤数据
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        private T[] ModelFilter(IEnumerable<T> datas)
        {
            Expression<Func<T, bool>> predicate = GetFilterExpressionProxy();

            if (predicate != null)
                return datas.Where(predicate.Compile()).ToArray();
            else
                return datas.ToArray();
        }

        /// <summary>
        /// 获取装饰者需要过滤的表达式
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        protected abstract Expression<Func<T, bool>> GetFilterExpressionProxy(Expression<Func<T, bool>> predicate = null);

        /// <summary>
        /// 开始事务
        /// </summary>
        /// <returns></returns>
        public ITransaction BeginTransaction(bool distributedLock = true, int weight = 0)
        {
            return new LockTransaction(m_editQuery.BeginTransaction(distributedLock, weight));
        }

        /// <summary>
        /// 开始事务
        /// </summary>
        /// <returns></returns>
        public async Task<ITransaction> BeginTransactionAsync(bool distributedLock = true, int weight = 0)
        {
            return new LockTransaction(await m_editQuery.BeginTransactionAsync(distributedLock, weight));
        }

        /// <summary>
        /// 逻辑删除
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="ids"></param>
        public void Delete(ITransaction transaction = null, params long[] ids)
        {
            m_editQuery.Delete(transaction, ids);
        }

        /// <summary>
        /// 逻辑删除
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="ids"></param>
        public async Task DeleteAsync(ITransaction transaction = null, params long[] ids)
        {
            await m_editQuery.DeleteAsync(transaction, ids);
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="datas"></param>
        public void Insert(ITransaction transaction = null, params T[] datas)
        {
            m_editQuery.Insert(transaction, ModelFilter(datas));
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="datas"></param>
        public async Task InsertAsync(ITransaction transaction = null, params T[] datas)
        {
            await m_editQuery.InsertAsync(transaction, ModelFilter(datas));
        }

        /// <summary>
        /// 新增或更新
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="datas"></param>
        public void Merge(ITransaction transaction = null, params T[] datas)
        {
            m_editQuery.Merge(transaction, ModelFilter(datas));
        }

        /// <summary>
        /// 新增或更新
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="datas"></param>
        public async Task MergeAsync(ITransaction transaction = null, params T[] datas)
        {
            await m_editQuery.MergeAsync(transaction, ModelFilter(datas));
        }

        /// <summary>
        /// 通过实体类更新
        /// </summary>
        /// <param name="data">实体类</param>
        /// <param name="transaction"></param>
        public void Update(T data, ITransaction transaction = null)
        {
            data = ModelFilter(data);

            if (data != null)
                m_editQuery.Update(data, transaction);
        }

        /// <summary>
        /// 通过实体类更新
        /// </summary>
        /// <param name="data">实体类</param>
        /// <param name="transaction"></param>
        public async Task UpdateAsync(T data, ITransaction transaction = null)
        {
            data = ModelFilter(data);

            if (data != null)
                await m_editQuery.UpdateAsync(data, transaction);
        }

        /// <summary>
        /// 通过Lambda表达式更新
        /// </summary>
        /// <param name="predicate">匹配体Lambda表达式</param>
        /// <param name="upateDictionary">更新的数据字典</param>
        /// <param name="transaction"></param>
        public void Update(Expression<Func<T, bool>> predicate, IDictionary<string, object> upateDictionary, ITransaction transaction = null)
        {
            m_editQuery.Update(GetFilterExpressionProxy(predicate), upateDictionary, transaction);
        }

        /// <summary>
        /// 通过Lambda表达式更新
        /// </summary>
        /// <param name="predicate">匹配体Lambda表达式</param>
        /// <param name="upateDictionary">更新的数据字典</param>
        /// <param name="transaction"></param>
        public async Task UpdateAsync(Expression<Func<T, bool>> predicate, IDictionary<string, object> upateDictionary, ITransaction transaction = null)
        {
            await m_editQuery.UpdateAsync(GetFilterExpressionProxy(predicate), upateDictionary, transaction);
        }
    }
}