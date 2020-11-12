using LinqToDB.Configuration;
using LinqToDB.Data;
using System;

namespace Common.DAL
{
    /// <summary>
    /// 资源池（两个队列锁（临时队列及固定队列））
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IResourcePoolManage<T>
    {
        /// <summary>
        /// 申请T资源
        /// </summary>
        /// <returns></returns>
        IResourceInstance<T> ApplyInstance();
    }

    public interface IResourceInstance<T> : IDisposable
    {
        T Instance { get; }
    }

    public class DataConnectionInstance : DataConnection
    {
        /// <summary>
        /// 资源过期时间
        /// </summary>
        public int OverTimeMilliseconds { get; }

        public DataConnectionInstance(int overTimeMilliseconds, LinqToDbConnectionOptions linqToDbConnectionOptions) : base(linqToDbConnectionOptions)
        {
            OverTimeMilliseconds = overTimeMilliseconds;
        }
    }

    public class DataConnectResourcePool : ResourcePool2<DataConnectionInstance>
    {
        private readonly Func<DataConnectionInstance> m_doCreateInstance;
        private readonly Action<DataConnectionInstance> m_doDisposableInstance;

        public DataConnectResourcePool(
            int fixedNum, 
            int fixResetTimeMilliseconds,
            int temporaryNum,
            int temporaryOverTimeMilliseconds, 
            Func<DataConnectionInstance> doCreateInstance,
            Action<DataConnectionInstance> doDisposableInstance) : base(fixedNum, fixResetTimeMilliseconds, temporaryNum, temporaryOverTimeMilliseconds, doCreateInstance, doDisposableInstance)
        {
            m_doCreateInstance = doCreateInstance;
            m_doDisposableInstance = doDisposableInstance;
        }

        public override IResourceInstance<DataConnectionInstance> ApplyInstance()
        {
            IResourceInstance<DataConnectionInstance> resourceInstance = base.ApplyInstance();

            if (resourceInstance.Instance.OverTimeMilliseconds < Environment.TickCount)
            {
                m_doDisposableInstance.Invoke(resourceInstance.Instance);
                typeof(IResourceInstance<DataConnectionInstance>).GetProperty(nameof(resourceInstance.Instance)).SetValue(resourceInstance, m_doCreateInstance.Invoke());
            }

            return resourceInstance;
        }
    }
}