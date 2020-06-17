using System.Collections.Concurrent;

namespace Common.DAL.Transaction
{
    public interface IResourceManage
    {
        IResource GetResource(string resourceName);
    }

    public class ResourceManage : IResourceManage
    {
        private ConcurrentDictionary<string, IResource> m_resourceManage;

        public ResourceManage()
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