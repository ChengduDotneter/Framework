using System;

namespace Common.Log
{
    public class KafkaLogHelper : ILogHelper
    {
        public void Error(string customCode, string message)
        {
            throw new NotImplementedException();
        }

        public void Error(string path, string methed, string parameters, string controllerName, string errorMessage)
        {
            throw new NotImplementedException();
        }

        public void Info(string customCode, string message)
        {
            throw new NotImplementedException();
        }

        public void Info(string path, string methed, string parameters, string controllerName)
        {
            throw new NotImplementedException();
        }

        public void SqlError(string sql, string message, string parameters = "")
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
