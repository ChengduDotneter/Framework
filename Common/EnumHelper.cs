using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Common
{
    /// <summary>
    /// 枚举帮助类
    /// </summary>
    public static class EnumHelper
    {
        /// <summary>
        /// 获取枚举值得Display
        /// </summary>
        /// <param name="enumType"></param>
        /// <param name="thisValue"></param>
        /// <returns></returns>
        public static string GetEnumDisplayName(object thisValue)
        {
            string displayName = "";

            Type enumType = thisValue.GetType();
            FieldInfo field = enumType.GetField(Enum.GetName(enumType, thisValue));

            if (field == null)
                return displayName;

            return ((DisplayAttribute)field.GetCustomAttributes(typeof(DisplayAttribute), true)[0])?.Name;
        }
    }
}