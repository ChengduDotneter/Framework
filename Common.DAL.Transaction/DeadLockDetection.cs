using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Common.DAL.Transaction
{
    public class DeadlockDetection : IDeadlockDetection
    {
        public event Action<long, string, bool> ApplyResponsed;

        private readonly static TimeSpan THREAD_TIME_SPAN = TimeSpan.FromMilliseconds(0.01);
        private readonly object m_lockThis = new object();
        private ConcurrentQueue<ApplyRequestData> m_applyRequestDatas;
        private Queue<ApplyRequestData> m_waitQueue;
        private HashSet<long> m_destoryIdentitys;
        private Thread m_doApplyThread;

        /// <summary>
        /// 默认事务资源数组长度
        /// </summary>
        private const int DEFAULT_RESOURCE_LENGTH = 32;

        /// <summary>
        /// 默认事务ID数组长度
        /// </summary>
        private const int DEFAULT_IDENTITY_LENGTH = 32;

        /// <summary>
        /// 权重字典<事务ID的索引，权重>
        /// </summary>
        private IDictionary<int, int> m_weights;

        /// <summary>
        /// 事务ID索引字典<事务ID，事务ID索引>
        /// </summary>
        private IDictionary<long, int> m_identityIndexs;

        /// <summary>
        /// 事务ID索引字典<事务ID索引，事务ID>
        /// </summary>
        private IDictionary<int, long> m_identityKeyIndexs;

        /// <summary>
        /// 事务资源名索引字典<事务资源名，事务资源名索引>
        /// </summary>
        private IDictionary<string, int> m_resourceNameIndexs;

        /// <summary>
        /// 事务资源名索引字典<事务资源名索引，事务资源名>
        /// </summary>
        private IDictionary<int, string> m_resourceNameKeyIndexs;

        /// <summary>
        /// 已被使用过的事务ID数组的索引
        /// </summary>
        private bool[] m_usedIdentityIndexs;

        /// <summary>
        /// 事务资源申请时序(事务ID，时序)
        /// </summary>
        private long[,] m_matrix;

        /// <summary>
        /// 时序
        /// </summary>
        private long m_tick;

        private class ApplyRequestData
        {
            public long Identity { get; }
            public string ResourceName { get; }
            public int Weight { get; }
            public int TimeOut { get; }
            public int ApplyTime { get; }

            public ApplyRequestData(long identity, string resourceName, int weight, int timeOut)
            {
                Identity = identity;
                ResourceName = resourceName;
                Weight = weight;
                TimeOut = timeOut;
                ApplyTime = Environment.TickCount;
            }
        }

        public DeadlockDetection()
        {
            Allocate(DEFAULT_IDENTITY_LENGTH, DEFAULT_RESOURCE_LENGTH);
            m_destoryIdentitys = new HashSet<long>();
            m_weights = new Dictionary<int, int>();
            m_identityIndexs = new Dictionary<long, int>();
            m_identityKeyIndexs = new Dictionary<int, long>();
            m_resourceNameIndexs = new Dictionary<string, int>();
            m_resourceNameKeyIndexs = new Dictionary<int, string>();
            m_applyRequestDatas = new ConcurrentQueue<ApplyRequestData>();
            m_waitQueue = new Queue<ApplyRequestData>();
            m_doApplyThread = new Thread(DoApply);
            m_doApplyThread.IsBackground = true;
            m_doApplyThread.Name = "DEADLOCK_DETECTION_THREAD";
            m_doApplyThread.Start();
        }

        public void ApplyRequest(long identity, string resourceName, int weight, int timeOut)
        {
            m_applyRequestDatas.Enqueue(new ApplyRequestData(identity, resourceName, weight, timeOut));
        }

        private void DoApply()
        {
            while (true)
            {
                if (!m_applyRequestDatas.IsEmpty)
                {
                    while (m_applyRequestDatas.TryDequeue(out ApplyRequestData applyRequestData))
                    {
                        int identityIndex;
                        int resourceNameIndex;

                        lock (m_lockThis)
                        {
                            if (!m_identityIndexs.ContainsKey(applyRequestData.Identity))
                            {
                                identityIndex = GetNextIdentityIndex();
                                m_identityIndexs.Add(applyRequestData.Identity, identityIndex);
                                m_identityKeyIndexs[identityIndex] = applyRequestData.Identity;
                            }
                            else
                            {
                                identityIndex = m_identityIndexs[applyRequestData.Identity];
                            }

                            if (!m_resourceNameIndexs.ContainsKey(applyRequestData.ResourceName))
                            {
                                resourceNameIndex = m_resourceNameIndexs.Count;
                                m_resourceNameIndexs.Add(applyRequestData.ResourceName, resourceNameIndex);
                                m_resourceNameKeyIndexs[resourceNameIndex] = applyRequestData.ResourceName;
                            }
                            else
                            {
                                resourceNameIndex = m_resourceNameIndexs[applyRequestData.ResourceName];
                            }

                            m_weights[identityIndex] = applyRequestData.Weight;

                            if (identityIndex > m_matrix.GetLength(0) - 2 || resourceNameIndex > m_matrix.GetLength(1) - 2)
                            {
                                Allocate(m_matrix.GetLength(0) * 2, m_matrix.GetLength(1) * 2);
                            }

                            CheckLock(m_identityIndexs[applyRequestData.Identity], m_resourceNameIndexs[applyRequestData.ResourceName], applyRequestData);
                        }
                    }

                    while (m_waitQueue.Count > 0)
                    {
                        ApplyRequestData applyRequestData = m_waitQueue.Dequeue();

                        if (!m_destoryIdentitys.Contains(applyRequestData.Identity))
                            m_applyRequestDatas.Enqueue(applyRequestData);
                    }

                    m_destoryIdentitys.Clear();
                }

                Thread.Sleep(THREAD_TIME_SPAN);
            }
        }

        /// <summary>
        /// 检查是否死锁
        /// </summary>
        /// <param name="lastIdentityIndex">最后一个进入的事务线程ID索引</param>
        /// <param name="lastResourceNameIndex">最后一个进入的事务资源索引</param>
        private void CheckLock(int lastIdentityIndex, int lastResourceNameIndex, ApplyRequestData applyRequestData)
        {
            if (m_matrix[lastIdentityIndex, lastResourceNameIndex] == 0)
                m_matrix[lastIdentityIndex, lastResourceNameIndex] = ++m_tick;

            for (int resourceNameIndex = 0; resourceNameIndex < m_matrix.GetLength(1); resourceNameIndex++)
            {
                if (m_matrix[lastIdentityIndex, resourceNameIndex] != 0 && resourceNameIndex != lastResourceNameIndex)
                {
                    for (int identityIndex = 0; identityIndex < m_matrix.GetLength(0); identityIndex++)
                    {
                        if (m_matrix[identityIndex, resourceNameIndex] > m_matrix[lastIdentityIndex, resourceNameIndex] && m_matrix[identityIndex, lastResourceNameIndex] > 0)
                        {
                            ConflictResolution(identityIndex, lastIdentityIndex, resourceNameIndex, lastResourceNameIndex, applyRequestData);
                            return;
                        }
                    }
                }
            }

            CheckWaitAndResponse(m_identityKeyIndexs[lastIdentityIndex], lastIdentityIndex, m_resourceNameKeyIndexs[lastResourceNameIndex], lastResourceNameIndex, applyRequestData);
        }

        private void CheckWaitAndResponse(long identity, int identityIndex, string resourceName, int resourceNameIndex, ApplyRequestData applyRequestData)
        {
            lock (m_lockThis)
            {
                int minIdentityIndex = 0;

                foreach (var item in m_identityKeyIndexs)
                {
                    if (m_destoryIdentitys.Contains(item.Value))
                        continue;
                    else if (m_matrix[minIdentityIndex, resourceNameIndex] == 0)
                        minIdentityIndex = item.Key;
                    else if (m_matrix[item.Key, resourceNameIndex] > 0 && m_matrix[item.Key, resourceNameIndex] <= m_matrix[minIdentityIndex, resourceNameIndex])
                        minIdentityIndex = item.Key;
                }

                if (identityIndex != minIdentityIndex)
                {
                    if (Environment.TickCount - applyRequestData.ApplyTime > applyRequestData.TimeOut)
                    {
                        //Console.WriteLine($"des: {m_identityKeyIndexs[minIdentityIndex]}, resourceName: {resourceName}");
                        ApplyResponsed?.Invoke(identity, resourceName, false);
                    }
                    else
                    {
                        m_waitQueue.Enqueue(applyRequestData);
                    }
                }
                else
                {
                    //Console.WriteLine($"identity: {identity}, resourceName: {resourceName}");
                    ApplyResponsed?.Invoke(identity, resourceName, true);
                }
            }
        }

        /// <summary>
        /// 获取下一个事务ID索引
        /// </summary>
        /// <returns></returns>
        private int GetNextIdentityIndex()
        {
            lock (m_lockThis)
            {
                for (int i = 0; i < m_usedIdentityIndexs.Length; i++)
                {
                    if (!m_usedIdentityIndexs[i])
                    {
                        m_usedIdentityIndexs[i] = true;
                        return i;
                    }
                }

                throw new Exception("索引分配错误。");
            }
        }

        /// <summary>
        /// 死锁时，进行的死锁解除策略
        /// </summary>
        /// <param name="identityIndexA">出现死锁的事务A的ID索引</param>
        /// <param name="identityIndexB">出现死锁的事务B的ID索引</param>
        /// <param name="resourceIndexA">出现死锁的事务A的资源索引</param>
        /// <param name="resourceIndexB">出现死锁的事务B的资源索引</param>
        private void ConflictResolution(int identityIndexA, int identityIndexB, int resourceIndexA, int resourceIndexB, ApplyRequestData applyRequestData)
        {
            if (m_weights[identityIndexA] >= m_weights[identityIndexB])
            {
                m_destoryIdentitys.Add(m_identityKeyIndexs[identityIndexB]);
                ApplyResponsed?.Invoke(m_identityKeyIndexs[identityIndexB], m_resourceNameKeyIndexs[resourceIndexB], false);
                CheckWaitAndResponse(m_identityKeyIndexs[identityIndexA], identityIndexA, m_resourceNameKeyIndexs[resourceIndexA], resourceIndexA, applyRequestData);
            }
            else
            {
                m_destoryIdentitys.Add(m_identityKeyIndexs[identityIndexA]);
                ApplyResponsed?.Invoke(m_identityKeyIndexs[identityIndexA], m_resourceNameKeyIndexs[resourceIndexA], false);
                CheckWaitAndResponse(m_identityKeyIndexs[identityIndexB], identityIndexB, m_resourceNameKeyIndexs[resourceIndexB], resourceIndexB, applyRequestData);
            }
        }

        /// <summary>
        /// 动态扩容资源数组
        /// </summary>
        /// <param name="identityLength">事务ID数组所需申请的数组长度</param>
        /// <param name="resourceLength">事务资源数组所需申请的数组长度</param>
        private void Allocate(int identityLength, int resourceLength)
        {
            if (m_matrix != null)
            {
                long[,] tempMatrix = m_matrix;
                bool[] tempUsedIdentityIndexs = m_usedIdentityIndexs;
                m_matrix = new long[identityLength, resourceLength];
                m_usedIdentityIndexs = new bool[identityLength];
                Array.Copy(tempMatrix, m_matrix, tempMatrix.Length);
                Array.Copy(tempUsedIdentityIndexs, m_usedIdentityIndexs, tempUsedIdentityIndexs.Length);
            }
            else
            {
                m_matrix = new long[identityLength, resourceLength];
                m_usedIdentityIndexs = new bool[identityLength];
            }
        }

        public void RemoveTranResource(long identity)
        {
            lock (m_lockThis)
            {
                if (m_identityIndexs.ContainsKey(identity))
                {
                    int identityIndex = m_identityIndexs[identity];

                    foreach (var item in m_resourceNameKeyIndexs)
                    {
                        m_matrix[identityIndex, item.Key] = 0;
                    }

                    m_usedIdentityIndexs[identityIndex] = false;
                    m_identityIndexs.Remove(identity);
                }
            }
        }
    }
}