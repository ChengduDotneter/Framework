namespace Common.Log
{
    public class SqlErrorLog : LogViewModelBase
    {
        /// <summary>
        /// Sql执行语句
        /// </summary>
        public string Sql { get; set; }

        /// <summary>
        /// Sql执行参数
        /// </summary>
        public string Parameters { get; set; }

        /// <summary>
        /// Sql执行报错信息
        /// </summary>
        public string Message { get; set; }
    }
}
