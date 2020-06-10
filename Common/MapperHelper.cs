using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Common
{
    internal delegate void ReadModelHandler<T>(T data, IDictionary<string, object> dataDictionary) where T : class, new();

    /// <summary>
    /// 实体映射帮组类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MapperModelHelper<T> where T : class, new()
    {
        private static ReadModelHandler<T> m_readModelHandler;

        static MapperModelHelper() => m_readModelHandler = CreateReadModelHandler();

        /// <summary>
        /// 根据字典获取实体列表
        /// </summary>
        /// <param name="dataDictionary"></param>
        /// <returns></returns>
        public static IEnumerable<T> ReadModel(IEnumerable<IDictionary<string, object>> dataDictionary)
        {
            return dataDictionary.Select(item =>
            {
                T data = new T();
                m_readModelHandler(data, item);
                return data;
            });
        }

        private static ReadModelHandler<T> CreateReadModelHandler()
        {
            ParameterExpression data = Expression.Parameter(typeof(T), "data");
            ParameterExpression reader = Expression.Parameter(typeof(IDictionary<string, object>), "reader");

            IList<Expression> methods = new List<Expression>();

            MethodInfo getDicMethod = typeof(IDictionary<string, object>).GetMethod("get_Item");
            MethodInfo contaisKeyMethod = typeof(IDictionary<string, object>).GetMethod("ContainsKey");

            foreach (MemberInfo memberInfo in typeof(T).GetMembers())
            {
                if (!CheckModelSerializabled(memberInfo, out Type type))
                    continue;

                methods.Add(Read(Expression.MakeMemberAccess(data, memberInfo), type, reader, getDicMethod, contaisKeyMethod));
            }

            return Expression.Lambda<ReadModelHandler<T>>(Expression.Block(methods), data, reader).Compile();
        }

        private static bool CheckModelSerializabled(MemberInfo memberInfo, out Type valueType)
        {
            if (memberInfo.MemberType != MemberTypes.Property ||
              !((PropertyInfo)memberInfo).CanWrite)
            {
                valueType = null;
                return false;
            }

            valueType = ((PropertyInfo)memberInfo).PropertyType;
            return true;
        }

        private static Expression Read(MemberExpression memberExpression, Type type, ParameterExpression reader, MethodInfo dicGetMethod, MethodInfo contaisKeyMethod)
        {
            MethodInfo methodInfo = null;
            bool isNullable = CheckIsNullable(type);

            bool isEnum = isNullable ? type.GenericTypeArguments[0].IsEnum : type.IsEnum;

            Expression dicKeyName = Expression.Constant(memberExpression.Member.Name.ToUpper(), typeof(string));

            switch (isNullable ? (isEnum ? typeof(Enum).FullName : type.GenericTypeArguments[0].FullName) : (isEnum ? typeof(Enum).FullName : type.FullName))
            {
                case "System.Byte": methodInfo = typeof(Convert).GetMethod(nameof(Convert.ToByte), new Type[] { typeof(object) }); break;
                case "System.Byte[]": methodInfo = typeof(ConvertExtend).GetMethod(nameof(ConvertExtend.ObjectToByteArray), new Type[] { typeof(object) }); break;
                case "System.Boolean": methodInfo = typeof(Convert).GetMethod(nameof(Convert.ToBoolean), new Type[] { typeof(object) }); break;
                case "System.Int16": methodInfo = typeof(Convert).GetMethod(nameof(Convert.ToInt16), new Type[] { typeof(object) }); break;
                case "System.Int32": methodInfo = typeof(Convert).GetMethod(nameof(Convert.ToInt32), new Type[] { typeof(object) }); break;
                case "System.Single": methodInfo = typeof(Convert).GetMethod(nameof(Convert.ToSingle), new Type[] { typeof(object) }); break;
                case "System.Int64": methodInfo = typeof(Convert).GetMethod(nameof(Convert.ToInt64), new Type[] { typeof(object) }); break;
                case "System.Double": methodInfo = typeof(Convert).GetMethod(nameof(Convert.ToDouble), new Type[] { typeof(object) }); break;
                case "System.DateTime": methodInfo = typeof(Convert).GetMethod(nameof(Convert.ToDateTime), new Type[] { typeof(object) }); break;
                case "System.Decimal": methodInfo = typeof(Convert).GetMethod(nameof(Convert.ToDecimal), new Type[] { typeof(object) }); break;
                case "System.String": methodInfo = typeof(Convert).GetMethod(nameof(Convert.ToString), new Type[] { typeof(object) }); break;
                case "System.Enum": methodInfo = typeof(Enum).GetMethod(nameof(Enum.Parse), new Type[] { typeof(string) }).MakeGenericMethod(isNullable ? type.GenericTypeArguments[0] : type); break;
                default: return Expression.Constant(true);
            }

            Expression getDic = Expression.Call(reader, dicGetMethod, dicKeyName);

            Expression value = isEnum ? Expression.Call(methodInfo, Expression.Call(typeof(Convert).GetMethod(nameof(Convert.ToString), new Type[] { typeof(object) }), getDic)) : Expression.Call(methodInfo, getDic);

            Expression ifCheck = Expression.Call(reader, contaisKeyMethod, dicKeyName);

            return Expression.IfThen(ifCheck, Expression.IfThen(Expression.NotEqual(getDic, Expression.Default(typeof(object))),
                Expression.Assign(memberExpression, SetParameter(isNullable, isNullable ? type.GenericTypeArguments[0] : type, value))));
        }

        private static bool CheckIsNullable(Type type)
        {
            return type.IsGenericType &&
                   type.IsValueType &&
                   type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private static Expression SetParameter(bool isNullable, Type valueType, Expression value)
        {
            if (isNullable)
                return Expression.New(typeof(Nullable<>).MakeGenericType(valueType).GetConstructor(new[] { valueType }), value);
            else
                return value;
        }
    }
}