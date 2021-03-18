using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Common.ServiceCommon
{
    public class MessageProcessorRouteAttribute : Attribute
    {
        public string Template { get; set; }

        public MessageProcessorRouteAttribute(string template)
        {
            Template = template;
        }
    }

    public abstract class MessageProcessor
    {
        internal BlockingCollection<string> SendDatas { get; }
        protected string Identity { get; }

        internal abstract Task RecieveMessage(object parameter);

        protected MessageProcessor(string identity)
        {
            Identity = identity;
            SendDatas = new BlockingCollection<string>();
        }
    }

    public abstract class MessageProcessor<T> : MessageProcessor
    {
        internal override Task RecieveMessage(object parameter)
        {
            return RecieveMessage((T)parameter);
        }

        public abstract Task RecieveMessage(T parameter);

        protected void SendMessage<TMessage>(TMessage message)
        {
            SendDatas.Add(JsonConvert.SerializeObject(message));
        }

        protected MessageProcessor(string identity) : base(identity) { }
    }

    /// <summary>
    /// WebSocket中间件
    /// </summary>
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate m_requestDelegate;
        private readonly IServiceProvider m_serviceProvider;
        private static readonly IDictionary<string, Type> m_processorTypes;

        static WebSocketMiddleware()
        {
            m_processorTypes = new Dictionary<string, Type>();

            foreach (Type type in Assembly.GetEntryAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(MessageProcessor)) && !type.IsAbstract)
                {
                    MessageProcessorRouteAttribute messageProcessorRouteAttribute = type.GetCustomAttribute<MessageProcessorRouteAttribute>();

                    if (messageProcessorRouteAttribute != null)
                    {
                        m_processorTypes.Add($"/{messageProcessorRouteAttribute.Template}", type);
                    }
                    else
                    {
                        throw new DealException("消息处理器必须标明MessageProcessorRoute特性。");
                    }
                }
            }
        }

        /// <summary>
        /// WebSocket中间件构造函数
        /// </summary>
        /// <param name="requestDelegate"></param>
        /// <param name="webSocketService"></param>
        public WebSocketMiddleware(RequestDelegate requestDelegate, IServiceProvider serviceProvider)
        {
            m_requestDelegate = requestDelegate;
            m_serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await m_requestDelegate.Invoke(context);
                return;
            }

            string identify = context.GetSecWebSocketProtocol();

            if (string.IsNullOrWhiteSpace(identify))
            {
                await m_requestDelegate.Invoke(context);
                return;
            }

            using (WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync())
            {
                if (!m_processorTypes.ContainsKey(context.Request.Path))
                {
                    await m_requestDelegate.Invoke(context);
                    return;
                }

                MessageProcessor messageProcessor = (MessageProcessor)m_serviceProvider.CreateInstanceFromServiceProvider(m_processorTypes[context.Request.Path], new object[] { identify });

                Task sendTask = Task.Factory.StartNew(async () =>
                {
                    while (true)
                    {
                        string data = messageProcessor.SendDatas.Take();

                        if (webSocket.State != WebSocketState.Open)
                            break;

                        await SendStringAsync(webSocket, data);
                    }
                });

                while (true)
                {
                    if (webSocket.State != WebSocketState.Open)
                        break;

                    (bool success, string data) = await ReceiveStringAsync(webSocket);

                    if (success)
                        await messageProcessor.RecieveMessage(data);

                    await Task.Delay(1);
                }

                await sendTask;
            }
        }

        private static Task SendStringAsync(WebSocket socket, string data)
        {
            var buffer = Encoding.UTF8.GetBytes(data);

            var segment = new ArraySegment<byte>(buffer);
            return socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private static async Task<Tuple<bool, string>> ReceiveStringAsync(WebSocket socket)
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[8192]);
            using (MemoryStream memoryStream = new MemoryStream())
            {
                WebSocketReceiveResult result;

                do
                {
                    try
                    {
                        result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    }
                    catch
                    {
                        return Tuple.Create(false, string.Empty);
                    }

                    memoryStream.Write(buffer.Array, buffer.Offset, result.Count);
                } while (!result.EndOfMessage);

                memoryStream.Seek(0, SeekOrigin.Begin);

                if (result.MessageType != WebSocketMessageType.Text)
                {
                    return Tuple.Create(false, string.Empty);
                }

                using (var reader = new StreamReader(memoryStream, Encoding.UTF8))
                {
                    return Tuple.Create(true, await reader.ReadToEndAsync());
                }
            }
        }
    }
}