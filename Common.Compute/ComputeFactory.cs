namespace Common.Compute
{
    public static class ComputeFactory
    {
        public static ICompute GetIgniteCompute()
        {
            return IgniteTask.CreateCompute();
        }

        public static IMapReduce GetIgniteMapReduce()
        {
            return IgniteTask.CreateMapReduce();
        }

        public static IAsyncMapReduce GetIgniteAsyncMapReduce()
        {
            return IgniteTask.CreateAsyncMapReduce();
        }
    }
}
