using Orleans;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.DAL.Transaction
{
    class DeadLockDetection : Grain, IDeadlockDetection
    {
        private const int DEFAULT_RESOURCE_LENGTH = 32;
        private const int DEFAULT_IDENTITY_LENGTH = 32;
        private IDictionary<int, int> m_weights;
        private IDictionary<long, int> m_identityIndexs;
        private IDictionary<int, long> m_identityKeyIndexs;
        private IDictionary<string, int> m_resourceNameIndexs;
        private IDictionary<int, string> m_resourceNameKeyIndexs;
        private bool[] m_usedIdentityIndexs;
        private long[,] m_matrix;
        private long m_tick;

        public DeadLockDetection()
        {
            Allocate(DEFAULT_IDENTITY_LENGTH, DEFAULT_RESOURCE_LENGTH);
            m_weights = new Dictionary<int, int>();
            m_identityIndexs = new Dictionary<long, int>();
            m_identityKeyIndexs = new Dictionary<int, long>();
            m_resourceNameIndexs = new Dictionary<string, int>();
            m_resourceNameKeyIndexs = new Dictionary<int, string>();
        }

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

        public Task ExitLock(long identity, string resourceName)
        {
            if (m_identityIndexs.ContainsKey(identity) && m_resourceNameIndexs.ContainsKey(resourceName))
            {
                int identityIndex = m_identityIndexs[identity];
                m_matrix[identityIndex, m_resourceNameIndexs[resourceName]] = 0;
                m_usedIdentityIndexs[identityIndex] = false;
            }

            m_identityIndexs.Remove(identity);
            m_resourceNameIndexs.Remove(resourceName);

            return Task.CompletedTask;
        }

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
        }

        private void ConflictResolution(int identityIndexA, int identityIndexB, int resourceIndexA, int resourceIndexB)
        {
            long destoryIdentity;
            string destoryResourceName;

            if (m_weights[identityIndexA] >= m_weights[identityIndexB])
            {
                destoryIdentity = m_identityKeyIndexs[identityIndexB];
                destoryResourceName = m_resourceNameKeyIndexs[resourceIndexB];
            }
            else
            {
                destoryIdentity = m_identityKeyIndexs[identityIndexA];
                destoryResourceName = m_resourceNameKeyIndexs[resourceIndexA];
            }

            ConflictResolution(destoryIdentity, destoryResourceName);
        }

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

        private async void ConflictResolution(long identity, string resourceName)
        {
            await GrainFactory.GetGrain<IResource>(resourceName).ConflictResolution(identity);
        }
    }
}
