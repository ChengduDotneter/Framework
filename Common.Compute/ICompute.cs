using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.Compute
{
    public interface ICompute
    {
        IEnumerable<TResult> Bordercast<TParameter, TResult>(IComputeFunc<TParameter, TResult> computeFunc, TParameter parameter);
        Task<IEnumerable<TResult>> BordercastAsync<TParameter, TResult>(IComputeFunc<TParameter, TResult> computeFunc, TParameter parameter);

        IEnumerable<TResult> Call<TResult>(IEnumerable<IComputeFunc<TResult>> computeFuncs);
        Task<IEnumerable<TResult>> CallAsync<TResult>(IEnumerable<IComputeFunc<TResult>> computeFuncs);

        IEnumerable<TResult> Apply<TParameter, TResult>(IComputeFunc<TParameter, TResult> computeFunc, IEnumerable<TParameter> parameters);
        Task<IEnumerable<TResult>> ApplyAsync<TParameter, TResult>(IComputeFunc<TParameter, TResult> computeFunc, IEnumerable<TParameter> parameters);
    }

    public interface IMapReduce : IDisposable
    {
        TResult Excute<TParameter, TResult, TSplitParameter, TSplitResult>(IMapReduceTask<TParameter, TResult, TSplitParameter, TSplitResult> mapReduceTask, TParameter parameter);
    }

    public interface IAsyncMapReduce : IDisposable
    {
        bool Running { get; }
        Task<TResult> ExcuteAsync<TParameter, TResult, TSplitParameter, TSplitResult>(IMapReduceTask<TParameter, TResult, TSplitParameter, TSplitResult> mapReduceTask, TParameter parameter);
        void Cancel();
    }

    public class MapReduceSplitJob<TParameter, TResult>
    {
        public TParameter Parameter { get; set; }
        public IComputeFunc<TParameter, TResult> ComputeFunc { get; set; }
    }

    public interface IMapReduceTask<TParameter, TResult, TSplitParameter, TSplitResult>
    {
        IEnumerable<MapReduceSplitJob<TSplitParameter, TSplitResult>> Split(int nodeCount, TParameter parameter);
        TResult Reduce(IEnumerable<TSplitResult> splitResults);
    }

    public interface IComputeFunc<TParameter, TResult>
    {
        TResult Excute(TParameter parameter);
    }

    public interface IComputeFunc<TResult>
    {
        TResult Excute();
    }
}
