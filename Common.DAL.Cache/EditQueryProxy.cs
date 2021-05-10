using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Common.DAL.Cache
{
    internal class TransactionProxy : ITransaction
    {
        private ITransaction m_transaction;
        private ICache m_keyCache;
        private ICache m_conditionCache;

        public bool ClearKeyCache { get; set; }

        public bool ClearConditionCache { get; set; }

        public object Context { get { return m_transaction.Context; } }

        public Queue<Task> TaskQueue { get; }

        public ITransaction Transaction { get { return m_transaction; } }

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
            m_transaction.Rollback();
        }

        public Task RollbackAsync()
        {
            return m_transaction.RollbackAsync();
        }

        public void Submit()
        {
            m_transaction.Submit();

            if (ClearKeyCache)
                m_keyCache.Clear();

            if (ClearConditionCache)
                m_conditionCache.Clear();

            if (!ClearKeyCache && !ClearConditionCache)
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
            await m_transaction.SubmitAsync();

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

    internal class EditQueryProxy<T> : IEditQuery<T>
        where T : class, IEntity, new()
    {
        private IEditQuery<T> m_editQuery;
        private ICache m_keyCache;
        private ICache m_conditionCache;

        public EditQueryProxy(IEditQuery<T> editQuery, ICache keyCache, ICache conditionCache)
        {
            m_editQuery = editQuery;
            m_keyCache = keyCache;
            m_conditionCache = conditionCache;
        }

        public ITransaction BeginTransaction(bool distributedLock = false, int weight = 0)
        {
            return new TransactionProxy(m_editQuery.BeginTransaction(distributedLock, weight), m_keyCache, m_conditionCache);
        }

        public async Task<ITransaction> BeginTransactionAsync(bool distributedLock = false, int weight = 0)
        {
            return new TransactionProxy(await m_editQuery.BeginTransactionAsync(distributedLock, weight), m_keyCache, m_conditionCache);
        }

        public void Delete(ITransaction transaction = null, params long[] ids)
        {
            Delete(string.Empty, transaction, ids);
        }

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

        public Task DeleteAsync(ITransaction transaction = null, params long[] ids)
        {
            return DeleteAsync(string.Empty, transaction, ids);
        }

        public async Task DeleteAsync(string systemID, ITransaction transaction = null, params long[] ids)
        {
            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                await m_editQuery.DeleteAsync(systemID, transactionProxy.Transaction, ids);

                transactionProxy.ClearConditionCache = true;
                transactionProxy.TaskQueue.Enqueue(new Task(async (state) =>
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

        public void Insert(ITransaction transaction = null, params T[] datas)
        {
            Insert(string.Empty, transaction, datas);
        }

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

        public Task InsertAsync(ITransaction transaction = null, params T[] datas)
        {
            return InsertAsync(string.Empty, transaction, datas);
        }

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

        public void Merge(ITransaction transaction = null, params T[] datas)
        {
            Merge(string.Empty, transaction, datas);
        }

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

        public Task MergeAsync(ITransaction transaction = null, params T[] datas)
        {
            return MergeAsync(string.Empty, transaction, datas);
        }

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

        public void Update(T data, ITransaction transaction = null)
        {
            Update(string.Empty, data, transaction);
        }

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

        public Task UpdateAsync(T data, ITransaction transaction = null)
        {
            return UpdateAsync(string.Empty, data, transaction);
        }

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

        public void Update(Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null)
        {
            Update(string.Empty, predicate, updateDictionary, transaction);
        }

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

        public Task UpdateAsync(Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null)
        {
            return UpdateAsync(string.Empty, predicate, updateDictionary, transaction);
        }

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