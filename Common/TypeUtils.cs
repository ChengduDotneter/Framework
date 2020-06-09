using System;
using System.Collections.Generic;

namespace Common
{
    /// <summary>
    /// Type类型扩展类
    /// </summary>
    public static class TypeUtils
    {
        /// <summary>
        /// 获取该类型的基类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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
