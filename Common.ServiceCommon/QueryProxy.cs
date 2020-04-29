using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Common.DAL;
using Common.Model;

namespace Common.ServiceCommon
{
    public static class ISearchQueryExtention
    {
        public static ISearchQuery<T> FilterIsDeleted<T>(this ISearchQuery<T> searchQuery)
            where T : ViewModelBase, new()
        {
            return new FilterIsDeletedSearchQueryProxy<T>(searchQuery);
        }

        public static ISearchQuery<T> OrderByIDDesc<T>(this ISearchQuery<T> searchQuery)
            where T : ViewModelBase, new()
        {
            return new OrderByIDDescSearchQueryProxy<T>(searchQuery);
        }
    }

    public static class IEditQueryExtention
    {
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

    public class OrderByIDDescSearchQueryProxy<T> : ISearchQuery<T>
        where T : ViewModelBase, new()
    {
        private const string ORDER_BY_ID_DESC = "ID DESC";
        private ISearchQuery<T> m_searchQuery;

        public T Get(long id)
        {
            return m_searchQuery.Get(id);
        }

        public int Count(Expression<Func<T, bool>> predicate = null)
        {
            return m_searchQuery.Count(predicate);
        }

        public int Count(string queryWhere)
        {
            return m_searchQuery.Count(queryWhere);
        }

        public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null,
                                     IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                     int startIndex = 0,
                                     int count = int.MaxValue)
        {
            IEnumerable<QueryOrderBy<T>> orderByIDDesc = new[] { new QueryOrderBy<T>(item => item.ID, OrderByType.Desc) };

            if (queryOrderBies != null)
                queryOrderBies = queryOrderBies.Concat(orderByIDDesc);
            else
                queryOrderBies = orderByIDDesc;

            return m_searchQuery.Search(predicate, queryOrderBies, startIndex, count);
        }

        public IEnumerable<T> Search(string queryWhere, string orderByFields = null, int startIndex = 0, int count = int.MaxValue)
        {
            return m_searchQuery.Search(queryWhere,
                                        string.IsNullOrEmpty(orderByFields) ? ORDER_BY_ID_DESC : $"{orderByFields},{ORDER_BY_ID_DESC}",
                                        startIndex,
                                        count);
        }

        public int Count<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                                     Expression<Func<T, TJoinTable, bool>> predicate = null)
            where TJoinTable : class, IEntity, new()
        {
            return m_searchQuery.Count(joinCondition, predicate);
        }

        public IEnumerable<JoinResult<T, TJoinTable>> Search<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                                                                         Expression<Func<T, TJoinTable, bool>> predicate = null,
                                                                         IEnumerable<QueryOrderBy<T, TJoinTable>> queryOrderBies = null,
                                                                         int startIndex = 0,
                                                                         int count = int.MaxValue)
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

            return m_searchQuery.Search(joinCondition, predicate, queryOrderBies, startIndex, count);
        }

        public IEnumerable<IDictionary<string, object>> Query(string sql, Dictionary<string, object> parameters = null)
        {
            return m_searchQuery.Query(sql, parameters);
        }

        public OrderByIDDescSearchQueryProxy(ISearchQuery<T> searchQuery) => m_searchQuery = searchQuery;
    }

    public class FilterIsDeletedSearchQueryProxy<T> : ISearchQuery<T>
        where T : ViewModelBase, new()
    {
        private ISearchQuery<T> m_searchQuery;

        public T Get(long id)
        {
            T data = m_searchQuery.Get(id);
            return !data?.IsDeleted ?? false ? data : null;
        }

        public int Count(Expression<Func<T, bool>> predicate = null)
        {
            return m_searchQuery.Count(QueryProxyHelper.GetIsDeletedCondition(predicate));
        }

        public int Count(string queryWhere)
        {
            return m_searchQuery.Count(QueryProxyHelper.GetIsDeletedCondition(queryWhere));
        }

        public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null,
                                     IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                     int startIndex = 0,
                                     int count = int.MaxValue)
        {
            return m_searchQuery.Search(QueryProxyHelper.GetIsDeletedCondition(predicate), queryOrderBies, startIndex, count);
        }

        public IEnumerable<T> Search(string queryWhere, string orderByFields = null, int startIndex = 0, int count = int.MaxValue)
        {
            return m_searchQuery.Search(QueryProxyHelper.GetIsDeletedCondition(queryWhere), orderByFields, startIndex, count);
        }

        public IEnumerable<IDictionary<string, object>> Query(string sql, Dictionary<string, object> parameters = null)
        {
            return m_searchQuery.Query(sql, parameters);
        }

        public int Count<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                                     Expression<Func<T, TJoinTable, bool>> predicate = null)
                where TJoinTable : class, IEntity, new()
        {
            return m_searchQuery.Count(joinCondition, QueryProxyHelper.GetJoinIsDeletedCondition(predicate));
        }

        public IEnumerable<JoinResult<T, TJoinTable>> Search<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                                                                                  Expression<Func<T, TJoinTable, bool>> predicate = null,
                                                                                  IEnumerable<QueryOrderBy<T, TJoinTable>> queryOrderBies = null,
                                                                                  int startIndex = 0,
                                                                                  int count = int.MaxValue)
            where TJoinTable : class, IEntity, new()
        {
            return m_searchQuery.Search(joinCondition, QueryProxyHelper.GetJoinIsDeletedCondition(predicate), queryOrderBies, startIndex, count);
        }

        public FilterIsDeletedSearchQueryProxy(ISearchQuery<T> searchQuery) => m_searchQuery = searchQuery;
    }

    public class FilterIsDeletedEditQueryProxy<T> : IEditQuery<T>
        where T : ViewModelBase, new()
    {
        private IEditQuery<T> m_editQuery;

        public ITransaction BeginTransaction()
        {
            return m_editQuery.BeginTransaction();
        }

        public void Delete(params long[] ids)
        {
            m_editQuery.Update(item => ids.Contains(item.ID), item => item.IsDeleted == true);
        }

        public void Insert(params T[] datas)
        {
            m_editQuery.Insert(datas);
        }

        public void Merge(params T[] datas)
        {
            m_editQuery.Merge(datas);
        }

        public void Update(T data, params string[] ignoreColumns)
        {
            m_editQuery.Update(data, ignoreColumns);
        }

        public void Update(Expression<Func<T, bool>> predicate, Expression<Func<T, bool>> updateExpression)
        {
            m_editQuery.Update(predicate, updateExpression);
        }

        public FilterIsDeletedEditQueryProxy(IEditQuery<T> editQuery) => m_editQuery = editQuery;
    }

}
