using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.DAL.Transaction
{
    class DeadLockDetection : Grain, IDeadlockDetection
    {
        private const int THREAD_TIME_SPAN = 1;
        private Thread m_checkThread;
        private IDictionary<long, LockInstance> m_locks;

        private class LockInstance
        {
            public int Weight { get; }
            public HashSet<string> Resources { get; }

            public LockInstance(int weight)
            {
                Weight = weight;
                Resources = new HashSet<string>();
            }
        }

        DeadLockDetection()
        {
            m_locks = new Dictionary<long, LockInstance>();
            m_checkThread = new Thread(Check);
            m_checkThread.IsBackground = true;
            m_checkThread.Name = "CHECK_THREAD";
            m_checkThread.Start();
        }

        public Task EnterLock(long identity, string resourceName, int weight)
        {
            if (!m_locks.ContainsKey(identity))
                m_locks.Add(identity, new LockInstance(weight));

            m_locks[identity].Resources.Add(resourceName);
            return Task.CompletedTask;
        }

        public Task ExitLock(long identity, string resourceName)
        {
            if (!m_locks.ContainsKey(identity))
                return Task.CompletedTask;

            m_locks[identity].Resources.Remove(resourceName);

            if (m_locks[identity].Resources.Count == 0)
                m_locks.Remove(identity);

            return Task.CompletedTask;
        }

        private void Check()
        {
            while (true)
            {



                Thread.Sleep(THREAD_TIME_SPAN);
            }
        }

        private async Task ConflictResolution(long identity, string resourceName)
        {
            await GrainFactory.GetGrain<IResource>(resourceName).ConflictResolution(identity);
        }
    }
}
