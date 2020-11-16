using System.Threading.Tasks;

namespace Common.Lock
{
    /// <summary>
    /// 分布式锁
    /// </summary>
    public interface ILock
    {
        /// <summary>
        /// 申请锁
        /// </summary>
        /// <param name="key">尝试锁定的资源KEY</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <returns></returns>
        bool Acquire(string key, string identity, int weight, int timeOut);

        /// <summary>
        /// 申请锁
        /// </summary>
        /// <param name="key">尝试锁定的资源KEY</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <returns></returns>
        Task<bool> AcquireAsync(string key, string identity, int weight, int timeOut);

        /// <summary>
        /// 释放锁
        /// </summary>
        /// <param name="identity">锁ID</param>
        void Release(string identity);

        /// <summary>
        /// 释放锁
        /// </summary>
        /// <param name="identity">锁ID</param>
        Task ReleaseAsync(string identity);
    }
}