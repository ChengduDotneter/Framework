namespace Common.Log
{
    /// <summary>
    /// TCC节点日志实体类
    /// </summary>
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