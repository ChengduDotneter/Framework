using System;

namespace Common.DAL
{
    public interface IDBResourceContent : IDisposable
    {
        event Action OnDispose;
    }

    public class DBResourceContent : IDBResourceContent
    {
        public event Action OnDispose;

        public void Dispose()
        {
            OnDispose?.Invoke();
        }
    }
}