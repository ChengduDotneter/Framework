﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Common.RPC.TransferAdapter
{
    internal class UDPCRCTransferAdapter : ITransferAdapter, IDisposable
    {
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
        private const int CLEAR_TIME_OUT = 1000 * 60 * 2;
        private const int CLEAR_TIME_SPAN = 1000;
        private const int MAX_SEND_BUFFER_COUNT = 5000;
        private IPEndPoint m_endPoint;
        private UDPCRCSocketTypeEnum m_udpCRCSocketType;
        private ConcurrentDictionary<long, SendBufferData> m_sendBuffers;
        private Thread m_recieveThread;
        private Thread m_sendThread;
        private UdpClient m_udp;
        private int m_localPort;

        public UDPCRCTransferAdapter(IPEndPoint endPoint, UDPCRCSocketTypeEnum udpCRCSocketType)
        {
            m_endPoint = endPoint;
            m_udpCRCSocketType = udpCRCSocketType;
            m_sendBuffers = new ConcurrentDictionary<long, SendBufferData>();

            if (m_udpCRCSocketType == UDPCRCSocketTypeEnum.Server)
            {
                m_udp = new UdpClient(m_endPoint);
                m_localPort = m_endPoint.Port;
            }
            else
            {
                m_udp = new UdpClient(0);
                m_localPort = ((IPEndPoint)m_udp.Client.LocalEndPoint).Port;
            }

            m_recieveThread = new Thread(DoRecieveBuffer);
            m_recieveThread.IsBackground = true;
            m_recieveThread.Name = "UDPCRC_RECIEVE_THREAD";
            m_sendThread = new Thread(DoSendBuffer);
            m_sendThread.IsBackground = true;
            m_sendThread.Name = "UDPCRC_SEND_THREAD";
        }

        public void SendBuffer(SessionContext sessionContext, byte[] buffer, int length)
        {
            if (m_sendBuffers.Count > MAX_SEND_BUFFER_COUNT)
                return;

            if (m_udpCRCSocketType == UDPCRCSocketTypeEnum.Server && (sessionContext.SessionID == 0 || SessionContext.IsDefaultContext(sessionContext)))
                throw new Exception("服务端不能调用SendData，请改用SendSessionData方法。");

            if (SessionContext.IsDefaultContext(sessionContext))
                sessionContext.Context = m_endPoint;

            byte[] sendBuffer = new byte[buffer.Length + DATA_ID_BUFFER_LENGTH + SESSION_ID_BUFFER_LENGTH + CRC_BUFFER_LENGTH];
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

                    byte[] crcBuffer = CRC16(sendBuffer, length + SESSION_ID_BUFFER_LENGTH);

                    fixed (byte* crcBufferPtr = crcBuffer)
                        Buffer.MemoryCopy(crcBufferPtr, sendBufferPtr + length + DATA_ID_BUFFER_LENGTH + SESSION_ID_BUFFER_LENGTH, sendBuffer.Length, CRC_BUFFER_LENGTH);

                    m_sendBuffers.TryAdd(dataID, new SendBufferData(sessionContext, sendBuffer));
                }
            }
        }

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
                            m_sendBuffers.TryRemove(dataIDs[i], out SendBufferData removeSendBufferData);
                            continue;
                        }
                        else if (sendBufferData == null || (sendBufferData.RepeatTime != 0 && Environment.TickCount - sendBufferData.RepeatTime < REPEAT_TIME_SPAN))
                            continue;

                        unsafe
                        {
                            fixed (byte* sendBufferPtr = sendBufferData.Buffer)
                            {
                                long sessionID = *(long*)(sendBufferPtr + DATA_ID_BUFFER_LENGTH);

                                try
                                {
#if OUTPUT_LOG
                                    LogManager.WriteLog(string.Format("send session_id: {0}, time: {1}", sessionID, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ff")));
#endif
                                    m_udp.Send(sendBufferData.Buffer, sendBufferData.Buffer.Length, (IPEndPoint)sendBufferData.SessionContext.Context);
                                    sendBufferData.RefreshRepeatTime();
                                    sendBufferData.RepeatCount++;
                                }
                                catch
                                {
#if OUTPUT_LOG
                                    LogManager.WriteLog("send error session_id: {0}", sessionID);
#endif
                                }
                            }
                        }
                    }
                }

                Thread.Sleep(THREAD_TIME_SPAN);
            }
        }

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
                                long sessionID = *(long*)(bufferPtr + DATA_ID_BUFFER_LENGTH);

                                if (m_sendBuffers.ContainsKey(dataID))
                                    m_sendBuffers.TryRemove(dataID, out SendBufferData removeSendBufferData);
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
                                Buffer.MemoryCopy(bufferCRCPtr, bufferPtr + buffer.Length - CRC_BUFFER_LENGTH, CRC_BUFFER_LENGTH, CRC_BUFFER_LENGTH);

                                byte[] valueCRC = CRC16(buffer, buffer.Length - CRC_BUFFER_LENGTH);

                                if (CheckCRC(bufferCRC, valueCRC))
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
#if OUTPUT_LOG
                                    LogManager.WriteLog(string.Format("recv session_id: {0}, time: {1}", sessionID, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ff")));
#endif
                                    OnBufferRecieved?.Invoke(new SessionContext(sessionID, ip), data);
                                }
                            }
                        }
                    }
                }
                catch
                {
#if OUTPUT_LOG
                    LogManager.WriteLog("recv error");
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

        private bool CheckCRC(byte[] bufferCRC, byte[] valueCRC)
        {
            for (int i = 0; i < CRC_BUFFER_LENGTH; i++)
                if (bufferCRC[i] != valueCRC[i])
                    return false;

            return true;
        }
    }

    public enum UDPCRCSocketTypeEnum
    {
        Server,
        Client,
    }
}