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

            if (!ClearKeyCache && !ClearConditionCache)
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

        public ITransaction BeginTransaction(int weight = 0)
        {
            return new TransactionProxy(m_editQuery.BeginTransaction(weight), m_keyCache, m_conditionCache);
        }

        public async Task<ITransaction> BeginTransactionAsync(int weight = 0)
        {
            return new TransactionProxy(await m_editQuery.BeginTransactionAsync(weight), m_keyCache, m_conditionCache);
        }

        public void Delete(ITransaction transaction = null, params long[] ids)
        {
            m_editQuery.Delete(transaction, ids);

            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
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
                for (int i = 0; i < ids.Length; i++)
                    m_keyCache.Remove(ids[i]);

                m_conditionCache.Clear();
            }
        }

        public async Task DeleteAsync(ITransaction transaction = null, params long[] ids)
        {
            await m_editQuery.DeleteAsync(transaction, ids);

            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
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
                IList<Task> tasks = new List<Task>();

                for (int i = 0; i < ids.Length; i++)
                    tasks.Add(m_keyCache.RemoveAsync(ids[i]));

                tasks.Add(m_conditionCache.ClearAsync());
                await Task.WhenAll(tasks);
            }
        }

        public void Insert(ITransaction transaction = null, params T[] datas)
        {
            m_editQuery.Insert(transaction, datas);

            if (transaction != null && transaction is TransactionProxy transactionProxy)
                transactionProxy.ClearConditionCache = true;
            else
                m_conditionCache.Clear();
        }

        public async Task InsertAsync(ITransaction transaction = null, params T[] datas)
        {
            await m_editQuery.InsertAsync(transaction, datas);

            if (transaction != null && transaction is TransactionProxy transactionProxy)
                transactionProxy.ClearConditionCache = true;
            else
                await m_conditionCache.ClearAsync();
        }

        public void Merge(ITransaction transaction = null, params T[] datas)
        {
            m_editQuery.Merge(transaction, datas);

            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                transactionProxy.ClearConditionCache = true;

                transactionProxy.TaskQueue.Enqueue(new Task((state) =>
                {
                    T[] datas = (T[])state;

                    for (int i = 0; i < datas.Length; i++)
                        if (m_keyCache.TryGetValue(datas[i].ID, out T _))
                            m_keyCache.Set(datas[i].ID, datas[i]);
                }, datas));
            }
            else
            {
                for (int i = 0; i < datas.Length; i++)
                    if (m_keyCache.TryGetValue(datas[i].ID, out T _))
                        m_keyCache.Set(datas[i].ID, datas[i]);

                m_conditionCache.Clear();
            }
        }

        public async Task MergeAsync(ITransaction transaction = null, params T[] datas)
        {
            await m_editQuery.MergeAsync(transaction, datas);

            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                transactionProxy.ClearConditionCache = true;

                transactionProxy.TaskQueue.Enqueue(new Task(async (state) =>
                {
                    T[] datas = (T[])state;

                    IList<Task> tasks = new List<Task>();

                    for (int i = 0; i < datas.Length; i++)
                    {
                        tasks.Add(Task.Factory.StartNew(async () =>
                        {
                            if (await m_keyCache.TryGetValueAsync(datas[i], out T _))
                                await m_keyCache.SetAsync(datas[i].ID, datas[i]);
                        }));
                    }

                    await Task.WhenAll(tasks);
                }, datas));
            }
            else
            {
                IList<Task> tasks = new List<Task>();

                for (int i = 0; i < datas.Length; i++)
                {
                    tasks.Add(Task.Factory.StartNew(async () =>
                    {
                        if (await m_keyCache.TryGetValueAsync(datas[i], out T _))
                            await m_keyCache.SetAsync(datas[i].ID, datas[i]);
                    }));
                }

                tasks.Add(m_conditionCache.ClearAsync());
                await Task.WhenAll(tasks);
            }
        }

        public void Update(T data, ITransaction transaction = null)
        {
            m_editQuery.Update(data, transaction);

            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                transactionProxy.ClearConditionCache = true;

                transactionProxy.TaskQueue.Enqueue(new Task((state) =>
                {
                    T data = (T)state;

                    if (m_keyCache.TryGetValue(data.ID, out T _))
                        m_keyCache.Set(data.ID, data);
                }, data));
            }
            else
            {
                if (m_keyCache.TryGetValue(data.ID, out T _))
                    m_keyCache.Set(data.ID, data);

                m_conditionCache.Clear();
            }
        }

        public async Task UpdateAsync(T data, ITransaction transaction = null)
        {
            await m_editQuery.UpdateAsync(data, transaction);

            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                transactionProxy.ClearConditionCache = true;

                transactionProxy.TaskQueue.Enqueue(new Task(async (state) =>
                {
                    T data = (T)state;

                    if (await m_keyCache.TryGetValueAsync(data.ID, out T _))
                        await m_keyCache.SetAsync(data.ID, data);
                }, data));
            }
            else
            {
                if (await m_keyCache.TryGetValueAsync(data.ID, out T _))
                    await m_keyCache.SetAsync(data.ID, data);

                await m_conditionCache.ClearAsync();
            }
        }

        public void Update(Expression<Func<T, bool>> predicate, IDictionary<string, object> upateDictionary, ITransaction transaction = null)
        {
            m_editQuery.Update(predicate, upateDictionary, transaction);

            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                transactionProxy.ClearKeyCache = true;
                transactionProxy.ClearConditionCache = true;
            }
            else
            {
                m_keyCache.Clear();
                m_conditionCache.Clear();
            }
        }

        public async Task UpdateAsync(Expression<Func<T, bool>> predicate, IDictionary<string, object> upateDictionary, ITransaction transaction = null)
        {
            await m_editQuery.UpdateAsync(predicate, upateDictionary, transaction);

            if (transaction != null && transaction is TransactionProxy transactionProxy)
            {
                transactionProxy.ClearKeyCache = true;
                transactionProxy.ClearConditionCache = true;
            }
            else
            {
                await m_keyCache.ClearAsync();
                await m_conditionCache.ClearAsync();
            }
        }
    }
}