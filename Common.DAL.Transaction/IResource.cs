using System.Threading.Tasks;
using Orleans;

namespace Common.DAL.Transaction
{
    /// <summary>
    /// 资源Grains接口
    /// </summary>
    public interface IResource : IGrainWithStringKey
    {
        /// <summary>
        /// 申请事务资源
        /// </summary>
        /// <param name="identity">事务线程ID</param>
        /// <param name="weight">权重</param>
        /// <param name="timeOut">超时时间</param>
        /// <returns></returns>
        Task<bool> Apply(long identity, int weight, int timeOut);

        /// <summary>
        /// 释放线程事务资源
        /// </summary>
        /// <param name="identity">事务线程ID</param>
        /// <returns></returns>
        Task Release(long identity);

        /// <summary>
        /// 死锁检测回调资源释放策略
        /// </summary>
        /// <param name="identity">线程事务ID</param>
        /// <returns></returns>
        Task ConflictResolution(long identity);
    }
}
