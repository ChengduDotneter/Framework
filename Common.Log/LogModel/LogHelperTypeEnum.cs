namespace Common.Log.LogModel
{
    /// <summary>
    /// 日志记录类型
    /// </summary>
    public enum LogHelperTypeEnum
    {
        /// <summary>
        /// Kafka的日志记录
        /// </summary>
        KafkaLog,
        /// <summary>
        /// Log4net本地日志记录
        /// </summary>
        Log4netLog
    }
}
