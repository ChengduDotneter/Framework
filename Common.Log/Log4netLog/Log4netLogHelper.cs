using System;
using System.Threading.Tasks;
using log4net;

namespace Common.Log.Log4netLog
{
    /// <summary>
    /// Log4net日志操作类
    /// </summary>
    public class Log4netLogHelper : ILogHelper
    {
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
        public async Task Error(string controllerName, string methed, int statusCode, string errorMessage, string path, string parameters, string stackTrace = "")
        {
            (await Log4netCreater.CreateLog("Controller", controllerName, methed)).
                Error($"path: {path}{Environment.NewLine}parameters: {Environment.NewLine}{parameters}{Environment.NewLine}http_status_code {statusCode}{Environment.NewLine}error_message: {Environment.NewLine}{errorMessage}{Environment.NewLine}stack_trace:{Environment.NewLine}{stackTrace}");
        }

        /// <summary>
        /// 自定义报错日志写入
        /// </summary>
        /// <param name="customCode">自定义编码</param>
        /// <param name="message">需要写入的日志信息</param>
        public async Task Error(string customCode, string message)
        {
            (await Log4netCreater.CreateLog("Custom", "Error", customCode)).Info(message);
        }

        /// <summary>
        /// 自定义日志写入
        /// </summary>
        /// <param name="customCode">自定义编码</param>
        /// <param name="message">需要写入的日志信息</param>
        public async Task Info(string customCode, string message)
        {
            (await Log4netCreater.CreateLog("Custom", "Info", customCode)).Info(message);
        }

        /// <summary>
        /// 接口日志写入
        /// </summary>
        /// <param name="path">接口路径</param>
        /// <param name="methed">请求方法</param>
        /// <param name="parameters">请求参数</param>
        /// <param name="controllerName">接口组名称</param>
        public async Task Info(string controllerName, string methed, string path, string parameters)
        {
            (await Log4netCreater.CreateLog("Controller", controllerName, methed)).Info($"path: {path}{Environment.NewLine}{Environment.NewLine}parameters: {Environment.NewLine}{parameters}");
        }

        /// <summary>
        /// TCCNode日志写入
        /// </summary>
        /// <param name="transcationID">TCC事务ID</param>
        /// <param name="isError">是否报错</param>
        /// <param name="message">TCC节点接口调用日志</param>
        public async Task TCCNode(long transcationID, bool? isError, string message)
        {
            ILog log = await Log4netCreater.CreateLog("TCC", "TCC", "TCCDetails");

            if (isError ?? false)
                log.Error($"transcationID: {transcationID}{Environment.NewLine}is_error:{isError}{Environment.NewLine}message:{message}{Environment.NewLine}stack_trace: {Environment.StackTrace}");
            else
                log.Info($"transcationID: {transcationID}{Environment.NewLine}is_error:{isError}{Environment.NewLine}message:{message}{Environment.NewLine}");
        }

        /// <summary>
        /// TCCServer日志写入
        /// </summary>
        /// <param name="transcationID">TCC事务ID</param>
        /// <param name="message">TCC服务端相关日志</param>
        public async Task TCCServer(long transcationID, string message)
        {
            (await Log4netCreater.CreateLog("TCC", "TCC", "TCCTransactions")).Info($"transcationID: {transcationID}{Environment.NewLine} message:{message}");
        }
    }
}