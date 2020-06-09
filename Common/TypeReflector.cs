using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Common
{
    /// <summary>
    /// 类型反射器
    /// </summary>
    public static class TypeReflector
    {
        /// <summary>
        /// 获取当前程序集中所有满足筛选条件的Type
        /// </summary>
        /// <param name="predicate">筛选条件</param>
        /// <returns></returns>
        public static Type[] ReflectType(Func<Type, bool> predicate)
        {
            IList<string> loadedAssemblyName = new List<string>();
            IList<Type> types = new List<Type>();

            foreach (Assembly loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    IList<Assembly> assemblies = new List<Assembly>();

                    assemblies.Add(loadedAssembly);
                    foreach (AssemblyName assemblyName in loadedAssembly.GetReferencedAssemblies())
                        assemblies.Add(Assembly.Load(assemblyName));

                    foreach (Assembly assembly in assemblies)
                    {
                        if (loadedAssemblyName.Contains(assembly.FullName))
                            continue;

                        loadedAssemblyName.Add(assembly.FullName);

                        foreach (Type type in assembly.GetTypes())
                        {
                            if (!predicate(type))
                                continue;

                            types.Add(type);
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            return types.ToArray();
        }
    }
}
