using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;

namespace Common.DAL.Transaction
{
    [Reentrant]
    public class Resource : Grain, IResource
    {
        private readonly int DEADLOCK_DETECTION_KEY;
        private readonly string PRIMARY_KEY;
        private const long DEFAULT_IDENTITY = -1L;
        private const int TASK_TIME_SPAN = 1;
        private long m_identity = DEFAULT_IDENTITY;
        private long m_destoryIdentity;

        Resource()
        {
            DEADLOCK_DETECTION_KEY = nameof(IDeadlockDetection).GetHashCode();
            PRIMARY_KEY = GrainReference.GetPrimaryKeyString();
        }

        public async Task<bool> Apply(long identity, int weight, int timeOut)
        {
            await GrainFactory.GetGrain<IDeadlockDetection>(DEADLOCK_DETECTION_KEY).EnterLock(identity, PRIMARY_KEY, weight);

            if (m_identity == DEFAULT_IDENTITY ||
                m_identity == identity)
            {
                m_identity = identity;
                return true;
            }

            int time = Environment.TickCount;

            while (m_identity != DEFAULT_IDENTITY && Environment.TickCount - time < timeOut && m_destoryIdentity != identity)
                await Task.Delay(TASK_TIME_SPAN);

            if (m_identity != DEFAULT_IDENTITY || m_destoryIdentity == identity)
            {
                return false;
            }

            m_identity = identity;
            return true;
        }

        public async Task Release(long identity)
        {
            if (m_identity != identity)
                return;

            m_identity = DEFAULT_IDENTITY;
            await GrainFactory.GetGrain<IDeadlockDetection>(DEADLOCK_DETECTION_KEY).ExitLock(identity, PRIMARY_KEY);
        }

        public Task ConflictResolution(long identity)
        {
            m_destoryIdentity = identity;
            return Task.CompletedTask;
        }
    }
}
