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

    public abstract class EditQueryProxy<T> : IEditQuery<T> where T : ViewModelBase, new()
    {
        private IEditQuery<T> m_editQuery;

        public EditQueryProxy(IEditQuery<T> editQuery)
        {
            m_editQuery = editQuery;
        }

        public virtual ITransaction BeginTransaction(bool distributedLock = true, int weight = 0)
        {
            return m_editQuery.BeginTransaction(distributedLock, weight);
        }

        public virtual Task<ITransaction> BeginTransactionAsync(bool distributedLock = true, int weight = 0)
        {
            return m_editQuery.BeginTransactionAsync(distributedLock, weight);
        }

        public virtual void Delete(string systemID, ITransaction transaction = null, params long[] ids)
        {
            m_editQuery.Delete(systemID, transaction, ids);
        }

        public virtual void Delete(ITransaction transaction = null, params long[] ids)
        {
            m_editQuery.Delete(transaction, ids);
        }

        public virtual Task DeleteAsync(string systemID, ITransaction transaction = null, params long[] ids)
        {
            return m_editQuery.DeleteAsync(systemID, transaction, ids);
        }

        public virtual Task DeleteAsync(ITransaction transaction = null, params long[] ids)
        {
            return m_editQuery.DeleteAsync(transaction, ids);
        }

        public virtual void Insert(string systemID, ITransaction transaction = null, params T[] datas)
        {
            m_editQuery.Insert(systemID, transaction, datas);
        }

        public virtual void Insert(ITransaction transaction = null, params T[] datas)
        {
            m_editQuery.Insert(transaction, datas);
        }

        public virtual Task InsertAsync(string systemID, ITransaction transaction = null, params T[] datas)
        {
            return m_editQuery.InsertAsync(systemID, transaction, datas);
        }

        public virtual Task InsertAsync(ITransaction transaction = null, params T[] datas)
        {
            return m_editQuery.InsertAsync(transaction, datas);
        }

        public virtual void Merge(string systemID, ITransaction transaction = null, params T[] datas)
        {
            m_editQuery.Merge(systemID, transaction, datas);
        }

        public virtual void Merge(ITransaction transaction = null, params T[] datas)
        {
            m_editQuery.Merge(transaction, datas);
        }

        public virtual Task MergeAsync(string systemID, ITransaction transaction = null, params T[] datas)
        {
            return m_editQuery.MergeAsync(systemID, transaction, datas);
        }

        public virtual Task MergeAsync(ITransaction transaction = null, params T[] datas)
        {
            return m_editQuery.MergeAsync(transaction, datas);
        }

        public virtual void Update(string systemID, T data, ITransaction transaction = null)
        {
            m_editQuery.Update(systemID, data, transaction);
        }

        public virtual void Update(string systemID, Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null)
        {
            m_editQuery.Update(systemID, predicate, updateDictionary, transaction);
        }

        public virtual void Update(T data, ITransaction transaction = null)
        {
            m_editQuery.Update(data, transaction);
        }

        public virtual void Update(Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null)
        {
            m_editQuery.Update(predicate, updateDictionary, transaction);
        }

        public virtual Task UpdateAsync(string systemID, T data, ITransaction transaction = null)
        {
            return m_editQuery.UpdateAsync(systemID, data, transaction);
        }

        public virtual Task UpdateAsync(string systemID, Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null)
        {
            return m_editQuery.UpdateAsync(systemID, predicate, updateDictionary, transaction);
        }

        public virtual Task UpdateAsync(T data, ITransaction transaction = null)
        {
            return m_editQuery.UpdateAsync(data, transaction);
        }

        public virtual Task UpdateAsync(Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null)
        {
            return m_editQuery.UpdateAsync(predicate, updateDictionary, transaction);
        }
    }

    public abstract class SearchQueryProxy<T> : ISearchQuery<T> where T : ViewModelBase, new()
    {
        private ISearchQuery<T> m_searchQuery;

        public SearchQueryProxy(ISearchQuery<T> searchQuery)
        {
            m_searchQuery = searchQuery;
        }

        public virtual int Count(string systemID, ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false)
        {
            return m_searchQuery.Count(systemID, transaction, predicate, forUpdate);
        }

        public virtual int Count(string systemID, Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.Count(systemID, predicate, dbResourceContent);
        }

        public virtual int Count(ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false)
        {
            return m_searchQuery.Count(transaction, predicate, forUpdate);
        }

        public virtual int Count(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.Count(predicate, dbResourceContent);
        }

        public virtual int Count<TResult>(ITransaction transaction, IQueryable<TResult> query)
        {
            return m_searchQuery.Count(transaction, query);
        }

        public virtual int Count<TResult>(IQueryable<TResult> query)
        {
            return m_searchQuery.Count(query);
        }

        public virtual Task<int> CountAsync(string systemID, ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false)
        {
            return m_searchQuery.CountAsync(systemID, transaction, predicate, forUpdate);
        }

        public virtual Task<int> CountAsync(string systemID, Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.CountAsync(systemID, predicate, dbResourceContent);
        }

        public virtual Task<int> CountAsync(ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false)
        {
            return m_searchQuery.CountAsync(transaction, predicate, forUpdate);
        }

        public virtual Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.CountAsync(predicate, dbResourceContent);
        }

        public virtual Task<int> CountAsync<TResult>(ITransaction transaction, IQueryable<TResult> query)
        {
            return m_searchQuery.CountAsync(transaction, query);
        }

        public virtual Task<int> CountAsync<TResult>(IQueryable<TResult> query)
        {
            return m_searchQuery.CountAsync(query);
        }

        public virtual T Get(string systemID, long id, ITransaction transaction, bool forUpdate = false)
        {
            return m_searchQuery.Get(systemID, id, transaction, forUpdate);
        }

        public virtual T Get(string systemID, long id, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.Get(systemID, id, dbResourceContent);
        }

        public virtual T Get(long id, ITransaction transaction, bool forUpdate = false)
        {
            return m_searchQuery.Get(id, transaction, forUpdate);
        }

        public virtual T Get(long id, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.Get(id, dbResourceContent);
        }

        public virtual Task<T> GetAsync(string systemID, long id, ITransaction transaction, bool forUpdate = false)
        {
            return m_searchQuery.GetAsync(systemID, id, transaction, forUpdate);
        }

        public virtual Task<T> GetAsync(string systemID, long id, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.GetAsync(systemID, id, dbResourceContent);
        }

        public virtual Task<T> GetAsync(long id, ITransaction transaction, bool forUpdate = false)
        {
            return m_searchQuery.GetAsync(id, transaction, forUpdate);
        }

        public virtual Task<T> GetAsync(long id, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.GetAsync(id, dbResourceContent);
        }

        public virtual ISearchQueryable<T> GetQueryable(string systemID, ITransaction transaction)
        {
            return m_searchQuery.GetQueryable(systemID, transaction);
        }

        public virtual ISearchQueryable<T> GetQueryable(string systemID, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.GetQueryable(systemID, dbResourceContent);
        }

        public virtual ISearchQueryable<T> GetQueryable(ITransaction transaction)
        {
            return m_searchQuery.GetQueryable(transaction);
        }

        public virtual ISearchQueryable<T> GetQueryable(IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.GetQueryable(dbResourceContent);
        }

        public virtual Task<ISearchQueryable<T>> GetQueryableAsync(string systemID, ITransaction transaction)
        {
            return m_searchQuery.GetQueryableAsync(systemID, transaction);
        }

        public virtual Task<ISearchQueryable<T>> GetQueryableAsync(string systemID, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.GetQueryableAsync(systemID, dbResourceContent);
        }

        public virtual Task<ISearchQueryable<T>> GetQueryableAsync(ITransaction transaction)
        {
            return m_searchQuery.GetQueryableAsync(transaction);
        }

        public virtual Task<ISearchQueryable<T>> GetQueryableAsync(IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.GetQueryableAsync(dbResourceContent);
        }

        public virtual IEnumerable<T> Search(string systemID, ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, bool forUpdate = false)
        {
            return m_searchQuery.Search(systemID, transaction, predicate, queryOrderBies, startIndex, count, forUpdate);
        }

        public virtual IEnumerable<T> Search(string systemID, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.Search(systemID, predicate, queryOrderBies, startIndex, count, dbResourceContent);
        }

        public virtual IEnumerable<T> Search(ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, bool forUpdate = false)
        {
            return m_searchQuery.Search(transaction, predicate, queryOrderBies, startIndex, count, forUpdate);
        }

        public virtual IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.Search(predicate, queryOrderBies, startIndex, count, dbResourceContent);
        }

        public virtual IEnumerable<TResult> Search<TResult>(ITransaction transaction, IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
        {
            return m_searchQuery.Search(transaction, query, startIndex, count);
        }

        public virtual IEnumerable<TResult> Search<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
        {
            return m_searchQuery.Search(query, startIndex, count);
        }

        public virtual Task<IEnumerable<T>> SearchAsync(string systemID, ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, bool forUpdate = false)
        {
            return m_searchQuery.SearchAsync(systemID, transaction, predicate, queryOrderBies, startIndex, count, forUpdate);
        }

        public virtual Task<IEnumerable<T>> SearchAsync(string systemID, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.SearchAsync(systemID, predicate, queryOrderBies, startIndex, count, dbResourceContent);
        }

        public virtual Task<IEnumerable<T>> SearchAsync(ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, bool forUpdate = false)
        {
            return m_searchQuery.SearchAsync(transaction, predicate, queryOrderBies, startIndex, count, forUpdate);
        }

        public virtual Task<IEnumerable<T>> SearchAsync(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.SearchAsync(predicate, queryOrderBies, startIndex, count, dbResourceContent);
        }

        public virtual Task<IEnumerable<TResult>> SearchAsync<TResult>(ITransaction transaction, IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
        {
            return m_searchQuery.SearchAsync(transaction, query, startIndex, count);
        }

        public virtual Task<IEnumerable<TResult>> SearchAsync<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
        {
            return m_searchQuery.SearchAsync(query, startIndex, count);
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
    public class OrderByIDDescSearchQueryProxy<T> : SearchQueryProxy<T>
        where T : ViewModelBase, new()
    {
        private ISearchQuery<T> m_searchQuery;

        public override IEnumerable<T> Search(string systemID, ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, bool forUpdate = false)
        {
            IEnumerable<QueryOrderBy<T>> orderByIDDesc = new[] { new QueryOrderBy<T>(item => item.ID, OrderByType.Desc) };

            if (queryOrderBies != null)
                queryOrderBies = queryOrderBies.Concat(orderByIDDesc);
            else
                queryOrderBies = orderByIDDesc;

            return m_searchQuery.Search(systemID, transaction, predicate, queryOrderBies, startIndex, count, forUpdate);
        }

        public override IEnumerable<T> Search(string systemID, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            IEnumerable<QueryOrderBy<T>> orderByIDDesc = new[] { new QueryOrderBy<T>(item => item.ID, OrderByType.Desc) };

            if (queryOrderBies != null)
                queryOrderBies = queryOrderBies.Concat(orderByIDDesc);
            else
                queryOrderBies = orderByIDDesc;

            return m_searchQuery.Search(systemID, predicate, queryOrderBies, startIndex, count, dbResourceContent);
        }

        public override async Task<IEnumerable<T>> SearchAsync(string systemID, ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, bool forUpdate = false)
        {
            IEnumerable<QueryOrderBy<T>> orderByIDDesc = new[] { new QueryOrderBy<T>(item => item.ID, OrderByType.Desc) };

            if (queryOrderBies != null)
                queryOrderBies = queryOrderBies.Concat(orderByIDDesc);
            else
                queryOrderBies = orderByIDDesc;

            return await m_searchQuery.SearchAsync(systemID, transaction, predicate, queryOrderBies, startIndex, count, forUpdate);
        }

        public override async Task<IEnumerable<T>> SearchAsync(string systemID, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            IEnumerable<QueryOrderBy<T>> orderByIDDesc = new[] { new QueryOrderBy<T>(item => item.ID, OrderByType.Desc) };

            if (queryOrderBies != null)
                queryOrderBies = queryOrderBies.Concat(orderByIDDesc);
            else
                queryOrderBies = orderByIDDesc;

            return await m_searchQuery.SearchAsync(systemID, predicate, queryOrderBies, startIndex, count, dbResourceContent);
        }

        public OrderByIDDescSearchQueryProxy(ISearchQuery<T> searchQuery) : base(searchQuery)
        {
            m_searchQuery = searchQuery;
        }
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
    public class FilterIsDeletedSearchQueryProxy<T> : SearchQueryProxy<T>
        where T : ViewModelBase, new()
    {
        private ISearchQuery<T> m_searchQuery;

        public override T Get(string systemID, long id, ITransaction transaction, bool forUpdate = false)
        {
            T data = m_searchQuery.Get(systemID, id, transaction, forUpdate);
            return !data?.IsDeleted ?? false ? data : null;
        }

        public override T Get(string systemID, long id, IDBResourceContent dbResourceContent = null)
        {
            T data = m_searchQuery.Get(systemID, id, dbResourceContent);
            return !data?.IsDeleted ?? false ? data : null;
        }

        public override async Task<T> GetAsync(string systemID, long id, ITransaction transaction, bool forUpdate = false)
        {
            T data = await m_searchQuery.GetAsync(systemID, id, transaction, forUpdate);
            return !data?.IsDeleted ?? false ? data : null;
        }

        public override async Task<T> GetAsync(string systemID, long id, IDBResourceContent dbResourceContent = null)
        {
            T data = await m_searchQuery.GetAsync(systemID, id, dbResourceContent);
            return !data?.IsDeleted ?? false ? data : null;
        }

        public override int Count(string systemID, ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false)
        {
            return m_searchQuery.Count(systemID, transaction, QueryProxyHelper.GetIsDeletedCondition(predicate), forUpdate);
        }

        public override int Count(string systemID, Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.Count(systemID, QueryProxyHelper.GetIsDeletedCondition(predicate), dbResourceContent);
        }

        public override async Task<int> CountAsync(string systemID, ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false)
        {
            return await m_searchQuery.CountAsync(systemID, transaction, QueryProxyHelper.GetIsDeletedCondition(predicate), forUpdate);
        }

        public override async Task<int> CountAsync(string systemID, Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
        {
            return await m_searchQuery.CountAsync(systemID, QueryProxyHelper.GetIsDeletedCondition(predicate), dbResourceContent);
        }

        public override IEnumerable<T> Search(string systemID, ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, bool forUpdate = false)
        {
            return m_searchQuery.Search(systemID, transaction, QueryProxyHelper.GetIsDeletedCondition(predicate), queryOrderBies, startIndex, count, forUpdate);
        }

        public override IEnumerable<T> Search(string systemID, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.Search(systemID, QueryProxyHelper.GetIsDeletedCondition(predicate), queryOrderBies, startIndex, count, dbResourceContent);
        }

        public override async Task<IEnumerable<T>> SearchAsync(string systemID, ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, bool forUpdate = false)
        {
            return await m_searchQuery.SearchAsync(systemID, transaction, QueryProxyHelper.GetIsDeletedCondition(predicate), queryOrderBies, startIndex, count, forUpdate);
        }

        public override async Task<IEnumerable<T>> SearchAsync(string systemID, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            return await m_searchQuery.SearchAsync(systemID, QueryProxyHelper.GetIsDeletedCondition(predicate), queryOrderBies, startIndex, count, dbResourceContent);
        }

        public override ISearchQueryable<T> GetQueryable(string systemID, ITransaction transaction)
        {
            ISearchQueryable<T> searchQuerable = m_searchQuery.GetQueryable(systemID, transaction);
            return new SearchQueryableProxy<T>(searchQuerable.Where(item => !item.IsDeleted), searchQuerable);
        }

        public override ISearchQueryable<T> GetQueryable(string systemID, IDBResourceContent dbResourceContent = null)
        {
            ISearchQueryable<T> searchQuerable = m_searchQuery.GetQueryable(systemID, dbResourceContent);
            return new SearchQueryableProxy<T>(searchQuerable.Where(item => !item.IsDeleted), searchQuerable);
        }

        public override async Task<ISearchQueryable<T>> GetQueryableAsync(string systemID, ITransaction transaction)
        {
            ISearchQueryable<T> searchQuerable = await m_searchQuery.GetQueryableAsync(systemID, transaction);
            return new SearchQueryableProxy<T>(searchQuerable.Where(item => !item.IsDeleted), searchQuerable);
        }

        public override async Task<ISearchQueryable<T>> GetQueryableAsync(string systemID, IDBResourceContent dbResourceContent = null)
        {
            ISearchQueryable<T> searchQuerable = await m_searchQuery.GetQueryableAsync(systemID, dbResourceContent);
            return new SearchQueryableProxy<T>(searchQuerable.Where(item => !item.IsDeleted), searchQuerable);
        }

        public FilterIsDeletedSearchQueryProxy(ISearchQuery<T> searchQuery) : base(searchQuery) => m_searchQuery = searchQuery;
    }

    /// <summary>
    /// 逻辑删除装饰者
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FilterIsDeletedEditQueryProxy<T> : EditQueryProxy<T>
        where T : ViewModelBase, new()
    {
        private IEditQuery<T> m_editQuery;

        public FilterIsDeletedEditQueryProxy(IEditQuery<T> editQuery) : base(editQuery)
        {
            m_editQuery = editQuery;
        }

        /// <summary>
        /// 逻辑删除
        /// </summary>
        /// <param name="systemID"></param>
        /// <param name="transaction"></param>
        /// <param name="ids"></param>
        public override void Delete(string systemID, ITransaction transaction = null, params long[] ids)
        {
            m_editQuery.Update(systemID, item => ids.Contains(item.ID), new Dictionary<string, object>() { [nameof(ViewModelBase.IsDeleted)] = true }, transaction);
        }

        /// <summary>
        /// 逻辑删除
        /// </summary>
        /// <param name="systemID"></param>
        /// <param name="transaction"></param>
        /// <param name="ids"></param>
        public override async Task DeleteAsync(string systemID, ITransaction transaction = null, params long[] ids)
        {
            await m_editQuery.UpdateAsync(item => ids.Contains(item.ID), new Dictionary<string, object>() { [nameof(ViewModelBase.IsDeleted)] = true }, transaction);
        }
    }

    //public static class QueryProxy
    //{
    //    public static ISearchQuery<T> SplitBySystemID<T>(this ISearchQuery<T> searchQuery, string systemID) where T : class, IEntity, new()
    //    {
    //        return new SplitBySystemIDSearchQuery<T>(searchQuery);
    //    }

    //    public static IEditQuery<T> SplitBySystemID<T>(this IEditQuery<T> editQuery, string systemID) where T : class, IEntity, new()
    //    {
    //        return new SplitBySystemIDEditQuery<T>(editQuery);
    //    }
    //}

    //internal class SplitBySystemIDSearchQuery<T> : ISearchQuery<T> where T : class, IEntity, new()
    //{
    //    private ISearchQuery<T> m_searchQuery;

    //    public SplitBySystemIDSearchQuery(ISearchQuery<T> searchQuery)
    //    {
    //        m_searchQuery = searchQuery;
    //    }

    //    public int Count(ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false)
    //    {
    //        return m_searchQuery.Count(transaction, predicate, forUpdate);
    //    }

    //    public int Count(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
    //    {
    //        return m_searchQuery.Count(predicate, dbResourceContent);
    //    }

    //    public int Count<TResult>(ITransaction transaction, IQueryable<TResult> query)
    //    {
    //        return m_searchQuery.Count(transaction, query);
    //    }

    //    public int Count<TResult>(IQueryable<TResult> query)
    //    {
    //        return m_searchQuery.Count(query);
    //    }

    //    public Task<int> CountAsync(ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false)
    //    {
    //        return m_searchQuery.CountAsync(transaction, predicate, forUpdate);
    //    }

    //    public Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
    //    {
    //        return m_searchQuery.CountAsync(predicate, dbResourceContent);
    //    }

    //    public Task<int> CountAsync<TResult>(ITransaction transaction, IQueryable<TResult> query)
    //    {
    //        return m_searchQuery.CountAsync(transaction, query);
    //    }

    //    public Task<int> CountAsync<TResult>(IQueryable<TResult> query)
    //    {
    //        return m_searchQuery.CountAsync(query);
    //    }

    //    public T Get(long id, ITransaction transaction, bool forUpdate = false)
    //    {
    //        return m_searchQuery.Get(id, transaction, forUpdate);
    //    }

    //    public T Get(long id, IDBResourceContent dbResourceContent = null)
    //    {
    //        return m_searchQuery.Get(id, dbResourceContent);
    //    }

    //    public Task<T> GetAsync(long id, ITransaction transaction, bool forUpdate = false)
    //    {
    //        return m_searchQuery.GetAsync(id, transaction, forUpdate);
    //    }

    //    public Task<T> GetAsync(long id, IDBResourceContent dbResourceContent = null)
    //    {
    //        return m_searchQuery.GetAsync(id, dbResourceContent);
    //    }

    //    public ISearchQueryable<T> GetQueryable(ITransaction transaction)
    //    {
    //        return m_searchQuery.GetQueryable(transaction);
    //    }

    //    public ISearchQueryable<T> GetQueryable(IDBResourceContent dbResourceContent = null)
    //    {
    //        return m_searchQuery.GetQueryable(dbResourceContent);
    //    }

    //    public Task<ISearchQueryable<T>> GetQueryableAsync(ITransaction transaction)
    //    {
    //        return m_searchQuery.GetQueryableAsync(transaction);
    //    }

    //    public Task<ISearchQueryable<T>> GetQueryableAsync(IDBResourceContent dbResourceContent = null)
    //    {
    //        return m_searchQuery.GetQueryableAsync(dbResourceContent);
    //    }

    //    public IEnumerable<T> Search(ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, bool forUpdate = false)
    //    {
    //        return m_searchQuery.Search(transaction, predicate, queryOrderBies, startIndex, count, forUpdate);
    //    }

    //    public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
    //    {
    //        return m_searchQuery.Search(predicate, queryOrderBies, startIndex, count, dbResourceContent);
    //    }

    //    public IEnumerable<TResult> Search<TResult>(ITransaction transaction, IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
    //    {
    //        return m_searchQuery.Search(transaction, query, startIndex, count);
    //    }

    //    public IEnumerable<TResult> Search<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
    //    {
    //        return m_searchQuery.Search(query, startIndex, count);
    //    }

    //    public Task<IEnumerable<T>> SearchAsync(ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, bool forUpdate = false)
    //    {
    //        return m_searchQuery.SearchAsync(transaction, predicate, queryOrderBies, startIndex, count, forUpdate);
    //    }

    //    public Task<IEnumerable<T>> SearchAsync(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
    //    {
    //        return m_searchQuery.SearchAsync(predicate, queryOrderBies, startIndex, count, dbResourceContent);
    //    }

    //    public Task<IEnumerable<TResult>> SearchAsync<TResult>(ITransaction transaction, IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
    //    {
    //        return m_searchQuery.SearchAsync(transaction, query, startIndex, count);
    //    }

    //    public Task<IEnumerable<TResult>> SearchAsync<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
    //    {
    //        return m_searchQuery.SearchAsync(query, startIndex, count);
    //    }
    //}

    //internal class SplitBySystemIDEditQuery<T> : IEditQuery<T> where T : class, IEntity, new()
    //{
    //    private IEditQuery<T> m_editQuery;

    //    public SplitBySystemIDEditQuery(IEditQuery<T> editQuery)
    //    {
    //        m_editQuery = editQuery;
    //    }

    //    public ITransaction BeginTransaction(bool distributedLock = true, int weight = 0)
    //    {
    //        return m_editQuery.BeginTransaction(distributedLock, weight);
    //    }

    //    public Task<ITransaction> BeginTransactionAsync(bool distributedLock = true, int weight = 0)
    //    {
    //        return m_editQuery.BeginTransactionAsync(distributedLock, weight);
    //    }

    //    public void Delete(ITransaction transaction = null, params long[] ids)
    //    {
    //        m_editQuery.Delete(transaction, ids);
    //    }

    //    public Task DeleteAsync(ITransaction transaction = null, params long[] ids)
    //    {
    //        return m_editQuery.DeleteAsync(transaction, ids);
    //    }

    //    public void Insert(ITransaction transaction = null, params T[] datas)
    //    {
    //        m_editQuery.Insert(transaction, datas);
    //    }

    //    public Task InsertAsync(ITransaction transaction = null, params T[] datas)
    //    {
    //        return m_editQuery.InsertAsync(transaction, datas);
    //    }

    //    public void Merge(ITransaction transaction = null, params T[] datas)
    //    {
    //        m_editQuery.Merge(transaction, datas);
    //    }

    //    public Task MergeAsync(ITransaction transaction = null, params T[] datas)
    //    {
    //        return m_editQuery.MergeAsync(transaction, datas);
    //    }

    //    public void Update(T data, ITransaction transaction = null)
    //    {
    //        m_editQuery.Update(data, transaction);
    //    }

    //    public void Update(Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null)
    //    {
    //        m_editQuery.Update(predicate, updateDictionary, transaction);
    //    }

    //    public Task UpdateAsync(T data, ITransaction transaction = null)
    //    {
    //        return m_editQuery.UpdateAsync(data, transaction);
    //    }

    //    public Task UpdateAsync(Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null)
    //    {
    //        return m_editQuery.UpdateAsync(predicate, updateDictionary, transaction);
    //    }
    //}
}