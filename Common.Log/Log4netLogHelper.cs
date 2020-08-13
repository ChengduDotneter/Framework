using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;
using log4net.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Common.Log
{
    /// <summary>
    /// Log4net日志操作类
    /// </summary>
    public class Log4netLogHelper : ILogHelper
    {
        private readonly static IDictionary<string, ILoggerRepository> m_loggerRepositorys;
        private readonly static IDictionary<string, ILog> m_logs;
        private readonly static string m_assemblyName;

        static Log4netLogHelper()
        {
            m_assemblyName = Assembly.GetEntryAssembly().GetName().Name;
            m_loggerRepositorys = new Dictionary<string, ILoggerRepository>();
            m_logs = new Dictionary<string, ILog>();
        }

        /// <summary>
        /// 接口报错日志写入
        /// </summary>
        /// <param name="path">接口路径</param>
        /// <param name="methed">请求方法</param>
        /// <param name="parameters">请求参数</param>
        /// <param name="controllerName">接口组名称</param>
        /// <param name="errorMessage">接口报错信息</param>
        /// <param name="statusCode">接口状态编码</param>
        public async Task Error(string controllerName, string methed, int statusCode, string errorMessage, string path, string parameters)
        {
            await Task.Factory.StartNew(() =>
                CreateLog("Controller", controllerName, methed).Error($" Error {Environment.NewLine} path: {path}{Environment.NewLine} parameters: {Environment.NewLine}{parameters} http_status_code {statusCode}{Environment.NewLine} error_message: {Environment.NewLine}{errorMessage} stack_trace:{Environment.NewLine}{Environment.StackTrace}")
            );
        }

        /// <summary>
        /// 自定义报错日志写入
        /// </summary>
        /// <param name="customCode">自定义编码</param>
        /// <param name="message">需要写入的日志信息</param>
        public async Task Error(string customCode, string message)
        {
            await Task.Factory.StartNew(() =>
               CreateLog("Custom", "Error", customCode).Info(message)
           );
        }

        /// <summary>
        /// 自定义日志写入
        /// </summary>
        /// <param name="customCode">自定义编码</param>
        /// <param name="message">需要写入的日志信息</param>
        public async Task Info(string customCode, string message)
        {
            await Task.Factory.StartNew(() =>
                CreateLog("Custom", "info", customCode).Info(message)
            );
        }

        /// <summary>
        /// 接口日志写入
        /// </summary>
        /// <param name="path">接口路径</param>
        /// <param name="methed">请求方法</param>
        /// <param name="parameters">请求参数</param>
        /// <param name="controllerName">接口组名称</param>
        public async Task Info(string controllerName, string methed, string path, string parameters)
        {
            await Task.Factory.StartNew(() =>
                CreateLog("Controller", controllerName, methed).Info($" path: {path}{Environment.NewLine}{Environment.NewLine} parameters: {Environment.NewLine}{parameters}")
            );
        }

        /// <summary>
        /// Sql错误日志写入
        /// </summary>
        /// <param name="sql">Sql语句</param>
        /// <param name="parameters">Sql请求参数</param>
        /// <param name="message">Sql执行结果</param>
        public async Task SqlError(string sql, string message, string parameters = "")
        {
            await Task.Factory.StartNew(() =>
                CreateLog("Sql", "error").Error($" message: {message}{Environment.NewLine} sql: {sql}{Environment.NewLine} parameters: {Environment.NewLine}{parameters} stack_trace:{Environment.NewLine}{Environment.StackTrace}")
            );
        }

        /// <summary>
        /// TCCNode日志写入
        /// </summary>
        /// <param name="transcationID">TCC事务ID</param>
        /// <param name="isError">是否报错</param>
        /// <param name="message">TCC节点接口调用日志</param>
        public async Task TCCNode(long transcationID, bool? isError, string message)
        {
            await Task.Factory.StartNew(() =>
            {
                ILog log = CreateLog("TCC", "TCC", "TCCDetails");
                if (isError ?? false)
                    log.Error($" transcationID: {transcationID}{Environment.NewLine} is_error:{isError} {Environment.NewLine} message:{message}{Environment.NewLine} stack_trace: {Environment.StackTrace}");
                else log.Info($" transcationID: {transcationID}{Environment.NewLine} is_error:{isError} {Environment.NewLine} message:{message}{Environment.NewLine}");
            });
        }


        /// <summary>
        /// TCCServer日志写入
        /// </summary>
        /// <param name="transcationID">TCC事务ID</param>
        /// <param name="message">TCC服务端相关日志</param>
        public async Task TCCServer(long transcationID, string message)
        {
            await Task.Factory.StartNew(() =>
                CreateLog("TCC", "TCC", "TCCTransactions").Info($" transcationID: {transcationID}{Environment.NewLine} message:{message}")
            );
        }

        /// <summary>
        /// 创建日志
        /// </summary>
        /// <param name="repositoryName"></param>
        /// <param name="names"></param>
        /// <returns></returns>
        private static ILog CreateLog(string repositoryName, params string[] names)
        {
            ILoggerRepository loggerRepository;

            string repositoryKey = $"{repositoryName}-{string.Join("-", names)}";

            if (!m_loggerRepositorys.ContainsKey(repositoryKey))
            {
                lock (m_loggerRepositorys)
                {
                    if (!m_loggerRepositorys.ContainsKey(repositoryKey))
                    {
                        loggerRepository = LogManager.CreateRepository(repositoryKey);
                        m_loggerRepositorys.Add(repositoryKey, loggerRepository);
                    }
                    else
                    {
                        loggerRepository = m_loggerRepositorys[repositoryKey];
                    }
                }
            }
            else
            {
                loggerRepository = m_loggerRepositorys[repositoryKey];
            }

            return DoCreateLog(loggerRepository, names.Length > 0 ? names : new string[] { repositoryName });
        }

        private static ILog DoCreateLog(ILoggerRepository loggerRepository, params string[] names)
        {
            string logKey = loggerRepository.Name + names[0];

            if (!m_logs.ContainsKey(logKey))
            {
                lock (m_logs)
                {
                    if (!m_logs.ContainsKey(logKey))
                    {
                        LevelRangeFilter infoFilter = new LevelRangeFilter();

                        infoFilter.LevelMax = Level.Error;
                        infoFilter.LevelMin = Level.Info;
                        infoFilter.ActivateOptions();

                        string layoutFormat = "@Log Begin%newline%date%newlineThread ID：[%thread]%newline%message%newlineLog End@%newline%newline%newline%newline";
                        string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", m_assemblyName);

                        for (int i = 0; i < names.Length; i++)
                            logDir = Path.Combine(logDir, names[i]);

                        RollingFileAppender fileAppender = new RollingFileAppender();
                        fileAppender.Name = $"{m_assemblyName}_{loggerRepository.Name}_{names[0]}_FileAppender";
                        fileAppender.File = logDir;
                        fileAppender.AppendToFile = true;
                        fileAppender.RollingStyle = RollingFileAppender.RollingMode.Date;
                        fileAppender.DatePattern = "_yyyy-MM-dd'.log'";
                        fileAppender.StaticLogFileName = false;
                        fileAppender.Layout = new PatternLayout(layoutFormat);
                        fileAppender.AddFilter(infoFilter);
                        fileAppender.ActivateOptions();

                        BasicConfigurator.Configure(loggerRepository, fileAppender);

                        m_logs.Add(logKey, LogManager.GetLogger(loggerRepository.Name, names[0]));
                    }
                }
            }

            return m_logs[logKey];
        }

    }
}
