namespace Common.Log
{
    public class LogHelperFactory
    {
        private static ILogHelper m_log4netLogHelper;
        private static ILogHelper m_kafkaLogHelper;

        static LogHelperFactory()
        {
            m_log4netLogHelper = new Log4netLogHelper();
            m_kafkaLogHelper = new KafkaLogHelper();
        }

        public static ILogHelper GetLog4netLogHelper()
        {
            return m_log4netLogHelper;
        }

        public static ILogHelper GetKafkaLogHelper()
        {
            return m_kafkaLogHelper;
        }
    }
}
