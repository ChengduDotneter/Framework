using Common.RPC.BufferSerializer;
using Common.RPC.TransferAdapter;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Common.RPC
{
    /// <summary>
    /// RPC服务核心模块
    /// </summary>
    public class ServiceClient : IDisposable
    {
        /// <summary>
        /// 接收的数据实体
        /// </summary>
        private class RecieveData
        {
            /// <summary>
            /// 通讯上下文
            /// </summary>
            public SessionContext SessionContext { get; }

            /// <summary>
            /// 字节流缓冲区
            /// </summary>
            public byte[] Buffer { get; }

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="sessionContext">通讯上下文</param>
            /// <param name="buffer">字节缓冲区</param>
            public RecieveData(SessionContext sessionContext, byte[] buffer)
            {
                SessionContext = sessionContext;
                Buffer = buffer;
            }
        }

        /// <summary>
        /// 发送的数据实体
        /// </summary>
        private class SendingData
        {
            /// <summary>
            /// 通讯上下文
            /// </summary>
            public SessionContext SessionContext { get; }

            /// <summary>
            /// RPC结构体数据
            /// </summary>
            public IRPCData Data { get; }

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="sessionContext">上下文</param>
            /// <param name="data">所需发送的数据</param>
            public SendingData(SessionContext sessionContext, IRPCData data)
            {
                SessionContext = sessionContext;
                Data = data;
            }
        }

        private const int BUFFER_LENGTH = 1024 * 1024 * 64;
        private ITransferAdapter m_transferAdapter;
        private IBufferSerializer m_bufferSerializer;
        private Thread m_recieveThread;
        private Thread m_sendThread;
        private BlockingCollection<SendingData> m_sendDatas;
        private BlockingCollection<RecieveData> m_recieveDatas;
        private byte[] m_sendBuffer;
        private ConcurrentDictionary<byte, Action<SessionContext, IRPCData>> m_recieveHandlers;

#if OUTPUT_LOG
        private static ILog m_log;
#endif

        static ServiceClient()
        {
#if OUTPUT_LOG
            m_log = LogHelper.CreateLog("RPC");
#endif
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="transferAdapter"></param>
        /// <param name="bufferSerializer"></param>
        public ServiceClient(ITransferAdapter transferAdapter, IBufferSerializer bufferSerializer)
        {
            m_sendBuffer = new byte[BUFFER_LENGTH];
            m_sendDatas = new BlockingCollection<SendingData>();
            m_recieveDatas = new BlockingCollection<RecieveData>();
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

        /// <summary>
        /// 开始
        /// </summary>
        public void Start()
        {
            m_sendThread.Start();
            m_recieveThread.Start();
            m_transferAdapter.Strat();
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            m_transferAdapter.OnBufferRecieved -= OnBufferRecieved;

            if (m_transferAdapter is IDisposable)
                ((IDisposable)m_transferAdapter).Dispose();
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="sessionID">通讯ID</param>
        /// <param name="data">所需发送的数据</param>
        internal void SendData(long sessionID, IRPCData data)
        {
            m_sendDatas.Add(new SendingData(new SessionContext(sessionID), data));
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="sessionContext">通讯上下文</param>
        /// <param name="data">所需发送的数据</param>
        internal void SendSessionData(SessionContext sessionContext, IRPCData data)
        {
            m_sendDatas.Add(new SendingData(sessionContext, data));
        }

        private void OnBufferRecieved(SessionContext sessionContext, byte[] buffer)
        {
            m_recieveDatas.Add(new RecieveData(sessionContext, buffer));
        }

        private void DoSend()
        {
            while (true)
            {
                SendingData sendingData = m_sendDatas.Take();

                try
                {
                    int count = m_bufferSerializer.Serialize(sendingData.Data, m_sendBuffer);
                    m_transferAdapter.SendBuffer(sendingData.SessionContext, m_sendBuffer, count);
                }
                catch (Exception ex)
                {
#if OUTPUT_LOG
                    m_log.Error($"serialize error, message_id: {sendingData.Data.MessageID}{Environment.NewLine}message: {Environment.NewLine}{ExceptionHelper.GetMessage(ex)}{Environment.NewLine}stack_trace: {Environment.NewLine}{ExceptionHelper.GetStackTrace(ex)}");
#endif
                }
            }
        }

        private void DoRecieve()
        {
            while (true)
            {
                RecieveData recieveData = m_recieveDatas.Take();

                try
                {
                    IRPCData data = m_bufferSerializer.Deserialize(recieveData.Buffer);
                    OnRecieveData(recieveData.SessionContext, data);
                }
                catch (Exception ex)
                {
#if OUTPUT_LOG
                    m_log.Error($"process error, message_id: {BitConverter.ToInt32(recieveData.Buffer, 0)}{Environment.NewLine}message: {Environment.NewLine}{ExceptionHelper.GetMessage(ex)}{Environment.NewLine}stack_trace: {Environment.NewLine}{ExceptionHelper.GetStackTrace(ex)}");
#endif
                }
            }
        }

        private void OnRecieveData(SessionContext sessionContext, IRPCData data)
        {
            if (!m_recieveHandlers.ContainsKey(data.MessageID))
                return;

            if (m_recieveHandlers.TryGetValue(data.MessageID, out Action<SessionContext, IRPCData> handler))
                Task.Factory.StartNew(() => { handler(sessionContext, data); });
        }

        /// <summary>
        /// 注册通讯端处理器
        /// </summary>
        /// <param name="processor"></param>
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

        /// <summary>
        /// 解除注册通讯端处理器
        /// </summary>
        /// <param name="processor"></param>
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