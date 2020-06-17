using Orleans;
using Orleans.Concurrency;
using System;
using System.Threading.Tasks;

namespace Common.DAL.Transaction
{
    //TODO: 去掉打印
    /// <summary>
    /// 事务资源操作Grain类
    /// </summary>
    [Reentrant]
    public class Resource : Grain, IResource
    {
        /// <summary>
        /// 死锁检测Grain的Key
        /// </summary>
        private readonly int DEADLOCK_DETECTION_KEY;

        /// <summary>
        /// 当前事务Grain的唯一Key，现包括表名
        /// </summary>
        private string PRIMARY_KEY;

        /// <summary>
        /// 默认事务线程ID -1 该资源未开启事务
        /// </summary>
        private const long DEFAULT_IDENTITY = -1L;

        /// <summary>
        /// 等待时间超时间隔
        /// </summary>
        private const int TASK_TIME_SPAN = 1;

        /// <summary>
        /// 当前事务身份ID
        /// </summary>
        private long m_identity = DEFAULT_IDENTITY;

        /// <summary>
        /// 需要释放的事务身份ID
        /// </summary>
        private long m_destoryIdentity;

        public Resource()
        {
            DEADLOCK_DETECTION_KEY = nameof(IDeadlockDetection).GetHashCode();
        }

        public override Task OnActivateAsync()
        {
            PRIMARY_KEY = GrainReference.GetPrimaryKeyString();
            return base.OnActivateAsync();
        }

        /// <summary>
        /// 申请事务资源
        /// </summary>
        /// <param name="identity">事务线程ID</param>
        /// <param name="weight">权重</param>
        /// <param name="timeOut">超时时间</param>
        /// <returns></returns>
        public async Task<bool> Apply(long identity, int weight, int timeOut)
        {
            //Console.WriteLine($"{identity} {PRIMARY_KEY} apply start");

            await GrainFactory.GetGrain<IDeadlockDetection>(DEADLOCK_DETECTION_KEY).EnterLock(identity, PRIMARY_KEY, weight);

            if (m_identity == DEFAULT_IDENTITY ||
                m_identity == identity)
            {
                m_identity = identity;

                //Console.WriteLine($"{identity} {PRIMARY_KEY} apply successed");

                return true;
            }

            int time = Environment.TickCount;

            while (m_identity != DEFAULT_IDENTITY && Environment.TickCount - time < timeOut && m_destoryIdentity != identity)
                await Task.Delay(TASK_TIME_SPAN);

            if (m_identity != DEFAULT_IDENTITY || m_destoryIdentity == identity)
            {
                if (m_destoryIdentity != identity)
                {
                    Console.WriteLine($"{identity} {PRIMARY_KEY} apply faild time out");
                }
                else
                {
                    await Release(m_destoryIdentity);
                    m_destoryIdentity = DEFAULT_IDENTITY;
                    Console.WriteLine($"{identity} {PRIMARY_KEY} apply faild deadlock");
                }

                return false;
            }

            m_identity = identity;

            //Console.WriteLine($"{identity} {PRIMARY_KEY} apply successed");

            return true;
        }

        /// <summary>
        /// 释放线程事务资源
        /// </summary>
        /// <param name="identity">事务线程ID</param>
        /// <returns></returns>
        public async Task Release(long identity)
        {
            if (m_identity != identity)
                return;

            m_identity = DEFAULT_IDENTITY;
            await GrainFactory.GetGrain<IDeadlockDetection>(DEADLOCK_DETECTION_KEY).ExitLock(identity, PRIMARY_KEY);
        }

        /// <summary>
        /// 死锁检测回调资源释放策略
        /// </summary>
        /// <param name="identity">线程事务ID</param>
        /// <returns></returns>
        public Task ConflictResolution(long identity)
        {
            m_destoryIdentity = identity;

            return Task.CompletedTask;
        }
    }
}