namespace Common.Log
{
    /// <summary>
    /// 接口错误日志实体类
    /// </summary>
    public class ErrorLog : LogViewModelBase
    {
        /// <summary>
        /// 请求路径
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 请求方式
        /// </summary>
        public string Methed { get; set; }

        /// <summary>
        /// 请求参数
        /// </summary>
        public string Parameters { get; set; }

        /// <summary>
        /// 报错信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 接口组名称
        /// </summary>
        public string ControllerName { get; set; }

        /// <summary>
        /// 接口状态编码
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// 调用堆栈
        /// </summary>
        public string StackTrace { get; set; }
    }
}