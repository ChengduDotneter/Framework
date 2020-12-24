namespace Common.Lock
{
    /// <summary>
    /// 读写锁类型枚举
    /// </summary>
    internal enum ReadWriteLockMode
    {
        /// <summary>
        /// 读锁
        /// </summary>
        ReadLock,
        /// <summary>
        /// 写锁
        /// </summary>
        WriteLock
    }
}
