using System;
using LinqToDB.Configuration;
using LinqToDB.Data;

namespace Common.DAL
{
    internal class DataConnectionInstance : DataConnection
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

    internal class DataConnectResourcePool : ResourcePool<DataConnectionInstance>
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
                IResourceInstance<DataConnectionInstance> resource = resourceInstance;
                
                if (resource is SafeDisposeableResourceInstance<DataConnectionInstance>)
                    resource = ((SafeDisposeableResourceInstance<DataConnectionInstance>)resource).Proxy;

                m_doDisposableInstance.Invoke(resource.Instance);
                resource.GetType().GetProperty(nameof(resource.Instance)).SetValue(resource, m_doCreateInstance.Invoke());
            }

            return resourceInstance;
        }
    }
}