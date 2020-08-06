namespace Common.Log
{
    public class TCCServerLog : LogViewModelBase
    {
        /// <summary>
        /// Sql执行语句
        /// </summary>
        public long TranscationID { get; set; }

        /// <summary>
        /// 日志
        /// </summary>
        public string Message { get; set; }
    }
}
