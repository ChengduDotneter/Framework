using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Common.ServiceCommon
{
    public interface IMessageReciever
    {
        void RecieveMessage(string identify, object parameter);
    }

    public interface IMessageReciever<T> : IMessageReciever
    {
        void RecieveMessage(string identify, T parameter);
    }

    public interface IMessageHandler
    {
        void AddReciever<T>(string identify, IMessageReciever<T> messageReciever);

        Task SendMessage<T>(string identify, T parameter);
    }

    /// <summary>
    /// WebSocket注册接口
    /// </summary>
    public interface IWebSocketService
    {
        /// <summary>
        /// 添加客户端的数据
        /// </summary>
        /// <param name="identify"></param>
        /// <param name="parameter"></param>
        void AddResponse(string identify, string parameter);

        /// <summary>
        /// 发送
        /// </summary>
        ConcurrentBag<Tuple<string, string>> SendMessages { get; }
    }

    /// <summary>
    /// WebSocket注册类
    /// </summary>
    public class WebSocketService : IMessageHandler, IWebSocketService
    {
        private ConcurrentDictionary<string, Tuple<Type, IMessageReciever>> m_messageRecievers;
        public ConcurrentBag<Tuple<string, string>> SendMessages { get; }

        public void AddResponse(string identify, string parameter)
        {
            if (!m_messageRecievers.ContainsKey(identify))
                return;

            (Type type, IMessageReciever messageReciever) = m_messageRecievers[identify];
            object message = JsonConvert.DeserializeObject(parameter, type);
            typeof(IMessageReciever<>).MakeGenericType(type).GetMethod(nameof(IMessageReciever.RecieveMessage)).Invoke(messageReciever, new object[] { message });
        }

        public void AddReciever<T>(string identify, IMessageReciever<T> messageReciever)
        {
            m_messageRecievers.AddOrUpdate(
                identify, 
                Tuple.Create(typeof(T), 
                (IMessageReciever)messageReciever), 
                (source, orign) => Tuple.Create(typeof(T), (IMessageReciever)messageReciever));
        }

        public Task SendMessage<T>(string identify, T parameter)
        {
            SendMessages.Add(Tuple.Create(identify, JsonConvert.SerializeObject(parameter)));
            return Task.CompletedTask;
        }

        public WebSocketService()
        {
            m_messageRecievers = new ConcurrentDictionary<string, Tuple<Type, IMessageReciever>>();
            SendMessages = new ConcurrentBag<Tuple<string, string>>();
        }
    }
}
