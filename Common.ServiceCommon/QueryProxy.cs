using Common.DAL;
using Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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

        public static Expression<Func<T, TJoinTable, bool>> GetJoinIsDeletedCondition<T, TJoinTable>(Expression<Func<T, TJoinTable, bool>> predicate)
            where T : IEntity, new()
            where TJoinTable : IEntity, new()
        {
            if (predicate == null)
            {
                ParameterExpression parameter1 = Expression.Parameter(typeof(T));
                ParameterExpression parameter2 = Expression.Parameter(typeof(T));

                return Expression.Lambda<Func<T, TJoinTable, bool>>(
                    Expression.AndAlso(Expression.Equal(Expression.Property(parameter1, nameof(ViewModelBase.IsDeleted)), Expression.Constant(false, typeof(bool))),
                                       Expression.Equal(Expression.Property(parameter2, nameof(ViewModelBase.IsDeleted)), Expression.Constant(false, typeof(bool)))),
                    parameter1, parameter2);
            }

            Expression<Func<T, TJoinTable, bool>> expression =
                Expression.Lambda<Func<T, TJoinTable, bool>>(
                    Expression.AndAlso(Expression.Equal(Expression.Property(predicate.Parameters[0], nameof(ViewModelBase.IsDeleted)), Expression.Constant(false, typeof(bool))),
                                       Expression.Equal(Expression.Property(predicate.Parameters[1], nameof(ViewModelBase.IsDeleted)), Expression.Constant(false, typeof(bool)))),
                    predicate.Parameters);

            return predicate.AndAlso(expression);
        }

        public static string GetIsDeletedCondition(string queryWhere)
        {
            string isDeleted = $"{nameof(ViewModelBase.IsDeleted)} = 0";

            if (string.IsNullOrWhiteSpace(queryWhere))
                return isDeleted;
            else
                return $"{queryWhere} AND {isDeleted}";
        }
    }

    /// <summary>
    /// 根据ID倒序的查询代理装饰者类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class OrderByIDDescSearchQueryProxy<T> : ISearchQuery<T>
        where T : ViewModelBase, new()
    {
        private const string ORDER_BY_ID_DESC = "ID DESC";
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
        /// 根据sql查询条件获取查询条数
        /// </summary>
        /// <param name="queryWhere"></param>
        /// <param name="parameters"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public int Count(string queryWhere, Dictionary<string, object> parameters = null, ITransaction transaction = null)
        {
            return m_searchQuery.Count(queryWhere, parameters, transaction);
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
        /// 根据sql查询条件获取查询结果列表
        /// </summary>
        /// <param name="queryWhere"></param>
        /// <param name="parameters"></param>
        /// <param name="orderByFields"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public IEnumerable<T> Search(string queryWhere, Dictionary<string, object> parameters = null, string orderByFields = null, int startIndex = 0, int count = int.MaxValue, ITransaction transaction = null)
        {
            return m_searchQuery.Search(queryWhere, parameters,
                                        string.IsNullOrEmpty(orderByFields) ? ORDER_BY_ID_DESC : $"{orderByFields},{ORDER_BY_ID_DESC}",
                                        startIndex,
                                        count,
                                        transaction);
        }

        /// <summary>
        /// 两表Linq联查时获取查询条数
        /// </summary>
        /// <typeparam name="TJoinTable"></typeparam>
        /// <param name="joinCondition"></param>
        /// <param name="predicate"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public int Count<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                                     Expression<Func<T, TJoinTable, bool>> predicate = null,
                                     ITransaction transaction = null)
            where TJoinTable : class, IEntity, new()
        {
            return m_searchQuery.Count(joinCondition, predicate, transaction);
        }

        /// <summary>
        /// 两表Linq联查时获取查询结果
        /// </summary>
        /// <typeparam name="TJoinTable"></typeparam>
        /// <param name="joinCondition"></param>
        /// <param name="predicate"></param>
        /// <param name="queryOrderBies"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public IEnumerable<JoinResult<T, TJoinTable>> Search<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                                                                         Expression<Func<T, TJoinTable, bool>> predicate = null,
                                                                         IEnumerable<QueryOrderBy<T, TJoinTable>> queryOrderBies = null,
                                                                         int startIndex = 0,
                                                                         int count = int.MaxValue,
                                                                         ITransaction transaction = null)
            where TJoinTable : class, IEntity, new()
        {
            IEnumerable<QueryOrderBy<T, TJoinTable>> orderByIDDesc = new[]
            {
                new QueryOrderBy<T, TJoinTable>((left, right) => left.ID, OrderByType.Desc),
                new QueryOrderBy<T, TJoinTable>((left, right) => right.ID, OrderByType.Desc)
            };

            if (queryOrderBies != null)
                queryOrderBies = queryOrderBies.Concat(orderByIDDesc);
            else
                queryOrderBies = orderByIDDesc;

            return m_searchQuery.Search(joinCondition, predicate, queryOrderBies, startIndex, count, transaction);
        }

        /// <summary>
        /// 根据sql查询数据
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public IEnumerable<IDictionary<string, object>> Query(string sql, Dictionary<string, object> parameters = null, ITransaction transaction = null)
        {
            return m_searchQuery.Query(sql, parameters, transaction);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="searchQuery"></param>
        public OrderByIDDescSearchQueryProxy(ISearchQuery<T> searchQuery) => m_searchQuery = searchQuery;
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
        /// 通过Sql语句获取Count
        /// </summary>
        /// <param name="queryWhere">sql语句</param>
        /// <param name="parameters">查询条件值</param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public int Count(string queryWhere, Dictionary<string, object> parameters = null, ITransaction transaction = null)
        {
            return m_searchQuery.Count(QueryProxyHelper.GetIsDeletedCondition(queryWhere), parameters, transaction);
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
        /// Sql语句分页查询
        /// </summary>
        /// <param name="queryWhere">Sql语句</param>
        /// <param name="parameters">查询条件值</param>
        /// <param name="orderByFields">排序方式</param>
        /// <param name="startIndex">开始位置</param>
        /// <param name="count">结果总数</param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public IEnumerable<T> Search(string queryWhere, Dictionary<string, object> parameters = null, string orderByFields = null, int startIndex = 0, int count = int.MaxValue, ITransaction transaction = null)
        {
            return m_searchQuery.Search(QueryProxyHelper.GetIsDeletedCondition(queryWhere), parameters, orderByFields, startIndex, count, transaction);
        }

        /// <summary>
        /// 自定义Sql查询
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="parameters">查询条件值</param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public IEnumerable<IDictionary<string, object>> Query(string sql, Dictionary<string, object> parameters = null, ITransaction transaction = null)
        {
            return m_searchQuery.Query(sql, parameters, transaction);
        }

        /// <summary>
        /// 关联表Count
        /// </summary>
        /// <typeparam name="TJoinTable">关联表实体</typeparam>
        /// <param name="joinCondition">关联查询条件</param>
        /// <param name="predicate">Lambda表达式</param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public int Count<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                                     Expression<Func<T, TJoinTable, bool>> predicate = null,
                                     ITransaction transaction = null)
                where TJoinTable : class, IEntity, new()
        {
            return m_searchQuery.Count(joinCondition, QueryProxyHelper.GetJoinIsDeletedCondition(predicate), transaction);
        }

        /// <summary>
        /// 关联表分页查询
        /// </summary>
        /// <typeparam name="TJoinTable">关联表实体</typeparam>
        /// <param name="joinCondition">关联查询条件</param>
        /// <param name="predicate">Lambda表达式</param>
        /// <param name="queryOrderBies">拍讯方式</param>
        /// <param name="startIndex">开始位置</param>
        /// <param name="count">结果总数</param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public IEnumerable<JoinResult<T, TJoinTable>> Search<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                                                                                  Expression<Func<T, TJoinTable, bool>> predicate = null,
                                                                                  IEnumerable<QueryOrderBy<T, TJoinTable>> queryOrderBies = null,
                                                                                  int startIndex = 0,
                                                                                  int count = int.MaxValue,
                                                                                  ITransaction transaction = null)
            where TJoinTable : class, IEntity, new()
        {
            return m_searchQuery.Search(joinCondition, QueryProxyHelper.GetJoinIsDeletedCondition(predicate), queryOrderBies, startIndex, count, transaction);
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
        /// 逻辑删除
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="ids"></param>
        public void Delete(ITransaction transaction = null, params long[] ids)
        {
            m_editQuery.Update(item => ids.Contains(item.ID), item => item.IsDeleted == true, transaction);
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
        /// 新增或更新
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="datas"></param>
        public void Merge(ITransaction transaction = null, params T[] datas)
        {
            m_editQuery.Merge(transaction, datas);
        }

        /// <summary>
        /// 通过实体类更新
        /// </summary>
        /// <param name="data">实体类</param>
        /// <param name="transaction"></param>
        /// <param name="ignoreColumns">忽略行</param>
        public void Update(T data, ITransaction transaction = null, params string[] ignoreColumns)
        {
            m_editQuery.Update(data, transaction, ignoreColumns);
        }

        /// <summary>
        /// 通过Lambda表达式更新
        /// </summary>
        /// <param name="predicate">匹配体Lambda表达式</param>
        /// <param name="updateExpression">更新体Lambda表达式</param>
        /// <param name="transaction"></param>
        public void Update(Expression<Func<T, bool>> predicate, Expression<Func<T, bool>> updateExpression, ITransaction transaction = null)
        {
            m_editQuery.Update(predicate, updateExpression, transaction);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="editQuery"></param>
        public FilterIsDeletedEditQueryProxy(IEditQuery<T> editQuery) => m_editQuery = editQuery;
    }
}