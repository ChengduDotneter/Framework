namespace Common.Log.LogModel
{
    /// <summary>
    /// TCC服务日志实体类
    /// </summary>
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