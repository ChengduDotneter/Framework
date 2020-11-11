using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Common.DAL
{
    /// <summary>
    /// 资源池（单个队列锁）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ResourcePool2<T> : IResourcePoolManage<T>
    {
        /// <summary>
        /// 资源池实例
        /// </summary>
        private class ResourceInstance
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

            public ResourceInstance(T instance, bool isTemp, int overTimeMilliseconds)
            {
                Instance = instance;
                IsTemp = isTemp;
                OverTimeMilliseconds = overTimeMilliseconds;
            }
        }

        private int m_fixedNum;
        private int m_fixResetTimeMilliseconds;
        private int m_temporaryNum;
        private int m_temporaryOverTimeMilliseconds;
        private static ConcurrentQueue<ResourceInstance> m_resourceInstanceQueue;
        private static IDictionary<T, ResourceInstance> m_resourceIndexValue;
        private const int GET_DATACONNECTION_THREAD_TIME_SPAN = 1;
        private const int MAX_REPLAY_RESOURCE_NUM = 5;
        private IDictionary<IDBResourceContent, ResourceInstance> m_dbResourceContentValue;

        /// <summary>
        /// 资源池示例
        /// </summary>
        /// <param name="fixedNum">固定资源数量</param>
        /// <param name="fixResetTimeMilliseconds">固定资源重置时间（毫秒）</param>
        /// <param name="temporaryNum">临时资源数量</param>
        /// <param name="temporaryOverTimeMilliseconds">临时资源释放时间（毫秒） 最后一次使用后释放</param>
        public ResourcePool2(int fixedNum, int fixResetTimeMilliseconds, int temporaryNum, int temporaryOverTimeMilliseconds)
        {
            m_fixedNum = fixedNum;
            m_fixResetTimeMilliseconds = fixResetTimeMilliseconds;
            m_temporaryNum = temporaryNum;
            m_temporaryOverTimeMilliseconds = temporaryOverTimeMilliseconds;
        }

        public abstract T DoCreateInstance();

        public abstract bool DoDisposableInstance(T instance);

        protected void IniResourcePool()
        {
            m_resourceInstanceQueue = new ConcurrentQueue<ResourceInstance>();
            m_resourceIndexValue = new Dictionary<T, ResourceInstance>();
            m_dbResourceContentValue = new Dictionary<IDBResourceContent, ResourceInstance>();

            for (int i = 0; i < m_fixedNum; i++)
            {
                lock (m_resourceInstanceQueue)
                {
                    T instance = DoCreateInstance();

                    ResourceInstance resourceInstance = new ResourceInstance(instance, false, Environment.TickCount + m_fixResetTimeMilliseconds);

                    m_resourceInstanceQueue.Enqueue(resourceInstance);
                    m_resourceIndexValue.Add(instance, resourceInstance);
                }
            }
        }

        private ResourceInstance ReplayApplyInstance(int replayNum = 0)
        {
            ResourceInstance resourceInstance;

            lock (m_resourceInstanceQueue)
            {
                if (m_resourceInstanceQueue.IsEmpty)
                {
                    if (m_resourceIndexValue.Count < m_temporaryNum + m_fixedNum)
                    {
                        T instance = DoCreateInstance();

                        resourceInstance = new ResourceInstance(instance, true, Environment.TickCount + m_temporaryOverTimeMilliseconds);

                        m_resourceIndexValue.Add(instance, resourceInstance);
                    }
                    else
                    {
                        replayNum++;

                        if (replayNum > MAX_REPLAY_RESOURCE_NUM)
                        {
                            Console.WriteLine($"重试超过{MAX_REPLAY_RESOURCE_NUM}次 线程:{Environment.CurrentManagedThreadId.GetHashCode()}");
                            throw new DealException("资源池繁忙，请稍后再试。");
                        }

                        Thread.Sleep(GET_DATACONNECTION_THREAD_TIME_SPAN * 10);
                        return ReplayApplyInstance(replayNum);
                    }
                    // throw new DealException("资源池繁忙，请稍后再试。");
                }
                else
                {
                    while (!m_resourceInstanceQueue.TryDequeue(out resourceInstance))
                        Thread.Sleep(GET_DATACONNECTION_THREAD_TIME_SPAN);

                    if (resourceInstance.OverTimeMilliseconds < Environment.TickCount)
                    {
                        lock (m_resourceIndexValue)
                        {
                            if (m_resourceIndexValue.ContainsKey(resourceInstance.Instance))
                                m_resourceIndexValue.Remove(resourceInstance.Instance);

                            //数据库注销当前资源
                            if (!DoDisposableInstance(resourceInstance.Instance))
                                throw new DealException("释放资源失败。");

                            if (resourceInstance.IsTemp)
                            {
                                return ReplayApplyInstance();
                            }
                            else
                            {
                                resourceInstance.Instance = DoCreateInstance();

                                resourceInstance.OverTimeMilliseconds = Environment.TickCount + m_fixResetTimeMilliseconds;

                                m_resourceIndexValue.Add(resourceInstance.Instance, resourceInstance);
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"资源数量：{m_resourceIndexValue.Count} 线程：{Environment.CurrentManagedThreadId.GetHashCode()}");

            return resourceInstance;
        }

        public virtual T ApplyInstance(IDBResourceContent dbResourceContent = null)
        {
            ResourceInstance resourceInstance;
            if (dbResourceContent != null && m_dbResourceContentValue.ContainsKey(dbResourceContent))
            {
                resourceInstance = m_dbResourceContentValue[dbResourceContent];
            }
            else
            {
                resourceInstance = ReplayApplyInstance();

                if (dbResourceContent != null)
                {
                    lock (m_dbResourceContentValue)
                    {
                        m_dbResourceContentValue.Add(dbResourceContent, resourceInstance);

                        dbResourceContent.OnDispose += new Action(() =>
                        {
                            lock (m_dbResourceContentValue)
                            {
                                if (m_dbResourceContentValue.ContainsKey(dbResourceContent))
                                {
                                    m_resourceInstanceQueue.Enqueue(m_dbResourceContentValue[dbResourceContent]);
                                    m_dbResourceContentValue.Remove(dbResourceContent);
                                }
                            }
                        });
                    }
                }
            }

            return resourceInstance.Instance;
        }

        public virtual bool RealseInstance(T instance)
        {
            if (m_resourceIndexValue.ContainsKey(instance))
            {
                lock (m_resourceIndexValue)
                {
                    if (m_resourceIndexValue[instance].IsTemp)
                    {
                        m_resourceIndexValue[instance].OverTimeMilliseconds = Environment.TickCount + m_temporaryOverTimeMilliseconds;
                    }

                    lock (m_dbResourceContentValue)
                    {
                        if (m_dbResourceContentValue.All(item => item.Value.GetHashCode() != m_resourceIndexValue[instance].GetHashCode()))
                        {
                            m_resourceInstanceQueue.Enqueue(m_resourceIndexValue[instance]);
                        }
                    }
                }
            }
            else
                throw new DealException("资源非法。");

            return true;
        }
    }
}