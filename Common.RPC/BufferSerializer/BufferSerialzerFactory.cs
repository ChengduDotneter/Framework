using System.Text;

namespace Common.RPC.BufferSerializer
{
    public static class BufferSerialzerFactory
    {
        public static IBufferSerializer CreateBinaryBufferSerializer(Encoding encoding)
        {
            return new BinaryBufferSerializer(encoding);
        }
    }
}
