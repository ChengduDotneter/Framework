using System;

namespace Common.Log
{
    public class Log4netLogHelper : ILogHelper
    {
        public void Error(string message)
        {
            throw new NotImplementedException();
        }

        public void Error(string path, string methed, string parameters, string controllerGroup, string errorMessage)
        {
            throw new NotImplementedException();
        }

        public void Info(string message)
        {
            throw new NotImplementedException();
        }

        public void Info(string path, string methed, string parameters, string controllerGroup)
        {
            throw new NotImplementedException();
        }

        public void Sql(string sql, string parameters = "", string message = "")
        {
            throw new NotImplementedException();
        }

        public void TCCNode(long transcationID, bool isError, string message)
        {
            throw new NotImplementedException();
        }

        public void TCCServer(long transcationID, string message)
        {
            throw new NotImplementedException();
        }
    }
}
