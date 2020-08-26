using System;
using System.Threading.Tasks;

namespace Common.Log
{
    /// <summary>
    /// 日志操作接口类
    /// </summary>
    public interface ILogHelper
    {
        /// <summary>
        /// 自定义日志写入
        /// </summary>
        /// <param name="customCode">自定义编码</param>
        /// <param name="message">需要写入的日志信息</param>
        Task Info(string customCode, string message);

        /// <summary>
        /// 接口日志写入
        /// </summary>
        /// <param name="path">接口路径</param>
        /// <param name="methed">请求方法</param>
        /// <param name="parameters">请求参数</param>
        /// <param name="controllerName">接口组名称</param>
        Task Info(string controllerName, string methed, string path, string parameters);

        /// <summary>
        /// 自定义报错日志写入
        /// </summary>
        /// <param name="customCode">自定义编码</param>
        /// <param name="message">需要写入的日志信息</param>
        Task Error(string customCode, string message);

        /// <summary>
        /// 接口报错日志写入
        /// </summary>
        /// <param name="path">接口路径</param>
        /// <param name="methed">请求方法</param>
        /// <param name="parameters">请求参数</param>
        /// <param name="stackTrace"></param>
        /// <param name="controllerName">接口组名称</param>
        /// <param name="errorMessage">接口报错信息</param>
        /// <param name="statusCode">接口状态编码</param>
        Task Error(string controllerName, string methed, int statusCode, string errorMessage, string path, string parameters, string stackTrace = "");

        /// <summary>
        /// TCCNode日志写入
        /// </summary>
        /// <param name="transcationID">TCC事务ID</param>
        /// <param name="isError">是否报错</param>
        /// <param name="message">TCC节点接口调用日志</param>
        Task TCCNode(long transcationID, bool? isError, string message);

        /// <summary>
        /// TCCServer日志写入
        /// </summary>
        /// <param name="transcationID">TCC事务ID</param>
        /// <param name="message">TCC服务端相关日志</param>
        Task TCCServer(long transcationID, string message);
    }
}
