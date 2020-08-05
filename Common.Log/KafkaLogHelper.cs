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

        public void Error(string customCode, string message)
        {
            using (IMQProducer<CustomLog> producer = MessageQueueFactory.GetKafkaProducer<CustomLog>())
            {
                producer.ProduceAsync(new MQContext(nameof(CustomLog), null),
                    new CustomLog
                    {
                        CustomCode = customCode,
                        IsError = true,
                        Message = message,
                        Node = Convert.ToInt32(ConfigManager.Configuration["Node"]),
                        NodeType = Convert.ToInt32(ConfigManager.Configuration["NodeType"]),
                        StackTrace = Environment.StackTrace
                    });
            }
        }

        public void Error(string path, string methed, string parameters, string controllerName, string errorMessage)
        {
            using (IMQProducer<ErrorLog> producer = MessageQueueFactory.GetKafkaProducer<ErrorLog>())
            {
                producer.ProduceAsync(new MQContext(nameof(ErrorLog), null),
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
        }

        public void Info(string customCode, string message)
        {
            using (IMQProducer<CustomLog> producer = MessageQueueFactory.GetKafkaProducer<CustomLog>())
            {
                producer.ProduceAsync(new MQContext(nameof(CustomLog), null),
                    new CustomLog
                    {
                        CustomCode = customCode,
                        Message = message,
                        Node = Convert.ToInt32(ConfigManager.Configuration["Node"]),
                        NodeType = Convert.ToInt32(ConfigManager.Configuration["NodeType"]),
                        StackTrace = Environment.StackTrace
                    });
            }
        }

        public void Info(string path, string methed, string parameters, string controllerName)
        {
            using (IMQProducer<InfoLog> producer = MessageQueueFactory.GetKafkaProducer<InfoLog>())
            {
                producer.ProduceAsync(new MQContext(nameof(InfoLog), null),
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
        }

        public void SqlError(string sql, string message, string parameters = "")
        {
            using (IMQProducer<SqlErrorLog> producer = MessageQueueFactory.GetKafkaProducer<SqlErrorLog>())
            {
                producer.ProduceAsync(new MQContext(nameof(SqlErrorLog), null),
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
        }

        public void TCCNode(long transcationID, bool? isError, string message)
        {
            using (IMQProducer<TCCNodeLog> producer = MessageQueueFactory.GetKafkaProducer<TCCNodeLog>())
            {
                producer.ProduceAsync(new MQContext(nameof(TCCNodeLog), null),
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
        }

        public void TCCServer(long transcationID, string message)
        {
            using (IMQProducer<TCCServerLog> producer = MessageQueueFactory.GetKafkaProducer<TCCServerLog>())
            {
                producer.ProduceAsync(new MQContext(nameof(TCCServerLog), null),
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
}
