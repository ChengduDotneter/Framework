using System.Text;

namespace Common.RPC.BufferSerializer
{
    /// <summary>
    /// 缓冲区序列化工厂
    /// </summary>
    public static class BufferSerialzerFactory
    {
        /// <summary>
        /// 创建二进制序列化器
        /// </summary>
        /// <param name="encoding">序列化编码规则</param>
        /// <returns></returns>
        public static IBufferSerializer CreateBinaryBufferSerializer(Encoding encoding)
        {
            return new BinaryBufferSerializer(encoding);
        }

        /// <summary>
        /// 创建JSON序列化器
        /// </summary>
        /// <param name="encoding">序列化编码规则</param>
        /// <returns></returns>
        public static IBufferSerializer CreateJsonBufferSerializer(Encoding encoding)
        {
            return new JsonBufferSerializer(encoding);
        }
    }
}