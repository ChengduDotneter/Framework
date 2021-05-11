using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.ServiceCommon
{
    /// <summary>
    /// WebSocket中间件
    /// </summary>
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate m_requestDelegate;
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
        public WebSocketMiddleware(RequestDelegate requestDelegate)
        {
            m_requestDelegate = requestDelegate;
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

            string identity = context.Request.Query["identity"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(identity))
            {
                await m_requestDelegate.Invoke(context);
                return;
            }

            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                using (WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync())
                {
                    if (!m_processorTypes.ContainsKey(context.Request.Path))
                    {
                        await m_requestDelegate.Invoke(context);
                        return;
                    }

                    MessageProcessor messageProcessor =
                        (MessageProcessor)context.RequestServices.CreateInstanceFromServiceProvider(m_processorTypes[context.Request.Path], new object[] { identity });

                    Task sendTask = Task.Factory.StartNew(async () =>
                    {
                        while (webSocket.State == WebSocketState.Open)
                        {
                            string data = string.Empty;

                            try
                            {
                                // ReSharper disable once AccessToDisposedClosure
                                data = messageProcessor.SendDatas.Take(cancellationTokenSource.Token);
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }

                            // ReSharper disable once AccessToDisposedClosure
                            await SendStringAsync(webSocket, data);
                        }
                    });

                    _ = Task.Factory.StartNew(async () =>
                    {
                        while (true)
                        {
                            if (webSocket.State != WebSocketState.Open)
                            {
                                cancellationTokenSource.Cancel(false);
                                break;
                            }

                            await Task.Delay(10);
                        }
                    });

                    //TODO:代码执行循环中必须加入Task.Delay进行CPU中断 使cancellationTokenSource生效
                    while (webSocket.State == WebSocketState.Open)
                    {
                        (bool success, string data) = await ReceiveStringAsync(webSocket);

                        if (success)
                            _ = messageProcessor.RecieveMessage(data, cancellationTokenSource.Token);

                        await Task.Delay(10);
                    }

                    await sendTask;
                }
            }
        }

        private static Task SendStringAsync(WebSocket socket, string data)
        {
            var segment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(data));
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

                if (result.CloseStatus.HasValue || result.MessageType != WebSocketMessageType.Text)
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