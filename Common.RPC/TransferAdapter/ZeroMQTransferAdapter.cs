using NetMQ;
using NetMQ.Sockets;
using System;
using System.Net;
using System.Text;
using System.Threading;

namespace Common.RPC.TransferAdapter
{
    internal class ZeroMQTransferAdapter : ITransferAdapter
    {
        public event OnBufferRecievedHandler OnBufferRecieved;

        private const int SESSION_ID_BUFFER_LENGTH = sizeof(long);
        private const int BUFFER_LENGTH = 1024 * 1024 * 64;
        private const int SPLIT_LENGTH = 65536 * 1024;
        private Thread m_recieveThread;
        private int m_offset;
        private bool m_start;
        private NetMQSocket m_socket;
        private ZeroMQSocketTypeEnum m_zeroMQSocketType;
#if OUTPUT_LOG
        private string m_identity;
#endif

        public ZeroMQTransferAdapter(IPEndPoint endPoint, ZeroMQSocketTypeEnum zeroMQSocketType, string identity)
        {
#if OUTPUT_LOG
            m_identity = identity;
#endif
            m_zeroMQSocketType = zeroMQSocketType;
            m_recieveThread = new Thread(DoRecieveBuffer);
            m_recieveThread.IsBackground = true;
            m_recieveThread.Name = "ZEROMQ_THREAD";
            m_socket = CreateNetMQSocket(
                zeroMQSocketType,
                string.Format("tcp://{0}:{1}", endPoint.Address.ToString(), endPoint.Port),
                Encoding.UTF8.GetBytes(identity));
        }

        private static NetMQSocket CreateNetMQSocket(ZeroMQSocketTypeEnum zeroMQSocketType, string connectionString, byte[] identity)
        {
            NetMQSocket socket;

            switch (zeroMQSocketType)
            {
                case ZeroMQSocketTypeEnum.Publisher:
                    socket = new PublisherSocket();
                    socket.Options.Identity = identity;
                    socket.Bind(connectionString);
                    break;

                case ZeroMQSocketTypeEnum.Subscriber:
                    socket = new SubscriberSocket();
                    socket.Options.Identity = identity;
                    socket.Connect(connectionString);
                    ((SubscriberSocket)socket).Subscribe(string.Empty);
                    break;

                case ZeroMQSocketTypeEnum.Client:
                    socket = new DealerSocket();
                    socket.Options.Identity = identity;
                    socket.Connect(connectionString);
                    break;

                case ZeroMQSocketTypeEnum.Server:
                    socket = new RouterSocket();
                    socket.Options.Identity = identity;
                    socket.Bind(connectionString);
                    socket.Options.RouterMandatory = true;
                    break;

                default:
                    throw new NotImplementedException();
            }

            return socket;
        }

        private static bool CanSend(ZeroMQSocketTypeEnum zeroMQSocketType)
        {
            switch (zeroMQSocketType)
            {
                case ZeroMQSocketTypeEnum.Publisher:
                case ZeroMQSocketTypeEnum.Client:
                case ZeroMQSocketTypeEnum.Server:
                    return true;

                case ZeroMQSocketTypeEnum.Subscriber:
                    return false;

                default:
                    throw new NotImplementedException();
            }
        }

        private static bool CanRecieve(ZeroMQSocketTypeEnum zeroMQSocketType)
        {
            switch (zeroMQSocketType)
            {
                case ZeroMQSocketTypeEnum.Subscriber:
                case ZeroMQSocketTypeEnum.Client:
                case ZeroMQSocketTypeEnum.Server:
                    return true;

                case ZeroMQSocketTypeEnum.Publisher:
                    return false;

                default:
                    throw new NotImplementedException();
            }
        }

        public void SendBuffer(SessionContext sessionContext, byte[] buffer, int length)
        {
            if (!m_start)
                throw new Exception("尚未打开ZeroMQTransferAdapter。");

            if (!CanSend(m_zeroMQSocketType))
                throw new Exception(string.Format("{0}模式不允许发送数据。", m_zeroMQSocketType));

            byte[] sendBuffer = buffer;

            if (buffer.Length < length + SESSION_ID_BUFFER_LENGTH)
            {
                sendBuffer = new byte[buffer.Length + SESSION_ID_BUFFER_LENGTH];

                unsafe
                {
                    fixed (byte* sendBufferPtr = sendBuffer)
                    fixed (byte* bufferPtr = buffer)
                        Buffer.MemoryCopy(bufferPtr, sendBufferPtr, sendBuffer.Length, length);
                }
            }

            unsafe
            {
                fixed (byte* sendBufferPtr = sendBuffer)
                {
                    long sessionID = sessionContext.SessionID;
                    Buffer.MemoryCopy(sendBufferPtr, sendBufferPtr + SESSION_ID_BUFFER_LENGTH, sendBuffer.Length, length);
                    Buffer.MemoryCopy(&sessionID, sendBufferPtr, sendBuffer.Length, SESSION_ID_BUFFER_LENGTH);
                }
            }

            try
            {
                DoSendBuffer(sessionContext, sendBuffer, length + SESSION_ID_BUFFER_LENGTH);
            }
            catch
            {
#if OUTPUT_LOG
                LogManager.WriteLog("send error session_id: {0}", sessionContext.SessionID);
#endif
            }
        }

