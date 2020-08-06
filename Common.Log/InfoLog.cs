namespace Common.Log
{
    public class InfoLog : LogViewModelBase
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
        /// 接口组名称
        /// </summary>
        public string ControllerName { get; set; }
    }
}
