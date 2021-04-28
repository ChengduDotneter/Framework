using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Common
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

    /// <summary>
    /// 资源实例接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IResourceInstance<T> : IDisposable
    {
        /// <summary>
        /// 资源实例
        /// </summary>
        T Instance { get; }
    }

    /// <summary>
    /// 安全释放资源池代理对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SafeDisposeableResourceInstance<T> : IResourceInstance<T>
    {
        private readonly IResourceInstance<T> m_resourceInstanceImplementation;
        private volatile bool m_disposed;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="resourceInstanceImplementation"></param>
        public SafeDisposeableResourceInstance(IResourceInstance<T> resourceInstanceImplementation)
        {
            m_disposed = false;
            m_resourceInstanceImplementation = resourceInstanceImplementation;
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            if (!m_disposed)
            {
                m_disposed = true;
                m_resourceInstanceImplementation.Dispose();
            }
        }

        /// <summary>
        /// 代理对象实例
        /// </summary>
        public T Instance => m_resourceInstanceImplementation.Instance;

        /// <summary>
        /// 代理对象
        /// </summary>
        public IResourceInstance<T> Proxy => m_resourceInstanceImplementation;
    }

    /// <summary>
    /// 资源池（单个队列锁）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ResourcePool<T> : IResourcePoolManage<T>
    {
        /// <summary>
        /// 资源池实例
        /// </summary>
        private class ResourceInstance : IResourceInstance<T>
        {
            /// <summary>
            /// 资源实例
            /// </summary>
            public T Instance { get; set; }

            /// <summary>
            /// 是否是临时资源
            /// </summary>
            public bool IsTemp { get; }

            /// <summary>
            /// 资源过期时间
            /// </summary>
            public int OverTimeMilliseconds { get; set; }

            private readonly ResourcePool<T> m_resourcePool;

            public ResourceInstance(T instance, bool isTemp, int overTimeMilliseconds, ResourcePool<T> resourcePool)
            {
                m_resourcePool = resourcePool;
                m_resourcePool.m_instanceCount++;
                Instance = instance;
                IsTemp = isTemp;
                OverTimeMilliseconds = overTimeMilliseconds;
            }

            public void Dispose()
            {
                if (IsTemp)
                {
                    if (OverTimeMilliseconds < Environment.TickCount)
                    {
                        m_resourcePool.m_doDisposableInstance.Invoke(Instance);
                        m_resourcePool.m_instanceCount--;
                    }
                    else
                    {
                        OverTimeMilliseconds = Environment.TickCount + m_resourcePool.m_temporaryOverTimeMilliseconds;
                        m_resourcePool.m_resourceInstanceQueue.Enqueue(this);
                    }
                }
                else
                    m_resourcePool.m_resourceInstanceQueue.Enqueue(this);
            }
        }

        private int m_fixedNum;
        private int m_fixResetTimeMilliseconds;
        private int m_temporaryNum;
        private int m_temporaryOverTimeMilliseconds;
        private volatile int m_instanceCount;
        private ConcurrentQueue<ResourceInstance> m_resourceInstanceQueue;
        private const int GET_DATACONNECTION_THREAD_TIME_SPAN = 1;
        private const int MAX_REPLAY_RESOURCE_NUM = 5;
        private Func<T> m_doCreateInstance;
        private Action<T> m_doDisposableInstance;

        /// <summary>
        /// 资源池示例
        /// </summary>
        /// <param name="fixedNum">固定资源数量</param>
        /// <param name="fixResetTimeMilliseconds">固定资源重置时间（毫秒）</param>
        /// <param name="temporaryNum">临时资源数量</param>
        /// <param name="temporaryOverTimeMilliseconds">临时资源释放时间（毫秒） 最后一次使用后释放</param>
        /// <param name="doCreateInstance"></param>
        /// <param name="doDisposableInstance"></param>
        public ResourcePool(int fixedNum, int fixResetTimeMilliseconds, int temporaryNum, int temporaryOverTimeMilliseconds, Func<T> doCreateInstance, Action<T> doDisposableInstance)
        {
            m_instanceCount = 0;
            m_fixedNum = fixedNum;
            m_fixResetTimeMilliseconds = fixResetTimeMilliseconds;
            m_temporaryNum = temporaryNum;
            m_temporaryOverTimeMilliseconds = temporaryOverTimeMilliseconds;
            m_doCreateInstance = doCreateInstance;
            m_doDisposableInstance = doDisposableInstance;
            IniResourcePool();
        }

        private void IniResourcePool()
        {
            m_resourceInstanceQueue = new ConcurrentQueue<ResourceInstance>();

            for (int i = 0; i < m_fixedNum; i++)
            {
                T instance = m_doCreateInstance.Invoke();
                ResourceInstance resourceInstance = new ResourceInstance(instance, false, Environment.TickCount + m_fixResetTimeMilliseconds, this);
                m_resourceInstanceQueue.Enqueue(resourceInstance);
            }
        }

        /// <summary>
        /// 申请资源实例
        /// </summary>
        /// <returns></returns>
        public virtual IResourceInstance<T> ApplyInstance()
        {
            int replayNum = 0;
            ResourceInstance resourceInstance = null;

            while (resourceInstance == null)
            {
                if (m_resourceInstanceQueue.IsEmpty)
                {
                    if (m_instanceCount < m_temporaryNum + m_fixedNum)
                    {
                        resourceInstance = new ResourceInstance(m_doCreateInstance.Invoke(), true, Environment.TickCount + m_temporaryOverTimeMilliseconds, this);
                    }
                    else
                    {
                        if (replayNum++ > MAX_REPLAY_RESOURCE_NUM)
                        {
                            throw new ResourceException("资源池已满，申请失败。");
                        }

                        Thread.Sleep(GET_DATACONNECTION_THREAD_TIME_SPAN * 10);
                    }
                }
                else
                {
                    while (!m_resourceInstanceQueue.TryDequeue(out resourceInstance))
                        Thread.Sleep(GET_DATACONNECTION_THREAD_TIME_SPAN);
                }
            }

            return new SafeDisposeableResourceInstance<T>(resourceInstance);
        }
    }
}