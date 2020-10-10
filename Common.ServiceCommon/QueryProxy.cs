using Common.DAL;
using Common.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Common.ServiceCommon
{
    /// <summary>
    /// ISearchQuery扩展
    /// </summary>
    public static class ISearchQueryExtention
    {
        /// <summary>
        /// 逻辑删除扩展
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="searchQuery"></param>
        /// <returns></returns>
        public static ISearchQuery<T> FilterIsDeleted<T>(this ISearchQuery<T> searchQuery)
            where T : ViewModelBase, new()
        {
            return new FilterIsDeletedSearchQueryProxy<T>(searchQuery);
        }

        /// <summary>
        /// 根据ID排序扩展
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="searchQuery"></param>
        /// <returns></returns>
        public static ISearchQuery<T> OrderByIDDesc<T>(this ISearchQuery<T> searchQuery)
            where T : ViewModelBase, new()
        {
            return new OrderByIDDescSearchQueryProxy<T>(searchQuery);
        }
    }

    /// <summary>
    /// IEditQuery扩展
    /// </summary>
    public static class IEditQueryExtention
    {
        /// <summary>
        /// 逻辑删除扩展
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="editQuery"></param>
        /// <returns></returns>
        public static IEditQuery<T> FilterIsDeleted<T>(this IEditQuery<T> editQuery)
            where T : ViewModelBase, new()
        {
            return new FilterIsDeletedEditQueryProxy<T>(editQuery);
        }
    }

    /// <summary>
    /// ViewModelBase扩展
    /// </summary>
    public static class ViewModelBaseExtention
    {
        /// <summary>
        /// 添加操作者信息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="viewModelBase"></param>
        /// <param name="ssoUserService"></param>
        /// <returns></returns>
        public static T AddUser<T>(this T viewModelBase, ISSOUserService ssoUserService)
            where T : ViewModelBase, new()
        {
            SSOUserInfo userInfo = ssoUserService.GetUser();

            if (viewModelBase.CreateUserID == 0)
                viewModelBase.CreateUserID = userInfo.ID;
            else
            {
                viewModelBase.UpdateTime = DateTime.Now;
                viewModelBase.UpdateUserID = userInfo.ID;
            }

            return viewModelBase;
        }
    }

    /// <summary>
    /// QueryProxyHelper
    /// </summary>
    internal static class QueryProxyHelper
    {
        public static Expression<Func<T, bool>> GetIsDeletedCondition<T>(Expression<Func<T, bool>> predicate)
            where T : ViewModelBase, new()
        {
            if (predicate == null)
            {
                ParameterExpression parameter = Expression.Parameter(typeof(T));
                return Expression.Lambda<Func<T, bool>>(
                 Expression.Equal(Expression.Property(parameter, nameof(ViewModelBase.IsDeleted)), Expression.Constant(false, typeof(bool))),
                 parameter);
            }

            Expression<Func<T, bool>> expression =
                Expression.Lambda<Func<T, bool>>(
                    Expression.Equal(Expression.Property(predicate.Parameters[0], nameof(ViewModelBase.IsDeleted)), Expression.Constant(false, typeof(bool))),
                    predicate.Parameters);

            return predicate.AndAlso(expression);
        }
    }

    /// <summary>
    /// 根据ID倒序的查询代理装饰者类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class OrderByIDDescSearchQueryProxy<T> : ISearchQuery<T>
        where T : ViewModelBase, new()
    {
        private ISearchQuery<T> m_searchQuery;

        /// <summary>
        /// 根据id获取实体
        /// </summary>
        /// <param name="id"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public T Get(long id, ITransaction transaction = null)
        {
            return m_searchQuery.Get(id, transaction);
        }

        /// <summary>
        /// 根据id获取实体
        /// </summary>
        /// <param name="id"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async Task<T> GetAsync(long id, ITransaction transaction = null)
        {
            return await m_searchQuery.GetAsync(id, transaction);
        }

        /// <summary>
        /// 根据linq查询条件获取查询条数
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public int Count(Expression<Func<T, bool>> predicate = null, ITransaction transaction = null)
        {
            return m_searchQuery.Count(predicate, transaction);
        }

        /// <summary>
        /// 根据linq查询条件获取查询条数
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, ITransaction transaction = null)
        {
            return await m_searchQuery.CountAsync(predicate, transaction);
        }

        /// <summary>
        /// 根据Linq查询条件获取查询结果列表
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="queryOrderBies"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null,
                                     IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                     int startIndex = 0,
                                     int count = int.MaxValue,
                                     ITransaction transaction = null)
        {
            IEnumerable<QueryOrderBy<T>> orderByIDDesc = new[] { new QueryOrderBy<T>(item => item.ID, OrderByType.Desc) };

            if (queryOrderBies != null)
                queryOrderBies = queryOrderBies.Concat(orderByIDDesc);
            else
                queryOrderBies = orderByIDDesc;

            return m_searchQuery.Search(predicate, queryOrderBies, startIndex, count, transaction);
        }

        /// <summary>
        /// 根据Linq查询条件获取查询结果列表
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="queryOrderBies"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> SearchAsync(Expression<Func<T, bool>> predicate = null,
                                                      IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                                      int startIndex = 0,
                                                      int count = int.MaxValue,
                                                      ITransaction transaction = null)
        {
            IEnumerable<QueryOrderBy<T>> orderByIDDesc = new[] { new QueryOrderBy<T>(item => item.ID, OrderByType.Desc) };

            if (queryOrderBies != null)
                queryOrderBies = queryOrderBies.Concat(orderByIDDesc);
            else
                queryOrderBies = orderByIDDesc;

            return await m_searchQuery.SearchAsync(predicate, queryOrderBies, startIndex, count, transaction);
        }

        /// <summary>
        /// 根据Linq筛选条件两表联查条数
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public int Count<TResult>(IQueryable<TResult> query, ITransaction transaction = null)
        {
            return m_searchQuery.Count(query, transaction);
        }

        /// <summary>
        /// 根据Linq筛选条件两表联查条数
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public Task<int> CountAsync<TResult>(IQueryable<TResult> query, ITransaction transaction = null)
        {
            return m_searchQuery.CountAsync(query, transaction);
        }

        /// <summary>
        /// 根据Linq筛选条件两表联查数据
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public IEnumerable<TResult> Search<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, ITransaction transaction = null)
        {
            return m_searchQuery.Search(query, startIndex, count, transaction);
        }

        /// <summary>
        /// 根据Linq筛选条件两表联查数据
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public Task<IEnumerable<TResult>> SearchAsync<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, ITransaction transaction = null)
        {
            return m_searchQuery.SearchAsync(query, startIndex, count, transaction);
        }

        /// <summary>
        /// 获取Linq查询接口
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public ISearchQueryable<T> GetQueryable(ITransaction transaction = null)
        {
            return m_searchQuery.GetQueryable(transaction);
        }

        /// <summary>
        /// 获取Linq查询接口
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public Task<ISearchQueryable<T>> GetQueryableAsync(ITransaction transaction = null)
        {
            return m_searchQuery.GetQueryableAsync(transaction);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="searchQuery"></param>
        public OrderByIDDescSearchQueryProxy(ISearchQuery<T> searchQuery) => m_searchQuery = searchQuery;
    }

    /// <summary>
    /// 装饰者的SearchQueryable实现
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SearchQueryableProxy<T> : ISearchQueryable<T>
    {
        private readonly IQueryable<T> m_queryable;
        private readonly IDisposable m_disposable;

        /// <summary>
        /// 类型
        /// </summary>
        public Type ElementType => m_queryable.ElementType;

        /// <summary>
        /// 表达式
        /// </summary>
        public Expression Expression => m_queryable.Expression;

        /// <summary>
        /// 查询提供者
        /// </summary>
        public IQueryProvider Provider => m_queryable.Provider;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="queable"></param>
        /// <param name="disposable"></param>
        public SearchQueryableProxy(IQueryable<T> queable, IDisposable disposable)
        {
            m_disposable = disposable;
            m_queryable = queable;
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose() => m_disposable.Dispose();

        /// <summary>
        /// 获取构造器
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator() => m_queryable.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)m_queryable).GetEnumerator();
    }

    /// <summary>
    /// 逻辑删除装饰者，继承ISearchQuery
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FilterIsDeletedSearchQueryProxy<T> : ISearchQuery<T>
        where T : ViewModelBase, new()
    {
        private ISearchQuery<T> m_searchQuery;

        /// <summary>
        /// Get
        /// </summary>
        /// <param name="id"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public T Get(long id, ITransaction transaction = null)
        {
            T data = m_searchQuery.Get(id, transaction);
            return !data?.IsDeleted ?? false ? data : null;
        }

        /// <summary>
        /// Get
        /// </summary>
        /// <param name="id"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async Task<T> GetAsync(long id, ITransaction transaction = null)
        {
            T data = await m_searchQuery.GetAsync(id, transaction);
            return !data?.IsDeleted ?? false ? data : null;
        }

        /// <summary>
        /// 通过Lambda表达式获取Count
        /// </summary>
        /// <param name="predicate">Lambda表达式</param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public int Count(Expression<Func<T, bool>> predicate = null, ITransaction transaction = null)
        {
            return m_searchQuery.Count(QueryProxyHelper.GetIsDeletedCondition(predicate), transaction);
        }

        /// <summary>
        /// 通过Lambda表达式获取Count
        /// </summary>
        /// <param name="predicate">Lambda表达式</param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, ITransaction transaction = null)
        {
            return await m_searchQuery.CountAsync(QueryProxyHelper.GetIsDeletedCondition(predicate), transaction);
        }

        /// <summary>
        /// Lambda表达式分页查询
        /// </summary>
        /// <param name="predicate">Lambda表达式</param>
        /// <param name="queryOrderBies">排序方式</param>
        /// <param name="startIndex">开始位置</param>
        /// <param name="count">结果总数</param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null,
                                     IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                     int startIndex = 0,
                                     int count = int.MaxValue,
                                     ITransaction transaction = null)
        {
            return m_searchQuery.Search(QueryProxyHelper.GetIsDeletedCondition(predicate), queryOrderBies, startIndex, count, transaction);
        }

        /// <summary>
        /// Lambda表达式分页查询
        /// </summary>
        /// <param name="predicate">Lambda表达式</param>
        /// <param name="queryOrderBies">排序方式</param>
        /// <param name="startIndex">开始位置</param>
        /// <param name="count">结果总数</param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> SearchAsync(Expression<Func<T, bool>> predicate = null,
                                                      IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                                      int startIndex = 0,
                                                      int count = int.MaxValue,
                                                      ITransaction transaction = null)
        {
            return await m_searchQuery.SearchAsync(QueryProxyHelper.GetIsDeletedCondition(predicate), queryOrderBies, startIndex, count, transaction);
        }

        /// <summary>
        /// 根据Linq筛选条件两表联查条数
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public int Count<TResult>(IQueryable<TResult> query, ITransaction transaction = null)
        {
            return m_searchQuery.Count(query, transaction);
        }

        /// <summary>
        /// 根据Linq筛选条件两表联查条数
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public Task<int> CountAsync<TResult>(IQueryable<TResult> query, ITransaction transaction = null)
        {
            return m_searchQuery.CountAsync(query, transaction);
        }

        /// <summary>
        /// 根据Linq筛选条件两表联查数据
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public IEnumerable<TResult> Search<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, ITransaction transaction = null)
        {
            return m_searchQuery.Search(query, startIndex, count, transaction);
        }

        /// <summary>
        /// 根据Linq筛选条件两表联查数据
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public Task<IEnumerable<TResult>> SearchAsync<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, ITransaction transaction = null)
        {
            return m_searchQuery.SearchAsync(query, startIndex, count, transaction);
        }

        /// <summary>
        /// 获取Linq查询接口
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public ISearchQueryable<T> GetQueryable(ITransaction transaction = null)
        {
            ISearchQueryable<T> searchQuerable = m_searchQuery.GetQueryable(transaction);
            return new SearchQueryableProxy<T>(searchQuerable.Where(item => !item.IsDeleted), searchQuerable);
        }

        /// <summary>
        /// 获取Linq查询接口
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async Task<ISearchQueryable<T>> GetQueryableAsync(ITransaction transaction = null)
        {
            ISearchQueryable<T> searchQuerable = await m_searchQuery.GetQueryableAsync(transaction);
            return new SearchQueryableProxy<T>(searchQuerable.Where(item => !item.IsDeleted), searchQuerable);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="searchQuery"></param>
        public FilterIsDeletedSearchQueryProxy(ISearchQuery<T> searchQuery) => m_searchQuery = searchQuery;
    }

    /// <summary>
    /// 逻辑删除装饰者，继承IEditQuery
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FilterIsDeletedEditQueryProxy<T> : IEditQuery<T>
        where T : ViewModelBase, new()
    {
        private IEditQuery<T> m_editQuery;

        /// <summary>
        /// 开始事务
        /// </summary>
        /// <returns></returns>
        public ITransaction BeginTransaction(int weight = 0)
        {
            return m_editQuery.BeginTransaction(weight);
        }

        /// <summary>
        /// 开始事务
        /// </summary>
        /// <returns></returns>
        public async Task<ITransaction> BeginTransactionAsync(int weight = 0)
        {
            return await m_editQuery.BeginTransactionAsync(weight);
        }

        /// <summary>
        /// 逻辑删除
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="ids"></param>
        public void Delete(ITransaction transaction = null, params long[] ids)
        {
            m_editQuery.Update(item => ids.Contains(item.ID), new Dictionary<string, object>() { [nameof(ViewModelBase.IsDeleted)] = true }, transaction);
        }

        /// <summary>
        /// 逻辑删除
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="ids"></param>
        public async Task DeleteAsync(ITransaction transaction = null, params long[] ids)
        {
            await m_editQuery.UpdateAsync(item => ids.Contains(item.ID), new Dictionary<string, object>() { [nameof(ViewModelBase.IsDeleted)] = true }, transaction);
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="datas"></param>
        public void Insert(ITransaction transaction = null, params T[] datas)
        {
            m_editQuery.Insert(transaction, datas);
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="datas"></param>
        public async Task InsertAsync(ITransaction transaction = null, params T[] datas)
        {
            await m_editQuery.InsertAsync(transaction, datas);
        }

        /// <summary>
        /// 新增或更新
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="datas"></param>
        public void Merge(ITransaction transaction = null, params T[] datas)
        {
            m_editQuery.Merge(transaction, datas);
        }

        /// <summary>
        /// 新增或更新
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="datas"></param>
        public async Task MergeAsync(ITransaction transaction = null, params T[] datas)
        {
            await m_editQuery.MergeAsync(transaction, datas);
        }

        /// <summary>
        /// 通过实体类更新
        /// </summary>
        /// <param name="data">实体类</param>
        /// <param name="transaction"></param>
        public void Update(T data, ITransaction transaction = null)
        {
            m_editQuery.Update(data, transaction);
        }

        /// <summary>
        /// 通过实体类更新
        /// </summary>
        /// <param name="data">实体类</param>
        /// <param name="transaction"></param>
        public async Task UpdateAsync(T data, ITransaction transaction = null)
        {
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
            m_editQuery.Update(predicate, upateDictionary, transaction);
        }

        /// <summary>
        /// 通过Lambda表达式更新
        /// </summary>
        /// <param name="predicate">匹配体Lambda表达式</param>
        /// <param name="upateDictionary">更新的数据字典</param>
        /// <param name="transaction"></param>
        public async Task UpdateAsync(Expression<Func<T, bool>> predicate, IDictionary<string, object> upateDictionary, ITransaction transaction = null)
        {
            await m_editQuery.UpdateAsync(predicate, upateDictionary, transaction);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="editQuery"></param>
        public FilterIsDeletedEditQueryProxy(IEditQuery<T> editQuery) => m_editQuery = editQuery;
    }
}