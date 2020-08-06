using Common.MessageQueueClient;
using System;

namespace Common.Log
{
    public class KafkaLogHelper : ILogHelper
    {
        private class KafkaInstance<T> where T : class, IMQData, new()
        {
            private static readonly IMQProducer<T> m_mQProducer;

            static KafkaInstance()
            {
                m_mQProducer = MessageQueueFactory.GetKafkaProducer<T>();

                AppDomain.CurrentDomain.ProcessExit += (sender, e) => { m_mQProducer.Dispose(); };
            }

            public static IMQProducer<T> GetMQProducer()
            {
                return m_mQProducer;
            }
        }

        private IMQProducer<T> GetKafkaInstance<T>() where T : class, IMQData, new()
        {
            return KafkaInstance<T>.GetMQProducer();
        }

        public void Error(string controllerName, string methed, int statusCode, string errorMessage, string path, string parameters)
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
                        Path = path,
                        StatusCode = statusCode
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

        public void Info(string controllerName, string methed, string path, string parameters)
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
