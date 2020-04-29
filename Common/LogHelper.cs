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

namespace Common
{
    public static class LogHelper
    {
        private static IDictionary<string, ILoggerRepository> m_loggerRepositorys;
        private static string m_assemblyName;

        static LogHelper()
        {
            m_assemblyName = Assembly.GetEntryAssembly().GetName().Name;
            m_loggerRepositorys = new Dictionary<string, ILoggerRepository>();
        }

        public static ILog CreateLog(string repositoryName, params string[] names)
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