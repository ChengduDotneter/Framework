using LinqToDB.Common;
using LinqToDB.Configuration;
using LinqToDB.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Common.DAL
{
    public interface IResourcePool<T>
    {
        /// <summary>
        /// 创建资源
        /// </summary>
        /// <returns></returns>
        T DoCreateInstance();

        /// <summary>
        /// 从资源池释放资源
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        bool DoDisposeInstance(T instance);

        /// <summary>
        /// 申请资源
        /// </summary>
        /// <returns></returns>
        T Apply(int timeOut);

        /// <summary>
        /// 释放资源到资源池
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        bool Release(T instance);
    }

    public abstract class ResourcePool<T> : IResourcePool<T>
    {
        private const int GET_DATACONNECTION_THREAD_TIME_SPAN = 1;
        private const int SLEEP_THREAD_TIME_SPAN = 100;
        private ConcurrentQueue<T> m_instancePool;
        private ConcurrentDictionary<T, int> m_connections;
        private Thread m_thread;

        private int m_minThreadCount;
        private int m_maxThreadCount;
        private int m_fixedTimeOut;
        private int m_temTimeOut;

        private int m_maxTimeOut;

        public T Apply(int timeOut = 0)
        {
            T instance;

            if (m_instancePool.IsEmpty)
            {
                lock (m_connections)
                {
                    if (m_connections.Count < m_maxThreadCount)
                    {
                        instance = DoCreateInstance();

                        if (!m_connections.TryAdd(instance, Environment.TickCount + m_temTimeOut))
                            return Apply(timeOut);
                    }
                    else
                    {
                        if (timeOut > m_maxTimeOut)
                            throw new DealException("资源池繁忙，请稍后再试。");

                        Thread.Sleep(SLEEP_THREAD_TIME_SPAN);

                        timeOut += SLEEP_THREAD_TIME_SPAN;

                        return Apply(timeOut);
                    }
                }
            }
            else
            {
                while (!m_instancePool.TryDequeue(out instance))
                    Thread.Sleep(GET_DATACONNECTION_THREAD_TIME_SPAN);

                if (m_connections[instance] < Environment.TickCount)
                {
                    DoDisposeInstance(instance);

                    instance = DoCreateInstance();

                    m_connections[instance] = Environment.TickCount + m_fixedTimeOut;
                }
            }

            Console.WriteLine($"申请资源：{m_connections.Count}");

            return instance;
        }

        public abstract T DoCreateInstance();

        public abstract bool DoDisposeInstance(T instance);

        public bool Release(T instance)
        {
            if (m_connections.ContainsKey(instance))
            {
                m_instancePool.Enqueue(instance);

                if (m_connections.Count > m_minThreadCount)
                    m_connections[instance] = Environment.TickCount + m_temTimeOut;
            }
            else
                throw new DealException("资源非法。");

            return true;
        }

        public void InitInstance()
        {
            m_instancePool = new ConcurrentQueue<T>();
            m_connections = new ConcurrentDictionary<T, int>();

            for (int i = 0; i < m_minThreadCount; i++)
            {
                T instance = DoCreateInstance();

                if (m_connections.TryAdd(instance, Environment.TickCount + m_fixedTimeOut))
                    m_instancePool.Enqueue(instance);
            }
        }

        private void DoWork()
        {
            while (true)
            {
                if (!m_instancePool.IsNullOrEmpty() && m_instancePool.Count > m_minThreadCount)
                {
                    IEnumerable<KeyValuePair<T, int>> connections = m_connections.Where(item => item.Value < Environment.TickCount);

                    if (connections.Count() > 0)
                    {
                        T instance;

                        while (!m_instancePool.TryDequeue(out instance))
                            Thread.Sleep(GET_DATACONNECTION_THREAD_TIME_SPAN);

                        if (m_connections[instance] > Environment.TickCount)
                            m_instancePool.Enqueue(instance);
                        else
                        {
                            DoDisposeInstance(instance);
                            m_connections.TryRemove(instance, out int value);
                        }

                        Console.WriteLine($"释放后资源：{m_connections.Count}");

                    }

                    Thread.Sleep(SLEEP_THREAD_TIME_SPAN);
                }
            }
        }


        public ResourcePool(int minThreadCount, int maxThreadCount, int fixedTimeOut, int temTimeOut)
        {
            m_minThreadCount = minThreadCount;
            m_maxThreadCount = maxThreadCount;
            m_fixedTimeOut = fixedTimeOut;
            m_temTimeOut = temTimeOut;
            m_maxTimeOut = Convert.ToInt32(ConfigManager.Configuration["MaxTimeOut"]);

            m_thread = new Thread(DoWork);
            m_thread.IsBackground = true;
            m_thread.Start();
        }
    }

    public class ConnectResourcePool : ResourcePool<DataConnection>
    {
        private LinqToDbConnectionOptions m_connectionOptions;

        public ConnectResourcePool(int minThreadCount, int maxThreadCount, int fixedTimeOut, int temTimeOut, LinqToDbConnectionOptions connectionOptions) : base(minThreadCount, maxThreadCount, fixedTimeOut, temTimeOut)
        {
            m_connectionOptions = connectionOptions;
            InitInstance();
        }

        public override DataConnection DoCreateInstance()
        {
            return new DataConnection(m_connectionOptions);
        }

        public override bool DoDisposeInstance(DataConnection instance)
        {
            instance.Close();
            instance.Dispose();

            return true;
        }
    }
}
