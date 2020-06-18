namespace Common.RPC.BufferSerializer
{
    /// <summary>
    /// 字节序列化缓冲器接口
    /// </summary>
    public interface IBufferSerializer
    {
        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="buffer">字节流缓冲区</param>
        /// <returns>返回序列化的长度</returns>
        int Serialize(IRPCData data, byte[] buffer);

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="buffer">字节流缓冲区</param>
        /// <returns></returns>
        IRPCData Deserialize(byte[] buffer);
    }
}