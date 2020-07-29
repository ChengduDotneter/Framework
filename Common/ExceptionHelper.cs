using System;

namespace Common
{
    /// <summary>
    /// 异常处理帮助
    /// </summary>
    public static class ExceptionHelper
    {
        /// <summary>
        /// 获取异常信息
        /// </summary>
        /// <param name="exception">异常实例</param>
        public static string GetMessage(Exception exception)
        {
            if (exception.InnerException != null)
                return GetMessage(exception.InnerException);
            else
                return exception.Message;
        }

        /// <summary>
        /// 获取异常调用堆栈
        /// </summary>
        /// <param name="exception">异常信实例</param>
        public static string GetStackTrace(Exception exception)
        {
            if (exception.InnerException != null)
                return GetStackTrace(exception.InnerException);
            else
                return exception.StackTrace;
        }
    }
}