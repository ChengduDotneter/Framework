using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Common.DAL
{
    public interface IEditQuery<T> where T : class, IEntity, new()
    {
        void Insert(params T[] datas);
        void Merge(params T[] datas);
        void Update(T data, params string[] ignoreColumns);
        void Update(Expression<Func<T, bool>> predicate, Expression<Func<T, bool>> updateExpression);
        void Delete(params long[] ids);

        //TODO: 读写分离查询问题
        //TODO: 多客户端并发问题
        ITransaction BeginTransaction();
    }

    //TODO: 用int表示分页和数据总条数会导致数据溢出
    public interface ISearchQuery<T> where T : class, IEntity, new()
    {
        T Get(long id);
        int Count(Expression<Func<T, bool>> predicate = null);
        int Count(string queryWhere, Dictionary<string, object> parameters = null);
        IEnumerable<IDictionary<string, object>> Query(string sql, Dictionary<string, object> parameters = null);

        IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null,
                              IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                              int startIndex = 0,
                              int count = int.MaxValue);

        //TODO: SQL注入
        IEnumerable<T> Search(string queryWhere, 
                              Dictionary<string, object> parameters = null,
                              string orderByFields = null,
                              int startIndex = 0,
                              int count = int.MaxValue);

        int Count<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                              Expression<Func<T, TJoinTable, bool>> predicate = null)
            where TJoinTable : class, IEntity, new();

        IEnumerable<JoinResult<T, TJoinTable>> Search<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                                                                  Expression<Func<T, TJoinTable, bool>> predicate = null,
                                                                  IEnumerable<QueryOrderBy<T, TJoinTable>> queryOrderBies = null,
                                                                  int startIndex = 0,
                                                                  int count = int.MaxValue)
            where TJoinTable : class, IEntity, new();
    }

    public interface ITransaction : IDisposable
    {
        object Context();
        void Submit();
        void Rollback();
    }

    public class QueryOrderBy<T>
    {
        public Expression<Func<T, object>> Expression { get; }
        public OrderByType OrderByType { get; }

        public QueryOrderBy(Expression<Func<T, object>> expression, OrderByType orderByType = OrderByType.Asc)
        {
            Expression = expression;
            OrderByType = orderByType;
        }
    }

    public class QueryOrderBy<TLeft, TRight>
    {
        public Expression<Func<TLeft, TRight, object>> Expression { get; }
        public OrderByType OrderByType { get; }

        public QueryOrderBy(Expression<Func<TLeft, TRight, object>> expression, OrderByType orderByType = OrderByType.Asc)
        {
            Expression = expression;
            OrderByType = orderByType;
        }
    }

    public class JoinCondition<TLeft, TRight>
    {
        public Expression<Func<TLeft, long>> LeftJoinExpression { get; }
        public Expression<Func<TRight, long>> RightJoinExression { get; }

        public JoinCondition(Expression<Func<TLeft, long>> leftJoinExpression, Expression<Func<TRight, long>> rightJoinExression)
        {
            LeftJoinExpression = leftJoinExpression;
            RightJoinExression = rightJoinExression;
        }
    }

    public class JoinResult<TLeft, TRight>
    {
        public TLeft Left { get; }
        public TRight Right { get; }

        public JoinResult(TLeft left, TRight right)
        {
            Left = left;
            Right = right;
        }
    }

    public enum OrderByType
    {
        Asc,
        Desc
    }
}
