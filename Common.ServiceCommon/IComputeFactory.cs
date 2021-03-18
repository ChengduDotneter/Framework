using Common.Compute;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Reflection;

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
        T CreateComputeFunc<T, TParameter, TResult>() where T : class, IComputeFunc<TParameter, TResult>;

        /// <summary>
        /// 创建并行计算Job
        /// </summary>
        /// <typeparam name="T">并行计算Job实现类型</typeparam>
        /// <typeparam name="TResult">Job返回值</typeparam>
        T CreateComputeFunc<T, TResult>() where T : class, IComputeFunc<TResult>;

        /// <summary>
        /// 创建并行计算任务
        /// </summary>
        /// <typeparam name="T">并行计算任务实现类型</typeparam>
        /// <typeparam name="TParameter">任务参数</typeparam>
        /// <typeparam name="TResult">任务返回值</typeparam>
        /// <typeparam name="TSplitParameter">Job参数</typeparam>
        /// <typeparam name="TSplitResult">Job返回值</
        T CreateComputeMapReduceTask<T, TParameter, TResult, TSplitParameter, TSplitResult>()
            where T : class, IMapReduceTask<TParameter, TResult, TSplitParameter, TSplitResult>;

        /// <summary>
        /// 创建并行计算Job
        /// </summary>
        /// <param name="computeFuncType">JOB类型</param>
        /// <returns></returns>
        object CreateComputeFunc(Type computeFuncType);
    }

    internal class ComputeFactory : IComputeFactory
    {
        private readonly IServiceProvider m_serviceProvider;

        public ComputeFactory(IServiceProvider serviceProvider)
        {
            m_serviceProvider = serviceProvider;
        }

        public T CreateComputeFunc<T, TParameter, TResult>() where T : class, IComputeFunc<TParameter, TResult>
        {
            return m_serviceProvider.CreateInstanceFromServiceProvider<T>();
        }

        public T CreateComputeFunc<T, TResult>() where T : class, IComputeFunc<TResult>
        {
            return m_serviceProvider.CreateInstanceFromServiceProvider<T>();
        }

        public T CreateComputeMapReduceTask<T, TParameter, TResult, TSplitParameter, TSplitResult>()
            where T : class, IMapReduceTask<TParameter, TResult, TSplitParameter, TSplitResult>
        {
            return m_serviceProvider.CreateInstanceFromServiceProvider<T>();
        }

        public object CreateComputeFunc(Type computeFuncType)
        {
            if (computeFuncType.GetInterfaces().All(item => !item.IsGenericType ||
                                                            (item.GetGenericTypeDefinition() != typeof(IComputeFunc<,>) &&
                                                             item.GetGenericTypeDefinition() != typeof(IComputeFunc<>))))
                throw new DealException("错误的JOB类型。");

            return m_serviceProvider.CreateInstanceFromServiceProvider(computeFuncType);
        }
    }
}