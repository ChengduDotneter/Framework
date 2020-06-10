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
        private const int TASK_TIME_SPAN = 1;
        private int m_identity;

        public async Task<bool> Apply(int identity, int timeOut)
        {
            if (m_identity == 0 ||
                m_identity == identity)
            {
                m_identity = identity;
                return true;
            }

            int time = Environment.TickCount;

            while (m_identity != 0 &&
                   Environment.TickCount - time < timeOut)
                await Task.Delay(TASK_TIME_SPAN);

            if (m_identity != 0)
                return false;

            m_identity = identity;
            return true;
        }

        public Task Release(int identity)
        {
            if (m_identity != identity)
                return Task.CompletedTask;

            m_identity = 0;
            return Task.CompletedTask;
        }
    }
}
