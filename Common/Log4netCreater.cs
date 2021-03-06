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
using Nito.AsyncEx;

namespace Common
{
    /// <summary>
    /// Log4net日志实例创建
    /// </summary>
    public class Log4netCreater
    {
        private readonly static IDictionary<string, ILoggerRepository> m_loggerRepositorys;
        private readonly static IDictionary<string, ILog> m_logs;
        private readonly static AsyncLock m_loggerRepositorMutex;
        private readonly static AsyncLock m_logMutex;
        private readonly static string m_assemblyName;

        static Log4netCreater()
        {
            m_assemblyName = Assembly.GetEntryAssembly().GetName().Name;
            m_loggerRepositorys = new Dictionary<string, ILoggerRepository>();
            m_logs = new Dictionary<string, ILog>();
            m_loggerRepositorMutex = new AsyncLock();
            m_logMutex = new AsyncLock();
        }

        /// <summary>
        /// 创建日志
        /// </summary>
        /// <param name="repositoryName"></param>
        /// <param name="names">参数组</param>
        /// <returns></returns>
        public static async Task<ILog> CreateLog(string repositoryName, params string[] names)
        {
            ILoggerRepository loggerRepository;

            string repositoryKey = $"{repositoryName}-{string.Join("-", names)}";

            if (!m_loggerRepositorys.ContainsKey(repositoryKey)) //不存在时去创建
            {
                using (await m_loggerRepositorMutex.LockAsync()) //加锁
                {
                    if (!m_loggerRepositorys.ContainsKey(repositoryKey)) //再次验证
                    {
                        loggerRepository = LogManager.CreateRepository(repositoryKey); //创建日志操作
                        m_loggerRepositorys.Add(repositoryKey, loggerRepository); //添加日志操作对象
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

            return await DoCreateLog(loggerRepository, names.Length > 0 ? names : new string[] { repositoryName });
        }

        /// <summary>
        /// 创建日志
        /// </summary>
        /// <param name="loggerRepository">日志操作对象</param>
        /// <param name="names">参数</param>
        /// <returns></returns>
        private static async Task<ILog> DoCreateLog(ILoggerRepository loggerRepository, params string[] names)
        {
            string logKey = loggerRepository.Name + names[0];

            if (!m_logs.ContainsKey(logKey))
            {
                using (await m_logMutex.LockAsync())
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
                        fileAppender.LockingModel = new FileAppender.MinimalLock();
                        fileAppender.Name = $"{m_assemblyName}_{loggerRepository.Name}_{names[0]}_FileAppender";
                        fileAppender.File = logDir;
                        fileAppender.AppendToFile = true;
                        fileAppender.RollingStyle = RollingFileAppender.RollingMode.Composite;
                        fileAppender.MaximumFileSize = "2MB";
                        fileAppender.DatePattern = "_yyyy-MM-dd'.log'";
                        fileAppender.MaxSizeRollBackups = -1;
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