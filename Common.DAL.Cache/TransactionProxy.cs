using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Common.DAL.Cache
{
    /// <summary>
    /// 事务代理器
    /// </summary>
    public class TransactionProxy : ITransaction
    {
        private const int THREAD_TIME_SPAN = 1;
        private ITransaction m_transaction;
        private ConcurrentQueue<Action> m_actions;
        private static readonly Mutex m_mutex;

        /// <summary>
        /// 
        /// </summary>
        public static TransactionProxy Instance { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public static bool InTransaction { get; private set; }

        static TransactionProxy()
        {
            m_mutex = new Mutex();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="transaction"></param>
        public TransactionProxy(ITransaction transaction)
        {
            m_mutex.WaitOne();
            m_transaction = transaction;
            m_actions = new ConcurrentQueue<Action>();
            Instance = this;
            InTransaction = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        public void AddAction(Action action)
        {
            m_actions.Enqueue(action);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public object Context()
        {
            return m_transaction.Context();
        }

        /// <summary>
        /// 回收
        /// </summary>
        public void Dispose()
        {
            m_transaction.Dispose();
            InTransaction = false;
            m_mutex.ReleaseMutex();
        }

        /// <summary>
        /// 回滚
        /// </summary>
        public void Rollback()
        {
            m_transaction.Rollback();
        }

        /// <summary>
        /// 提交
        /// </summary>
        public void Submit()
        {
            m_transaction.Submit();

            while (!m_actions.IsEmpty)
            {
                if (!m_actions.TryDequeue(out Action action))
                    Thread.Sleep(THREAD_TIME_SPAN);
                else
                    action.Invoke();
            }
        }
    }
}
