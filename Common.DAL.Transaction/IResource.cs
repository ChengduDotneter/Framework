using System.Threading.Tasks;
using Orleans;

namespace Common.DAL.Transaction
{
    public interface IResource : IGrainWithStringKey
    {
        Task<bool> Apply(int identity, int timeOut);
        Task Release(int identity);
    }
}
