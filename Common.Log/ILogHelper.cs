namespace Common.Log
{
    public interface ILogHelper
    {
        /// <summary>
        /// 基本日志写入
        /// </summary>
        /// <param name="message">需要写入的日志信息</param>
        void Info(string message);

        /// <summary>
        /// 接口日志写入
        /// </summary>
        /// <param name="path">接口路径</param>
        /// <param name="methed">请求方法</param>
        /// <param name="parameters">请求参数</param>
        /// <param name="controllerGroup">接口组名称</param>
        void Info(string path, string methed, string parameters, string controllerGroup);

        /// <summary>
        /// 基本报错信息写入
        /// </summary>
        /// <param name="message">需要写入的错误日志信息</param>
        void Error(string message);

        /// <summary>
        /// 接口报错日志写入
        /// </summary>
        /// <param name="path">接口路径</param>
        /// <param name="methed">请求方法</param>
        /// <param name="parameters">请求参数</param>
        /// <param name="controllerGroup">接口组名称</param>
        /// <param name="errorMessage">接口报错信息</param>
        void Error(string path, string methed, string parameters, string controllerGroup, string errorMessage);

        /// <summary>
        /// Sql日志写入
        /// </summary>
        /// <param name="sql">Sql语句</param>
        /// <param name="parameters">Sql请求参数</param>
        /// <param name="message">Sql执行结果</param>
        void Sql(string sql, string parameters = "", string message = "");

        /// <summary>
        /// TCCNode日志写入
        /// </summary>
        /// <param name="transcationID">TCC事务ID</param>
        /// <param name="isError">是否报错</param>
        /// <param name="message">TCC节点接口调用日志</param>
        void TCCNode(long transcationID, bool isError, string message);

        /// <summary>
        /// TCCServer日志写入
        /// </summary>
        /// <param name="transcationID">TCC事务ID</param>
        /// <param name="message">TCC服务端相关日志</param>
        void TCCServer(long transcationID, string message);
    }
}
