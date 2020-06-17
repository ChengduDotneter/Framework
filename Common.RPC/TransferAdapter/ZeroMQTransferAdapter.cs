﻿using log4net;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
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
        private const int MAX_SEND_QUEUE_COUNT = 5000;
        private Thread m_recieveThread;
        private Thread m_sendThread;
        private int m_offset;
        private bool m_start;
        private NetMQSocket m_socket;
        private ZeroMQSocketTypeEnum m_zeroMQSocketType;
        private ConcurrentQueue<SendData> m_sendQueue;
#if OUTPUT_LOG
        private string m_identity;
        private static ILog m_log;
#endif

        class SendData
        {
            public SessionContext SessionContext { get; }
            public byte[] Buffer { get; }

            public unsafe SendData(SessionContext sessionContext, byte[] buffer, int length)
            {
                SessionContext = sessionContext;
                Buffer = new byte[length];

                fixed (byte* sourPtr = buffer)
                fixed (byte* destPtr = Buffer)
                {
                    System.Buffer.MemoryCopy(sourPtr, destPtr, length, length);
                }
            }
        }

        static ZeroMQTransferAdapter()
        {
#if OUTPUT_LOG
            m_log = LogHelper.CreateLog("Transfer", "ZeroMQ");
#endif
        }

        public ZeroMQTransferAdapter(IPEndPoint endPoint, ZeroMQSocketTypeEnum zeroMQSocketType, string identity)
        {
#if OUTPUT_LOG
            m_identity = identity;
#endif
            m_sendQueue = new ConcurrentQueue<SendData>();
            m_zeroMQSocketType = zeroMQSocketType;
            m_recieveThread = new Thread(DoRecieveBuffer);
            m_recieveThread.IsBackground = true;
            m_recieveThread.Name = "ZEROMQ_RECIEVE_THREAD";
            m_sendThread = new Thread(Send);
            m_sendThread.IsBackground = true;
            m_sendThread.Name = "ZEROMQ_SEND_THREAD";
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

            if (m_sendQueue.Count > MAX_SEND_QUEUE_COUNT)
                return;

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

            m_sendQueue.Enqueue(new SendData(sessionContext, sendBuffer, length + SESSION_ID_BUFFER_LENGTH));
        }

        private unsafe void DoSendBuffer(SessionContext sessionContext, byte[] buffer)
        {
            if (m_zeroMQSocketType == ZeroMQSocketTypeEnum.Server && (sessionContext.SessionID == 0 || SessionContext.IsDefaultContext(sessionContext)))
                throw new Exception("服务端不能调用SendData，请改用SendSessionData方法。");
            else if (m_zeroMQSocketType == ZeroMQSocketTypeEnum.Server)
                m_socket.SendFrame((byte[])sessionContext.Context, true);
            else if (m_zeroMQSocketType != ZeroMQSocketTypeEnum.Server)
                m_socket.SendFrameEmpty(true);

            if (buffer.Length > SPLIT_LENGTH)
            {
                int totalCount = 0;
                byte[] sendBuffer = new byte[SPLIT_LENGTH];

                unsafe
                {
                    fixed (byte* bufferPtr = buffer)
                    fixed (byte* sendBufferPtr = sendBuffer)
                    {
                        while (totalCount < buffer.Length)
                        {
                            int count = buffer.Length - totalCount;
                            int sendCount = count > SPLIT_LENGTH ? SPLIT_LENGTH : count;

                            Buffer.MemoryCopy(bufferPtr + totalCount, sendBufferPtr, sendBuffer.Length, sendCount);
                            totalCount += sendCount;
                            m_socket.SendFrame(sendBuffer, sendCount, totalCount < buffer.Length);
                        }
                    }
                }
            }
            else
            {
                m_socket.SendFrame(buffer, buffer.Length);
            }
        }

        private void Send()
        {
            while (true)
            {
                if (m_sendQueue.IsEmpty)
                    Thread.Sleep(1);

                while (m_sendQueue.TryDequeue(out SendData sendData))
                {
                    try
                    {
                        DoSendBuffer(sendData.SessionContext, sendData.Buffer);
                    }
                    catch (Exception ex)
                    {
#if OUTPUT_LOG
                        m_log.Error($"send error{Environment.NewLine}message: {Environment.NewLine}{ex.Message}{Environment.NewLine}stack_trace: {Environment.NewLine}{ex.StackTrace}");
#endif
                    }
                }
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
                                        OnBufferRecieved?.Invoke(new SessionContext(sessionID, identity), data);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
#if OUTPUT_LOG
                            m_log.Error($"recv error{Environment.NewLine}message: {Environment.NewLine}{ex.Message}{Environment.NewLine}stack_trace: {Environment.NewLine}{ex.StackTrace}");
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

            if (CanSend(m_zeroMQSocketType))
                m_sendThread.Start();
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