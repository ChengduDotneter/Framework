using Common.Log.LogModel;
using System;

namespace Common.Log
{
    /// <summary>
    /// 日志帮助对象工厂
    /// </summary>
    public class LogHelperFactory
    {
        private static ILogHelper m_log4netLogHelper;
        private static ILogHelper m_kafkaLogHelper;
        private static LogHelperTypeEnum? m_defaultLogHelperType;

        static LogHelperFactory()
        {
            m_log4netLogHelper = new Log4netLogHelper();
            m_kafkaLogHelper = new KafkaLogHelper();
        }

        /// <summary>
        /// 获取Log4net 日志帮助对象
        /// </summary>
        /// <returns></returns>
        public static ILogHelper GetLog4netLogHelper()
        {
            return m_log4netLogHelper;
        }

        /// <summary>
        /// 获取Kafka 日志帮助对象
        /// </summary>
        /// <returns></returns>
        public static ILogHelper GetKafkaLogHelper()
        {
            return m_kafkaLogHelper;
        }

        /// <summary>
        /// 获取默认日志帮助实例
        /// </summary>
        /// <returns></returns>
        public static ILogHelper GetDefaultLogHelper() => m_defaultLogHelperType switch
        {
            LogHelperTypeEnum.KafkaLog => GetKafkaLogHelper(),
            null => GetKafkaLogHelper(),
            LogHelperTypeEnum.Log4netLog => GetLog4netLogHelper(),
            _ => throw new NotSupportedException()
        };

        /// <summary>
        /// 日志默认日志类型设置
        /// </summary>
        /// <param name="defaultLogHelperType"></param>
        public static void LogHelperDefaultTypeConfig(LogHelperTypeEnum? defaultLogHelperType)
        {
            m_defaultLogHelperType = defaultLogHelperType;
        }
    }
}
