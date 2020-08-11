using Common.MessageQueueClient;
using System;
using System.Threading.Tasks;

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

        public async Task Error(string controllerName, string methed, int statusCode, string errorMessage, string path, string parameters)
        {
            await GetKafkaInstance<ErrorLog>().ProduceAsync(new MQContext(nameof(ErrorLog), null),
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

        public async Task Info(string customCode, string message)
        {
            await GetKafkaInstance<CustomLog>().ProduceAsync(new MQContext(nameof(CustomLog), null),
                    new CustomLog
                    {
                        CustomCode = customCode,
                        Message = message,
                        Node = Convert.ToInt32(ConfigManager.Configuration["Node"]),
                        NodeType = Convert.ToInt32(ConfigManager.Configuration["NodeType"]),
                    });
        }

        public async Task Info(string controllerName, string methed, string path, string parameters)
        {
            await GetKafkaInstance<InfoLog>().ProduceAsync(new MQContext(nameof(InfoLog), null),
                    new InfoLog
                    {
                        Node = Convert.ToInt32(ConfigManager.Configuration["Node"]),
                        NodeType = Convert.ToInt32(ConfigManager.Configuration["NodeType"]),
                        ControllerName = controllerName,
                        Methed = methed,
                        Parameters = parameters,
                        Path = path
                    });
        }

        public async Task SqlError(string sql, string message, string parameters = "")
        {
            await GetKafkaInstance<SqlErrorLog>().ProduceAsync(new MQContext(nameof(SqlErrorLog), null),
                    new SqlErrorLog
                    {
                        Node = Convert.ToInt32(ConfigManager.Configuration["Node"]),
                        NodeType = Convert.ToInt32(ConfigManager.Configuration["NodeType"]),
                        Sql = sql,
                        Message = message,
                        Parameters = parameters,
                        StackTrace = Environment.StackTrace,
                    });
        }

        public async Task TCCNode(long transcationID, bool? isError, string message)
        {
            await GetKafkaInstance<TCCNodeLog>().ProduceAsync(new MQContext(nameof(TCCNodeLog), null),
                     new TCCNodeLog
                     {
                         Node = Convert.ToInt32(ConfigManager.Configuration["Node"]),
                         NodeType = Convert.ToInt32(ConfigManager.Configuration["NodeType"]),
                         Message = message,
                         IsError = isError,
                         TranscationID = transcationID
                     });
        }

        public async Task TCCServer(long transcationID, string message)
        {
            await GetKafkaInstance<TCCServerLog>().ProduceAsync(new MQContext(nameof(TCCServerLog), null),
                    new TCCServerLog
                    {
                        Node = Convert.ToInt32(ConfigManager.Configuration["Node"]),
                        NodeType = Convert.ToInt32(ConfigManager.Configuration["NodeType"]),
                        Message = message,
                        TranscationID = transcationID
                    });
        }
    }
}
