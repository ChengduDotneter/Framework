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
        /// <param name="systemID">系统ID</param>
        /// <param name="transaction">执行事务</param>
        /// <param name="datas">添加的参数</param>
        void Insert(string systemID, ITransaction transaction = null, params T[] datas);

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="transaction">执行的事务</param>
        /// <param name="datas">添加的参数</param>
        Task InsertAsync(string systemID, ITransaction transaction = null, params T[] datas);

        /// <summary>
        /// 合并
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="transaction">执行的事务</param>
        /// <param name="datas"></param>
        void Merge(string systemID, ITransaction transaction = null, params T[] datas);

        /// <summary>
        /// 合并
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="transaction">执行的事务</param>
        /// <param name="datas"></param>
        Task MergeAsync(string systemID, ITransaction transaction = null, params T[] datas);

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="data"></param>
        /// <param name="transaction">执行的事务</param>
        void Update(string systemID, T data, ITransaction transaction = null);

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="data"></param>
        /// <param name="transaction">执行的事务</param>
        Task UpdateAsync(string systemID, T data, ITransaction transaction = null);

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="predicate"></param>
        /// <param name="updateDictionary"></param>
        /// <param name="transaction">执行的事务</param>
        void Update(string systemID, Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null);

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="predicate"></param>
        /// <param name="updateDictionary"></param>
        /// <param name="transaction">执行的事务</param>
        Task UpdateAsync(string systemID, Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null);

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="transaction">执行的事务</param>
        /// <param name="ids"></param>
        void Delete(string systemID, ITransaction transaction = null, params long[] ids);

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="transaction">执行的事务</param>
        /// <param name="ids"></param>
        Task DeleteAsync(string systemID, ITransaction transaction = null, params long[] ids);

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
        /// <param name="updateDictionary"></param>
        /// <param name="transaction">执行的事务</param>
        void Update(Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null);

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="updateDictionary"></param>
        /// <param name="transaction">执行的事务</param>
        Task UpdateAsync(Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null);

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
        /// <param name="distributedLock">是否启用分布式事务锁</param>
        /// <param name="weight">事务权重</param>
        ITransaction BeginTransaction(bool distributedLock = false, int weight = 0);

        /// <summary>
        /// 开启事务
        /// </summary>
        /// <param name="distributedLock">是否启用分布式事务锁</param>
        /// <param name="weight">事务权重</param>
        Task<ITransaction> BeginTransactionAsync(bool distributedLock = false, int weight = 0);
    }

    /// <summary>
    /// 数据查询接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISearchQuery<T> where T : class, IEntity, new()
    {
        /// <summary>
        /// 事务中根据ID查询
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="id"></param>
        /// <param name="transaction"></param>
        /// <param name="forUpdate"></param>
        /// <returns></returns>
        T Get(string systemID, long id, ITransaction transaction, bool forUpdate = false);

        /// <summary>
        /// 根据ID查询
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="id"></param>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        T Get(string systemID, long id, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 事务中根据ID查询（异步）
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="id"></param>
        /// <param name="transaction"></param>
        /// <param name="forUpdate"></param>
        /// <returns></returns>
        Task<T> GetAsync(string systemID, long id, ITransaction transaction, bool forUpdate = false);

        /// <summary>
        /// 根据ID查询（异步）
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="id"></param>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        Task<T> GetAsync(string systemID, long id, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 事务中根据Linq筛选条件查询条数
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="transaction"></param>
        /// <param name="predicate"></param>
        /// <param name="forUpdate"></param>
        /// <returns></returns>
        int Count(string systemID, ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false);

        /// <summary>
        /// 根据Linq筛选条件查询条数
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="predicate"></param>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        int Count(string systemID, Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 事务中根据Linq筛选条件查询条数（异步）
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="transaction"></param>
        /// <param name="predicate"></param>
        /// <param name="forUpdate"></param>
        /// <returns></returns>
        Task<int> CountAsync(string systemID, ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false);

        /// <summary>
        /// 根据Linq筛选条件查询条数（异步）
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="predicate"></param>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        Task<int> CountAsync(string systemID, Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 事务中根据Linq筛选条件查询
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="transaction"></param>
        /// <param name="predicate"></param>
        /// <param name="queryOrderBies"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="forUpdate"></param>
        /// <returns></returns>
        IEnumerable<T> Search(string systemID,
                              ITransaction transaction,
                              Expression<Func<T, bool>> predicate = null,
                              IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                              int startIndex = 0,
                              int count = int.MaxValue,
                              bool forUpdate = false);

        /// <summary>
        /// 根据Linq筛选条件查询
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="predicate"></param>
        /// <param name="queryOrderBies"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        IEnumerable<T> Search(string systemID,
                              Expression<Func<T, bool>> predicate = null,
                              IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                              int startIndex = 0,
                              int count = int.MaxValue,
                              IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 事务中根据Linq筛选条件查询（异步）
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="transaction"></param>
        /// <param name="predicate"></param>
        /// <param name="queryOrderBies"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="forUpdate"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> SearchAsync(string systemID,
                                         ITransaction transaction,
                                         Expression<Func<T, bool>> predicate = null,
                                         IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                         int startIndex = 0,
                                         int count = int.MaxValue,
                                         bool forUpdate = false);

        /// <summary>
        /// 根据Linq筛选条件查询（异步）
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="predicate"></param>
        /// <param name="queryOrderBies"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> SearchAsync(string systemID,
                                         Expression<Func<T, bool>> predicate = null,
                                         IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                         int startIndex = 0,
                                         int count = int.MaxValue,
                                         IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 事务中获取Linq查询接口
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        ISearchQueryable<T> GetQueryable(string systemID, ITransaction transaction);

        /// <summary>
        /// 获取Linq查询接口
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        ISearchQueryable<T> GetQueryable(string systemID, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 事务中获取Linq查询接口（异步）
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<ISearchQueryable<T>> GetQueryableAsync(string systemID, ITransaction transaction);

        /// <summary>
        /// 获取Linq查询接口（异步）
        /// </summary>
        /// <param name="systemID">系统ID</param>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        Task<ISearchQueryable<T>> GetQueryableAsync(string systemID, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 事务中根据ID查询
        /// </summary>
        /// <param name="id"></param>
        /// <param name="transaction"></param>
        /// <param name="forUpdate"></param>
        /// <returns></returns>
        T Get(long id, ITransaction transaction, bool forUpdate = false);

        /// <summary>
        /// 根据ID查询
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        T Get(long id, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 事务中根据ID查询（异步）
        /// </summary>
        /// <param name="id"></param>
        /// <param name="transaction"></param>
        /// <param name="forUpdate"></param>
        /// <returns></returns>
        Task<T> GetAsync(long id, ITransaction transaction, bool forUpdate = false);

        /// <summary>
        /// 根据ID查询（异步）
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        Task<T> GetAsync(long id, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 事务中根据Linq筛选条件查询条数
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="predicate"></param>
        /// <param name="forUpdate"></param>
        /// <returns></returns>
        int Count(ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false);

        /// <summary>
        /// 根据Linq筛选条件查询条数
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        int Count(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 事务中根据Linq筛选条件查询条数（异步）
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="predicate"></param>
        /// <param name="forUpdate"></param>
        /// <returns></returns>
        Task<int> CountAsync(ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false);

        /// <summary>
        /// 根据Linq筛选条件查询条数（异步）
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 事务中根据Linq筛选条件查询
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="predicate"></param>
        /// <param name="queryOrderBies"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="forUpdate"></param>
        /// <returns></returns>
        IEnumerable<T> Search(ITransaction transaction,
                              Expression<Func<T, bool>> predicate = null,
                              IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                              int startIndex = 0,
                              int count = int.MaxValue,
                              bool forUpdate = false);

        /// <summary>
        /// 根据Linq筛选条件查询
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="queryOrderBies"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null,
                              IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                              int startIndex = 0,
                              int count = int.MaxValue,
                              IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 事务中根据Linq筛选条件查询（异步）
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="predicate"></param>
        /// <param name="queryOrderBies"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="forUpdate"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> SearchAsync(ITransaction transaction,
                                         Expression<Func<T, bool>> predicate = null,
                                         IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                         int startIndex = 0,
                                         int count = int.MaxValue,
                                         bool forUpdate = false);

        /// <summary>
        /// 根据Linq筛选条件查询（异步）
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="queryOrderBies"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> SearchAsync(Expression<Func<T, bool>> predicate = null,
                                         IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                         int startIndex = 0,
                                         int count = int.MaxValue,
                                         IDBResourceContent dbResourceContent = null);


        /// <summary>
        /// 根据LinqQueryable筛选条件联查条数
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        int Count<TResult>(IQueryable<TResult> query);

        /// <summary>
        /// 根据LinqQueryable筛选条件联查条数（异步）
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        Task<int> CountAsync<TResult>(IQueryable<TResult> query);

        /// <summary>
        /// 根据LinqQueryable筛选条件联查数据
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        IEnumerable<TResult> Search<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue);

        /// <summary>
        /// 根据LinqQueryable筛选条件联查数据（异步）
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="query"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        Task<IEnumerable<TResult>> SearchAsync<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue);

        /// <summary>
        /// 事务中获取Linq查询接口
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        ISearchQueryable<T> GetQueryable(ITransaction transaction);

        /// <summary>
        /// 获取Linq查询接口
        /// </summary>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        ISearchQueryable<T> GetQueryable(IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 事务中获取Linq查询接口（异步）
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        Task<ISearchQueryable<T>> GetQueryableAsync(ITransaction transaction);

        /// <summary>
        /// 获取Linq查询接口（异步）
        /// </summary>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        Task<ISearchQueryable<T>> GetQueryableAsync(IDBResourceContent dbResourceContent = null);
    }

    /// <summary>
    /// 查询Queryable
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISearchQueryable<T> : IQueryable<T>, IDisposable
    {
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

    /// <summary>
    /// 创建表接口
    /// </summary>
    public interface ICreateTableQuery
    {
        /// <summary>
        /// 创建表
        /// </summary>
        /// <param name="systemID"></param>
        /// <param name="tableTypes"></param>
        /// <returns></returns>
        Task CreateTable(string systemID, IEnumerable<Type> tableTypes);
    }
}