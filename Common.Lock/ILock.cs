using System.Threading.Tasks;

namespace Common.Lock
{
    /// <summary>
    /// 分布式锁
    /// </summary>
    public interface ILock
    {
        /// <summary>
        /// 互斥锁同步申请锁资源
        /// </summary>
        /// <param name="key">尝试锁定的资源KEY</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <returns></returns>
        bool AcquireMutex(string key, string identity, int weight, int timeOut);

        /// <summary>
        /// 互斥锁异步申请锁资源
        /// </summary>
        /// <param name="key">尝试锁定的资源KEY</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <returns></returns>
        Task<bool> AcquireMutexAsync(string key, string identity, int weight, int timeOut);

        /// <summary>
        /// 多写多读读锁同步申请
        /// </summary>
        /// <param name="groupKey">与一读多写读写锁互斥的唯一键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <returns></returns>
        bool AcquireReadLockWithGroupKey(string groupKey, string identity, int weight, int timeOut);


        /// <summary>
        /// 多写多读读锁异步申请
        /// </summary>
        /// <param name="groupKey">与一读多写读写锁互斥的唯一键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <returns></returns>
        Task<bool> AcquireReadLockWithGroupKeyAsync(string groupKey, string identity, int weight, int timeOut);

        /// <summary>
        /// 多写多读写锁同步申请
        /// </summary>
        /// <param name="groupKey">与一读多写读写锁互斥的唯一键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <returns></returns>
        bool AcquireWriteLockWithGroupKey(string groupKey, string identity, int weight, int timeOut);

        /// <summary>
        /// 多写多读写锁异步申请
        /// </summary>
        /// <param name="groupKey">与一读多写读写锁互斥的唯一键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <returns></returns>
        Task<bool> AcquireWriteLockWithGroupKeyAsync(string groupKey, string identity, int weight, int timeOut);

        /// <summary>
        /// 一写多读读锁同步申请
        /// </summary>
        /// <param name="groupKey">与多读多写读写锁互斥的唯一键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <param name="resourceKeys">所需锁的资源数组</param>
        /// <returns></returns>
        bool AcquireReadLockWithResourceKeys(string groupKey, string identity, int weight, int timeOut, params string[] resourceKeys);

        /// <summary>
        /// 一写多读读锁异步申请
        /// </summary>
        /// <param name="groupKey">与多读多写读写锁互斥的唯一键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <param name="resourceKeys">所需锁的资源数组</param>
        /// <returns></returns>
        Task<bool> AcquireReadLockWithResourceKeysAsync(string groupKey, string identity, int weight, int timeOut, params string[] resourceKeys);

        /// <summary>
        /// 一写多读写锁同步申请
        /// </summary>
        /// <param name="groupKey">与多读多写读写锁互斥的唯一键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <param name="resourceKeys">所需锁的资源数组</param>
        /// <returns></returns>
        bool AcquireWriteLockWithResourceKeys(string groupKey, string identity, int weight, int timeOut, params string[] resourceKeys);

        /// <summary>
        /// 一写多读写锁异步申请
        /// </summary>
        /// <param name="groupKey">与多读多写读写锁互斥的唯一键</param>
        /// <param name="identity">锁ID</param>
        /// <param name="weight">锁权重</param>
        /// <param name="timeOut">锁超时时间</param>
        /// <param name="resourceKeys">所需锁的资源数组</param>
        /// <returns></returns>
        Task<bool> AcquireWriteLockWithResourceKeysAsync(string groupKey, string identity, int weight, int timeOut, params string[] resourceKeys);

        /// <summary>
        /// 锁资源同步释放
        /// </summary>
        /// <param name="identity">锁ID</param>
        void Release(string identity);

        /// <summary>
        /// 锁资源异步释放
        /// </summary>
        /// <param name="identity">锁ID</param>
        Task ReleaseAsync(string identity);
    }
}