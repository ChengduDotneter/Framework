using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;

namespace Common.DAL.Transaction
{
    //TODO: 死锁检测
    [Reentrant]
    public class Resource : Grain, IResource
    {
        private const long DEFAULT_IDENTITY = -1L;
        private const int TASK_TIME_SPAN = 1;
        private long m_identity = DEFAULT_IDENTITY;

        public async Task<bool> Apply(long identity, int timeOut)
        {
            if (m_identity == DEFAULT_IDENTITY ||
                m_identity == identity)
            {
                m_identity = identity;
                return true;
            }

            int time = Environment.TickCount;

            while (m_identity != DEFAULT_IDENTITY &&
                   Environment.TickCount - time < timeOut)
                await Task.Delay(TASK_TIME_SPAN);

            if (m_identity != DEFAULT_IDENTITY)
                return false;

            m_identity = identity;
            return true;
        }

        public Task Release(long identity)
        {
            if (m_identity != identity)
                return Task.CompletedTask;

            m_identity = DEFAULT_IDENTITY;
            return Task.CompletedTask;
        }
    }
}
