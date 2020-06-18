namespace Common.RPC.TransferAdapter
{
    /// <summary>
    /// 字节流数据处理委托
    /// </summary>
    /// <param name="sessionContext">通讯上下文</param>
    /// <param name="buffer">字节流缓冲区</param>
    public delegate void OnBufferRecievedHandler(SessionContext sessionContext, byte[] buffer);

    /// <summary>
    /// 通讯数据转换器适配器接口
    /// </summary>
    public interface ITransferAdapter
    {
        /// <summary>
        /// 数据转换事件
        /// </summary>
        event OnBufferRecievedHandler OnBufferRecieved;

        /// <summary>
        /// 开始
        /// </summary>
        void Strat();

        /// <summary>
        /// 发送字节流缓冲区中的数据
        /// </summary>
        /// <param name="sessionContext">通讯上下文</param>
        /// <param name="buffer">字节流缓冲区</param>
        /// <param name="length">数据总长度</param>
        void SendBuffer(SessionContext sessionContext, byte[] buffer, int length);
    }

    /// <summary>
    /// 通讯端上下文实体
    /// </summary>
    public class SessionContext
    {
        private const string DEFAULT_CONTEXT = "DEFAULT_CONTEXT";

        /// <summary>
        /// 通讯ID
        /// </summary>
        public long SessionID { get; }

        /// <summary>
        /// 通讯内容
        /// </summary>
        public object Context { get; internal set; }

        /// <summary>
        /// 是否默认通讯上下文
        /// </summary>
        /// <param name="sessionContext">通讯上下文</param>
        /// <returns></returns>
        public static bool IsDefaultContext(SessionContext sessionContext)
        {
            if (sessionContext.Context is string)
                return (string)sessionContext.Context == DEFAULT_CONTEXT;
            return
                false;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="sessionID">通讯ID</param>
        /// <param name="context">通讯内容</param>
        public SessionContext(long sessionID, object context)
        {
            SessionID = sessionID;
            Context = context;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="sessionID">通讯ID</param>
        public SessionContext(long sessionID) : this(sessionID, DEFAULT_CONTEXT)
        {
        }
    }
}