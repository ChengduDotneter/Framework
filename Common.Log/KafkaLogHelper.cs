using Common.MessageQueueClient;
using System;
using System.Collections.Generic;

namespace Common.Log
{
    public class KafkaLogHelper : ILogHelper
    {
        private static readonly IDictionary<string, object> m_producers;

        static KafkaLogHelper()
        {
            m_producers = new Dictionary<string, object>();
        }

        private IMQProducer<T> GetKafkaInstance<T>() where T : class, IMQData, new()
        {
            if (!m_producers.ContainsKey(nameof(T)))
                m_producers.Add(nameof(T), MessageQueueFactory.GetKafkaProducer<T>());

            return (IMQProducer<T>)m_producers[nameof(T)];
        }



        public void Error(string path, string methed, string parameters, string controllerName, string errorMessage)
        {
            GetKafkaInstance<ErrorLog>().ProduceAsync(new MQContext(nameof(ErrorLog), null),
                    new ErrorLog
                    {
                        Node = Convert.ToInt32(ConfigManager.Configuration["Node"]),
                        NodeType = Convert.ToInt32(ConfigManager.Configuration["NodeType"]),
                        StackTrace = Environment.StackTrace,
                        ControllerName = controllerName,
                        ErrorMessage = errorMessage,
                        Methed = methed,
                        Parameters = parameters,
                        Path = path
                    });
        }

        public void Info(string customCode, string message)
        {
            GetKafkaInstance<CustomLog>().ProduceAsync(new MQContext(nameof(CustomLog), null),
                    new CustomLog
                    {
                        CustomCode = customCode,
                        Message = message,
                        Node = Convert.ToInt32(ConfigManager.Configuration["Node"]),
                        NodeType = Convert.ToInt32(ConfigManager.Configuration["NodeType"]),
                        StackTrace = Environment.StackTrace
                    });
        }

        public void Info(string path, string methed, string parameters, string controllerName)
        {
            GetKafkaInstance<InfoLog>().ProduceAsync(new MQContext(nameof(InfoLog), null),
                    new InfoLog
                    {
                        Node = Convert.ToInt32(ConfigManager.Configuration["Node"]),
                        NodeType = Convert.ToInt32(ConfigManager.Configuration["NodeType"]),
                        StackTrace = Environment.StackTrace,
                        ControllerName = controllerName,
                        Methed = methed,
                        Parameters = parameters,
                        Path = path
                    });
        }

        public void SqlError(string sql, string message, string parameters = "")
        {
            GetKafkaInstance<SqlErrorLog>().ProduceAsync(new MQContext(nameof(SqlErrorLog), null),
                    new SqlErrorLog
                    {
                        Node = Convert.ToInt32(ConfigManager.Configuration["Node"]),
                        NodeType = Convert.ToInt32(ConfigManager.Configuration["NodeType"]),
                        StackTrace = Environment.StackTrace,
                        Sql = sql,
                        Message = message,
                        Parameters = parameters
                    });
        }

        public void TCCNode(long transcationID, bool? isError, string message)
        {
            GetKafkaInstance<TCCNodeLog>().ProduceAsync(new MQContext(nameof(TCCNodeLog), null),
                    new TCCNodeLog
                    {
                        Node = Convert.ToInt32(ConfigManager.Configuration["Node"]),
                        NodeType = Convert.ToInt32(ConfigManager.Configuration["NodeType"]),
                        StackTrace = Environment.StackTrace,
                        Message = message,
                        IsError = isError,
                        TranscationID = transcationID
                    });
        }

        public void TCCServer(long transcationID, string message)
        {
            GetKafkaInstance<TCCServerLog>().ProduceAsync(new MQContext(nameof(TCCServerLog), null),
                    new TCCServerLog
                    {
                        Node = Convert.ToInt32(ConfigManager.Configuration["Node"]),
                        NodeType = Convert.ToInt32(ConfigManager.Configuration["NodeType"]),
                        StackTrace = Environment.StackTrace,
                        Message = message,
                        TranscationID = transcationID
                    });
        }
    }
}
