namespace Common.RPC.TransferAdapter
{
    public delegate void OnBufferRecievedHandler(SessionContext sessionContext, byte[] buffer);

    public interface ITransferAdapter
    {
        event OnBufferRecievedHandler OnBufferRecieved;
        void Strat();
        void SendBuffer(SessionContext sessionContext, byte[] buffer, int length);
    }

    public class SessionContext
    {
        private const string DEFAULT_CONTEXT = "DEFAULT_CONTEXT";
        public long SessionID { get; }
        public object Context { get; internal set; }

        public static bool IsDefaultContext(SessionContext sessionContext)
        {
            if (sessionContext.Context is string)
                return (string)sessionContext.Context == DEFAULT_CONTEXT;
            return
                false;
        }

        public SessionContext(long sessionID, object context)
        {
            SessionID = sessionID;
            Context = context;
        }

        public SessionContext(long sessionID) : this(sessionID, DEFAULT_CONTEXT) { }
    }
}
