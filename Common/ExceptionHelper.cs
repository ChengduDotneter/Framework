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
        /// <param name="ex">异常实例</param>
        public static string GetMessage(Exception ex)
        {
            if (ex.InnerException != null)
                return GetMessage(ex.InnerException);
            else
                return ex.Message;
        }

        /// <summary>
        /// 获取异常调用堆栈
        /// </summary>
        /// <param name="ex">异常信实例</param>
        public static string GetStackTrace(Exception ex)
        {
            if (ex.InnerException != null)
                return GetStackTrace(ex.InnerException);
            else
                return ex.StackTrace;
        }
    }
}