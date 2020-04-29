using System.Collections.Generic;

namespace Common.DAL.Cache
{
    public interface IKeyCache<T> where T : IEntity
    {
        T Get(long id);
    }

    public interface IConditionCache<T> where T : IEntity
    {
        IEnumerable<T> Get(string condition);
    }
}
