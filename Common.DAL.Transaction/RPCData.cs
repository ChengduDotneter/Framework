using Common.RPC;

namespace Common.DAL.Transaction
{
    /// <summary>
    /// RPC申请资源请求数据结构体
    /// </summary>
    public struct ApplyRequestData : IRPCData
    {
        /// <summary>
        /// 所需申请的资源名，现包括数据表名
        /// </summary>
        public string ResourceName { get; set; }

        /// <summary>
        /// 事务线程ID
        /// </summary>
        public long Identity { get; set; }

        /// <summary>
        /// 权重
        /// </summary>
        public int Weight { get; set; }

        /// <summary>
        /// 超时时间,时间戳,单位ms
        /// </summary>
        public int TimeOut { get; set; }

        /// <summary>
        /// 全局唯一RPCID
        /// </summary>
        public byte MessageID { get { return 0x1; } }
    }

    /// <summary>
    /// RPC申请资源返回结果数据结构体
    /// </summary>
    public struct ApplyResponseData : IRPCData
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 全局唯一RPCID
        /// </summary>
        public byte MessageID { get { return 0x2; } }
    }

    /// <summary>
    /// RPC释放资源请求数据结构体
    /// </summary>
    public struct ReleaseRequestData : IRPCData
    {
        /// <summary>
        /// 所需释放的资源名，现包括数据表名
        /// </summary>
        public string ResourceName { get; set; }

        /// <summary>
        /// 事务线程ID
        /// </summary>
        public long Identity { get; set; }

        /// <summary>
        /// 全局唯一RPCID
        /// </summary>
        public byte MessageID { get { return 0x3; } }
    }

    /// <summary>
    /// RPC释放资源返回结果数据结构体
    /// </summary>
    public struct ReleaseResponseData : IRPCData
    {
        /// <summary>
        /// 全局唯一RPCID
        /// </summary>
        public byte MessageID { get { return 0x4; } }
    }

    /// <summary>
    /// 资源占用心跳检测请求数据结构体
    /// </summary>
    public struct ResourceHeartBeatReqesut : IRPCData
    {
        /// <summary>
        /// 事务线程ID
        /// </summary>
        public long Identity { get; set; }

        /// <summary>
        /// 全局唯一RPCID
        /// </summary>
        public byte MessageID { get { return 0x5; } }
    }
}