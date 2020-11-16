using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache.Configuration;
using Common.DAL;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Common.ServiceCommon
{
    internal delegate void WriteBinaryHandler<T>(T data, IBinaryWriter binaryWriter) where T : class, IEntity, new();

    internal delegate void ReadBinaryHandler<T>(T data, IBinaryReader binaryReader) where T : class, IEntity, new();

    internal class IgniteBinaryBufferSerializer<T> : IBinarySerializer
        where T : class, IEntity, new()
    {
        private WriteBinaryHandler<T> m_writeBinaryHandler;
        private ReadBinaryHandler<T> m_readBinaryHandler;

        public void ReadBinary(object data, IBinaryReader reader)
        {
            m_readBinaryHandler((T)data, reader);
        }

        public void WriteBinary(object data, IBinaryWriter writer)
        {
            m_writeBinaryHandler((T)data, writer);
        }

        public IgniteBinaryBufferSerializer()
        {
            ParameterExpression data = Expression.Parameter(typeof(T), "data");
            m_writeBinaryHandler = CreateWriteBinaryHandler(data);
            m_readBinaryHandler = CreateReadBinaryHandler(data);
        }

        private static WriteBinaryHandler<T> CreateWriteBinaryHandler(ParameterExpression data)
        {
            ParameterExpression writer = Expression.Parameter(typeof(IBinaryWriter), "writer");
            IList<Expression> methods = new List<Expression>();

            foreach (MemberInfo memberInfo in typeof(T).GetMembers())
            {
                if (!CheckSerializabled(memberInfo, out Type type))
                    continue;

                methods.Add(Write(Expression.MakeMemberAccess(data, memberInfo), type, writer));
            }

            return Expression.Lambda<WriteBinaryHandler<T>>(Expression.Block(methods), data, writer).Compile();
        }

        private static ReadBinaryHandler<T> CreateReadBinaryHandler(ParameterExpression data)
        {
            ParameterExpression reader = Expression.Parameter(typeof(IBinaryReader), "reader");
            IList<Expression> methods = new List<Expression>();

            foreach (MemberInfo memberInfo in typeof(T).GetMembers())
            {
                if (!CheckSerializabled(memberInfo, out Type type))
                    continue;

                methods.Add(Read(Expression.MakeMemberAccess(data, memberInfo), type, reader));
            }

            return Expression.Lambda<ReadBinaryHandler<T>>(Expression.Block(methods), data, reader).Compile();
        }

        private static bool CheckSerializabled(MemberInfo memberInfo, out Type valueType)
        {
            if (memberInfo.MemberType != MemberTypes.Property ||
              !((PropertyInfo)memberInfo).CanWrite)
            {
                valueType = null;
                return false;
            }

            QuerySqlFieldAttribute querySqlFieldAttribute = memberInfo.GetCustomAttribute<QuerySqlFieldAttribute>();

            if (querySqlFieldAttribute == null)
            {
                valueType = null;
                return false;
            }

            valueType = ((PropertyInfo)memberInfo).PropertyType;
            return true;
        }

        private static bool CheckIsNullable(Type type)
        {
            return type.IsGenericType &&
                   type.IsValueType &&
                   type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private static Expression Write(MemberExpression memberExpression, Type type, ParameterExpression writer)
        {
            MethodInfo methodInfo = null;
            bool isNullable = CheckIsNullable(type);
            bool isEnum = isNullable ? type.GenericTypeArguments[0].IsEnum : type.IsEnum;

            switch (isNullable ? (isEnum ? typeof(Enum).FullName : type.GenericTypeArguments[0].FullName) : (isEnum ? typeof(Enum).FullName : type.FullName))
            {
                case "System.Byte": methodInfo = typeof(IBinaryWriter).GetMethod(nameof(IBinaryWriter.WriteByte)); break;
                case "System.Byte[]": methodInfo = typeof(IBinaryWriter).GetMethod(nameof(IBinaryWriter.WriteByteArray)); break;
                case "System.Boolean": methodInfo = typeof(IBinaryWriter).GetMethod(nameof(IBinaryWriter.WriteInt)); break;
                case "System.Int16": methodInfo = typeof(IBinaryWriter).GetMethod(nameof(IBinaryWriter.WriteShort)); break;
                case "System.Int32": methodInfo = typeof(IBinaryWriter).GetMethod(nameof(IBinaryWriter.WriteInt)); break;
                case "System.Single": methodInfo = typeof(IBinaryWriter).GetMethod(nameof(IBinaryWriter.WriteFloat)); break;
                case "System.Int64": methodInfo = typeof(IBinaryWriter).GetMethod(nameof(IBinaryWriter.WriteLong)); break;
                case "System.Double": methodInfo = typeof(IBinaryWriter).GetMethod(nameof(IBinaryWriter.WriteDouble)); break;
                case "System.DateTime": methodInfo = typeof(IBinaryWriter).GetMethod(nameof(IBinaryWriter.WriteTimestamp)); break;
                case "System.Decimal": methodInfo = typeof(IBinaryWriter).GetMethod(nameof(IBinaryWriter.WriteDecimal)); break;
                case "System.String": methodInfo = typeof(IBinaryWriter).GetMethod(nameof(IBinaryWriter.WriteString)); break;
                case "System.Enum": methodInfo = typeof(IBinaryWriter).GetMethod(nameof(IBinaryWriter.WriteInt)); break;
                default: throw new NotSupportedException();
            }

            if (isNullable)
            {
                PropertyInfo nullableHasValue = type.GetProperty("HasValue");
                PropertyInfo value = type.GetProperty("Value");
                Expression expression = Expression.Call(writer, methodInfo,
                                                        Expression.Constant(memberExpression.Member.Name, typeof(string)),
                                                        GetParameter(methodInfo, Expression.Property(memberExpression, value)));

                return Expression.IfThen(Expression.Property(memberExpression, nullableHasValue), expression);
            }
            else
            {
                return Expression.Call(writer, methodInfo,
                                       Expression.Constant(memberExpression.Member.Name, typeof(string)),
                                       GetParameter(methodInfo, memberExpression));
            }
        }

        private static Expression GetParameter(MethodInfo methodInfo, Expression parameter)
        {
            Type parameterType = methodInfo.GetParameters()[1].ParameterType;

            if (parameter.Type == typeof(DateTime))
                parameter = Expression.Call(parameter, typeof(DateTime).GetMethod(nameof(DateTime.ToUniversalTime)));
            else if (parameter.Type.IsEnum || parameter.Type == typeof(bool))
                parameter = Expression.Convert(parameter, typeof(int));

            if (CheckIsNullable(parameterType))
                return Expression.New(parameterType.GetConstructor(new[] { parameterType.GenericTypeArguments[0] }), parameter);
            else
                return parameter;
        }

        private static Expression Read(MemberExpression memberExpression, Type type, ParameterExpression reader)
        {
            MethodInfo methodInfo = null;
            bool isNullable = CheckIsNullable(type);
            bool isEnum = isNullable ? type.GenericTypeArguments[0].IsEnum : type.IsEnum;

            switch (isNullable ? (isEnum ? typeof(Enum).FullName : type.GenericTypeArguments[0].FullName) : (isEnum ? typeof(Enum).FullName : type.FullName))
            {
                case "System.Byte": methodInfo = typeof(IBinaryReader).GetMethod(nameof(IBinaryReader.ReadObject)).MakeGenericMethod(type); break;
                case "System.Byte[]": methodInfo = typeof(IBinaryReader).GetMethod(nameof(IBinaryReader.ReadObject)).MakeGenericMethod(type); break;
                case "System.Boolean": methodInfo = typeof(IBinaryReader).GetMethod(nameof(IBinaryReader.ReadObject)).MakeGenericMethod(isNullable ? typeof(int?) : typeof(int)); break;
                case "System.Int16": methodInfo = typeof(IBinaryReader).GetMethod(nameof(IBinaryReader.ReadObject)).MakeGenericMethod(type); break;
                case "System.Int32": methodInfo = typeof(IBinaryReader).GetMethod(nameof(IBinaryReader.ReadObject)).MakeGenericMethod(type); break;
                case "System.Single": methodInfo = typeof(IBinaryReader).GetMethod(nameof(IBinaryReader.ReadObject)).MakeGenericMethod(type); break;
                case "System.Int64": methodInfo = typeof(IBinaryReader).GetMethod(nameof(IBinaryReader.ReadObject)).MakeGenericMethod(type); break;
                case "System.Double": methodInfo = typeof(IBinaryReader).GetMethod(nameof(IBinaryReader.ReadObject)).MakeGenericMethod(type); break;
                case "System.DateTime": methodInfo = typeof(IBinaryReader).GetMethod(nameof(IBinaryReader.ReadObject)).MakeGenericMethod(type); break;
                case "System.Decimal": methodInfo = typeof(IBinaryReader).GetMethod(nameof(IBinaryReader.ReadObject)).MakeGenericMethod(type); break;
                case "System.String": methodInfo = typeof(IBinaryReader).GetMethod(nameof(IBinaryReader.ReadObject)).MakeGenericMethod(type); break;
                case "System.Enum": methodInfo = typeof(IBinaryReader).GetMethod(nameof(IBinaryReader.ReadObject)).MakeGenericMethod(type); break;
                default: throw new NotSupportedException();
            }

            Expression value = Expression.Call(reader, methodInfo, Expression.Constant(memberExpression.Member.Name, typeof(string)));
            Expression ifCheck = isNullable ? Expression.Property(value, "HasValue") : (Expression)Expression.Constant(true);

            return Expression.IfThen(ifCheck, Expression.Assign(memberExpression, SetParameter(isNullable,
                                                                                               isNullable ? type.GenericTypeArguments[0] : type,
                                                                                               value)));
        }

        private static Expression SetParameter(bool isNullable, Type valueType, Expression value)
        {
            ConstantExpression @null = Expression.Constant(null);

            if (valueType == typeof(DateTime))
            {
                Expression parameter = Expression.Call(isNullable ? Expression.Property(value, "Value") : value, typeof(DateTime).GetMethod(nameof(DateTime.ToLocalTime)));

                if (isNullable)
                    return Expression.New(typeof(Nullable<>).MakeGenericType(valueType).GetConstructor(new[] { valueType }), parameter);
                else
                    return parameter;
            }
            else if (valueType == typeof(bool))
            {
                Expression parameter = isNullable ? Expression.Property(value, "Value") : value;
                parameter = Expression.Call(typeof(Convert).GetMethod(nameof(Convert.ToBoolean), new Type[] { typeof(int) }), parameter);

                if (isNullable)
                    return Expression.New(typeof(Nullable<>).MakeGenericType(valueType).GetConstructor(new[] { valueType }), parameter);
                else
                    return parameter;
            }
            else
                return value;
        }
    }
}