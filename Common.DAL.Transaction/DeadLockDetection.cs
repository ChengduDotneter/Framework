using Orleans;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.DAL.Transaction
{
    class DeadLockDetection : Grain, IDeadlockDetection
    {
        private const int DEFAULT_RESOURCE_LENGTH = 1024;
        private const int DEFAULT_IDENTITY_LENGTH = 1024;
        private IDictionary<int, int> m_weights;
        private IDictionary<long, int> m_identityIndexs;
        private IDictionary<int, long> m_identityKeyIndexs;
        private IDictionary<string, int> m_resourceNameIndexs;
        private IDictionary<int, string> m_resourceNameKeyIndexs;
        private long[,] m_matrix;
        private int m_tick;

        DeadLockDetection()
        {
            Allocate(DEFAULT_IDENTITY_LENGTH, DEFAULT_RESOURCE_LENGTH);
            m_weights = new Dictionary<int, int>();
            m_identityIndexs = new Dictionary<long, int>();
            m_identityKeyIndexs = new Dictionary<int, long>();
            m_resourceNameIndexs = new Dictionary<string, int>();
            m_resourceNameKeyIndexs = new Dictionary<int, string>();
        }

        public async Task EnterLock(long identity, string resourceName, int weight)
        {
            int identityIndex;

            if (!m_identityIndexs.ContainsKey(identity))
            {
                identityIndex = m_identityIndexs.Count;
                m_identityIndexs.Add(identity, identityIndex);
                m_identityKeyIndexs.Add(identityIndex, identity);
            }
            else
            {
                identityIndex = m_identityIndexs[identity];
            }

            if (!m_weights.ContainsKey(identityIndex))
            {
                m_weights.Add(identityIndex, weight);
            }

            if (!m_resourceNameIndexs.ContainsKey(resourceName))
            {
                int resourceNameIndex = m_resourceNameIndexs.Count;
                m_resourceNameIndexs.Add(resourceName, resourceNameIndex);
                m_resourceNameKeyIndexs.Add(resourceNameIndex, resourceName);
            }

            if (m_identityIndexs.Count >= m_matrix.GetLength(0) || m_resourceNameIndexs.Count >= m_matrix.GetLength(1))
            {
                Allocate(m_matrix.GetLength(0) * 2, m_matrix.GetLength(1) * 2);
            }

            await CheckLock(m_identityIndexs[identity], m_resourceNameIndexs[resourceName]);










        }

        public Task ExitLock(long identity, string resourceName)
        {
            //if (!m_locks.ContainsKey(identity))
            //    return Task.CompletedTask;

            //m_locks[identity].Resources.Remove(resourceName);

            //if (m_locks[identity].Resources.Count == 0)
            //    m_locks.Remove(identity);

            return Task.CompletedTask;
        }

        private async Task CheckLock(int lastIdentityIndex, int lastResourceNameIndex)
        {
            m_matrix[lastIdentityIndex, lastResourceNameIndex] = ++m_tick;

            for (int resourceIndex = 0; resourceIndex < m_matrix.GetLength(1); resourceIndex++)
            {
                if (m_matrix[lastIdentityIndex, resourceIndex] != 0 && resourceIndex != lastResourceNameIndex)
                {
                    for (int identityIndex = 0; identityIndex < m_matrix.GetLength(0); identityIndex++)
                    {
                        if (m_matrix[identityIndex, resourceIndex] > m_matrix[lastIdentityIndex, resourceIndex] && m_matrix[identityIndex, lastResourceNameIndex] > 0)
                        {
                            await CheckWeight(identityIndex, lastIdentityIndex);
                            return;
                        }
                    }
                }
            }
        }

        private async Task CheckWeight(int identityIndexA, int identityIndexB)
        {
            long destoryIdentity = 0;
            int destoryIdentityIndex = 0;

            if (m_weights[identityIndexA] >= m_weights[identityIndexB])
            {
                destoryIdentity = m_identityKeyIndexs[identityIndexB];
                destoryIdentityIndex = identityIndexB;
            }
            else
            {
                destoryIdentity = m_identityKeyIndexs[identityIndexA];
                destoryIdentityIndex = identityIndexA;
            }

            await ConflictResolution(destoryIdentity, m_resourceNameKeyIndexs[destoryIdentityIndex]);
        }

        private void Allocate(int identityLength, int resourceLength)
        {
            if (m_matrix != null)
            {
                long[,] tempMatrix = m_matrix;
                m_matrix = new long[identityLength, resourceLength];
                //Array.Copy(temp, );
            }
            else
            {
                m_matrix = new long[identityLength, resourceLength];
            }
        }

        private async Task ConflictResolution(long identity, string resourceName)
        {
            await GrainFactory.GetGrain<IResource>(resourceName).ConflictResolution(identity);
        }
    }
}
