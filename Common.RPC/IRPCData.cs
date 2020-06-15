namespace Common.RPC
{
    /// <summary>
    /// RPC数据结构体接口
    /// </summary>
    public interface IRPCData
    {
        /// <summary>
        /// 全局唯一RPCID
        /// </summary>
        byte MessageID { get; }
    }
}