using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Common.DAL.Cache
{
    /// <summary>
    /// 事务代理
    /// </summary>
    internal class TransactionProxy : ITransaction
    {
        private ITransaction m_transaction;
        private ICache m_keyCache;
        private ICache m_conditionCache;

        public bool ClearKeyCache { get; set; }//清楚key缓存

        public bool ClearConditionCache { get; set; }//是否清楚条件缓存

        public object Context { get { return m_transaction.Context; } }//事务上下文

        public Queue<Task> TaskQueue { get; }//任务队列

        public ITransaction Transaction { get { return m_transaction; } }//事务

        public TransactionProxy(ITransaction transaction, ICache keyCache, ICache conditionCache)
        {
            m_transaction = transaction;
            m_keyCache = keyCache;
            m_conditionCache = conditionCache;
            TaskQueue = new Queue<Task>();
        }

        public void Dispose()
        {
            m_transaction.Dispose();
        }

        public void Rollback()
        {
            m_transaction.Rollback();//回滚
        }

        public Task RollbackAsync()
        {
            return m_transaction.RollbackAsync();//异步回滚
        }

        public void Submit()//提交
        {
            m_transaction.Submit();

            if (ClearKeyCache)
                m_keyCache.Clear();//*清空缓存

            if (ClearConditionCache)
                m_conditionCache.Clear();//*清空条件缓存

            if (!ClearKeyCache && !ClearConditionCache)//判断是否清空
            {
                while (TaskQueue.Count > 0)
                {
                    Task task = TaskQueue.Dequeue();
                    task.Start();
                    task.Wait();
                }
            }
        }

        public async Task SubmitAsync()
        {
            await m_transaction.SubmitAsync();//异步提交

            IList<Task> tasks = new List<Task>();

            tasks.Add(Task.Factory.StartNew(async () =>
            {
                if (ClearKeyCache)
                    await m_keyCache.ClearAsync();
            }));

            tasks.Add(Task.Factory.StartNew(async () =>
            {
                if (ClearConditionCache)
                    await m_conditionCache.ClearAsync();
            }));

            await Task.WhenAll(tasks);

            if (!ClearKeyCache || !ClearConditionCache)
            {
                while (TaskQueue.Count > 0)
                {
                    Task task = TaskQueue.Dequeue();
                    task.Start();
                    await Task.WhenAll(task);
                }
            }
        }
    }
    /// <summary>
    /// 修改对象代理类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class EditQueryProxy<T> : IEditQuery<T>
        where T : class, IEntity, new()
    {
        private IEditQuery<T> m_editQuery;//修改操作对象
        private ICache m_keyCache;
        private ICache m_conditionCache;

        public EditQueryProxy(IEditQuery<T> editQuery, ICache keyCache, ICache conditionCache)
        {
            m_editQuery = editQuery;
            m_keyCache = keyCache;
            m_conditionCache = conditionCache;
        }
        /// <summary>
        /// 使用事务代理开启事务
        /// </summary>
        /// <param name="distributedLock"></param>
        /// <param name="weight"></param>
        /// <returns></returns>
        public ITransaction BeginTransaction(bool distributedLock = false, int weight = 0)
        {
            return new TransactionProxy(m_editQuery.BeginTransaction(distributedLock, weight), m_keyCache, m_conditionCache);
        }
        /// <summary>
        /// 异步开启事务
        /// </summary>
        /// <param name="distributedLock"></param>
        /// <param name="weight"></param>
        /// <returns></returns>
        public async Task<ITransaction> BeginTransactionAsync(bool distributedLock = false, int weight = 0)
        {
            return new TransactionProxy(await m_editQuery.BeginTransactionAsync(distributedLock, weight), m_keyCache, m_conditionCache);
        }
        /// <summary>
        /// 事务中删除
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="ids"></param>
        public void Delete(ITransaction transaction = null, params long[] ids)
        {
            Delete(string.Empty, transaction, ids);
        }
        /// <summary>
        /// 传系统id的事务中删除 会清除缓存
        /// </summary>
        /// <param name="systemID"></param>
        /// <param name="transaction"></param>
        /// <param name="ids"></param>
        public void Delete(string systemID, ITransaction transaction = null, params long[] ids)
        {
            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                m_editQuery.Delete(systemID, transactionProxy.Transaction, ids);
                transactionProxy.ClearConditionCache = true;
                transactionProxy.TaskQueue.Enqueue(new Task((state) =>
                {
                    long[] idArray = (long[])state;

                    for (int i = 0; i < idArray.Length; i++)
                        m_keyCache.Remove(idArray[i]);
                }, ids.Select(item => item.ToSystemObjectID(systemID))));
            }
            else
            {
                m_editQuery.Delete(systemID, transaction, ids);

                for (int i = 0; i < ids.Length; i++)
                    m_keyCache.Remove(ids[i].ToSystemObjectID(systemID));

                m_conditionCache.Clear();
            }
        }
        /// <summary>
        /// 事务中异步删除
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public Task DeleteAsync(ITransaction transaction = null, params long[] ids)
        {
            return DeleteAsync(string.Empty, transaction, ids);
        }
        /// <summary>
        /// 传系统id的事务中异步删除 会清楚缓存
        /// </summary>
        /// <param name="systemID"></param>
        /// <param name="transaction"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task DeleteAsync(string systemID, ITransaction transaction = null, params long[] ids)
        {
            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                await m_editQuery.DeleteAsync(systemID, transactionProxy.Transaction, ids);

                transactionProxy.ClearConditionCache = true;
                transactionProxy.TaskQueue.Enqueue(new Task(async (state) =>//事务代理队列
                {
                    IList<Task> tasks = new List<Task>();

                    for (int i = 0; i < ids.Length; i++)
                        tasks.Add(m_keyCache.RemoveAsync(ids[i].ToSystemObjectID(systemID)));

                    tasks.Add(m_conditionCache.ClearAsync());
                    await Task.WhenAll(tasks);
                }, ids));
            }
            else
            {
                await m_editQuery.DeleteAsync(systemID, transaction, ids);

                IList<Task> tasks = new List<Task>();

                for (int i = 0; i < ids.Length; i++)
                    tasks.Add(m_keyCache.RemoveAsync(ids[i].ToSystemObjectID(systemID)));

                tasks.Add(m_conditionCache.ClearAsync());
                await Task.WhenAll(tasks);
            }
        }
        /// <summary>
        /// 事务中插入
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="datas"></param>
        public void Insert(ITransaction transaction = null, params T[] datas)
        {
            Insert(string.Empty, transaction, datas);
        }
        /// <summary>
        /// 事务中插入 会清楚缓存
        /// </summary>
        /// <param name="systemID"></param>
        /// <param name="transaction"></param>
        /// <param name="datas"></param>
        public void Insert(string systemID, ITransaction transaction = null, params T[] datas)
        {
            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                m_editQuery.Insert(systemID, transactionProxy.Transaction, datas);
                transactionProxy.ClearConditionCache = true;
            }
            else
            {
                m_editQuery.Insert(systemID, transaction, datas);
                m_conditionCache.Clear();
            }
        }
        /// <summary>
        /// 事务中异步插入
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="datas"></param>
        /// <returns></returns>
        public Task InsertAsync(ITransaction transaction = null, params T[] datas)
        {
            return InsertAsync(string.Empty, transaction, datas);
        }
        /// <summary>
        /// 事务中异步插入 会清楚缓存
        /// </summary>
        /// <param name="systemID"></param>
        /// <param name="transaction"></param>
        /// <param name="datas"></param>
        /// <returns></returns>
        public async Task InsertAsync(string systemID, ITransaction transaction = null, params T[] datas)
        {
            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                await m_editQuery.InsertAsync(systemID, transactionProxy.Transaction, datas);
                transactionProxy.ClearConditionCache = true;
            }
            else
            {
                await m_editQuery.InsertAsync(systemID, transaction, datas);
                await m_conditionCache.ClearAsync();
            }
        }
        /// <summary>
        /// 事务中合并
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="datas"></param>
        public void Merge(ITransaction transaction = null, params T[] datas)
        {
            Merge(string.Empty, transaction, datas);
        }
        /// <summary>
        /// 使用事务代理的合并 会清除缓存
        /// </summary>
        /// <param name="systemID"></param>
        /// <param name="transaction"></param>
        /// <param name="datas"></param>
        public void Merge(string systemID, ITransaction transaction = null, params T[] datas)
        {
            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                m_editQuery.Merge(systemID, transactionProxy.Transaction, datas);
                transactionProxy.ClearConditionCache = true;

                transactionProxy.TaskQueue.Enqueue(new Task((state) =>
                {
                    T[] datas = (T[])state;

                    for (int i = 0; i < datas.Length; i++)
                    {
                        (bool exists, _) = m_keyCache.TryGetValue<T>(datas[i].ID.ToSystemObjectID(systemID));

                        if (exists)
                            m_keyCache.Set(datas[i].ID.ToSystemObjectID(systemID), datas[i]);
                    }
                }, datas));
            }
            else
            {
                m_editQuery.Merge(systemID, transaction, datas);

                for (int i = 0; i < datas.Length; i++)
                {
                    (bool exists, _) = m_keyCache.TryGetValue<T>(datas[i].ID.ToSystemObjectID(systemID));

                    if (exists)
                        m_keyCache.Set(datas[i].ID.ToSystemObjectID(systemID), datas[i]);
                }

                m_conditionCache.Clear();
            }
        }
        /// <summary>
        /// 事务中异步合并
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="datas"></param>
        /// <returns></returns>
        public Task MergeAsync(ITransaction transaction = null, params T[] datas)
        {
            return MergeAsync(string.Empty, transaction, datas);
        }
        /// <summary>
        /// 使用代理事务异步合并
        /// </summary>
        /// <param name="systemID"></param>
        /// <param name="transaction"></param>
        /// <param name="datas"></param>
        /// <returns></returns>
        public async Task MergeAsync(string systemID, ITransaction transaction = null, params T[] datas)
        {
            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                await m_editQuery.MergeAsync(systemID, transactionProxy.Transaction, datas);
                transactionProxy.ClearConditionCache = true;

                transactionProxy.TaskQueue.Enqueue(new Task(async (state) =>
                {
                    T[] datas = (T[])state;

                    IList<Task> tasks = new List<Task>();

                    for (int i = 0; i < datas.Length; i++)
                    {
                        int index = i;
                        
                        tasks.Add(Task.Factory.StartNew(async () =>
                        {
                            (bool exists, _) = await m_keyCache.TryGetValueAsync<T>(datas[index].ID.ToSystemObjectID(systemID));

                            if (exists)
                                await m_keyCache.SetAsync(datas[index].ID.ToSystemObjectID(systemID), datas[index]);
                        }));
                    }

                    await Task.WhenAll(tasks);
                }, datas));
            }
            else
            {
                await m_editQuery.MergeAsync(systemID, transaction, datas);

                IList<Task> tasks = new List<Task>();

                for (int i = 0; i < datas.Length; i++)
                {
                    int index = i;
                    
                    tasks.Add(Task.Factory.StartNew(async () =>
                    {
                        (bool exists, _) = await m_keyCache.TryGetValueAsync<T>(datas[index].ID.ToSystemObjectID(systemID));

                        if (exists)
                            await m_keyCache.SetAsync(datas[index].ID.ToSystemObjectID(systemID), datas[index]);
                    }));
                }

                tasks.Add(m_conditionCache.ClearAsync());
                await Task.WhenAll(tasks);
            }
        }
        /// <summary>
        /// 事务中修改
        /// </summary>
        /// <param name="data"></param>
        /// <param name="transaction"></param>
        public void Update(T data, ITransaction transaction = null)
        {
            Update(string.Empty, data, transaction);
        }
        /// <summary>
        /// 使用代理事务修改
        /// </summary>
        /// <param name="systemID"></param>
        /// <param name="data"></param>
        /// <param name="transaction"></param>
        public void Update(string systemID, T data, ITransaction transaction = null)
        {
            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                m_editQuery.Update(systemID, data, transactionProxy.Transaction);
                transactionProxy.ClearConditionCache = true;

                transactionProxy.TaskQueue.Enqueue(new Task((state) =>
                {
                    T data = (T)state;
                    (bool exists, _) = m_keyCache.TryGetValue<T>(data.ID.ToSystemObjectID(systemID));

                    if (exists)
                        m_keyCache.Set(data.ID.ToSystemObjectID(systemID), data);
                }, data));
            }
            else
            {
                m_editQuery.Update(systemID, data, transaction);
                (bool exists, _) = m_keyCache.TryGetValue<T>(data.ID.ToSystemObjectID(systemID));

                if (exists)
                    m_keyCache.Set(data.ID.ToSystemObjectID(systemID), data);

                m_conditionCache.Clear();
            }
        }
        /// <summary>
        /// 事务中异步修改
        /// </summary>
        /// <param name="data"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public Task UpdateAsync(T data, ITransaction transaction = null)
        {
            return UpdateAsync(string.Empty, data, transaction);
        }
        /// <summary>
        /// 使用代理事务修改
        /// </summary>
        /// <param name="systemID"></param>
        /// <param name="data"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async Task UpdateAsync(string systemID, T data, ITransaction transaction = null)
        {
            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                await m_editQuery.UpdateAsync(systemID, data, transactionProxy.Transaction);
                transactionProxy.ClearConditionCache = true;

                transactionProxy.TaskQueue.Enqueue(new Task(async (state) =>
                {
                    T data = (T)state;
                    (bool exists, _) = await m_keyCache.TryGetValueAsync<T>(data.ID.ToSystemObjectID(systemID));

                    if (exists)
                        await m_keyCache.SetAsync(data.ID.ToSystemObjectID(systemID), data);
                }, data));
            }
            else
            {
                await m_editQuery.UpdateAsync(systemID, data, transaction);
                (bool exists, _) = await m_keyCache.TryGetValueAsync<T>(data.ID.ToSystemObjectID(systemID));

                if (exists)
                    await m_keyCache.SetAsync(data.ID.ToSystemObjectID(systemID), data);

                await m_conditionCache.ClearAsync();
            }
        }
        /// <summary>
        /// 事务中满足条件的修改
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="updateDictionary"></param>
        /// <param name="transaction"></param>
        public void Update(Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null)
        {
            Update(string.Empty, predicate, updateDictionary, transaction);
        }
        /// <summary>
        /// 事务中满足条件的修改
        /// </summary>
        /// <param name="systemID"></param>
        /// <param name="predicate"></param>
        /// <param name="updateDictionary"></param>
        /// <param name="transaction"></param>
        public void Update(string systemID, Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null)
        {
            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                m_editQuery.Update(systemID, predicate, updateDictionary, transactionProxy.Transaction);
                transactionProxy.ClearKeyCache = true;
                transactionProxy.ClearConditionCache = true;
            }
            else
            {
                m_editQuery.Update(systemID, predicate, updateDictionary, transaction);
                m_keyCache.Clear();
                m_conditionCache.Clear();
            }
        }
        /// <summary>
        /// 事务中有条件的修改
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="updateDictionary"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public Task UpdateAsync(Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null)
        {
            return UpdateAsync(string.Empty, predicate, updateDictionary, transaction);
        }
        /// <summary>
        /// 代理事务中有条件的修稿
        /// </summary>
        /// <param name="systemID"></param>
        /// <param name="predicate"></param>
        /// <param name="updateDictionary"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async Task UpdateAsync(string systemID, Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null)
        {
            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                await m_editQuery.UpdateAsync(systemID, predicate, updateDictionary, transactionProxy.Transaction);
                transactionProxy.ClearKeyCache = true;
                transactionProxy.ClearConditionCache = true;
            }
            else
            {
                await m_editQuery.UpdateAsync(systemID, predicate, updateDictionary, transaction);
                await m_keyCache.ClearAsync();
                await m_conditionCache.ClearAsync();
            }
        }
    }
}