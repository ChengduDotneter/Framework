using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Common.DAL.Transaction
{
    /// <summary>
    /// 死锁检测类
    /// </summary>
    public class DeadlockDetection : IDeadlockDetection
    {
        /// <summary>
        /// 资源申请订阅事件
        /// </summary>
        public event Action<long, string, bool> ApplyResponsed;

        /// <summary>
        /// 锁
        /// </summary>
        private readonly object m_lockThis = new object();

        /// <summary>
        /// 资源申请队列
        /// </summary>
        private BlockingCollection<ApplyRequestData> m_applyRequestDatas;

        /// <summary>
        /// 资源等待队列
        /// </summary>
        private Queue<ApplyRequestData> m_waitQueue;

        /// <summary>
        /// 待释放的事务线程ID标识
        /// </summary>
        private HashSet<long> m_destoryIdentitys;

        /// <summary>
        /// 死锁检查事务
        /// </summary>
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
        /// 权重字典：事务ID的索引和权重
        /// </summary>
        private IDictionary<int, int> m_weights;

        /// <summary>
        /// 事务ID索引字典：事务ID和事务ID索引
        /// </summary>
        private IDictionary<long, int> m_identityIndexs;

        /// <summary>
        /// 事务ID索引字典：事务ID索引和事务ID
        /// </summary>
        private IDictionary<int, long> m_identityKeyIndexs;

        /// <summary>
        /// 事务资源名索引字典：事务资源名和事务资源名索引
        /// </summary>
        private IDictionary<string, int> m_resourceNameIndexs;

        /// <summary>
        /// 事务资源名索引字典：事务资源名索引和事务资源名
        /// </summary>
        private IDictionary<int, string> m_resourceNameKeyIndexs;

        /// <summary>
        /// 已被使用过的事务ID数组的索引
        /// </summary>
        private bool[] m_usedIdentityIndexs;

        /// <summary>
        /// 事务资源申请时序(事务ID和时序)
        /// </summary>
        private long[,] m_matrix;

        /// <summary>
        /// 时序
        /// </summary>
        private long m_tick;

        /// <summary>
        /// 事务申请请求数据实体
        /// </summary>
        private class ApplyRequestData
        {
            /// <summary>
            /// 线程事务ID
            /// </summary>
            public long Identity { get; }

            /// <summary>
            /// 事务申请资源名
            /// </summary>
            public string ResourceName { get; }

            /// <summary>
            /// 权重
            /// </summary>
            public int Weight { get; }

            /// <summary>
            /// 超时时间
            /// </summary>
            public int TimeOut { get; }

            /// <summary>
            /// 申请时间
            /// </summary>
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

        /// <summary>
        /// 构造函数
        /// </summary>
        public DeadlockDetection()
        {
            Allocate(DEFAULT_IDENTITY_LENGTH, DEFAULT_RESOURCE_LENGTH);
            m_destoryIdentitys = new HashSet<long>();
            m_weights = new Dictionary<int, int>();
            m_identityIndexs = new Dictionary<long, int>();
            m_identityKeyIndexs = new Dictionary<int, long>();
            m_resourceNameIndexs = new Dictionary<string, int>();
            m_resourceNameKeyIndexs = new Dictionary<int, string>();
            m_applyRequestDatas = new BlockingCollection<ApplyRequestData>();
            m_waitQueue = new Queue<ApplyRequestData>();

            //死锁检测执行线程初始化
            m_doApplyThread = new Thread(DoApply);
            m_doApplyThread.IsBackground = true;
            m_doApplyThread.Name = "DEADLOCK_DETECTION_THREAD";
            m_doApplyThread.Start();
        }

        /// <summary>
        /// 事务申请资源时，进入死锁监测，监测是否出现死锁情况
        /// </summary>
        /// <param name="identity">事务线程ID</param>
        /// <param name="resourceName">事务申请的资源名，现包括数据表名</param>
        /// <param name="weight">事务权重</param>
        /// <param name="timeOut">超时时间</param>
        /// <returns></returns>
        public void ApplyRequest(long identity, string resourceName, int weight, int timeOut)
        {
            m_applyRequestDatas.Add(new ApplyRequestData(identity, resourceName, weight, timeOut));
        }

        /// <summary>
        /// 事务资源申请
        /// </summary>
        private void DoApply()
        {
            while (true)
            {
                ApplyRequestData applyRequestData = m_applyRequestDatas.Take();

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
                        identityIndex = m_identityIndexs[applyRequestData.Identity];

                    if (!m_resourceNameIndexs.ContainsKey(applyRequestData.ResourceName))
                    {
                        resourceNameIndex = m_resourceNameIndexs.Count;
                        m_resourceNameIndexs.Add(applyRequestData.ResourceName, resourceNameIndex);
                        m_resourceNameKeyIndexs[resourceNameIndex] = applyRequestData.ResourceName;
                    }
                    else
                        resourceNameIndex = m_resourceNameIndexs[applyRequestData.ResourceName];

                    m_weights[identityIndex] = applyRequestData.Weight;

                    if (identityIndex > m_matrix.GetLength(0) - 2 || resourceNameIndex > m_matrix.GetLength(1) - 2)
                        Allocate(m_matrix.GetLength(0) * 2, m_matrix.GetLength(1) * 2);

                    CheckLock(m_identityIndexs[applyRequestData.Identity], m_resourceNameIndexs[applyRequestData.ResourceName], applyRequestData);
                }

                while (m_waitQueue.Count > 0)
                {
                    ApplyRequestData waitApplyRequestData = m_waitQueue.Dequeue();

                    if (!m_destoryIdentitys.Contains(waitApplyRequestData.Identity))
                        m_applyRequestDatas.Add(waitApplyRequestData);
                }

                m_destoryIdentitys.Clear();
            }
        }

        /// <summary>
        /// 检查是否死锁
        /// </summary>
        /// <param name="lastIdentityIndex">最后一个进入的事务线程ID索引</param>
        /// <param name="lastResourceNameIndex">最后一个进入的事务资源索引</param>
        /// <param name="applyRequestData">资源申请数据</param>
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

        /// <summary>
        /// 检查事务请求的资源是否需要排队等待
        /// </summary>
        /// <param name="identity">事务线程ID</param>
        /// <param name="identityIndex">事务线程ID索引</param>
        /// <param name="resourceName">资源名</param>
        /// <param name="resourceNameIndex">资源名索引</param>
        /// <param name="applyRequestData">事务申请的资源对象</param>
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
                        ApplyResponsed?.Invoke(identity, resourceName, false);
                    else
                        m_waitQueue.Enqueue(applyRequestData);
                }
                else
                    ApplyResponsed?.Invoke(identity, resourceName, true);
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
        /// <param name="applyRequestData">资源申请数据</param>
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
        private unsafe void Allocate(int identityLength, int resourceLength)
        {
            if (m_matrix != null)
            {
                fixed (long* matrixSourcePtr = m_matrix)
                {
                    int orignIdentityLength = m_matrix.GetLength(0);
                    int orignResourceLength = m_matrix.GetLength(1);
                    m_matrix = new long[identityLength, resourceLength];

                    fixed (long* matrixDestPtr = m_matrix)
                    {
                        for (int i = 0; i < orignIdentityLength; i++)
                            Buffer.MemoryCopy(matrixSourcePtr + i * orignResourceLength, matrixDestPtr + i * resourceLength, resourceLength, orignResourceLength);
                    }
                }

                fixed (bool* usedIdentityIndexsSourcePtr = m_usedIdentityIndexs)
                {
                    int orignIdentityLength = m_usedIdentityIndexs.Length;
                    m_usedIdentityIndexs = new bool[identityLength];

                    fixed (bool* usedIdentityIndexsDestPtr = m_usedIdentityIndexs)
                        Buffer.MemoryCopy(usedIdentityIndexsSourcePtr, usedIdentityIndexsDestPtr, identityLength, orignIdentityLength);
                }
            }
            else
            {
                m_matrix = new long[identityLength, resourceLength];
                m_usedIdentityIndexs = new bool[identityLength];
            }
        }

        /// <summary>
        /// 退出死锁监测，移除已有资源标识
        /// </summary>
        /// <param name="identity">事务线程ID</param>
        /// <returns></returns>
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