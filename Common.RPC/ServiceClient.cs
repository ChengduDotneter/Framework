﻿using Common.RPC;
using Common.RPC.BufferSerializer;
using Common.RPC.TransferAdapter;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace Common.RPC
{
    public class ServiceClient : IDisposable
    {
        private class RecieveData
        {
            public SessionContext SessionContext { get; }
            public byte[] Buffer { get; }

            public RecieveData(SessionContext sessionContext, byte[] buffer)
            {
                SessionContext = sessionContext;
                Buffer = buffer;
            }
        }

        private class SendingData
        {
            public SessionContext SessionContext { get; }
            public IRPCData Data { get; }

            public SendingData(SessionContext sessionContext, IRPCData data)
            {
                SessionContext = sessionContext;
                Data = data;
            }
        }

        private const int SLEEP_TIME_SPAN = 1;
        private const int BUFFER_LENGTH = 1024 * 1024 * 64;
        private ITransferAdapter m_transferAdapter;
        private IBufferSerializer m_bufferSerializer;
        private Thread m_recieveThread;
        private Thread m_sendThread;
        private ConcurrentQueue<SendingData> m_sendQueue;
        private ConcurrentQueue<RecieveData> m_recieveQueue;
        private byte[] m_sendBuffer;
        private ConcurrentDictionary<byte, Action<SessionContext, IRPCData>> m_recieveHandlers;

        public ServiceClient(ITransferAdapter transferAdapter, IBufferSerializer bufferSerializer)
        {
            m_sendBuffer = new byte[BUFFER_LENGTH];
            m_sendQueue = new ConcurrentQueue<SendingData>();
            m_recieveQueue = new ConcurrentQueue<RecieveData>();
            m_transferAdapter = transferAdapter;
            m_transferAdapter.OnBufferRecieved += OnBufferRecieved;
            m_bufferSerializer = bufferSerializer;
            m_sendThread = new Thread(DoSend);
            m_recieveThread = new Thread(DoRecieve);
            m_sendThread.IsBackground = true;
            m_recieveThread.IsBackground = true;
            m_sendThread.Name = "SEND_THREAD";
            m_recieveThread.Name = "RECIEVE_THREAD";
            m_recieveHandlers = new ConcurrentDictionary<byte, Action<SessionContext, IRPCData>>();
        }

        public void Start()
        {
            m_sendThread.Start();
            m_recieveThread.Start();
            m_transferAdapter.Strat();
        }

        public void Dispose()
        {
            m_transferAdapter.OnBufferRecieved -= OnBufferRecieved;

            if (m_transferAdapter is IDisposable)
                ((IDisposable)m_transferAdapter).Dispose();
        }

        internal void SendData(long sessionID, IRPCData data)
        {
            m_sendQueue.Enqueue(new SendingData(new SessionContext(sessionID), data));
        }

        internal void SendSessionData(SessionContext sessionContext, IRPCData data)
        {
            m_sendQueue.Enqueue(new SendingData(sessionContext, data));
        }

        private void OnBufferRecieved(SessionContext sessionContext, byte[] buffer)
        {
            m_recieveQueue.Enqueue(new RecieveData(sessionContext, buffer));
        }

        private void DoSend()
        {
            while (true)
            {
                if (m_sendQueue.IsEmpty || !m_sendQueue.TryDequeue(out SendingData sendingData))
                {
                    Thread.Sleep(SLEEP_TIME_SPAN);
                    continue;
                }

                try
                {
                    int count = m_bufferSerializer.Serialize(sendingData.Data, m_sendBuffer);
                    m_transferAdapter.SendBuffer(sendingData.SessionContext, m_sendBuffer, count);
                }
                catch
                {
#if OUTPUT_LOG
                    LogManager.WriteLog(string.Format("serialize error, message_id: {0}", sendingData.Data.MessageID));
#endif
                }
            }
        }

        private void DoRecieve()
        {
            while (true)
            {
                if (m_recieveQueue.IsEmpty || !m_recieveQueue.TryDequeue(out RecieveData recieveData))
                {
                    Thread.Sleep(SLEEP_TIME_SPAN);
                    continue;
                }

                try
                {
                    IRPCData data = m_bufferSerializer.Deserialize(recieveData.Buffer);
                    OnRecieveData(recieveData.SessionContext, data);
                }
                catch
                {
#if OUTPUT_LOG
                    LogManager.WriteLog(string.Format("process error, message_id: {0}", BitConverter.ToInt32(recieveData.Buffer, 0)));
#endif
                }
            }
        }

        private void OnRecieveData(SessionContext sessionContext, IRPCData data)
        {
            if (!m_recieveHandlers.ContainsKey(data.MessageID))
                return;

            if (m_recieveHandlers.TryGetValue(data.MessageID, out Action<SessionContext, IRPCData> handler))
                handler(sessionContext, data);
        }

        public void RegisterProcessor(ProcessorBase processor)
        {
            Type[] baseTypes = processor.GetType().GetBaseTypes().ToArray();

            for (int i = 0; i < baseTypes.Length; i++)
            {
                if (baseTypes[i].Name == typeof(ResponseProcessorBase<>).Name)
                {
                    Type dataType = baseTypes[i].GenericTypeArguments[0];
                    ParameterExpression sessionContext = Expression.Parameter(typeof(SessionContext), "sessionContext");
                    ParameterExpression data = Expression.Parameter(typeof(IRPCData), "data");
                    UnaryExpression instance = Expression.Convert(Expression.Constant(processor), baseTypes[i]);
                    Expression body = Expression.Call(instance, baseTypes[i].GetMethod("ProcessData", BindingFlags.NonPublic | BindingFlags.Instance), sessionContext, Expression.Convert(data, dataType));
                    Action<SessionContext, IRPCData> serviceContractHandler = Expression.Lambda<Action<SessionContext, IRPCData>>(body, sessionContext, data).Compile();
                    m_recieveHandlers.TryAdd(((IRPCData)Activator.CreateInstance(dataType)).MessageID, serviceContractHandler);
                    return;
                }
            }
        }

        public void UnRegisterProcessor(object processor)
        {
            Type[] baseTypes = processor.GetType().GetBaseTypes().ToArray();

            for (int i = 0; i < baseTypes.Length; i++)
            {
                if (baseTypes[i].Name == typeof(ResponseProcessorBase<>).Name)
                {
                    Type dataType = baseTypes[i].GenericTypeArguments[0];
                    byte messageID = ((IRPCData)Activator.CreateInstance(dataType)).MessageID;

                    if (m_recieveHandlers.ContainsKey(messageID))
                        m_recieveHandlers.TryRemove(messageID, out Action<SessionContext, IRPCData> action);
                    return;
                }
            }
        }
    }
}