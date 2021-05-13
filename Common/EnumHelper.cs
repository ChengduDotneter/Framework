using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Common
{
    /// <summary>
    /// DisplayAttribute帮助类
    /// </summary>
    public static class EnumHelper
    {
        /// <summary>
        /// 获取DisplayAttribute的Name属性
        /// </summary>
        /// <param name="thisValue"></param>
        /// <returns></returns>
        public static string GetEnumDisplayName(object thisValue)
        {
            Type thisType = thisValue.GetType();
            FieldInfo field = thisType.GetField(Enum.GetName(thisType, thisValue));

            if (field == null)
                return string.Empty;

            object[] attrs = field.GetCustomAttributes(typeof(DisplayAttribute), false);
            if (attrs.Count() != 1)
                throw new DealException("获取描述失败");

            return ((DisplayAttribute)attrs[0]).Name;
        }
    }
}