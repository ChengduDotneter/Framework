namespace Common.Compute
{
    /// <summary>
    /// 并行计算工厂
    /// </summary>
    public static class ComputeFactory
    {
        /// <summary>
        /// 创建并行计算
        /// </summary>
        public static ICompute GetIgniteCompute()
        {
            return IgniteTask.CreateCompute();
        }

        /// <summary>
        /// 创建同步MapReduce
        /// </summary>
        public static IMapReduce GetIgniteMapReduce()
        {
            return IgniteTask.CreateMapReduce();
        }

        /// <summary>
        /// 创建异步MapReduce
        /// </summary>
        public static IAsyncMapReduce GetIgniteAsyncMapReduce()
        {
            return IgniteTask.CreateAsyncMapReduce();
        }
    }
}
