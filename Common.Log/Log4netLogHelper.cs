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
        private readonly static string m_assemblyName;

        static Log4netLogHelper()
        {
            m_assemblyName = Assembly.GetEntryAssembly().GetName().Name;
            m_loggerRepositorys = new Dictionary<string, ILoggerRepository>();
        }

        public void Error(string customCode, string message)
        {
            CreateLog("custom", "error", customCode).Error(message);
        }

        public void Error(string path, string methed, string parameters, string controllerName, string errorMessage)
        {
            CreateLog("controller", controllerName, methed).Error($"Erro {Environment.NewLine} path: {path}{Environment.NewLine} parameters: {Environment.NewLine}{parameters} errorMessage: {Environment.NewLine}{errorMessage} stackTrace:{Environment.NewLine}{Environment.StackTrace}");
        }

        public void Info(string customCode, string message)
        {
            CreateLog("custom", "info", customCode).Info(message);
        }

        public void Info(string path, string methed, string parameters, string controllerName)
        {
            CreateLog("controller", controllerName, methed).Info($"path: {path}{Environment.NewLine}{Environment.NewLine} parameters: {Environment.NewLine}{parameters}");
        }

        public void SqlError(string sql, string message, string parameters = "")
        {
            CreateLog("sql", "error").Error($"message: {message}{Environment.NewLine} stack_trace: {Environment.StackTrace}{Environment.NewLine} sql: {sql}{Environment.NewLine} parameters: {Environment.NewLine}{parameters}");
        }

        public void TCCNode(long transcationID, bool isError, string message)
        {
            ILog log = CreateLog("TCC", "TCC", "TCCDetails");
            if (isError)
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

            return LogManager.GetLogger(loggerRepository.Name, names[0]);
        }
    }
}
