using System;

namespace Common.DAL.Transaction
{
    /// <summary>
    /// 死锁检测接口
    /// </summary>
    public interface IDeadlockDetection
    {
        /// <summary>
        /// 资源申请订阅时间
        /// </summary>
        event Action<long, string, bool> ApplyResponsed;

        /// <summary>
        /// 事务申请资源时，进入死锁监测，监测是否出现死锁情况
        /// </summary>
        /// <param name="hostID">HOSTID</param>
        /// <param name="identity">事务线程ID</param>
        /// <param name="resourceName">事务申请的资源名，现包括数据表名</param>
        /// <param name="weight">事务权重</param>
        /// <param name="timeOut">超时时间</param>
        /// <returns></returns>
        void ApplyRequest(long hostID, long identity, string resourceName, int weight, int timeOut);

        /// <summary>
        /// 退出死锁监测，移除已有资源标识
        /// </summary>
        /// <param name="hostID">HOSTID</param>
        /// <param name="identity">事务线程ID</param>
        /// <returns></returns>
        void RemoveTranResource(long hostID, long identity);

        /// <summary>
        /// 强制关闭死锁监测，移除已有资源标识
        /// </summary>
        /// <param name="hostID">HOSTID</param>
        /// <returns></returns>
        void KillTranResource(long hostID);
    }
}