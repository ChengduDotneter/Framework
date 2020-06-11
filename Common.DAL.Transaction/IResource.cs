using System.Threading.Tasks;
using Orleans;

namespace Common.DAL.Transaction
{
    public interface IResource : IGrainWithStringKey
    {
        Task<bool> Apply(long identity, int weight, int timeOut);
        Task Release(long identity);
        Task ConflictResolution(long identity);
    }
}
