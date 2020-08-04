namespace Common.Log
{
    public interface ILogHelper
    {
        void Info(string message);

        void Error(string message);

        void Sql(string message);

        void TCCNode(string message);

        void TCCServer(string message);
    }
}
