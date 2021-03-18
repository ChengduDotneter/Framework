using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Common
{
    public static class ServiceProviderExtend
    {
        public static object CreateInstanceFromServiceProvider(this IServiceProvider serviceProvider, Type type, object[] arguments = null)
        {
            ConstructorInfo[] constructorInfos = type.GetConstructors();

            if (constructorInfos.IsNullOrEmpty())
                throw new DealException($"无法实例化{type.FullName}的实例。");

            ParameterInfo[] parameterInfos = constructorInfos.First().GetParameters();
            object[] parameters = new object[parameterInfos.Length];

            if (!arguments.IsNullOrEmpty())
            {
                for (int i = 0; i < arguments.Length; i++)
                {
                    if (arguments[i].GetType() != parameterInfos[i].ParameterType)
                        throw new DealException($"无法实例化{type.FullName}的实例，传入参数arguments与实际构造参数类型不匹配，请考虑将arguments包含的参数放在构造函数最前面。");

                    parameters[i] = arguments[i];
                }
            }

            for (int i = arguments.IsNullOrEmpty() ? 0 : arguments.Length; i < parameters.Length; i++)
                parameters[i] = serviceProvider.GetRequiredService(parameterInfos[i].ParameterType);

            return Activator.CreateInstance(type, parameters);
        }

        public static T CreateInstanceFromServiceProvider<T>(this IServiceProvider serviceProvider, object[] arguments = null) where T : class
        {
            return (T)CreateInstanceFromServiceProvider(serviceProvider, typeof(T), arguments);
        }
    }
}
