namespace Common.RPC
{
    /// <summary>
    /// RPC数据结构体接口，由于类存在浅拷贝问题，则继承该接口的数据类型强制定义为结构体
    /// </summary>
    public interface IRPCData
    {
        /// <summary>
        /// 全局唯一RPCID
        /// </summary>
        byte MessageID { get; }
    }
}