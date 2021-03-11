using System;
using System.Linq;

namespace CommonFunction
{
    public static class LockKeyGenerator
    {
        public static string UniqueLockKeyGenerator(Type tableType, params string[] values)
        {
            return $"{tableType.FullName}:{string.Join(".", values.OrderBy(item => item))}";
        }
    }
}
