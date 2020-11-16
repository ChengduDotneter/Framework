using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.Compute
{
    /// <summary>
    /// 并行计算接口
    /// </summary>
    public interface ICompute
    {
        /// <summary>
        /// 执行同步广播并行计算任务
        /// </summary>
        /// <typeparam name="TParameter">任务参数类型</typeparam>
        /// <typeparam name="TResult">任务返回值类型</typeparam>
        /// <param name="computeFunc">任务执行委托</param>
        /// <param name="parameter">任务参数</param>
        IEnumerable<TResult> Bordercast<TParameter, TResult>(IComputeFunc<TParameter, TResult> computeFunc, TParameter parameter);

        /// <summary>
        /// 执行异步广播并行计算任务
        /// </summary>
        /// <typeparam name="TParameter">任务参数类型</typeparam>
        /// <typeparam name="TResult">任务返回值类型</typeparam>
        /// <param name="computeFunc">任务执行委托</param>
        /// <param name="parameter">任务参数</param>
        Task<IEnumerable<TResult>> BordercastAsync<TParameter, TResult>(IComputeFunc<TParameter, TResult> computeFunc, TParameter parameter);

        /// <summary>
        /// 执行同步无参数并行计算任务
        /// </summary>
        /// <typeparam name="TResult">任务返回值类型</typeparam>
        /// <param name="computeFuncs">任务执行委托集合</param>
        IEnumerable<TResult> Call<TResult>(IEnumerable<IComputeFunc<TResult>> computeFuncs);

        /// <summary>
        /// 执行异步无参数并行计算任务
        /// </summary>
        /// <typeparam name="TResult">任务返回值类型</typeparam>
        /// <param name="computeFuncs">任务执行委托集合</param>
        Task<IEnumerable<TResult>> CallAsync<TResult>(IEnumerable<IComputeFunc<TResult>> computeFuncs);

        /// <summary>
        /// 执行同步有参数并行计算任务
        /// </summary>
        /// <typeparam name="TParameter">任务参数类型</typeparam>
        /// <typeparam name="TResult">任务返回值类型</typeparam>
        /// <param name="computeFunc">任务执行委托</param>
        /// <param name="parameters">任务参数集合</param>
        IEnumerable<TResult> Apply<TParameter, TResult>(IComputeFunc<TParameter, TResult> computeFunc, IEnumerable<TParameter> parameters);

        /// <summary>
        /// 执行异步有参数并行计算任务
        /// </summary>
        /// <typeparam name="TParameter">任务参数类型</typeparam>
        /// <typeparam name="TResult">任务返回值类型</typeparam>
        /// <param name="computeFunc">任务执行委托</param>
        /// <param name="parameters">任务参数集合</param>
        Task<IEnumerable<TResult>> ApplyAsync<TParameter, TResult>(IComputeFunc<TParameter, TResult> computeFunc, IEnumerable<TParameter> parameters);
    }

    /// <summary>
    /// 同步MapReduce接口
    /// </summary>
    public interface IMapReduce
    {
        /// <summary>
        /// 执行MapReduce
        /// </summary>
        /// <typeparam name="TComputeFunc">任务类型</typeparam>
        /// <typeparam name="TParameter">任务参数类型</typeparam>
        /// <typeparam name="TResult">任务返回值类型</typeparam>
        /// <typeparam name="TSplitParameter">Job参数</typeparam>
        /// <typeparam name="TSplitResult">Job返回值</typeparam>
        /// <param name="mapReduceTask">MapReduceTask</param>
        /// <param name="parameter">任务参数</param>
        TResult Excute<TComputeFunc, TParameter, TResult, TSplitParameter, TSplitResult>(IMapReduceTask<TParameter, TResult, TSplitParameter, TSplitResult> mapReduceTask, TParameter parameter);
    }

    /// <summary>
    /// 异步MapReduce接口
    /// </summary>
    public interface IAsyncMapReduce
    {
        /// <summary>
        /// 执行MapReduce
        /// </summary>
        /// <typeparam name="TComputeFunc">任务类型</typeparam>
        /// <typeparam name="TParameter">任务参数类型</typeparam>
        /// <typeparam name="TResult">任务返回值类型</typeparam>
        /// <typeparam name="TSplitParameter">Job参数</typeparam>
        /// <typeparam name="TSplitResult">Job返回值</typeparam>
        /// <param name="mapReduceTask">MapReduceTask</param>
        /// <param name="parameter">任务参数</param>
        Task<TResult> ExcuteAsync<TComputeFunc, TParameter, TResult, TSplitParameter, TSplitResult>(IMapReduceTask<TParameter, TResult, TSplitParameter, TSplitResult> mapReduceTask, TParameter parameter);
    }

    /// <summary>
    /// MapReduceJob
    /// </summary>
    /// <typeparam name="TParameter">Job参数类型</typeparam>
    /// <typeparam name="TResult">Job返回值</typeparam>
    public class MapReduceSplitJob<TParameter, TResult>
    {
        /// <summary>
        /// Job参数
        /// </summary>
        public TParameter Parameter { get; set; }

        /// <summary>
        /// Job执行方法接口
        /// </summary>
        public IComputeFunc<TParameter, TResult> ComputeFunc { get; set; }
    }

    /// <summary>
    /// MapReduceTask
    /// </summary>
    /// <typeparam name="TParameter">任务参数类型</typeparam>
    /// <typeparam name="TResult">任务返回值类型</typeparam>
    /// <typeparam name="TSplitParameter">Job参数类型</typeparam>
    /// <typeparam name="TSplitResult">Job返回值类型</typeparam>
    public interface IMapReduceTask<TParameter, TResult, TSplitParameter, TSplitResult>
    {
        /// <summary>
        /// 拆分任务
        /// </summary>
        /// <param name="nodeCount">执行任务节点数量</param>
        /// <param name="parameter">任务参数</param>
        IEnumerable<MapReduceSplitJob<TSplitParameter, TSplitResult>> Split(int nodeCount, TParameter parameter);

        /// <summary>
        /// 合并Job执行结果
        /// </summary>
        /// <param name="splitResults">Job执行结果集合</param>
        TResult Reduce(IEnumerable<TSplitResult> splitResults);
    }

    /// <summary>
    /// Job执行方法
    /// </summary>
    /// <typeparam name="TParameter">Job参数类型</typeparam>
    /// <typeparam name="TResult">Job返回值类型</typeparam>
    public interface IComputeFunc<TParameter, TResult>
    {
        /// <summary>
        /// 执行方法
        /// </summary>
        /// <param name="parameter">Job参数</param>
        TResult Excute(TParameter parameter);
    }

    /// <summary>
    /// Job执行方法
    /// </summary>
    /// <typeparam name="TResult">Job返回值类型</typeparam>
    public interface IComputeFunc<TResult>
    {
        /// <summary>
        /// 执行方法
        /// </summary>
        /// <returns></returns>
        TResult Excute();
    }
}