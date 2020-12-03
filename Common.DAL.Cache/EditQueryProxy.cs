using System;
using System.Collections.Generic;
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

        public ITransaction BeginTransaction(bool distributedLock = true, int weight = 0)
        {
            return new TransactionProxy(m_editQuery.BeginTransaction(distributedLock, weight), m_keyCache, m_conditionCache);
        }

        public async Task<ITransaction> BeginTransactionAsync(bool distributedLock = true, int weight = 0)
        {
            return new TransactionProxy(await m_editQuery.BeginTransactionAsync(distributedLock, weight), m_keyCache, m_conditionCache);
        }

        public void Delete(ITransaction transaction = null, params long[] ids)
        {
            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                m_editQuery.Delete(transactionProxy.Transaction, ids);
                transactionProxy.ClearConditionCache = true;
                transactionProxy.TaskQueue.Enqueue(new Task((state) =>
                {
                    long[] ids = (long[])state;

                    for (int i = 0; i < ids.Length; i++)
                        m_keyCache.Remove(ids[i]);
                }, ids));
            }
            else
            {
                m_editQuery.Delete(transaction, ids);

                for (int i = 0; i < ids.Length; i++)
                    m_keyCache.Remove(ids[i]);

                m_conditionCache.Clear();
            }
        }

        public async Task DeleteAsync(ITransaction transaction = null, params long[] ids)
        {
            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                await m_editQuery.DeleteAsync(transactionProxy.Transaction, ids);

                transactionProxy.ClearConditionCache = true;
                transactionProxy.TaskQueue.Enqueue(new Task(async (state) =>
                {
                    IList<Task> tasks = new List<Task>();

                    for (int i = 0; i < ids.Length; i++)
                        tasks.Add(m_keyCache.RemoveAsync(ids[i]));

                    tasks.Add(m_conditionCache.ClearAsync());
                    await Task.WhenAll(tasks);
                }, ids));
            }
            else
            {
                await m_editQuery.DeleteAsync(transaction, ids);

                IList<Task> tasks = new List<Task>();

                for (int i = 0; i < ids.Length; i++)
                    tasks.Add(m_keyCache.RemoveAsync(ids[i]));

                tasks.Add(m_conditionCache.ClearAsync());
                await Task.WhenAll(tasks);
            }
        }

        public void Insert(ITransaction transaction = null, params T[] datas)
        {
            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                m_editQuery.Insert(transactionProxy.Transaction, datas);
                transactionProxy.ClearConditionCache = true;
            }
            else
            {
                m_editQuery.Insert(transaction, datas);
                m_conditionCache.Clear();
            }
        }

        public async Task InsertAsync(ITransaction transaction = null, params T[] datas)
        {
            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                await m_editQuery.InsertAsync(transactionProxy.Transaction, datas);
                transactionProxy.ClearConditionCache = true;
            }
            else
            {
                await m_editQuery.InsertAsync(transaction, datas);
                await m_conditionCache.ClearAsync();
            }
        }

        public void Merge(ITransaction transaction = null, params T[] datas)
        {
            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                m_editQuery.Merge(transactionProxy.Transaction, datas);
                transactionProxy.ClearConditionCache = true;

                transactionProxy.TaskQueue.Enqueue(new Task((state) =>
                {
                    T[] datas = (T[])state;

                    for (int i = 0; i < datas.Length; i++)
                    {
                        (bool exists, T result) = m_keyCache.TryGetValue<T>(datas[i].ID);

                        if (exists)
                            m_keyCache.Set(datas[i].ID, datas[i]);
                    }
                }, datas));
            }
            else
            {
                m_editQuery.Merge(transaction, datas);

                for (int i = 0; i < datas.Length; i++)
                {
                    (bool exists, T result) = m_keyCache.TryGetValue<T>(datas[i].ID);

                    if (exists)
                        m_keyCache.Set(datas[i].ID, datas[i]);
                }

                m_conditionCache.Clear();
            }
        }

        public async Task MergeAsync(ITransaction transaction = null, params T[] datas)
        {
            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                await m_editQuery.MergeAsync(transactionProxy.Transaction, datas);
                transactionProxy.ClearConditionCache = true;

                transactionProxy.TaskQueue.Enqueue(new Task(async (state) =>
                {
                    T[] datas = (T[])state;

                    IList<Task> tasks = new List<Task>();

                    for (int i = 0; i < datas.Length; i++)
                    {
                        tasks.Add(Task.Factory.StartNew(async () =>
                        {
                            (bool exists, T result) = await m_keyCache.TryGetValueAsync<T>(datas[i].ID);

                            if (exists)
                                await m_keyCache.SetAsync(datas[i].ID, datas[i]);
                        }));
                    }

                    await Task.WhenAll(tasks);
                }, datas));
            }
            else
            {
                await m_editQuery.MergeAsync(transaction, datas);

                IList<Task> tasks = new List<Task>();

                for (int i = 0; i < datas.Length; i++)
                {
                    tasks.Add(Task.Factory.StartNew(async () =>
                    {
                        (bool exists, T result) = await m_keyCache.TryGetValueAsync<T>(datas[i].ID);

                        if (exists)
                            await m_keyCache.SetAsync(datas[i].ID, datas[i]);
                    }));
                }

                tasks.Add(m_conditionCache.ClearAsync());
                await Task.WhenAll(tasks);
            }
        }

        public void Update(T data, ITransaction transaction = null)
        {
            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                m_editQuery.Update(data, transactionProxy.Transaction);
                transactionProxy.ClearConditionCache = true;

                transactionProxy.TaskQueue.Enqueue(new Task((state) =>
                {
                    T data = (T)state;
                    (bool exists, T result) = m_keyCache.TryGetValue<T>(data.ID);

                    if (exists)
                        m_keyCache.Set(data.ID, data);
                }, data));
            }
            else
            {
                m_editQuery.Update(data, transaction);
                (bool exists, T result) = m_keyCache.TryGetValue<T>(data.ID);

                if (exists)
                    m_keyCache.Set(data.ID, data);

                m_conditionCache.Clear();
            }
        }

        public async Task UpdateAsync(T data, ITransaction transaction = null)
        {
            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                await m_editQuery.UpdateAsync(data, transactionProxy.Transaction);
                transactionProxy.ClearConditionCache = true;

                transactionProxy.TaskQueue.Enqueue(new Task(async (state) =>
                {
                    T data = (T)state;
                    (bool exists, T result) = await m_keyCache.TryGetValueAsync<T>(data.ID);

                    if (exists)
                        await m_keyCache.SetAsync(data.ID, data);
                }, data));
            }
            else
            {
                await m_editQuery.UpdateAsync(data, transaction);
                (bool exists, T result) = await m_keyCache.TryGetValueAsync<T>(data.ID);

                if (exists)
                    await m_keyCache.SetAsync(data.ID, data);

                await m_conditionCache.ClearAsync();
            }
        }

        public void Update(Expression<Func<T, bool>> predicate, IDictionary<string, object> upateDictionary, ITransaction transaction = null)
        {
            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                m_editQuery.Update(predicate, upateDictionary, transactionProxy.Transaction);
                transactionProxy.ClearKeyCache = true;
                transactionProxy.ClearConditionCache = true;
            }
            else
            {
                m_editQuery.Update(predicate, upateDictionary, transaction);
                m_keyCache.Clear();
                m_conditionCache.Clear();
            }
        }

        public async Task UpdateAsync(Expression<Func<T, bool>> predicate, IDictionary<string, object> upateDictionary, ITransaction transaction = null)
        {
            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                await m_editQuery.UpdateAsync(predicate, upateDictionary, transactionProxy.Transaction);
                transactionProxy.ClearKeyCache = true;
                transactionProxy.ClearConditionCache = true;
            }
            else
            {
                await m_editQuery.UpdateAsync(predicate, upateDictionary, transaction);
                await m_keyCache.ClearAsync();
                await m_conditionCache.ClearAsync();
            }
        }
    }
}