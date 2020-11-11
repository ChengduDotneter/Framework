using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

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
        /// <param name="transaction">执行的事务</param>
        /// <param name="datas"></param>
        void Insert(ITransaction transaction = null, params T[] datas);

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="transaction">执行的事务</param>
        /// <param name="datas"></param>
        Task InsertAsync(ITransaction transaction = null, params T[] datas);

        /// <summary>
        /// 合并
        /// </summary>
        /// <param name="transaction">执行的事务</param>
        /// <param name="datas"></param>
        void Merge(ITransaction transaction = null, params T[] datas);

        /// <summary>
        /// 合并
        /// </summary>
        /// <param name="transaction">执行的事务</param>
        /// <param name="datas"></param>
        Task MergeAsync(ITransaction transaction = null, params T[] datas);

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="data"></param>
        /// <param name="transaction">执行的事务</param>
        void Update(T data, ITransaction transaction = null);

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="data"></param>
        /// <param name="transaction">执行的事务</param>
        Task UpdateAsync(T data, ITransaction transaction = null);

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="upateDictionary"></param>
        /// <param name="transaction">执行的事务</param>
        void Update(Expression<Func<T, bool>> predicate, IDictionary<string, object> upateDictionary, ITransaction transaction = null);

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="upateDictionary"></param>
        /// <param name="transaction">执行的事务</param>
        Task UpdateAsync(Expression<Func<T, bool>> predicate, IDictionary<string, object> upateDictionary, ITransaction transaction = null);

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="transaction">执行的事务</param>
        /// <param name="ids"></param>
        void Delete(ITransaction transaction = null, params long[] ids);

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="transaction">执行的事务</param>
        /// <param name="ids"></param>
        Task DeleteAsync(ITransaction transaction = null, params long[] ids);

        /// <summary>
        /// 开启事务
        /// </summary>
        /// <param name="weight">事务权重</param>
        ITransaction BeginTransaction(int weight = 0);

        /// <summary>
        /// 开启事务
        /// </summary>
        /// <param name="weight">事务权重</param>
        Task<ITransaction> BeginTransactionAsync(int weight = 0);
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
        /// <param name="transaction"></param>
        /// <returns></returns>
        T Get(long id, ITransaction transaction = null, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 根据ID查询
        /// </summary>
        /// <param name="id"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<T> GetAsync(long id, ITransaction transaction = null, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 根据Linq筛选条件查询条数
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        int Count(Expression<Func<T, bool>> predicate = null, ITransaction transaction = null, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 根据Linq筛选条件查询条数
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, ITransaction transaction = null, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 根据Linq筛选条件查询
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="queryOrderBies"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null,
                              IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                              int startIndex = 0,
                              int count = int.MaxValue,
                              ITransaction transaction = null, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 根据Linq筛选条件查询
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="queryOrderBies"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> SearchAsync(Expression<Func<T, bool>> predicate = null,
                              IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                              int startIndex = 0,
                              int count = int.MaxValue,
                              ITransaction transaction = null, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 根据Linq筛选条件两表联查条数
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        int Count<TResult>(IQueryable<TResult> query, ITransaction transaction = null, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 根据Linq筛选条件两表联查条数
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<int> CountAsync<TResult>(IQueryable<TResult> query, ITransaction transaction = null, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 根据Linq筛选条件两表联查数据
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        IEnumerable<TResult> Search<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, ITransaction transaction = null, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 根据Linq筛选条件两表联查数据
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<IEnumerable<TResult>> SearchAsync<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, ITransaction transaction = null, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 获取Linq查询接口
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        ISearchQueryable<T> GetQueryable(ITransaction transaction = null, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 获取Linq查询接口
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<ISearchQueryable<T>> GetQueryableAsync(ITransaction transaction = null, IDBResourceContent dbResourceContent = null);
    }

    /// <summary>
    /// 查询Queryable
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISearchQueryable<T> : IQueryable<T>, IDisposable { }

    /// <summary>
    /// 事务接口
    /// </summary>
    public interface ITransaction : IDisposable
    {
        /// <summary>
        /// 上下文
        /// </summary>
        /// <returns></returns>
        object Context { get; }

        /// <summary>
        /// 提交
        /// </summary>
        void Submit();

        /// <summary>
        /// 提交
        /// </summary>
        Task SubmitAsync();

        /// <summary>
        /// 回滚
        /// </summary>
        void Rollback();

        /// <summary>
        /// 回滚
        /// </summary>
        Task RollbackAsync();
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