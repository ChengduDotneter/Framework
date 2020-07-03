using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Compute;
using Apache.Ignite.Core.Discovery.Tcp;
using Apache.Ignite.Core.Discovery.Tcp.Multicast;

namespace Common.Compute
{
    internal static class IgniteTask
    {
        private readonly static IIgnite m_ignite;

        static IgniteTask()
        {
            IgniteConfiguration igniteConfiguration = new IgniteConfiguration()
            {
                Localhost = ConfigManager.Configuration["IgniteService:LocalHost"],

                IgniteInstanceName = "ComputeGrid",

                UserAttributes = new Dictionary<string, object>()
                {
                    ["NodeType"] = "ComputeGrid"
                },

                DiscoverySpi = new TcpDiscoverySpi()
                {
                    IpFinder = new TcpDiscoveryMulticastIpFinder()
                    {
                        Endpoints = new[] { ConfigManager.Configuration["IgniteService:TcpDiscoveryMulticastIpFinderEndPoint"] }
                    }
                }
            };

            m_ignite = Ignition.Start(igniteConfiguration);
        }

        public static ICompute CreateCompute()
        {
            return new IgniteComputeInstance();
        }

        public static IMapReduce CreateMapReduce()
        {
            return new IgniteMapReduce();
        }

        public static IAsyncMapReduce CreateAsyncMapReduce()
        {
            return new IgniteMapReduce();
        }

        private static Apache.Ignite.Core.Compute.ICompute GetCompute()
        {
            return m_ignite.GetCluster().ForAttribute("NodeType", "ComputeGrid").GetCompute();
        }

        internal class IgniteComputeInstance : ICompute
        {
            [Serializable]
            private class ComputeFuncInstance<TParameter, TResult> : Apache.Ignite.Core.Compute.IComputeFunc<TParameter, TResult>
            {
                public IComputeFunc<TParameter, TResult> ComputeFunc { get; set; }

                public TResult Invoke(TParameter parameter)
                {
                    return ComputeFunc.Excute(parameter);
                }
            }

            [Serializable]
            private class ComputeFuncInstance<TResult> : Apache.Ignite.Core.Compute.IComputeFunc<TResult>
            {
                public IComputeFunc<TResult> ComputeFunc { get; set; }

                public TResult Invoke()
                {
                    return ComputeFunc.Excute();
                }
            }

            public IEnumerable<TResult> Apply<TParameter, TResult>(IComputeFunc<TParameter, TResult> computeFunc, IEnumerable<TParameter> parameters)
            {
                return GetCompute().Apply(new ComputeFuncInstance<TParameter, TResult>() { ComputeFunc = computeFunc }, parameters);
            }

            public async Task<IEnumerable<TResult>> ApplyAsync<TParameter, TResult>(IComputeFunc<TParameter, TResult> computeFunc, IEnumerable<TParameter> parameters)
            {
                return await GetCompute().ApplyAsync(new ComputeFuncInstance<TParameter, TResult>() { ComputeFunc = computeFunc }, parameters);
            }

            public IEnumerable<TResult> Bordercast<TParameter, TResult>(IComputeFunc<TParameter, TResult> computeFunc, TParameter parameter)
            {
                return GetCompute().Broadcast(new ComputeFuncInstance<TParameter, TResult>() { ComputeFunc = computeFunc }, parameter);
            }

            public async Task<IEnumerable<TResult>> BordercastAsync<TParameter, TResult>(IComputeFunc<TParameter, TResult> computeFunc, TParameter parameter)
            {
                return await GetCompute().BroadcastAsync(new ComputeFuncInstance<TParameter, TResult>() { ComputeFunc = computeFunc }, parameter);
            }

            public IEnumerable<TResult> Call<TResult>(IEnumerable<IComputeFunc<TResult>> computeFuncs)
            {
                return GetCompute().Call(computeFuncs.Select(computeFunc => new ComputeFuncInstance<TResult>() { ComputeFunc = computeFunc }));
            }

            public async Task<IEnumerable<TResult>> CallAsync<TResult>(IEnumerable<IComputeFunc<TResult>> computeFuncs)
            {
                return await GetCompute().CallAsync(computeFuncs.Select(computeFunc => new ComputeFuncInstance<TResult>() { ComputeFunc = computeFunc }));
            }
        }

        internal class IgniteMapReduce : IMapReduce, IAsyncMapReduce
        {
            private CancellationTokenSource m_cancellationTokenSource;
            public bool Running { get; private set; }

            private class IgniteComputeJobAdapter<TParameter, TResult> : ComputeJobAdapter<TResult>
            {
                public TParameter Parameter { get; set; }
                public IComputeFunc<TParameter, TResult> ComputeFunc { get; set; }

                public override TResult Execute()
                {
                    return ComputeFunc.Excute(Parameter);
                }
            }

            private class IgniteComputeTaskSplitAdapter<TParameter, TResult, TSplitParameter, TSplitResult> : ComputeTaskSplitAdapter<TParameter, TSplitResult, TResult>
            {
                private readonly IMapReduceTask<TParameter, TResult, TSplitParameter, TSplitResult> m_mapReduceTask;

                public IgniteComputeTaskSplitAdapter(IMapReduceTask<TParameter, TResult, TSplitParameter, TSplitResult> mapReduceTask)
                {
                    m_mapReduceTask = mapReduceTask;
                }

                protected override ICollection<IComputeJob<TSplitResult>> Split(int gridSize, TParameter arg)
                {
                    return m_mapReduceTask.Split(gridSize, arg).Select(mapReduceSplitJob => new IgniteComputeJobAdapter<TSplitParameter, TSplitResult>() { ComputeFunc = mapReduceSplitJob.ComputeFunc, Parameter = mapReduceSplitJob.Parameter }).ToArray();
                }

                public override TResult Reduce(IList<IComputeJobResult<TSplitResult>> results)
                {
                    return m_mapReduceTask.Reduce(results.Where(result => !result.Cancelled && result.Exception == null).Select(result => result.Data));
                }
            }

            public TResult Excute<TParameter, TResult, TSplitParameter, TSplitResult>(IMapReduceTask<TParameter, TResult, TSplitParameter, TSplitResult> mapReduceTask, TParameter parameter)
            {
                return ExcuteAsync(mapReduceTask, parameter).Result;
            }

            public async Task<TResult> ExcuteAsync<TParameter, TResult, TSplitParameter, TSplitResult>(IMapReduceTask<TParameter, TResult, TSplitParameter, TSplitResult> mapReduceTask, TParameter parameter)
            {
                if (Running)
                    throw new Exception("任务已经执行。");

                if (m_cancellationTokenSource != null)
                    throw new Exception("已经执行过的任务需要销毁。");

                Running = true;
                m_cancellationTokenSource = new CancellationTokenSource();

                TResult result = await GetCompute().ExecuteAsync(new IgniteComputeTaskSplitAdapter<TParameter, TResult, TSplitParameter, TSplitResult>(mapReduceTask), parameter, m_cancellationTokenSource.Token);

                Running = false;

                return result;
            }

            public void Cancel()
            {
                Running = false;
                m_cancellationTokenSource.Cancel();
            }

            public void Dispose()
            {
                if (Running)
                    throw new Exception("尝试销毁正在执行中的任务。");
            }
        }
    }
}
