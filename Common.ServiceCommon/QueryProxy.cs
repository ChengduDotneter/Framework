using Common.DAL;
using Common.DAL.Cache;
using Common.Model;
using Microsoft.Extensions.DependencyInjection;
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

        /// <summary>
        /// 创建KeyCache扩展
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="searchQuery"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public static IKeyCache<T> KeyCache<T>(this ISearchQuery<T> searchQuery, IServiceProvider serviceProvider)
            where T : ViewModelBase, new()
        {
            return serviceProvider.GetRequiredService<ICacheProvider<T>>().CreateKeyCache(searchQuery);
        }

        /// <summary>
        /// 创建ConditionCache扩展
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="searchQuery"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public static IConditionCache<T> ConditionCache<T>(this ISearchQuery<T> searchQuery, IServiceProvider serviceProvider)
            where T : ViewModelBase, new()
        {
            return serviceProvider.GetRequiredService<ICacheProvider<T>>().CreateConditionCache(searchQuery);
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

        /// <summary>
        /// 创建Cache扩展
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="editQuery"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public static IEditQuery<T> Cache<T>(this IEditQuery<T> editQuery, IServiceProvider serviceProvider)
            where T : ViewModelBase, new()
        {
            return serviceProvider.GetRequiredService<ICacheProvider<T>>().CreateEditQueryCache(editQuery);
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
            if (viewModelBase.CreateUserID == 0)
                return viewModelBase.AddCreateUser(ssoUserService);
            else
                return viewModelBase.AddUpdateUser(ssoUserService);
        }

        /// <summary>
        /// 添加创建及修改操作者信息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="viewModelBase"></param>
        /// <param name="ssoUserService"></param>
        /// <returns></returns>
        public static T AddBothUser<T>(this T viewModelBase, ISSOUserService ssoUserService)
            where T : ViewModelBase, new()
        {
            SSOUserInfo ssoUserInfo = ssoUserService.GetUser();

            SetCreateUser(viewModelBase, ssoUserInfo);
            SetUpdateUser(viewModelBase, ssoUserInfo);

            return viewModelBase;
        }

        /// <summary>
        /// 添加创建操作者信息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="viewModelBase"></param>
        /// <param name="ssoUserService"></param>
        /// <returns></returns>
        public static T AddCreateUser<T>(this T viewModelBase, ISSOUserService ssoUserService)
            where T : ViewModelBase, new()
        {
            return SetCreateUser(viewModelBase, ssoUserService.GetUser());
        }

        /// <summary>
        /// 添加创建操作者信息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="viewModelBase"></param>
        /// <param name="ssoUserService"></param>
        /// <returns></returns>
        public static T AddUpdateUser<T>(this T viewModelBase, ISSOUserService ssoUserService)
            where T : ViewModelBase, new()
        {
            return SetUpdateUser(viewModelBase, ssoUserService.GetUser());
        }

        /// <summary>
        /// 设置创建操作者信息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="viewModelBase"></param>
        /// <param name="ssoUserInfo"></param>
        /// <returns></returns>
        public static T SetCreateUser<T>(T viewModelBase, SSOUserInfo ssoUserInfo)
            where T : ViewModelBase, new()
        {
            viewModelBase.CreateUserID = ssoUserInfo.ID;
            viewModelBase.CreateTime = DateTime.Now;

            return viewModelBase;
        }

        /// <summary>
        /// 设置修改操作者信息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="viewModelBase"></param>
        /// <param name="ssoUserInfo"></param>
        /// <returns></returns>
        public static T SetUpdateUser<T>(T viewModelBase, SSOUserInfo ssoUserInfo)
            where T : ViewModelBase, new()
        {
            viewModelBase.UpdateTime = DateTime.Now;
            viewModelBase.UpdateUserID = ssoUserInfo.ID;

            return viewModelBase;
        }

        public static T GenerateInitialization<T>(this T viewModelBase, ISSOUserService ssoUserService)
            where T : ViewModelBase
        {
            viewModelBase.ID = IDGenerator.NextID();
            viewModelBase.CreateTime = DateTime.Now;
            viewModelBase.CreateUserID = ssoUserService.GetUser().ID;
            viewModelBase.IsDeleted = false;

            return viewModelBase;
        }

        public static T UpdateInitialization<T>(this T viewModelBase, ISSOUserService ssoUserService)
            where T : ViewModelBase
        {
            viewModelBase.UpdateTime = DateTime.Now;
            viewModelBase.UpdateUserID = ssoUserService.GetUser().ID;
            viewModelBase.IsDeleted = false;

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

        public T Get(long id, ITransaction transaction)
        {
            return m_searchQuery.Get(id, transaction);
        }

        public T Get(long id, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.Get(id, dbResourceContent);
        }

        public async Task<T> GetAsync(long id, ITransaction transaction)
        {
            return await m_searchQuery.GetAsync(id, transaction);
        }

        public async Task<T> GetAsync(long id, IDBResourceContent dbResourceContent = null)
        {
            return await m_searchQuery.GetAsync(id, dbResourceContent);
        }

        public int Count(ITransaction transaction, Expression<Func<T, bool>> predicate = null)
        {
            return m_searchQuery.Count(transaction, predicate);
        }

        public int Count(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.Count(predicate, dbResourceContent);
        }

        public async Task<int> CountAsync(ITransaction transaction, Expression<Func<T, bool>> predicate = null)
        {
            return await m_searchQuery.CountAsync(transaction, predicate);
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
        {
            return await m_searchQuery.CountAsync(predicate, dbResourceContent);
        }

        public IEnumerable<T> Search(ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue)
        {
            IEnumerable<QueryOrderBy<T>> orderByIDDesc = new[] { new QueryOrderBy<T>(item => item.ID, OrderByType.Desc) };

            if (queryOrderBies != null)
                queryOrderBies = queryOrderBies.Concat(orderByIDDesc);
            else
                queryOrderBies = orderByIDDesc;

            return m_searchQuery.Search(transaction, predicate, queryOrderBies, startIndex, count);
        }

        public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            IEnumerable<QueryOrderBy<T>> orderByIDDesc = new[] { new QueryOrderBy<T>(item => item.ID, OrderByType.Desc) };

            if (queryOrderBies != null)
                queryOrderBies = queryOrderBies.Concat(orderByIDDesc);
            else
                queryOrderBies = orderByIDDesc;

            return m_searchQuery.Search(predicate, queryOrderBies, startIndex, count, dbResourceContent);
        }

        public async Task<IEnumerable<T>> SearchAsync(ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue)
        {
            IEnumerable<QueryOrderBy<T>> orderByIDDesc = new[] { new QueryOrderBy<T>(item => item.ID, OrderByType.Desc) };

            if (queryOrderBies != null)
                queryOrderBies = queryOrderBies.Concat(orderByIDDesc);
            else
                queryOrderBies = orderByIDDesc;

            return await m_searchQuery.SearchAsync(transaction, predicate, queryOrderBies, startIndex, count);
        }

        public async Task<IEnumerable<T>> SearchAsync(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            IEnumerable<QueryOrderBy<T>> orderByIDDesc = new[] { new QueryOrderBy<T>(item => item.ID, OrderByType.Desc) };

            if (queryOrderBies != null)
                queryOrderBies = queryOrderBies.Concat(orderByIDDesc);
            else
                queryOrderBies = orderByIDDesc;

            return await m_searchQuery.SearchAsync(predicate, queryOrderBies, startIndex, count, dbResourceContent);
        }

        public int Count<TResult>(ITransaction transaction, IQueryable<TResult> query)
        {
            return m_searchQuery.Count(transaction, query);
        }

        public int Count<TResult>(IQueryable<TResult> query, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.Count(query, dbResourceContent);
        }

        public async Task<int> CountAsync<TResult>(ITransaction transaction, IQueryable<TResult> query)
        {
            return await m_searchQuery.CountAsync(transaction, query);
        }

        public async Task<int> CountAsync<TResult>(IQueryable<TResult> query, IDBResourceContent dbResourceContent = null)
        {
            return await m_searchQuery.CountAsync(query, dbResourceContent);
        }

        public IEnumerable<TResult> Search<TResult>(ITransaction transaction, IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
        {
            return m_searchQuery.Search(transaction, query, startIndex, count);
        }

        public IEnumerable<TResult> Search<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.Search(query, startIndex, count, dbResourceContent);
        }

        public async Task<IEnumerable<TResult>> SearchAsync<TResult>(ITransaction transaction, IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
        {
            return await m_searchQuery.SearchAsync(transaction, query, startIndex, count);
        }

        public async Task<IEnumerable<TResult>> SearchAsync<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            return await m_searchQuery.SearchAsync(query, startIndex, count, dbResourceContent);
        }

        public ISearchQueryable<T> GetQueryable(ITransaction transaction)
        {
            return m_searchQuery.GetQueryable(transaction);
        }

        public ISearchQueryable<T> GetQueryable(IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.GetQueryable(dbResourceContent);
        }

        public async Task<ISearchQueryable<T>> GetQueryableAsync(ITransaction transaction)
        {
            return await m_searchQuery.GetQueryableAsync(transaction);
        }

        public async Task<ISearchQueryable<T>> GetQueryableAsync(IDBResourceContent dbResourceContent = null)
        {
            return await m_searchQuery.GetQueryableAsync(dbResourceContent);
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

        public T Get(long id, ITransaction transaction)
        {
            T data = m_searchQuery.Get(id, transaction);
            return !data?.IsDeleted ?? false ? data : null;
        }

        public T Get(long id, IDBResourceContent dbResourceContent = null)
        {
            T data = m_searchQuery.Get(id, dbResourceContent);
            return !data?.IsDeleted ?? false ? data : null;
        }

        public async Task<T> GetAsync(long id, ITransaction transaction)
        {
            T data = await m_searchQuery.GetAsync(id, transaction);
            return !data?.IsDeleted ?? false ? data : null;
        }

        public async Task<T> GetAsync(long id, IDBResourceContent dbResourceContent = null)
        {
            T data = await m_searchQuery.GetAsync(id, dbResourceContent);
            return !data?.IsDeleted ?? false ? data : null;
        }

        public int Count(ITransaction transaction, Expression<Func<T, bool>> predicate = null)
        {
            return m_searchQuery.Count(transaction, QueryProxyHelper.GetIsDeletedCondition(predicate));
        }

        public int Count(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.Count(QueryProxyHelper.GetIsDeletedCondition(predicate), dbResourceContent);
        }

        public async Task<int> CountAsync(ITransaction transaction, Expression<Func<T, bool>> predicate = null)
        {
            return await m_searchQuery.CountAsync(transaction, QueryProxyHelper.GetIsDeletedCondition(predicate));
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
        {
            return await m_searchQuery.CountAsync(QueryProxyHelper.GetIsDeletedCondition(predicate), dbResourceContent);
        }

        public IEnumerable<T> Search(ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue)
        {
            return m_searchQuery.Search(transaction, QueryProxyHelper.GetIsDeletedCondition(predicate), queryOrderBies, startIndex, count);
        }

        public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.Search(QueryProxyHelper.GetIsDeletedCondition(predicate), queryOrderBies, startIndex, count, dbResourceContent);
        }

        public async Task<IEnumerable<T>> SearchAsync(ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue)
        {
            return await m_searchQuery.SearchAsync(transaction, QueryProxyHelper.GetIsDeletedCondition(predicate), queryOrderBies, startIndex, count);
        }

        public async Task<IEnumerable<T>> SearchAsync(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            return await m_searchQuery.SearchAsync(QueryProxyHelper.GetIsDeletedCondition(predicate), queryOrderBies, startIndex, count, dbResourceContent);
        }

        public int Count<TResult>(ITransaction transaction, IQueryable<TResult> query)
        {
            return m_searchQuery.Count(transaction, query);
        }

        public int Count<TResult>(IQueryable<TResult> query, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.Count(query, dbResourceContent);
        }

        public async Task<int> CountAsync<TResult>(ITransaction transaction, IQueryable<TResult> query)
        {
            return await m_searchQuery.CountAsync(transaction, query);
        }

        public async Task<int> CountAsync<TResult>(IQueryable<TResult> query, IDBResourceContent dbResourceContent = null)
        {
            return await m_searchQuery.CountAsync(query, dbResourceContent);
        }

        public IEnumerable<TResult> Search<TResult>(ITransaction transaction, IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
        {
            return m_searchQuery.Search(transaction, query, startIndex, count);
        }

        public IEnumerable<TResult> Search<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.Search(query, startIndex, count, dbResourceContent);
        }

        public async Task<IEnumerable<TResult>> SearchAsync<TResult>(ITransaction transaction, IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
        {
            return await m_searchQuery.SearchAsync(transaction, query, startIndex, count);
        }

        public async Task<IEnumerable<TResult>> SearchAsync<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            return await m_searchQuery.SearchAsync(query, startIndex, count, dbResourceContent);
        }

        public ISearchQueryable<T> GetQueryable(ITransaction transaction)
        {
            ISearchQueryable<T> searchQuerable = m_searchQuery.GetQueryable(transaction);
            return new SearchQueryableProxy<T>(searchQuerable.Where(item => !item.IsDeleted), searchQuerable);
        }

        public ISearchQueryable<T> GetQueryable(IDBResourceContent dbResourceContent = null)
        {
            ISearchQueryable<T> searchQuerable = m_searchQuery.GetQueryable(dbResourceContent);
            return new SearchQueryableProxy<T>(searchQuerable.Where(item => !item.IsDeleted), searchQuerable);
        }

        public async Task<ISearchQueryable<T>> GetQueryableAsync(ITransaction transaction)
        {
            ISearchQueryable<T> searchQuerable = await m_searchQuery.GetQueryableAsync(transaction);
            return new SearchQueryableProxy<T>(searchQuerable.Where(item => !item.IsDeleted), searchQuerable);
        }

        public async Task<ISearchQueryable<T>> GetQueryableAsync(IDBResourceContent dbResourceContent = null)
        {
            ISearchQueryable<T> searchQuerable = await m_searchQuery.GetQueryableAsync(dbResourceContent);
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
        public ITransaction BeginTransaction(bool distributedLock = true, int weight = 0)
        {
            return m_editQuery.BeginTransaction(distributedLock, weight);
        }

        /// <summary>
        /// 开始事务
        /// </summary>
        /// <returns></returns>
        public async Task<ITransaction> BeginTransactionAsync(bool distributedLock = true, int weight = 0)
        {
            return await m_editQuery.BeginTransactionAsync(distributedLock, weight);
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