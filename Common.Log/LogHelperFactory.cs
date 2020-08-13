namespace Common.Log
{
    /// <summary>
    /// 日志帮助对象工厂
    /// </summary>
    public class LogHelperFactory
    {
        private static ILogHelper m_log4netLogHelper;
        private static ILogHelper m_kafkaLogHelper;

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
    }
}
