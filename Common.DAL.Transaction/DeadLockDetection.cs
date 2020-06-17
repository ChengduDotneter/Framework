using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Common.DAL.Transaction
{
    public enum QueueDataTypeEnum
    {
        /// <summary>
        /// 申请
        /// </summary>
        Apply,

        /// <summary>
        /// 释放
        /// </summary>
        Release
    }

    public struct EnQueueData
    {
        public long Identity { get; set; }

        public string ResourceName { get; set; }

        public int Weight { get; set; }

        public int TimeOutTick { get; set; }

        public long EnQueueTick { get; set; }

        public QueueDataTypeEnum QueueDataType { get; set; }
    }

    public struct DeQueueData
    {
        public long DestoryIdentity { get; set; }

        public long ContinueIdentity { get; set; }

        public QueueDataTypeEnum QueueDataType { get; set; }
    }

    /// <summary>
    /// 死锁监测Grain类，单例
    /// </summary>
    public class DeadLockDetection : IDeadlockDetection, IHostedService
    {
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

        private static ConcurrentQueue<EnQueueData> m_enQueueDatas;

        public delegate void DeQueueHandle(DeQueueData deQueueData);

        public static event DeQueueHandle DeQueueEvent;

        public DeadLockDetection()
        {
            Allocate(DEFAULT_IDENTITY_LENGTH, DEFAULT_RESOURCE_LENGTH);
            m_weights = new Dictionary<int, int>();
            m_identityIndexs = new Dictionary<long, int>();
            m_identityKeyIndexs = new Dictionary<int, long>();
            m_resourceNameIndexs = new Dictionary<string, int>();
            m_resourceNameKeyIndexs = new Dictionary<int, string>();
            m_enQueueDatas = new ConcurrentQueue<EnQueueData>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            RunDeadLockDetection();
            return Task.CompletedTask;
        }

        private Task RunDeadLockDetection()
        {
            return Task.Factory.StartNew(() =>
            {

                while (true)
                {
                    if (m_enQueueDatas.TryDequeue(out EnQueueData enQueueData))
                        switch (enQueueData.QueueDataType)
                        {
                            case QueueDataTypeEnum.Apply:
                                EnterLock(enQueueData.Identity, enQueueData.ResourceName, enQueueData.Weight);
                                break;

                            case QueueDataTypeEnum.Release:
                                ExitLock(enQueueData.Identity);
                                break;

                            default:
                                break;
                        }
                };

            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public static Task EnQueued(EnQueueData enQueueData)
        {
            m_enQueueDatas.Enqueue(enQueueData);

            return Task.CompletedTask;
        }

        private void DeQueueChange(DeQueueData deQueueData)
        {
            DeQueueEvent?.Invoke(deQueueData);
        }

        /// <summary>
        /// 事务申请资源时，进入死锁监测，监测是否出现死锁情况
        /// </summary>
        /// <param name="identity">事务线程ID</param>
        /// <param name="resourceName">事务申请的资源名，现包括数据表名</param>
        /// <param name="weight">事务权重</param>
        /// <returns></returns>
        public Task EnterLock(long identity, string resourceName, int weight)
        {
            int identityIndex;
            int resourceNameIndex;

            if (!m_identityIndexs.ContainsKey(identity))
            {
                identityIndex = GetNextIdentityIndex();
                m_identityIndexs.Add(identity, identityIndex);
                m_identityKeyIndexs[identityIndex] = identity;
            }
            else
            {
                identityIndex = m_identityIndexs[identity];
            }

            if (!m_resourceNameIndexs.ContainsKey(resourceName))
            {
                resourceNameIndex = m_resourceNameIndexs.Count;
                m_resourceNameIndexs.Add(resourceName, resourceNameIndex);
                m_resourceNameKeyIndexs[resourceNameIndex] = resourceName;
            }
            else
            {
                resourceNameIndex = m_resourceNameIndexs[resourceName];
            }

            m_weights[identityIndex] = weight;

            if (identityIndex > m_matrix.GetLength(0) - 2 || resourceNameIndex > m_matrix.GetLength(1) - 2)
            {
                Allocate(m_matrix.GetLength(0) * 2, m_matrix.GetLength(1) * 2);
            }

            CheckLock(m_identityIndexs[identity], m_resourceNameIndexs[resourceName]);

            return Task.CompletedTask;
        }

        /// <summary>
        /// 退出死锁监测，移除已有资源标识
        /// </summary>
        /// <param name="identity">事务线程ID</param>
        /// <param name="resourceName">事务申请的资源名，现包括数据表名</param>
        /// <returns></returns>
        public Task ExitLock(long identity)
        {
            if (m_identityIndexs.ContainsKey(identity))
            {
                int identityIndex = m_identityIndexs[identity];

                for (int i = 0; i < m_matrix.GetLength(0); i++)
                {
                    //if (m_matrix[identityIndex, i] > 0)
                    //{
                    m_matrix[identityIndex, i] = 0;
                    //m_resourceNameIndexs.Remove(m_resourceNameKeyIndexs[i]);
                    //}
                }

                m_usedIdentityIndexs[identityIndex] = false;
                m_identityIndexs.Remove(identity);

                DeQueueChange(new DeQueueData() { DestoryIdentity = identity, QueueDataType = QueueDataTypeEnum.Release });
            }

            Console.WriteLine($"还剩事务数：{m_identityIndexs.Count}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 获取下一个事务ID索引
        /// </summary>
        /// <returns></returns>
        private int GetNextIdentityIndex()
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

        /// <summary>
        /// 检查是否死锁
        /// </summary>
        /// <param name="lastIdentityIndex">最后一个进入的事务线程ID索引</param>
        /// <param name="lastResourceNameIndex">最后一个进入的事务资源索引</param>
        private void CheckLock(int lastIdentityIndex, int lastResourceNameIndex)
        {
            m_matrix[lastIdentityIndex, lastResourceNameIndex] = ++m_tick;

            for (int resourceNameIndex = 0; resourceNameIndex < m_matrix.GetLength(1); resourceNameIndex++)
            {
                if (m_matrix[lastIdentityIndex, resourceNameIndex] != 0 && resourceNameIndex != lastResourceNameIndex)
                {
                    for (int identityIndex = 0; identityIndex < m_matrix.GetLength(0); identityIndex++)
                    {
                        if (m_matrix[identityIndex, resourceNameIndex] > m_matrix[lastIdentityIndex, resourceNameIndex] && m_matrix[identityIndex, lastResourceNameIndex] > 0)
                        {
                            ConflictResolution(identityIndex, lastIdentityIndex, resourceNameIndex, lastResourceNameIndex);
                            return;
                        }
                    }
                }
            }

            DeQueueChange(new DeQueueData() { ContinueIdentity = m_identityKeyIndexs[lastIdentityIndex], QueueDataType = QueueDataTypeEnum.Apply });
        }

        /// <summary>
        /// 死锁时，进行的死锁解除策略
        /// </summary>
        /// <param name="identityIndexA">出现死锁的事务A的ID索引</param>
        /// <param name="identityIndexB">出现死锁的事务B的ID索引</param>
        /// <param name="resourceIndexA">出现死锁的事务A的资源索引</param>
        /// <param name="resourceIndexB">出现死锁的事务A的资源索引</param>
        private void ConflictResolution(int identityIndexA, int identityIndexB, int resourceIndexA, int resourceIndexB)
        {
            long destoryIdentity;
            long continueIdentity;

            if (m_weights[identityIndexA] >= m_weights[identityIndexB])
            {
                destoryIdentity = m_identityKeyIndexs[identityIndexB];
                continueIdentity = m_identityKeyIndexs[identityIndexA];
            }
            else
            {
                destoryIdentity = m_identityKeyIndexs[identityIndexA];
                continueIdentity = m_identityKeyIndexs[identityIndexB];
            }

            DeQueueChange(new DeQueueData() { DestoryIdentity = destoryIdentity, ContinueIdentity = continueIdentity, QueueDataType = QueueDataTypeEnum.Apply });
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
    }
}