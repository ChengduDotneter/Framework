using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Common.RPC.BufferSerializer
{
    internal class BinaryBufferSerializer : IBufferSerializer
    {
        private class SerializerContext
        {
            public GetBuffer GetBuffer { get; set; }
            public CreateData CreateData { get; set; }
            public GetDataSize GetDataSize { get; set; }
            public Type ObjectType { get; }

            public SerializerContext(Type objectType)
            {
                ObjectType = objectType;
            }
        }

        private static readonly DateTime MIN_TIME;
        private delegate int GetDataSize(IRPCData data, Encoding encoding);
        private delegate byte[] GetBuffer(IRPCData data, Encoding encoding);
        private delegate IRPCData CreateData(byte[] buffer, Encoding encoding, ref int offset);
        private static IDictionary<byte, SerializerContext> m_serializerContexts;
        private Encoding m_encoding;

        public BinaryBufferSerializer(Encoding encoding)
        {
            m_encoding = encoding;
        }

        static BinaryBufferSerializer()
        {
            MIN_TIME = DateTime.MinValue.AddYears(1970);
            m_serializerContexts = new Dictionary<byte, SerializerContext>();

            Type[] dataTypes = TypeReflector.ReflectType(type =>
            {
                if (type.GetInterface(nameof(IRPCData)) == null || type.IsInterface)
                    return false;

                return true;
            });

            foreach (Type type in dataTypes)
            {
                if (type.IsClass)
                    throw new Exception("序列化对象必须为结构体。");

                IRPCData template = (IRPCData)Activator.CreateInstance(type);
                byte messageID = template.MessageID;

                if (m_serializerContexts.ContainsKey(messageID))
                    throw new Exception(string.Format("序列化对象ID重复，重复ID：{0}，对象类型：{1}和{2}。", messageID, m_serializerContexts[messageID].ObjectType.FullName, type.FullName));

                m_serializerContexts.Add(messageID, new SerializerContext(type));
                InitGetDataSizeHandler(type, messageID);
                InitGetBufferHandler(type, messageID);
                InitCreateDataHandler(type, messageID);
            }
        }

        private static bool CheckSerializabled(MemberInfo memberInfo, out Type valueType)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    if (!((FieldInfo)memberInfo).IsPublic ||
                        !((FieldInfo)memberInfo).IsInitOnly ||
                        ((FieldInfo)memberInfo).IsStatic)
                    {
                        valueType = null;
                        return false;
                    }
                    valueType = ((FieldInfo)memberInfo).FieldType;
                    break;
                case MemberTypes.Property:
                    if (!((PropertyInfo)memberInfo).CanWrite)
                    {
                        valueType = null;
                        return false;
                    }
                    valueType = ((PropertyInfo)memberInfo).PropertyType;
                    break;
                default:
                    {
                        valueType = null;
                        return false;
                    }
            }

            return true;
        }

        private static int GetArrayLength(Array array)
        {
            if (array == null)
                return 0;

            return array.Length;
        }

        private static int GetStringLength(string value, Encoding encoding)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            return encoding.GetByteCount(value);
        }

        private static Array MakeArrayNotNull(Array array, Type elementType)
        {
            if (array == null)
                return Array.CreateInstance(elementType, 0);

            return array;
        }

        private static void InitGetDataSizeHandler(Type dataType, byte messageID)
        {
            ParameterExpression data = Expression.Parameter(typeof(IRPCData), "data");
            ParameterExpression encoding = Expression.Parameter(typeof(Encoding), "encoding");
            Expression body = InitGetDataSizeHandler(dataType, data, encoding);
            GetDataSize getDataSizeHandler = Expression.Lambda<GetDataSize>(body, data, encoding).Compile();
            m_serializerContexts[messageID].GetDataSize = getDataSizeHandler;
        }

        private static Expression InitGetDataSizeHandler(Type dataType, Expression data, ParameterExpression encoding)
        {
            ParameterExpression bufferLength = Expression.Variable(typeof(int), "bufferLength");
            IList<Expression> bufferLengthAssigns = new List<Expression>();
            LabelTarget target = Expression.Label(typeof(int));
            GotoExpression gotoExpression = Expression.Return(target, bufferLength);
            LabelExpression label = Expression.Label(target, bufferLength);

            bufferLengthAssigns.Add(Expression.Assign(bufferLength, Expression.Constant(0)));

            foreach (MemberInfo memberInfo in dataType.GetMembers())
            {
                if (!CheckSerializabled(memberInfo, out Type valueType))
                    continue;

                MemberExpression member = Expression.MakeMemberAccess(Expression.Convert(data, dataType), memberInfo);

                if (valueType.GetInterface(nameof(IRPCData)) != null)
                    bufferLengthAssigns.Add(Expression.AddAssign(bufferLength, InitGetDataSizeHandler(valueType, member, encoding)));
                else if (valueType == typeof(DateTime))
                    bufferLengthAssigns.Add(Expression.AddAssign(bufferLength, Expression.Constant(sizeof(long))));
                else if (valueType == typeof(bool))
                    bufferLengthAssigns.Add(Expression.AddAssign(bufferLength, Expression.Constant(sizeof(bool))));
                else if (valueType.IsValueType)
                    bufferLengthAssigns.Add(Expression.AddAssign(bufferLength, Expression.Constant(GetSize(valueType))));
                else if (valueType.BaseType == typeof(Array))
                {
                    Type elementType = valueType.Assembly.GetType(valueType.FullName.Replace("[]", string.Empty));
                    Expression arrayLength = Expression.Call(typeof(BinaryBufferSerializer).GetMethod(nameof(BinaryBufferSerializer.GetArrayLength), BindingFlags.Static | BindingFlags.NonPublic), member);
                    bufferLengthAssigns.Add(Expression.AddAssign(bufferLength, Expression.Constant(sizeof(int))));

                    if (elementType.GetInterface(nameof(IRPCData)) != null)
                    {
                        ParameterExpression length = Expression.Variable(typeof(int));
                        ParameterExpression value = Expression.Variable(typeof(int));
                        LabelTarget breakTarget = Expression.Label();
                        Expression ifTrue = Expression.Block(Expression.AddAssign(bufferLength, InitGetDataSizeHandler(elementType, Expression.ArrayIndex(member, value), encoding)), Expression.AddAssign(value, Expression.Constant(1)));
                        Expression ifFalse = Expression.Break(breakTarget);

                        bufferLengthAssigns.Add(Expression.Block(new ParameterExpression[] { length, value }, Expression.Assign(length, arrayLength), Expression.Loop(Expression.IfThenElse(Expression.LessThan(value, length), ifTrue, ifFalse), breakTarget)));
                    }
                    else
                    {
                        bufferLengthAssigns.Add(Expression.AddAssign(bufferLength, Expression.Multiply(arrayLength, Expression.Constant(GetSize(elementType)))));
                    }
                }
                else if (valueType == typeof(string))
                {
                    MethodCallExpression length = Expression.Call(typeof(BinaryBufferSerializer).GetMethod(nameof(BinaryBufferSerializer.GetStringLength), BindingFlags.Static | BindingFlags.NonPublic), member, encoding);
                    bufferLengthAssigns.Add(Expression.AddAssign(bufferLength, Expression.Constant(sizeof(int))));
                    bufferLengthAssigns.Add(Expression.AddAssign(bufferLength, length));
                }
                else
                    throw new Exception();
            }

            bufferLengthAssigns.Add(gotoExpression);
            bufferLengthAssigns.Add(label);

            return Expression.Block(new ParameterExpression[] { bufferLength }, bufferLengthAssigns);
        }

        private static void InitGetBufferHandler(Type dataType, byte messageID)
        {
            ParameterExpression data = Expression.Parameter(typeof(IRPCData), "data");
            ParameterExpression encoding = Expression.Parameter(typeof(Encoding), "encoding");
            ParameterExpression buffer = Expression.Variable(typeof(byte[]), "buffer");
            ParameterExpression offset = Expression.Variable(typeof(int), "offset");
            Expression bufferLength = Expression.Call(typeof(BinaryBufferSerializer).GetMethod(nameof(BinaryBufferSerializer.GetSize), BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(IRPCData), typeof(Encoding) }, null), data, encoding);
            BinaryExpression bufferAssign = Expression.Assign(buffer, Expression.Convert(Expression.Call(typeof(Array).GetMethod("CreateInstance", new Type[] { typeof(Type), typeof(int) }), Expression.Constant(typeof(byte), typeof(Type)), bufferLength), typeof(byte[])));
            LabelTarget target = Expression.Label(typeof(byte[]));
            GotoExpression gotoExpression = Expression.Return(target, buffer);
            LabelExpression label = Expression.Label(target, buffer);
            IList<Expression> bufferLengthAssigns = new List<Expression>();

            BlockExpression body = Expression.Block(new ParameterExpression[] { buffer, offset }, bufferAssign, InitGetBufferHandler(dataType, data, encoding, buffer, offset), gotoExpression, label);
            GetBuffer getBufferHandler = Expression.Lambda<GetBuffer>(body, data, encoding).Compile();
            m_serializerContexts[messageID].GetBuffer = getBufferHandler;
        }

        private static Expression InitGetBufferHandler(Type dataType, Expression data, ParameterExpression encoding, ParameterExpression buffer, ParameterExpression offset)
        {
            IList<Expression> methods = new List<Expression>();

            foreach (MemberInfo memberInfo in dataType.GetMembers())
            {
                if (!CheckSerializabled(memberInfo, out Type valueType))
                    continue;

                MemberExpression member = Expression.MakeMemberAccess(Expression.Convert(data, dataType), memberInfo);

                if (valueType.GetInterface(nameof(IRPCData)) != null)
                    methods.Add(InitGetBufferHandler(valueType, member, encoding, buffer, offset));
                else if (valueType.BaseType == typeof(Array))
                {
                    Type elementType = valueType.Assembly.GetType(valueType.FullName.Replace("[]", string.Empty));
                    ParameterExpression array = Expression.Variable(valueType);
                    ParameterExpression arrayLength = Expression.Variable(typeof(int));
                    MethodInfo lengthMethodInfo = typeof(BinaryBufferSerializer).GetMethod("CopyBytes", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(int), typeof(byte[]), typeof(int).MakeByRefType() }, null);
                    Expression method = null;

                    if (elementType.GetInterface(nameof(IRPCData)) != null)
                    {
                        ParameterExpression value = Expression.Variable(typeof(int));
                        LabelTarget breakTarget = Expression.Label();
                        Expression ifTrue = Expression.Block(InitGetBufferHandler(elementType, Expression.ArrayIndex(array, value), encoding, buffer, offset), Expression.AddAssign(value, Expression.Constant(1)));
                        Expression ifFalse = Expression.Break(breakTarget);
                        method = Expression.Block(new ParameterExpression[] { value }, Expression.Loop(Expression.IfThenElse(Expression.LessThan(value, arrayLength), ifTrue, ifFalse), breakTarget));
                    }
                    else
                    {
                        MethodInfo methodInfo = methodInfo = typeof(BinaryBufferSerializer).GetMethod("CopyBytes", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { valueType, typeof(byte[]), typeof(int).MakeByRefType() }, null);
                        method = Expression.Call(methodInfo, array, buffer, offset);
                    }

                    methods.Add(Expression.Block(new ParameterExpression[] { arrayLength, array }, Expression.Assign(array, Expression.Convert(Expression.Call(typeof(BinaryBufferSerializer).GetMethod(nameof(BinaryBufferSerializer.MakeArrayNotNull), BindingFlags.Static | BindingFlags.NonPublic), member, Expression.Constant(elementType)), valueType)), Expression.Assign(arrayLength, Expression.ArrayLength(array)), Expression.Call(lengthMethodInfo, arrayLength, buffer, offset), method));
                }
                else if (valueType == typeof(string))
                {
                    MethodInfo lengthMethodInfo = typeof(BinaryBufferSerializer).GetMethod("CopyBytes", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(int), typeof(byte[]), typeof(int).MakeByRefType() }, null);
                    methods.Add(Expression.Call(lengthMethodInfo, Expression.Call(typeof(BinaryBufferSerializer).GetMethod(nameof(BinaryBufferSerializer.GetStringLength), BindingFlags.Static | BindingFlags.NonPublic), member, encoding), buffer, offset));

                    MethodInfo methodInfo = methodInfo = typeof(BinaryBufferSerializer).GetMethod("CopyBytes", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(string), typeof(Encoding), typeof(byte[]), typeof(int).MakeByRefType() }, null);
                    methods.Add(Expression.Call(methodInfo, member, encoding, buffer, offset));
                }
                else if (valueType.IsValueType)
                {
                    MethodInfo methodInfo = methodInfo = typeof(BinaryBufferSerializer).GetMethod("CopyBytes", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { valueType, typeof(byte[]), typeof(int).MakeByRefType() }, null);
                    methods.Add(Expression.Call(methodInfo, member, buffer, offset));
                }
                else
                    throw new Exception();
            }

            if (methods.Count > 0)
                return Expression.Block(methods);
            else
                return Expression.Empty();
        }

        private static void InitCreateDataHandler(Type dataType, byte messageID)
        {
            ParameterExpression buffer = Expression.Parameter(typeof(byte[]), "buffer");
            ParameterExpression encoding = Expression.Parameter(typeof(Encoding), "encoding");
            ParameterExpression offset = Expression.Parameter(typeof(int).MakeByRefType(), "offset");

            Expression body = InitCreateDataHandler(dataType, encoding, buffer, offset);
            CreateData createDataHandler = Expression.Lambda<CreateData>(body, buffer, encoding, offset).Compile();
            m_serializerContexts[messageID].CreateData = createDataHandler;
        }

        private static Expression InitCreateDataHandler(Type dataType, ParameterExpression encoding, ParameterExpression buffer, ParameterExpression offset)
        {
            ParameterExpression data = Expression.Variable(dataType, "data");
            BinaryExpression dataAssign = Expression.Assign(data, Expression.New(dataType));
            LabelTarget target = Expression.Label(typeof(IRPCData));
            GotoExpression gotoExpression = Expression.Return(target, Expression.Convert(data, typeof(IRPCData)));
            LabelExpression label = Expression.Label(target, Expression.Convert(data, typeof(IRPCData)));
            IList<Expression> methods = new List<Expression>();
            IList<ParameterExpression> variables = new List<ParameterExpression>();

            variables.Add(data);
            methods.Add(dataAssign);

            foreach (MemberInfo memberInfo in dataType.GetMembers())
            {
                if (!CheckSerializabled(memberInfo, out Type valueType))
                    continue;

                MemberExpression member = Expression.MakeMemberAccess(data, memberInfo);

                if (valueType.GetInterface(nameof(IRPCData)) != null)
                {
                    ParameterExpression valueParameter = Expression.Variable(valueType);
                    variables.Add(valueParameter);
                    BinaryExpression valueParameterAssign = Expression.Assign(valueParameter, Expression.Convert(InitCreateDataHandler(valueType, encoding, buffer, offset), valueType));
                    BinaryExpression valueAssign = Expression.Assign(member, valueParameter);
                    methods.Add(valueParameterAssign);
                    methods.Add(valueAssign);
                }
                else if (valueType == typeof(DateTime[]))
                {
                    MethodCallExpression count = Expression.Call(typeof(BinaryBufferSerializer).GetMethod(nameof(BinaryBufferSerializer.GetInt32), BindingFlags.Static | BindingFlags.NonPublic), buffer, offset);
                    methods.Add(Expression.Assign(member, Expression.Call(typeof(BinaryBufferSerializer).GetMethod(nameof(BinaryBufferSerializer.GetDateTimeArray), BindingFlags.Static | BindingFlags.NonPublic), buffer, count, offset)));
                }
                else if (valueType.BaseType == typeof(Array))
                {
                    Type elementType = valueType.Assembly.GetType(valueType.FullName.Replace("[]", string.Empty));
                    MethodCallExpression arrayLength = Expression.Call(typeof(BinaryBufferSerializer).GetMethod(nameof(BinaryBufferSerializer.GetInt32), BindingFlags.Static | BindingFlags.NonPublic), buffer, offset);

                    if (elementType.GetInterface(nameof(IRPCData)) != null)
                    {
                        ParameterExpression length = Expression.Variable(typeof(int));
                        Expression lengthAssign = Expression.Assign(length, arrayLength);
                        Expression newArray = Expression.Assign(member, Expression.Convert(Expression.Call(typeof(Array).GetMethod(nameof(Array.CreateInstance), new Type[] { typeof(Type), typeof(int) }), Expression.Constant(elementType), length), valueType));
                        ParameterExpression value = Expression.Variable(typeof(int));
                        LabelTarget breakTarget = Expression.Label();
                        Expression ifTrue = Expression.Block(Expression.Assign(Expression.ArrayAccess(member, value), Expression.Convert(InitCreateDataHandler(elementType, encoding, buffer, offset), elementType)), Expression.AddAssign(value, Expression.Constant(1)));
                        Expression ifFalse = Expression.Break(breakTarget);


                        methods.Add(Expression.Block(new ParameterExpression[] { length, value }, lengthAssign, newArray, Expression.Loop(Expression.IfThenElse(Expression.LessThan(value, length), ifTrue, ifFalse), breakTarget)));
                    }
                    else
                    {
                        methods.Add(Expression.Assign(member, Expression.Convert(Expression.Call(typeof(BinaryBufferSerializer).GetMethod(nameof(BinaryBufferSerializer.GetArray), BindingFlags.Static | BindingFlags.NonPublic), buffer, arrayLength, Expression.Constant(elementType, typeof(Type)), offset), valueType)));
                    }
                }
                else if (valueType == typeof(string))
                {
                    MethodCallExpression count = Expression.Call(typeof(BinaryBufferSerializer).GetMethod(nameof(BinaryBufferSerializer.GetInt32), BindingFlags.Static | BindingFlags.NonPublic), buffer, offset);
                    methods.Add(Expression.Assign(member, Expression.Call(typeof(BinaryBufferSerializer).GetMethod(nameof(BinaryBufferSerializer.GetString), BindingFlags.Static | BindingFlags.NonPublic), buffer, encoding, count, offset)));
                }
                else if (valueType.IsValueType)
                    methods.Add(Expression.Assign(member, Expression.Call(typeof(BinaryBufferSerializer).GetMethod(string.Format("Get{0}", valueType.Name), BindingFlags.Static | BindingFlags.NonPublic), buffer, offset)));
                else
                    throw new Exception();
            }

            methods.Add(gotoExpression);
            methods.Add(label);

            return Expression.Block(variables, methods);
        }

        public IRPCData Deserialize(byte[] buffer)
        {
            int offset = 0;
            int messageID = GetInt32(buffer, ref offset);
            int length = GetInt32(buffer, ref offset);
            IRPCData data = m_serializerContexts[(byte)messageID].CreateData(buffer, m_encoding, ref offset);

            if (length > offset)
                throw new Exception("反序列化异常。");

            return data;
        }

        public int Serialize(IRPCData data, byte[] buffer)
        {
            int offset = 0;
            CopyBytes((int)data.MessageID, buffer, ref offset);
            byte[] bytes = m_serializerContexts[data.MessageID].GetBuffer(data, m_encoding);
            CopyBytes(bytes.Length, buffer, ref offset);
            CopyBytes(bytes, buffer, ref offset);

            return offset;
        }

        private static int GetSize(Type type)
        {
            switch (type.FullName)
            {
                case "System.Byte":
                case "System.Boolean": return 1;
                case "System.Int16": return 2;
                case "System.Int32":
                case "System.Single": return 4;
                case "System.Int64":
                case "System.Double":
                case "System.DateTime": return 8;
                default: throw new NotImplementedException();
            }
        }

        private static int GetSize(IRPCData data, Encoding encoding)
        {
            return m_serializerContexts[data.MessageID].GetDataSize(data, encoding);
        }

        #region CopyBytes
        private static void CopyBytes(byte[] value, byte[] buffer, ref int offset)
        {
            Array.Copy(value, 0, buffer, offset, value.Length);
            offset += value.Length;
        }

        private static void CopyBytes(Array value, byte[] buffer, ref int offset)
        {
            int length = Buffer.ByteLength(value);
            Buffer.BlockCopy(value, 0, buffer, offset, length);
            offset += length;
        }

        private static void CopyBytes(DateTime[] value, byte[] buffer, ref int offset)
        {
            long[] valueArray = new long[value.Length];

            for (int i = 0; i < valueArray.Length; i++)
                valueArray[i] = (long)(value[i] - MIN_TIME).TotalMilliseconds;

            CopyBytes(valueArray, buffer, ref offset);
        }

        private static void CopyBytes(string value, Encoding encoding, byte[] buffer, ref int offset)
        {
            byte[] bytes = string.IsNullOrWhiteSpace(value) ? new byte[0] : encoding.GetBytes(value);
            CopyBytes(bytes, buffer, ref offset);
        }

        private static void CopyBytes(byte value, byte[] buffer, ref int offset)
        {
            buffer[offset] = value;
            offset += 1;
        }

        private static void CopyBytes(int value, byte[] buffer, ref int offset)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, buffer, offset, sizeof(int));
            offset += sizeof(int);
        }

        private static void CopyBytes(bool value, byte[] buffer, ref int offset)
        {
            buffer[offset] = (byte)(value ? 1 : 0);
            offset += sizeof(bool);
        }

        private static void CopyBytes(double value, byte[] buffer, ref int offset)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, buffer, offset, sizeof(double));
            offset += sizeof(double);
        }

        private static void CopyBytes(float value, byte[] buffer, ref int offset)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, buffer, offset, sizeof(float));
            offset += sizeof(float);
        }

        private static void CopyBytes(long value, byte[] buffer, ref int offset)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, buffer, offset, sizeof(long));
            offset += sizeof(long);
        }

        private static void CopyBytes(short value, byte[] buffer, ref int offset)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, buffer, offset, sizeof(short));
            offset += sizeof(short);
        }

        private static void CopyBytes(DateTime value, byte[] buffer, ref int offset)
        {
            long longValue = (long)(value - MIN_TIME).TotalMilliseconds;
            CopyBytes(longValue, buffer, ref offset);
        }
        #endregion

        #region GetData
        private static Array GetArray(byte[] buffer, int arrayLength, Type elementType, ref int offset)
        {
            Array array = Array.CreateInstance(elementType, arrayLength);
            int byteLength = Buffer.ByteLength(array);
            Buffer.BlockCopy(buffer, offset, array, 0, byteLength);
            offset += byteLength;

            return array;
        }

        private static DateTime[] GetDateTimeArray(byte[] buffer, int count, ref int offset)
        {
            DateTime[] array = new DateTime[count];

            for (int i = 0; i < array.Length; i++)
                array[i] = GetDateTime(buffer, ref offset);

            return array;
        }

        private static string GetString(byte[] buffer, Encoding encoding, int count, ref int offset)
        {
            string data = encoding.GetString(buffer, offset, count);
            offset += count;

            return data;
        }

        private static byte GetByte(byte[] buffer, ref int offset)
        {
            byte data = buffer[offset];
            offset += 1;

            return data;
        }

        private static int GetInt32(byte[] buffer, ref int offset)
        {
            int data = BitConverter.ToInt32(buffer, offset);
            offset += sizeof(int);

            return data;
        }

        private static bool GetBoolean(byte[] buffer, ref int offset)
        {
            bool data = BitConverter.ToBoolean(buffer, offset);
            offset += sizeof(bool);

            return data;
        }

        private static double GetDouble(byte[] buffer, ref int offset)
        {
            double data = BitConverter.ToDouble(buffer, offset);
            offset += sizeof(double);

            return data;
        }

        private static float GetSingle(byte[] buffer, ref int offset)
        {
            float data = BitConverter.ToSingle(buffer, offset);
            offset += sizeof(float);

            return data;
        }

        private static long GetInt64(byte[] buffer, ref int offset)
        {
            long data = BitConverter.ToInt64(buffer, offset);
            offset += sizeof(long);

            return data;
        }

        private static short GetInt16(byte[] buffer, ref int offset)
        {
            short data = BitConverter.ToInt16(buffer, offset);
            offset += sizeof(short);

            return data;
        }

        private static DateTime GetDateTime(byte[] buffer, ref int offset)
        {
            long longValue = BitConverter.ToInt64(buffer, offset);
            offset += sizeof(long);

            DateTime data = MIN_TIME.AddMilliseconds(longValue);
            return data;
        }
        #endregion
    }
}
