namespace Common.Lock
{
    /// <summary>
    /// 锁工厂
    /// </summary>
    public static class LockFactory
    {
        /// <summary>
        /// 获取Consul锁
        /// </summary>
        /// <returns></returns>
        public static ILock GetConsulLock()
        {
            return new ConsulLock();
        }

        /// <summary>
        /// 获取Redis锁
        /// </summary>
        /// <returns></returns>
        public static ILock GetRedisLock()
        {
            return new RedisLock();
        }
    }
}