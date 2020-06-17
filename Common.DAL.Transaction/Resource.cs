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
        /// 等待时间超时间隔
        /// </summary>
        private const int TASK_TIME_SPAN = 1;

        /// <summary>
        /// 需要释放的线程ID
        /// </summary>
        private long m_destoryIdentity;

        /// <summary>
        /// 可以继续的线程ID
        /// </summary>
        private long m_continueIdentity;

        public Resource(string primaryKey)
        {
            PRIMARY_KEY = primaryKey;

            ResourceDetection.DeQueueEvent += parameter =>
            {
                switch (parameter.QueueDataType)
                {
                    case QueueDataTypeEnum2.Apply:
                        if (parameter.ResourceName == PRIMARY_KEY)
                            m_continueIdentity = parameter.Identity;
                        break;

                    case QueueDataTypeEnum2.Release:
                        m_destoryIdentity = parameter.Identity;
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
            //Console.WriteLine($"{identity}  apply resource {PRIMARY_KEY} start m_destoryIdentity {m_destoryIdentity} m_continueIdentity {m_continueIdentity}");

            await ResourceDetection.EnQueued(new EnQueueData2()
            {
                Identity = identity,
                ResourceName = PRIMARY_KEY,
                Weight = weight,
                TimeOutTick = timeOut,
                QueueDataType = QueueDataTypeEnum2.Apply,
                EnQueueTick = DateTime.Now.Ticks
            });

            int time = Environment.TickCount;

            while (Environment.TickCount - time < timeOut)
            {
                if (m_continueIdentity == identity)
                {
                    //Console.WriteLine($" 申请资源成功 identity {identity} m_continueIdentity {m_continueIdentity} m_destoryIdentity {m_destoryIdentity} resource {PRIMARY_KEY} ");
                    //Console.WriteLine($" 申请成功时间 {Environment.TickCount - time}");
                    return true;
                }

                if (m_destoryIdentity == identity)
                {
                    //Console.WriteLine($" 申请资源失败 identity {identity} m_continueIdentity {m_continueIdentity} m_destoryIdentity {m_destoryIdentity} resource {PRIMARY_KEY}");
                    //Console.WriteLine($" 申请失败时间 {Environment.TickCount - time}");
                    return false;
                }

                await Task.Delay(TASK_TIME_SPAN);
            }

            //Console.WriteLine($" 申请资源失败最后 identity {identity} m_continueIdentity {m_continueIdentity} m_destoryIdentity {m_destoryIdentity} resource {PRIMARY_KEY}");
            //Console.WriteLine($" 申请超时失败时间 {Environment.TickCount - time}");
            return false;
        }
    }
}