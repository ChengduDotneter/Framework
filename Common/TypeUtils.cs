using System;
using System.Collections.Generic;

namespace Common
{
    public static class TypeUtils
    {
        public static IList<Type> GetBaseTypes(this Type type)
        {
            IList<Type> baseTypes = new List<Type>();

            if (type.BaseType != null)
            {
                baseTypes.Add(type.BaseType);
                baseTypes.AddRange(GetBaseTypes(type.BaseType));
            }

            return baseTypes;
        }
    }
}
