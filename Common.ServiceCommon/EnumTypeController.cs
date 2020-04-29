﻿using Common;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Common.ServiceCommon
{
    [Route("enum")]
    [ApiController]
    public class EnumTypeController : ControllerBase
    {
        private const string UNKNOWN = "UNKNOWN";

        public class EnumValues
        {
            /// <summary>
            /// 获取枚举显示的中文注释
            /// </summary>
            public string DisplayString { get; set; }

            /// <summary>
            /// 获取枚举的值
            /// </summary>
            public string Value { get; set; }

            /// <summary>
            /// 获取枚举对应的代码
            /// </summary>
            public int Key { get; set; }
        }

        public EnumTypeController()
        {
        }

        [HttpGet("{objectName}/{properyName}")]
        public IEnumerable<EnumValues> GetEnumItem(string objectName, string properyName)
        {
            Type objectType = GetTypeByObjectTypeName(JsonUtils.PropertyNameToCSharpStyle(objectName));

            if (string.IsNullOrWhiteSpace(properyName))
                throw new NullReferenceException();

            PropertyInfo propertyInfo = objectType.GetProperty(JsonUtils.PropertyNameToCSharpStyle(properyName));

            if (propertyInfo == null)
                throw new NotSupportedException();

            return GetEnumItemValuesByEnumType(propertyInfo.PropertyType);
        }

        private static Type GetTypeByObjectTypeName(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                throw new NullReferenceException();

            Assembly[] assemblyArray = AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < assemblyArray.Length; ++i)
            {
                Type type = assemblyArray[i].GetType(typeName);

                if (type != null)
                    return type;
            }

            for (int i = 0; i < assemblyArray.Length; ++i)
            {
                Type[] typeArray = assemblyArray[i].GetTypes();

                for (int j = 0; j < typeArray.Length; ++j)
                {
                    if (typeArray[j].Name.Equals(typeName))
                        return typeArray[j];
                }
            }

            throw new NotSupportedException();
        }

        ///<summary>  
        /// 获取枚举值+描述  
        ///</summary>  
        ///<param name="enumType">Type,该参数的格式为typeof(需要读的枚举类型)</param>  
        ///<returns>键值对</returns>  
        private static IEnumerable<EnumValues> GetEnumItemValuesByEnumType(Type enumType)
        {
            if (enumType.IsGenericType && enumType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (enumType.GetGenericArguments().Length == 0)
                    throw new NotSupportedException();
                else
                    enumType = enumType.GetGenericArguments()[0];
            }

            if (!enumType.IsEnum)
                throw new NotSupportedException();

            IList<EnumValues> enumDetails = new List<EnumValues>();

            foreach (FieldInfo field in enumType.GetFields())
            {
                if (field.FieldType.IsEnum)
                {
                    EnumValues enumValue = new EnumValues
                    {
                        Key = ((int)enumType.InvokeMember(field.Name, BindingFlags.GetField, null, null, null)),
                        Value = (enumType.InvokeMember(field.Name, BindingFlags.GetField, null, null, null)).ToString()
                    };

                    object[] arr = field.GetCustomAttributes(typeof(DisplayAttribute), true);

                    if (arr.Length > 0)
                    {
                        DisplayAttribute attribute = (DisplayAttribute)arr[0];
                        enumValue.DisplayString = attribute.Name;
                    }
                    else
                        enumValue.DisplayString = UNKNOWN;

                    enumDetails.Add(enumValue);
                }
            }

            return enumDetails;
        }
    }
}
