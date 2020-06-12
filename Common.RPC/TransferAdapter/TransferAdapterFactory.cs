using System.Net;

namespace Common.RPC.TransferAdapter
{
    public static class TransferAdapterFactory
    {
        public static ITransferAdapter CreateZeroMQTransferAdapter(IPEndPoint endPoint, ZeroMQSocketTypeEnum zeroMQSocketType, string identity)
        {
            return new ZeroMQTransferAdapter(endPoint, zeroMQSocketType, identity);
        }

        public static ITransferAdapter CreatePackageSpliterZeroMQTransferAdapter(IPEndPoint endPoint, ZeroMQSocketTypeEnum zeroMQSocketType, string identity)
        {
            return new PackageSpliterTransferAdapter(CreateZeroMQTransferAdapter(endPoint, zeroMQSocketType, identity));
        }

        public static ITransferAdapter CreateUDPCRCTransferAdapter(IPEndPoint endPoint, UDPCRCSocketTypeEnum udpCRCSocketType)
        {
            return new UDPCRCTransferAdapter(endPoint, udpCRCSocketType);
        }

        public static ITransferAdapter CreatePackageSpliterUDPCRCTransferAdapter(IPEndPoint endPoint, UDPCRCSocketTypeEnum udpCRCSocketType)
        {
            return new PackageSpliterTransferAdapter(CreateUDPCRCTransferAdapter(endPoint, udpCRCSocketType));
        }
    }
}
