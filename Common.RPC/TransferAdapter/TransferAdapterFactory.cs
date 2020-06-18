using System.Net;

namespace Common.RPC.TransferAdapter
{
    /// <summary>
    /// RPC数据处理器适配工厂
    /// </summary>
    public static class TransferAdapterFactory
    {
        /// <summary>
        /// 创建ZeroMQ数据处理器
        /// </summary>
        /// <param name="endPoint">终结点端口</param>
        /// <param name="zeroMQSocketType">ZeroMQ连接类型</param>
        /// <param name="identity">RPC通讯端标识</param>
        /// <returns></returns>
        public static ITransferAdapter CreateZeroMQTransferAdapter(IPEndPoint endPoint, ZeroMQSocketTypeEnum zeroMQSocketType, string identity)
        {
            return new ZeroMQTransferAdapter(endPoint, zeroMQSocketType, identity);
        }

        /// <summary>
        /// 创建ZeroMQ拆包数据处理器
        /// </summary>
        /// <param name="endPoint">终结点端口</param>
        /// <param name="zeroMQSocketType">ZeroMQ连接类型</param>
        /// <param name="identity">RPC通讯端标识</param>
        /// <returns></returns>
        public static ITransferAdapter CreatePackageSpliterZeroMQTransferAdapter(IPEndPoint endPoint, ZeroMQSocketTypeEnum zeroMQSocketType, string identity)
        {
            return new PackageSpliterTransferAdapter(CreateZeroMQTransferAdapter(endPoint, zeroMQSocketType, identity));
        }

        /// <summary>
        /// 创建UDP数据处理器
        /// </summary>
        /// <param name="endPoint">终结点端口</param>
        /// <param name="udpCRCSocketType">UDP连接类型</param>
        /// <returns></returns>
        public static ITransferAdapter CreateUDPCRCTransferAdapter(IPEndPoint endPoint, UDPCRCSocketTypeEnum udpCRCSocketType)
        {
            return new UDPCRCTransferAdapter(endPoint, udpCRCSocketType);
        }

        /// <summary>
        /// 创建UDP拆包数据处理器
        /// </summary>
        /// <param name="endPoint">终结点端口</param>
        /// <param name="udpCRCSocketType">UDP连接类型</param>
        /// <returns></returns>
        public static ITransferAdapter CreatePackageSpliterUDPCRCTransferAdapter(IPEndPoint endPoint, UDPCRCSocketTypeEnum udpCRCSocketType)
        {
            return new PackageSpliterTransferAdapter(CreateUDPCRCTransferAdapter(endPoint, udpCRCSocketType));
        }
    }
}