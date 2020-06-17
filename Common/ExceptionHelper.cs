using System;

namespace Common
{
    public static class ExceptionHelper
    {
        public static string GetMessage(Exception ex)
        {
            if (ex.InnerException != null)
                return GetMessage(ex.InnerException);
            else
                return ex.Message;
        }

        public static string GetStackTrace(Exception ex)
        {
            if (ex.InnerException != null)
                return GetStackTrace(ex.InnerException);
            else
                return ex.StackTrace;
        }
    }
}
