using Orleans;
using System.Threading.Tasks;

namespace Common.DAL.Transaction
{
    /// <summary>
    /// 死锁监测Grain接口
    /// </summary>
    internal interface IDeadlockDetection
    {
        /// <summary>
        /// 事务申请资源时，进入死锁监测，监测是否出现死锁情况
        /// </summary>
        /// <param name="identity">事务线程ID</param>
        /// <param name="resourceName">事务申请的资源名，现包括数据表名</param>
        /// <param name="weight">事务权重</param>
        /// <returns></returns>
        Task EnterLock(long identity, string resourceName, int weight);

        /// <summary>
        /// 退出死锁监测，移除已有资源标识
        /// </summary>
        /// <param name="identity">事务线程ID</param>
        /// <param name="resourceName">事务申请的资源名，现包括数据表名</param>
        /// <returns></returns>
        Task ExitLock(long identity);
    }
}