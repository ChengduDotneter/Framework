namespace Common.RPC.BufferSerializer
{
    public interface IBufferSerializer
    {
        int Serialize(IRPCData data, byte[] buffer);
        IRPCData Deserialize(byte[] buffer);
    }
}
