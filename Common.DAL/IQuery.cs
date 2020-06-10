using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Common.DAL
{
    /// <summary>
    /// 数据修改接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEditQuery<T> where T : class, IEntity, new()
    {
        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="datas"></param>
        void Insert(params T[] datas);

        /// <summary>
        /// 合并
        /// </summary>
        /// <param name="datas"></param>
        void Merge(params T[] datas);

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ignoreColumns"></param>
        void Update(T data, params string[] ignoreColumns);

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="updateExpression"></param>
        void Update(Expression<Func<T, bool>> predicate, Expression<Func<T, bool>> updateExpression);

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="ids"></param>
        void Delete(params long[] ids);

        /// <summary>
        /// 开启事务
        /// </summary>
        /// <returns></returns>
        ITransaction BeginTransaction();
    }

    /// <summary>
    /// 数据查询接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISearchQuery<T> where T : class, IEntity, new()
    {
        /// <summary>
        /// 根据ID查询
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        T Get(long id);

        /// <summary>
        /// 根据Linq筛选条件查询条数
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        int Count(Expression<Func<T, bool>> predicate = null);

        /// <summary>
        /// 根据Sql筛选条件查询条数
        /// </summary>
        /// <param name="queryWhere"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        int Count(string queryWhere, Dictionary<string, object> parameters = null);

        /// <summary>
        /// 根据SQL查询，用于复合查询
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        IEnumerable<IDictionary<string, object>> Query(string sql, Dictionary<string, object> parameters = null);

        /// <summary>
        /// 根据Linq筛选条件查询
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="queryOrderBies"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null,
                              IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                              int startIndex = 0,
                              int count = int.MaxValue);

        /// <summary>
        /// 根据SQL筛选条件查询
        /// </summary>
        /// <param name="queryWhere"></param>
        /// <param name="parameters"></param>
        /// <param name="orderByFields"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        IEnumerable<T> Search(string queryWhere,
                              Dictionary<string, object> parameters = null,
                              string orderByFields = null,
                              int startIndex = 0,
                              int count = int.MaxValue);

        /// <summary>
        /// 根据Linq筛选条件两表联查条数
        /// </summary>
        /// <typeparam name="TJoinTable"></typeparam>
        /// <param name="joinCondition"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        int Count<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                              Expression<Func<T, TJoinTable, bool>> predicate = null)
            where TJoinTable : class, IEntity, new();

        /// <summary>
        /// 根据Linq筛选条件两表联查数据
        /// </summary>
        /// <typeparam name="TJoinTable"></typeparam>
        /// <param name="joinCondition"></param>
        /// <param name="predicate"></param>
        /// <param name="queryOrderBies"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        IEnumerable<JoinResult<T, TJoinTable>> Search<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                                                                  Expression<Func<T, TJoinTable, bool>> predicate = null,
                                                                  IEnumerable<QueryOrderBy<T, TJoinTable>> queryOrderBies = null,
                                                                  int startIndex = 0,
                                                                  int count = int.MaxValue)
            where TJoinTable : class, IEntity, new();
    }

    /// <summary>
    /// 事务接口
    /// </summary>
    public interface ITransaction : IDisposable
    {
        /// <summary>
        /// 上下文
        /// </summary>
        /// <returns></returns>
        object Context();

        /// <summary>
        /// 提交
        /// </summary>
        void Submit();

        /// <summary>
        /// 回滚
        /// </summary>
        void Rollback();
    }

    /// <summary>
    /// 查询排序类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QueryOrderBy<T>
    {
        /// <summary>
        /// 排序Linq
        /// </summary>
        public Expression<Func<T, object>> Expression { get; }

        /// <summary>
        /// 排序方式
        /// </summary>
        public OrderByType OrderByType { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="orderByType"></param>
        public QueryOrderBy(Expression<Func<T, object>> expression, OrderByType orderByType = OrderByType.Asc)
        {
            Expression = expression;
            OrderByType = orderByType;
        }
    }

    /// <summary>
    /// 两表查询排序类
    /// </summary>
    /// <typeparam name="TLeft"></typeparam>
    /// <typeparam name="TRight"></typeparam>
    public class QueryOrderBy<TLeft, TRight>
    {
        /// <summary>
        /// 排序Linq
        /// </summary>
        public Expression<Func<TLeft, TRight, object>> Expression { get; }

        /// <summary>
        /// 排序方式
        /// </summary>
        public OrderByType OrderByType { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="orderByType"></param>
        public QueryOrderBy(Expression<Func<TLeft, TRight, object>> expression, OrderByType orderByType = OrderByType.Asc)
        {
            Expression = expression;
            OrderByType = orderByType;
        }
    }

    /// <summary>
    /// 两表联查条件类
    /// </summary>
    /// <typeparam name="TLeft"></typeparam>
    /// <typeparam name="TRight"></typeparam>
    public class JoinCondition<TLeft, TRight>
    {
        /// <summary>
        /// 左表查询条件Linq
        /// </summary>
        public Expression<Func<TLeft, long>> LeftJoinExpression { get; }

        /// <summary>
        /// 右表查询条件Linq
        /// </summary>
        public Expression<Func<TRight, long>> RightJoinExression { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="leftJoinExpression"></param>
        /// <param name="rightJoinExression"></param>
        public JoinCondition(Expression<Func<TLeft, long>> leftJoinExpression, Expression<Func<TRight, long>> rightJoinExression)
        {
            LeftJoinExpression = leftJoinExpression;
            RightJoinExression = rightJoinExression;
        }
    }

    /// <summary>
    /// 两表联查结果类
    /// </summary>
    /// <typeparam name="TLeft"></typeparam>
    /// <typeparam name="TRight"></typeparam>
    public class JoinResult<TLeft, TRight>
    {
        /// <summary>
        /// 左表查询结果
        /// </summary>
        public TLeft Left { get; }

        /// <summary>
        /// 右表查询结果
        /// </summary>
        public TRight Right { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public JoinResult(TLeft left, TRight right)
        {
            Left = left;
            Right = right;
        }
    }

    /// <summary>
    /// 排序类型
    /// </summary>
    public enum OrderByType
    {
        /// <summary>
        /// 正序
        /// </summary>
        Asc,

        /// <summary>
        /// 逆序
        /// </summary>
        Desc
    }
}