namespace Common.Log
{
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
