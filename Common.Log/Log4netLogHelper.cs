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

namespace Common.Log
{
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

        public void Error(string controllerName, string methed, int statusCode, string errorMessage, string path, string parameters)
        {
            CreateLog("Controller", controllerName, methed).Error($"Error {Environment.NewLine} path: {path}{Environment.NewLine} parameters: {Environment.NewLine}{parameters} http_status_code {statusCode}{Environment.NewLine} error_message: {Environment.NewLine}{errorMessage} stack_trace:{Environment.NewLine}{Environment.StackTrace}");
        }

        public void Info(string customCode, string message)
        {
            CreateLog("Custom", "info", customCode).Info(message);
        }

        public void Info(string controllerName, string methed, string path, string parameters)
        {
            CreateLog("Controller", controllerName, methed).Info($"path: {path}{Environment.NewLine}{Environment.NewLine} parameters: {Environment.NewLine}{parameters}");
        }

        public void SqlError(string sql, string message, string parameters = "")
        {
            CreateLog("Sql", "error").Error($"message: {message}{Environment.NewLine} stack_trace: {Environment.StackTrace}{Environment.NewLine} sql: {sql}{Environment.NewLine} parameters: {Environment.NewLine}{parameters}");
        }

        public void TCCNode(long transcationID, bool? isError, string message)
        {
            ILog log = CreateLog("TCC", "TCC", "TCCDetails");
            if (isError ?? false)
                log.Error($"transcationID: {transcationID}{Environment.NewLine} is_error:{isError} {Environment.NewLine} message:{message}{Environment.NewLine} stack_trace: {Environment.StackTrace}");
            else log.Info($"transcationID: {transcationID}{Environment.NewLine} is_error:{isError} {Environment.NewLine} message:{message}{Environment.NewLine}");
        }

        public void TCCServer(long transcationID, string message)
        {
            CreateLog("TCC", "TCC", "TCCTransactions").Info($"transcationID: {transcationID}{Environment.NewLine} message:{message}");
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
