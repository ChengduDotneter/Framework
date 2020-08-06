namespace Common.Log
{
    public class TCCNodeLog : LogViewModelBase
    {
        /// <summary>
        /// TCC事务ID
        /// </summary>
        public long TranscationID { get; set; }

        /// <summary>
        /// 是否出现错误
        /// </summary>
        public bool? IsError { get; set; }

        /// <summary>
        /// 日志
        /// </summary>
        public string Message { get; set; }
    }
}
