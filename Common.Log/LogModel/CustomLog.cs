namespace Common.Log
{
    /// <summary>
    /// 用户自定义日志实体类
    /// </summary>
    public class CustomLog : LogViewModelBase
    {
        /// <summary>
        /// 日志
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 自定义日志编码
        /// </summary>
        public string CustomCode { get; set; }
    }
}
