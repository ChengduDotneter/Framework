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
        private readonly int DEADLOCK_DETECTION_KEY;
        private readonly string PRIMARY_KEY;
        private const long DEFAULT_IDENTITY = -1L;
        private const int TASK_TIME_SPAN = 1;
        private long m_identity = DEFAULT_IDENTITY;

        Resource()
        {
            DEADLOCK_DETECTION_KEY = nameof(IDeadlockDetection).GetHashCode();
            PRIMARY_KEY = GrainReference.GetPrimaryKeyString();
        }

        public async Task<bool> Apply(long identity, int timeOut)
        {
            //TODO: WEIGHT
            await GrainFactory.GetGrain<IDeadlockDetection>(DEADLOCK_DETECTION_KEY).EnterLock(identity, PRIMARY_KEY, int.MaxValue);

            Console.WriteLine($"{identity} apply {GrainReference.GetPrimaryKeyString()}");

            

            if (m_identity == DEFAULT_IDENTITY ||
                m_identity == identity)
            {
                m_identity = identity;


                Console.WriteLine($"{identity} apply successed {GrainReference.GetPrimaryKeyString()}");

                return true;
            }

            int time = Environment.TickCount;

            while (m_identity != DEFAULT_IDENTITY &&
                   Environment.TickCount - time < timeOut)
                await Task.Delay(TASK_TIME_SPAN);

            if (m_identity != DEFAULT_IDENTITY)
            {

                Console.WriteLine($"{identity} apply timeout {GrainReference.GetPrimaryKeyString()} end total time {Environment.TickCount - time}");






                return false;
            }

            m_identity = identity;


            Console.WriteLine($"{identity} apply {GrainReference.GetPrimaryKeyString()} end total time {Environment.TickCount - time}");

            return true;
        }

        public async Task Release(long identity)
        {
            if (m_identity != identity)
                return;

            m_identity = DEFAULT_IDENTITY;
            await GrainFactory.GetGrain<IDeadlockDetection>(DEADLOCK_DETECTION_KEY).ExitLock(identity, PRIMARY_KEY);


            Console.WriteLine($"{identity} release {GrainReference.GetPrimaryKeyString()}");
        }

        public Task ConflictResolution(long identity)
        {

            throw new NotImplementedException();
        }
    }
}
