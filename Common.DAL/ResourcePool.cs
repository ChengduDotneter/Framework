using LinqToDB.Configuration;
using LinqToDB.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Common.DAL
{ 
    /// <summary>
   /// 资源池（两个队列锁（临时队列及固定队列））
   /// </summary>
   /// <typeparam name="T"></typeparam>
    public interface IResourcePoolManage<T>
    {
        /// <summary>
        /// 初始化创建T实例
        /// </summary>
        /// <returns></returns>
        T DoCreateInstance();

        /// <summary>
        /// 注销T实例
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        bool DoDisposableInstance(T instance);

        /// <summary>
        /// 申请T资源
        /// </summary>
        /// <returns></returns>
        T ApplyInstance(IDBResourceContent dbResourceContent = null);

        /// <summary>
        /// 使用完成后释放T资源
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        bool RealseInstance(T instance);
    }

    public abstract class ResourcePool<T> : IResourcePoolManage<T>
    {
        private int m_fixedNum;
        private int m_fixResetTimeMilliseconds;
        private int m_temporaryNum;
        private int m_temporaryOverTimeMilliseconds;

        private static IDictionary<T, int> m_fixedCreatureTimePool;
        private static ConcurrentQueue<T> m_fixedInstances;
        private static IDictionary<T, int> m_temporaryCreatureTimePool;
        private static ConcurrentQueue<T> m_temporaryInstances;
        private const int GET_DATACONNECTION_THREAD_TIME_SPAN = 1;
        private const int MAX_REPLAY_RESOURCE_NUM = 5;

        /// <summary>
        /// 资源池构造函数
        /// </summary>
        /// <param name="fixedNum">固定资源数量</param>
        /// <param name="fixResetTimeMilliseconds">固定资源重置时间（毫秒）</param>
        /// <param name="temporaryNum">临时资源数量</param>
        /// <param name="temporaryOverTimeMilliseconds">临时资源释放时间（毫秒） 最后一次使用后释放</param>
        public ResourcePool(int fixedNum, int fixResetTimeMilliseconds, int temporaryNum, int temporaryOverTimeMilliseconds)
        {
            m_fixedNum = fixedNum;
            m_fixResetTimeMilliseconds = fixResetTimeMilliseconds;
            m_temporaryNum = temporaryNum;
            m_temporaryOverTimeMilliseconds = temporaryOverTimeMilliseconds;
        }

        protected void IniResourcePool()
        {
            m_fixedInstances = new ConcurrentQueue<T>();
            m_fixedCreatureTimePool = new Dictionary<T, int>();

            m_temporaryInstances = new ConcurrentQueue<T>();
            m_temporaryCreatureTimePool = new Dictionary<T, int>();

            for (int i = 0; i < m_fixedNum; i++)
            {
                lock (m_fixedInstances)
                {
                    T instance = DoCreateInstance();

                    m_fixedInstances.Enqueue(instance);
                    m_fixedCreatureTimePool.Add(instance, Environment.TickCount + m_fixResetTimeMilliseconds);
                }
            }
        }

        public abstract T DoCreateInstance();

        public abstract bool DoDisposableInstance(T instance);

        private T ReplayApplyInstance(int replayNum = 0)
        {
            T instance;

            if (m_fixedInstances.IsEmpty)
            {
                lock (m_temporaryInstances)
                {
                    if (!m_temporaryInstances.IsEmpty)
                    {
                        while (!m_temporaryInstances.TryDequeue(out instance))
                            Thread.Sleep(GET_DATACONNECTION_THREAD_TIME_SPAN);
                    }
                    else if (m_temporaryCreatureTimePool.Count < m_temporaryNum)
                    {
                        lock (m_temporaryCreatureTimePool)
                        {
                            instance = DoCreateInstance();
                            m_temporaryCreatureTimePool.Add(instance, Environment.TickCount + m_temporaryOverTimeMilliseconds);
                        }
                    }
                    else
                    {
                        replayNum++;

                        if (replayNum > MAX_REPLAY_RESOURCE_NUM)
                        {
                            throw new DealException("资源池繁忙，请稍后再试。");
                        }

                        Thread.Sleep(GET_DATACONNECTION_THREAD_TIME_SPAN);
                        return ReplayApplyInstance(replayNum);
                    }
                    // throw new DealException("资源池繁忙，请稍后再试。");
                }
            }
            else
            {
                lock (m_fixedInstances)
                {
                    while (!m_fixedInstances.TryDequeue(out instance))
                        Thread.Sleep(GET_DATACONNECTION_THREAD_TIME_SPAN);

                    if (m_fixedCreatureTimePool.ContainsKey(instance))
                    {
                        if (m_fixedCreatureTimePool[instance] < Environment.TickCount)
                        {
                            lock (m_fixedCreatureTimePool)
                            {
                                if (!DoDisposableInstance(instance))
                                    throw new DealException("释放资源失败。");

                                m_fixedCreatureTimePool.Remove(instance);

                                instance = DoCreateInstance();

                                m_fixedCreatureTimePool.Add(instance, Environment.TickCount + m_fixResetTimeMilliseconds);
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"资源数量：{m_fixedCreatureTimePool.Count + m_temporaryCreatureTimePool.Count}");

            return instance;
        }

        public T ApplyInstance(IDBResourceContent dbResourceContent = null)
        {
            
            return ReplayApplyInstance();
        }

        public bool RealseInstance(T instance)
        {
            if (m_fixedCreatureTimePool.ContainsKey(instance))
                lock (m_fixedInstances)
                {
                    if (!m_fixedInstances.Contains(instance))
                        m_fixedInstances.Enqueue(instance);
                }
            else if (m_temporaryCreatureTimePool.ContainsKey(instance))
            {
                lock (m_temporaryInstances)
                {
                    m_temporaryCreatureTimePool[instance] = Environment.TickCount + m_temporaryOverTimeMilliseconds;

                    m_temporaryInstances.Enqueue(instance);
                }
            }
            else
                throw new DealException("资源非法。");

            return true;
        }
    }

    public class DataConnectResourcePool : ResourcePool2<DataConnection>
    {
        private LinqToDbConnectionOptions m_options;

        public DataConnectResourcePool(
            int fixedNum,
            int fixResetTimeMilliseconds,
            int temporaryNum,
            int temporaryOverTimeMilliseconds,
            LinqToDbConnectionOptions options) : base(fixedNum, fixResetTimeMilliseconds, temporaryNum, temporaryOverTimeMilliseconds)
        {
            m_options = options;
            IniResourcePool();
        }

        public override DataConnection DoCreateInstance()
        {
            return new DataConnection(m_options);
        }

        public override bool DoDisposableInstance(DataConnection instance)
        {
            instance.Close();
            instance.Dispose();

            return true;
        }
    }
}