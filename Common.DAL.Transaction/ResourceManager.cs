using System.Collections.Concurrent;

namespace Common.DAL.Transaction
{
    public class ResourceManager : IResourceManager
    {
        private ConcurrentDictionary<string, IResource> m_resourceManage;

        public ResourceManager()
        {
            m_resourceManage = new ConcurrentDictionary<string, IResource>();
        }

        public IResource GetResource(string resourceName)
        {
            if (!m_resourceManage.ContainsKey(resourceName))
                m_resourceManage.TryAdd(resourceName, new Resource(resourceName));

            return m_resourceManage[resourceName];
        }
    }
}
