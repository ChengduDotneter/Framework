using System;
using System.Threading.Tasks;

namespace Common.DAL.Transaction
{
    //TODO: 去掉打印
    /// <summary>
    /// 事务资源操作Grain类
    /// </summary>
    public class Resource : IResource
    {
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
        /// 当前事务线程ID
        /// </summary>
        private long m_identity = DEFAULT_IDENTITY;

        /// <summary>
        /// 需要释放的线程ID
        /// </summary>
        private long m_destoryIdentity;

        /// <summary>
        /// 可以继续的线程ID
        /// </summary>
        private long m_continueIdentity;

        private object m_lockThis = new object();

        public Resource(string primaryKey)
        {
            PRIMARY_KEY = primaryKey;

            DeadLockDetection.DeQueueEvent += parameter =>
            {
                switch (parameter.QueueDataType)
                {
                    case QueueDataTypeEnum.Apply:
                        m_destoryIdentity = parameter.DestoryIdentity;
                        m_continueIdentity = parameter.ContinueIdentity;
                        break;

                    case QueueDataTypeEnum.Release:
                        if (m_identity == parameter.DestoryIdentity)
                        {
                            m_identity = DEFAULT_IDENTITY;
                            m_destoryIdentity = 0;
                            m_continueIdentity = 0;
                        }
                        break;

                    default:
                        break;
                }
            };
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

            await DeadLockDetection.EnQueued(new EnQueueData()
            {
                Identity = identity,
                ResourceName = PRIMARY_KEY,
                Weight = weight,
                TimeOutTick = timeOut,
                QueueDataType = QueueDataTypeEnum.Apply,
                EnQueueTick = DateTime.Now.Ticks
            });

            if (m_identity == DEFAULT_IDENTITY ||
                m_identity == identity)
            {
                m_identity = identity;

                //Console.WriteLine($"{identity} {PRIMARY_KEY} apply successed");

                return true;
            }

            int time = Environment.TickCount;

            while (Environment.TickCount - time < timeOut)
            {
                if (m_continueIdentity == identity)
                {
                    m_identity = identity;
                    return true;
                }

                if (m_destoryIdentity == identity)
                    return false;

                await Task.Delay(TASK_TIME_SPAN);
            }

            Console.WriteLine($" While： {Environment.TickCount - time} ");

            return false;
        }
    }
}