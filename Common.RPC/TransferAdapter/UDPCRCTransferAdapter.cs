using Common.Log;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Common.RPC.TransferAdapter
{
    /// <summary>
    /// udp适配器
    /// </summary>
    internal class UDPCRCTransferAdapter : ITransferAdapter, IDisposable
    {
        /// <summary>
        /// 发送的数据体
        /// </summary>
        private class SendBufferData
        {
            public int RepeatCount { get; set; }
            public int RepeatTime { get; private set; }
            public byte[] Buffer { get; }
            public SessionContext SessionContext { get; }

            public void RefreshRepeatTime()
            {
                RepeatTime = Environment.TickCount;
            }

            public SendBufferData(SessionContext sessionContext, byte[] buffer)
            {
                SessionContext = sessionContext;
                Buffer = buffer;
            }
        }

        public event OnBufferRecievedHandler OnBufferRecieved;

        private const int SESSION_ID_BUFFER_LENGTH = sizeof(long);
        private const int DATA_ID_BUFFER_LENGTH = sizeof(long);
        private const int CRC_BUFFER_LENGTH = 2;
        private const int THREAD_TIME_SPAN = 1;
        private const int REPEAT_SEND_MAX_COUNT = 150;
        private const int REPEAT_TIME_SPAN = 4000;
        private const int MAX_SEND_BUFFER_COUNT = 5000;
        private IPEndPoint m_endPoint;
        private UDPCRCSocketTypeEnum m_udpCRCSocketType;
        private ConcurrentDictionary<long, SendBufferData> m_sendBuffers;
        private Thread m_recieveThread;
        private Thread m_sendThread;
        private UdpClient m_udp;

#if OUTPUT_LOG
        private static readonly ILogHelper m_logHelper;
#endif

        static UDPCRCTransferAdapter()
        {
#if OUTPUT_LOG
            m_logHelper = LogHelperFactory.GetLog4netLogHelper();
#endif
        }

        public UDPCRCTransferAdapter(IPEndPoint endPoint, UDPCRCSocketTypeEnum udpCRCSocketType)
        {
            m_endPoint = endPoint;
            m_udpCRCSocketType = udpCRCSocketType;
            m_sendBuffers = new ConcurrentDictionary<long, SendBufferData>();

            if (m_udpCRCSocketType == UDPCRCSocketTypeEnum.Server)
                m_udp = new UdpClient(m_endPoint);
            else
                m_udp = new UdpClient(0);

            m_recieveThread = new Thread(DoRecieveBuffer);
            m_recieveThread.IsBackground = true;
            m_recieveThread.Name = "UDPCRC_RECIEVE_THREAD";
            m_sendThread = new Thread(DoSendBuffer);
            m_sendThread.IsBackground = true;
            m_sendThread.Name = "UDPCRC_SEND_THREAD";
        }
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="sessionContext">通讯上下文</param>
        /// <param name="buffer">发送的字节数组</param>
        /// <param name="length">数据长度</param>
        public void SendBuffer(SessionContext sessionContext, byte[] buffer, int length)
        {
            if (m_sendBuffers.Count > MAX_SEND_BUFFER_COUNT)
                throw new Exception("发送队列超长。");

            if (m_udpCRCSocketType == UDPCRCSocketTypeEnum.Server && (sessionContext.SessionID == 0 || SessionContext.IsDefaultContext(sessionContext)))
                throw new Exception("服务端不能调用SendData，请改用SendSessionData方法。");

            if (SessionContext.IsDefaultContext(sessionContext))
                sessionContext.Context = m_endPoint;

            byte[] sendBuffer = new byte[length + DATA_ID_BUFFER_LENGTH + SESSION_ID_BUFFER_LENGTH + CRC_BUFFER_LENGTH];
            long dataID = IDGenerator.NextID();
            long sessionID = sessionContext.SessionID;

            unsafe
            {
                fixed (byte* sendBufferPtr = sendBuffer)
                fixed (byte* bufferPtr = buffer)
                {
                    Buffer.MemoryCopy(bufferPtr, sendBufferPtr + DATA_ID_BUFFER_LENGTH + SESSION_ID_BUFFER_LENGTH, sendBuffer.Length, length);
                    Buffer.MemoryCopy(&dataID, sendBufferPtr, sendBuffer.Length, SESSION_ID_BUFFER_LENGTH);
                    Buffer.MemoryCopy(&sessionID, sendBufferPtr + DATA_ID_BUFFER_LENGTH, sendBuffer.Length, SESSION_ID_BUFFER_LENGTH);

                    byte[] crcBuffer = CRC16(sendBuffer, length + DATA_ID_BUFFER_LENGTH + SESSION_ID_BUFFER_LENGTH);

                    fixed (byte* crcBufferPtr = crcBuffer)
                        Buffer.MemoryCopy(crcBufferPtr, sendBufferPtr + length + DATA_ID_BUFFER_LENGTH + SESSION_ID_BUFFER_LENGTH, sendBuffer.Length, CRC_BUFFER_LENGTH);

                    m_sendBuffers.TryAdd(dataID, new SendBufferData(sessionContext, sendBuffer));
                }
            }
        }
        /// <summary>
        /// 发送数据
        /// </summary>
        private unsafe void DoSendBuffer()
        {
            while (true)
            {
                if (!m_sendBuffers.IsEmpty)
                {
                    long[] dataIDs = m_sendBuffers.Keys.ToArray();

                    for (int i = 0; i < dataIDs.Length; i++)
                    {
                        if (m_sendBuffers.TryGetValue(dataIDs[i], out SendBufferData sendBufferData) && sendBufferData.RepeatCount > REPEAT_SEND_MAX_COUNT)
                        {
                            m_sendBuffers.TryRemove(dataIDs[i], out _);
                            continue;
                        }
                        else if (sendBufferData == null || (sendBufferData.RepeatTime != 0 && Environment.TickCount - sendBufferData.RepeatTime < REPEAT_TIME_SPAN))
                            continue;

                        fixed (byte* sendBufferPtr = sendBufferData.Buffer)
                        {
                            // ReSharper disable once UnusedVariable
                            long sessionID = *(long*)(sendBufferPtr + DATA_ID_BUFFER_LENGTH);

                            try
                            {
                                m_udp.Send(sendBufferData.Buffer, sendBufferData.Buffer.Length, (IPEndPoint)sendBufferData.SessionContext.Context);
                                sendBufferData.RefreshRepeatTime();
                                sendBufferData.RepeatCount++;
                            }
#pragma warning disable CS0168 // 声明了变量，但从未使用过
                            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量，但从未使用过
                            {
                                // ignored
#if OUTPUT_LOG
                                m_logHelper.Info("Transfer_UDP",
                                                 $"send error session_id: {sessionID}{Environment.NewLine}message: {Environment.NewLine}{ex.Message}{Environment.NewLine}stack_trace: {Environment.NewLine}{ex.StackTrace}");
#endif
                            }
                        }
                    }
                }

                Thread.Sleep(THREAD_TIME_SPAN);
            }
        }
        /// <summary>
        /// 接收数据
        /// </summary>
        private void DoRecieveBuffer()
        {
            IPEndPoint ip;

            if (m_udpCRCSocketType == UDPCRCSocketTypeEnum.Server)
                ip = new IPEndPoint(IPAddress.Any, 0);
            else
                ip = m_endPoint;

            while (true)
            {
                try
                {
                    byte[] buffer = m_udp.Receive(ref ip);

                    if (buffer.Length < DATA_ID_BUFFER_LENGTH + SESSION_ID_BUFFER_LENGTH)
                    {
                        continue;
                    }
                    else if (buffer.Length == DATA_ID_BUFFER_LENGTH + SESSION_ID_BUFFER_LENGTH)
                    {
                        unsafe
                        {
                            fixed (byte* bufferPtr = buffer)
                            {
                                long dataID = *(long*)bufferPtr;

                                if (m_sendBuffers.ContainsKey(dataID))
                                    m_sendBuffers.TryRemove(dataID, out _);
                            }
                        }
                    }
                    else if (buffer.Length < DATA_ID_BUFFER_LENGTH + SESSION_ID_BUFFER_LENGTH + CRC_BUFFER_LENGTH)
                    {
                        continue;
                    }
                    else
                    {
                        byte[] bufferCRC = new byte[CRC_BUFFER_LENGTH];

                        unsafe
                        {
                            fixed (byte* bufferCRCPtr = bufferCRC)
                            fixed (byte* bufferPtr = buffer)
                            {
                                Buffer.MemoryCopy(bufferPtr + buffer.Length - CRC_BUFFER_LENGTH, bufferCRCPtr, CRC_BUFFER_LENGTH, CRC_BUFFER_LENGTH);
                                byte[] valueCRC = CRC16(buffer, buffer.Length - CRC_BUFFER_LENGTH);

                                if (!CheckCRC(bufferCRC, valueCRC))
                                    continue;

                                byte[] data = new byte[buffer.Length - DATA_ID_BUFFER_LENGTH - SESSION_ID_BUFFER_LENGTH - CRC_BUFFER_LENGTH];

                                fixed (byte* dataPtr = data)
                                {
                                    byte[] callbackBuffer = new byte[DATA_ID_BUFFER_LENGTH + SESSION_ID_BUFFER_LENGTH];

                                    fixed (byte* callbackBufferPtr = callbackBuffer)
                                        Buffer.MemoryCopy(bufferPtr, callbackBufferPtr, DATA_ID_BUFFER_LENGTH + SESSION_ID_BUFFER_LENGTH, DATA_ID_BUFFER_LENGTH + SESSION_ID_BUFFER_LENGTH);

                                    m_udp.Send(callbackBuffer, DATA_ID_BUFFER_LENGTH + SESSION_ID_BUFFER_LENGTH, ip);

                                    long sessionID = *(long*)(bufferPtr + DATA_ID_BUFFER_LENGTH);
                                    Buffer.MemoryCopy(bufferPtr + DATA_ID_BUFFER_LENGTH + SESSION_ID_BUFFER_LENGTH, dataPtr, data.Length, data.Length);
                                    OnBufferRecieved?.Invoke(new SessionContext(sessionID, ip), data);
                                }
                            }
                        }
                    }
                }
#pragma warning disable CS0168 // 声明了变量，但从未使用过
                catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量，但从未使用过
                {
                    // ignored
#if OUTPUT_LOG
                    m_logHelper.Info("Transfer_UDP",
                                     $"recv error{Environment.NewLine}message: {Environment.NewLine}{ex.Message}{Environment.NewLine}stack_trace: {Environment.NewLine}{ex.StackTrace}");
#endif
                }
            }
        }

        public void Strat()
        {
            m_recieveThread.Start();
            m_sendThread.Start();
        }

        public void Dispose()
        {
            m_udp.Close();
            m_udp.Dispose();
        }

        private static byte[] CRC16(byte[] data, int length)
        {
            if (length > 0)
            {
                ushort crc = 0xFFFF;

                for (int i = 0; i < length; i++)
                {
                    crc = (ushort)(crc ^ (data[i]));

                    for (int j = 0; j < 8; j++)
                        crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
                }

                byte hi = (byte)((crc & 0xFF00) >> 8); //高位置
                byte lo = (byte)(crc & 0x00FF); //低位置

                return new byte[] { hi, lo };
            }

            return new byte[] { 0, 0 };
        }

        private static bool CheckCRC(byte[] bufferCRC, byte[] valueCRC)
        {
            for (int i = 0; i < CRC_BUFFER_LENGTH; i++)
                if (bufferCRC[i] != valueCRC[i])
                    return false;

            return true;
        }
    }

    /// <summary>
    /// UDP连接类型枚举
    /// </summary>
    public enum UDPCRCSocketTypeEnum
    {
        /// <summary>
        /// 服务端
        /// </summary>
        Server,

        /// <summary>
        /// 客户端
        /// </summary>
        Client,
    }
}