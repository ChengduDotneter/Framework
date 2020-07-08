using System;
using System.Reflection;
using Common.Compute;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Common.ServiceCommon
{
    /// <summary>
    /// 并行计算任务创建工厂接口
    /// </summary>
    public interface IComputeFactory
    {
        /// <summary>
        /// 创建并行计算Job
        /// </summary>
        /// <typeparam name="T">并行计算Job实现类型</typeparam>
        /// <typeparam name="TParameter">Job参数</typeparam>
        /// <typeparam name="TResult">Job返回值</typeparam>
        IComputeFunc<TParameter, TResult> CreateComputeFunc<T, TParameter, TResult>() where T : IComputeFunc<TParameter, TResult>;

        /// <summary>
        /// 创建并行计算Job
        /// </summary>
        /// <typeparam name="T">并行计算Job实现类型</typeparam>
        /// <typeparam name="TResult">Job返回值</typeparam>
        IComputeFunc<TResult> CreateComputeFunc<T, TResult>() where T : IComputeFunc<TResult>;

        /// <summary>
        /// 创建并行计算任务
        /// </summary>
        /// <typeparam name="T">并行计算任务实现类型</typeparam>
        /// <typeparam name="TParameter">任务参数</typeparam>
        /// <typeparam name="TResult">任务返回值</typeparam>
        /// <typeparam name="TSplitParameter">Job参数</typeparam>
        /// <typeparam name="TSplitResult">Job返回值</typeparam>
        IMapReduceTask<TParameter, TResult, TSplitParameter, TSplitResult> CreateComputeMapReduceTask<T, TParameter, TResult, TSplitParameter, TSplitResult>() where T : IMapReduceTask<TParameter, TResult, TSplitParameter, TSplitResult>;
    }

    /// <summary>
    /// 并行计算任务创建工厂
    /// </summary>
    public class ComputeFactory : IComputeFactory
    {
        private IHost m_host;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="host">服务宿主</param>
        public ComputeFactory(IHost host)
        {
            m_host = host;
        }

        private static object CreateInstance(IHost host, Type type)
        {
            ConstructorInfo[] constructorInfos = type.GetConstructors(BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.Instance);

            if (constructorInfos.Length != 1)
                throw new Exception($"{type.FullName}有且只能有一个公有构造函数。");

            IServiceProvider serviceProvider = host.Services.CreateScope().ServiceProvider;
            ParameterInfo[] parameterInfos = constructorInfos[0].GetParameters();
            object[] parameters = new object[parameterInfos.Length];

            for (int i = 0; i < parameters.Length; i++)
                parameters[i] = serviceProvider.GetRequiredService(parameterInfos[i].ParameterType);

            return constructorInfos[0].Invoke(parameters);
        }

        /// <summary>
        /// 创建并行计算Job
        /// </summary>
        /// <typeparam name="T">并行计算Job实现类型</typeparam>
        /// <typeparam name="TParameter">Job参数</typeparam>
        /// <typeparam name="TResult">Job返回值</typeparam>
        public IComputeFunc<TParameter, TResult> CreateComputeFunc<T, TParameter, TResult>() where T : IComputeFunc<TParameter, TResult>
        {
            return (IComputeFunc<TParameter, TResult>)CreateInstance(m_host, typeof(T));
        }

        /// <summary>
        /// 创建并行计算Job
        /// </summary>
        /// <typeparam name="T">并行计算Job实现类型</typeparam>
        /// <typeparam name="TResult">Job返回值</typeparam>
        public IComputeFunc<TResult> CreateComputeFunc<T, TResult>() where T : IComputeFunc<TResult>
        {
            return (IComputeFunc<TResult>)CreateInstance(m_host, typeof(T));
        }

        /// <summary>
        /// 创建并行计算任务
        /// </summary>
        /// <typeparam name="T">并行计算任务实现类型</typeparam>
        /// <typeparam name="TParameter">任务参数</typeparam>
        /// <typeparam name="TResult">任务返回值</typeparam>
        /// <typeparam name="TSplitParameter">Job参数</typeparam>
        /// <typeparam name="TSplitResult">Job返回值</typeparam>
        public IMapReduceTask<TParameter, TResult, TSplitParameter, TSplitResult> CreateComputeMapReduceTask<T, TParameter, TResult, TSplitParameter, TSplitResult>()
            where T : IMapReduceTask<TParameter, TResult, TSplitParameter, TSplitResult>
        {
            return (IMapReduceTask<TParameter, TResult, TSplitParameter, TSplitResult>)CreateInstance(m_host, typeof(T));
        }
    }
}
