using System;

namespace Common.DAL
{
    public interface IDBResourceContent : IDisposable
    {
        object Content { get; }
    }
}