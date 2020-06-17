using System.Threading.Tasks;

namespace Common.DAL.Transaction
{
    /// <summary>
    /// 资源Grains接口
    /// </summary>
    public interface IResource
    {
        /// <summary>
        /// 申请事务资源
        /// </summary>
        /// <param name="identity">事务线程ID</param>
        /// <param name="weight">权重</param>
        /// <param name="timeOut">超时时间</param>
        /// <returns></returns>
        Task<bool> Apply(long identity, int weight, int timeOut);
    }
}