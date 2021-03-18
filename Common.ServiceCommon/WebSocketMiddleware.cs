using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
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
        private ConcurrentDictionary<string, WebSocket> m_webSockets;
        private readonly RequestDelegate m_requestDelegate;
        private readonly IWebSocketService m_webSocketService;

        private Thread m_sendThread;
        private Thread m_recieveThread;

        /// <summary>
        /// WebSocket中间件构造函数
        /// </summary>
        /// <param name="requestDelegate"></param>
        /// <param name="webSocketService"></param>
        public WebSocketMiddleware(RequestDelegate requestDelegate, IWebSocketService webSocketService)
        {
            m_requestDelegate = requestDelegate;
            m_webSocketService = webSocketService;
            m_webSockets = new ConcurrentDictionary<string, WebSocket>();

            m_sendThread = new Thread(DoSend);
            m_sendThread.Name = "WEBSOCKET_SEND_THREAD";
            m_sendThread.IsBackground = true;
            m_sendThread.Start();

            m_recieveThread = new Thread(DoRecieve);
            m_recieveThread.Name = "WEBSOCKET_RECIEVE_THREAD";
            m_recieveThread.IsBackground = true;
            m_recieveThread.Start();
        }

        private void DoRecieve()
        {
            while (true)
            {
                string[] keys = null;

                lock (m_webSockets)
                    keys = m_webSockets.Keys.ToArray();

                foreach (string key in keys)
                {
                    WebSocket webSocket = m_webSockets[key];

                    if (webSocket.State != WebSocketState.Open)
                        m_webSockets.TryRemove(key, out _);
                    else
                    {
                        if(ReceiveString(webSocket,out string message))
                            m_webSocketService.AddResponse(key, message);
                    }
                }

                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 根据identifName确定发送的WebSocket
        /// </summary>
        private void DoSend()
        {
            while (true)
            {
                if (!m_webSocketService.SendMessages.TryTake(out Tuple<string, string> message))
                {
                    Thread.Sleep(1);
                    continue;
                }

                (string identity, string data) = message;

                if (!m_webSockets.ContainsKey(identity))
                {
                    Thread.Sleep(1);
                    continue;
                }

                WebSocket webSocket = m_webSockets[identity];
                SendString(webSocket, data);
            }
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

            //if (string.IsNullOrWhiteSpace(identify))
            //{
            //    await m_requestDelegate.Invoke(context);
            //    return;
            //}

            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
            m_webSockets.AddOrUpdate(identify, webSocket, (source, orgin) => webSocket);

            //while (true)
            //{

            //}
        }

        private static void SendString(WebSocket socket, string data)
        {
            var buffer = Encoding.UTF8.GetBytes(data);
            var segment = new ArraySegment<byte>(buffer);
            socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None).Wait();
        }

        private static bool ReceiveString(WebSocket socket, out string message)
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[8192]);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                WebSocketReceiveResult result;

                do
                {
                    try
                    {
                        result = socket.ReceiveAsync(buffer, CancellationToken.None).Result;
                    }
                    catch
                    {
                        message = string.Empty;
                        return false;
                    }

                    memoryStream.Write(buffer.Array, buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                memoryStream.Seek(0, SeekOrigin.Begin);

                if (result.MessageType != WebSocketMessageType.Text)
                {
                    message = string.Empty;
                    return false;
                }

                using (var reader = new StreamReader(memoryStream, Encoding.UTF8))
                {
                    message = reader.ReadToEnd();
                    return true;
                }
            }
        }
    }
}
