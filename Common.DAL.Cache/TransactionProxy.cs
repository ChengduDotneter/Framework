using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Common.DAL.Cache
{
    public class TransactionProxy : ITransaction
    {
        private const int THREAD_TIME_SPAN = 1;
        private ITransaction m_transaction;
        private ConcurrentQueue<Action> m_actions;
        private static readonly Mutex m_mutex;

        public static TransactionProxy Instance { get; private set; }
        public static bool InTransaction { get; private set; }

        static TransactionProxy()
        {
            m_mutex = new Mutex();
        }

        public TransactionProxy(ITransaction transaction)
        {
            m_mutex.WaitOne();
            m_transaction = transaction;
            m_actions = new ConcurrentQueue<Action>();
            Instance = this;
            InTransaction = true;
        }

        public void AddAction(Action action)
        {
            m_actions.Enqueue(action);
        }

        public object Context()
        {
            return m_transaction.Context();
        }

        public void Dispose()
        {
            m_transaction.Dispose();
            InTransaction = false;
            m_mutex.ReleaseMutex();
        }

        public void Rollback()
        {
            m_transaction.Rollback();
        }

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