        private unsafe void DoSendBuffer(SessionContext sessionContext, byte[] buffer, int length)
        {
#if OUTPUT_LOG
            LogManager.WriteLog(string.Format("identity: {0}, session_id: {1}, send time: {2}", m_identity, sessionContext.SessionID, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ff")));
#endif
            if (m_zeroMQSocketType == ZeroMQSocketTypeEnum.Server && (sessionContext.SessionID == 0 || SessionContext.IsDefaultContext(sessionContext)))
                throw new Exception("服务端不能调用SendData，请改用SendSessionData方法。");
            else if (m_zeroMQSocketType == ZeroMQSocketTypeEnum.Server)
                m_socket.SendFrame((byte[])sessionContext.Context, true);
            else if (m_zeroMQSocketType != ZeroMQSocketTypeEnum.Server)
                m_socket.SendFrameEmpty(true);

            if (length > SPLIT_LENGTH)
            {
                int totalCount = 0;
                byte[] sendBuffer = new byte[SPLIT_LENGTH];

                unsafe
                {
                    fixed (byte* bufferPtr = buffer)
                    fixed (byte* sendBufferPtr = sendBuffer)
                    {
                        while (totalCount < length)
                        {
                            int count = length - totalCount;
                            int sendCount = count > SPLIT_LENGTH ? SPLIT_LENGTH : count;

                            Buffer.MemoryCopy(bufferPtr + totalCount, sendBufferPtr, sendBuffer.Length, sendCount);
                            totalCount += sendCount;
                            m_socket.SendFrame(sendBuffer, sendCount, totalCount < length);
                        }
                    }
                }
            }
            else
            {
                m_socket.SendFrame(buffer, length);
            }
        }

        private void DoRecieveBuffer()
        {
            unsafe
            {
                byte[] recieveBuffer = new byte[BUFFER_LENGTH];

                fixed (byte* recieveBufferPtr = recieveBuffer)
                {
                    while (true)
                    {
                        try
                        {
                            bool more = true;
                            byte[] identity = null;

                            if (m_zeroMQSocketType == ZeroMQSocketTypeEnum.Server)
                                identity = m_socket.ReceiveFrameBytes();

                            while (more)
                            {
                                byte[] buffer = m_socket.ReceiveFrameBytes(out more);

                                if (buffer.Length == 0)
                                    continue;

                                fixed (byte* bufferPtr = buffer)
                                    Buffer.MemoryCopy(bufferPtr, recieveBufferPtr + m_offset, recieveBuffer.Length, buffer.Length);

                                m_offset += buffer.Length;

                                if (!more)
                                {
                                    byte[] data = new byte[m_offset - SESSION_ID_BUFFER_LENGTH];
                                    m_offset = 0;

                                    fixed (byte* dataPtr = data)
                                    {
                                        Buffer.MemoryCopy(recieveBufferPtr + SESSION_ID_BUFFER_LENGTH, dataPtr, data.Length, data.Length);

                                        long sessionID = *(long*)recieveBufferPtr;
#if OUTPUT_LOG
                                        LogManager.WriteLog(string.Format("identity: {0}, recv session_id: {1}, time: {2}", m_identity, sessionID, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ff")));
#endif
                                        OnBufferRecieved?.Invoke(new SessionContext(sessionID, identity), data);
                                    }
                                }
                            }
                        }
                        catch
                        {
#if OUTPUT_LOG
                            LogManager.WriteLogWriteLine("recv error");
#endif
                        }
                    }
                }
            }
        }

        public void Strat()
        {
            m_start = true;

            if (CanRecieve(m_zeroMQSocketType))
                m_recieveThread.Start();
        }
    }

    public enum ZeroMQSocketTypeEnum
    {
        Publisher,
        Subscriber,
        Client,
        Server,
    }
}