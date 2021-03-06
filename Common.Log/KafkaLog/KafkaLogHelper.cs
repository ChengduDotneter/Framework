using System;
using System.Threading.Tasks;
using Common.Log.LogModel;
using Common.MessageQueueClient;

namespace Common.Log.KafkaLog
{
    /// <summary>
    /// Kafka日志操作类
    /// </summary>
    public class KafkaLogHelper : ILogHelper
    {
        /// <summary>
        /// Kafka日志操作实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class KafkaInstance<T> where T : class, IMQData, new()
        {
            private static readonly IMQProducer<T> m_mQProducer; //mq生产者

            static KafkaInstance()
            {
                m_mQProducer = MessageQueueFactory.GetKafkaProducer<T>(nameof(T)); //通过工厂获取kafka生产者
                AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                {
                    m_mQProducer.Dispose();
                }; //父进程退出时注销
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

        /// <summary>
        /// 接口报错日志写入
        /// </summary>
        /// <param name="path">接口路径</param>
        /// <param name="methed">请求方法</param>
        /// <param name="parameters">请求参数</param>
        /// <param name="stackTrace"></param>
        /// <param name="controllerName">接口组名称</param>
        /// <param name="errorMessage">接口报错信息</param>
        /// <param name="statusCode">接口状态编码</param>
        public Task Error(string controllerName, string methed, int statusCode, string errorMessage, string path, string parameters, string stackTrace = "")
        {
            return GetKafkaInstance<ErrorLog>().ProduceAsync(new ErrorLog
            {
                Node = Convert.ToInt32(ConfigManager.Configuration["Node"]),
                NodeType = Convert.ToInt32(ConfigManager.Configuration["NodeType"]),
                StackTrace = stackTrace,
                ControllerName = controllerName,
                ErrorMessage = errorMessage,
                Methed = methed,
                Parameters = parameters,
                Path = path,
                StatusCode = statusCode
            });
        }

        /// <summary>
        /// 自定义报错日志写入
        /// </summary>
        /// <param name="customCode">自定义编码</param>
        /// <param name="message">需要写入的日志信息</param>
        public Task Error(string customCode, string message)
        {
            return GetKafkaInstance<CustomErrorLog>().ProduceAsync(new CustomErrorLog
            {
                CustomCode = customCode,
                Message = message,
                Node = Convert.ToInt32(ConfigManager.Configuration["Node"]),
                NodeType = Convert.ToInt32(ConfigManager.Configuration["NodeType"]),
            });
        }

        /// <summary>
        /// 自定义日志写入
        /// </summary>
        /// <param name="customCode">自定义编码</param>
        /// <param name="message">需要写入的日志信息</param>
        public Task Info(string customCode, string message)
        {
            return GetKafkaInstance<CustomLog>().ProduceAsync(new CustomLog
            {
                CustomCode = customCode,
                Message = message,
                Node = Convert.ToInt32(ConfigManager.Configuration["Node"]),
                NodeType = Convert.ToInt32(ConfigManager.Configuration["NodeType"]),
            });
        }

        /// <summary>
        /// 接口日志写入
        /// </summary>
        /// <param name="path">接口路径</param>
        /// <param name="methed">请求方法</param>
        /// <param name="parameters">请求参数</param>
        /// <param name="controllerName">接口组名称</param>
        public Task Info(string controllerName, string methed, string path, string parameters)
        {
            return GetKafkaInstance<InfoLog>().ProduceAsync(new InfoLog
            {
                Node = Convert.ToInt32(ConfigManager.Configuration["Node"]),
                NodeType = Convert.ToInt32(ConfigManager.Configuration["NodeType"]),
                ControllerName = controllerName,
                Methed = methed,
                Parameters = parameters,
                Path = path
            });
        }

        /// <summary>
        /// TCCNode日志写入
        /// </summary>
        /// <param name="transcationID">TCC事务ID</param>
        /// <param name="isError">是否报错</param>
        /// <param name="message">TCC节点接口调用日志</param>
        public Task TCCNode(long transcationID, bool? isError, string message)
        {
            return GetKafkaInstance<TCCNodeLog>().ProduceAsync(new TCCNodeLog
            {
                Node = Convert.ToInt32(ConfigManager.Configuration["Node"]),
                NodeType = Convert.ToInt32(ConfigManager.Configuration["NodeType"]),
                Message = message,
                IsError = isError,
                TranscationID = transcationID
            });
        }

        /// <summary>
        /// TCCServer日志写入
        /// </summary>
        /// <param name="transcationID">TCC事务ID</param>
        /// <param name="message">TCC服务端相关日志</param>
        public Task TCCServer(long transcationID, string message)
        {
            return GetKafkaInstance<TCCServerLog>().ProduceAsync(new TCCServerLog
            {
                Node = Convert.ToInt32(ConfigManager.Configuration["Node"]),
                NodeType = Convert.ToInt32(ConfigManager.Configuration["NodeType"]),
                Message = message,
                TranscationID = transcationID
            });
        }
    }
}