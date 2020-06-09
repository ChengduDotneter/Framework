using System;
using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    /// <summary>
    /// 枚举值存在验证器特性类
    /// </summary>
    public class EnumValueExistAttribute : ValidationBaseAttribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public EnumValueExistAttribute() { }

        /// <summary>
        /// 获取验证失败的错误信息
        /// </summary>
        /// <param name="validationContext"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}对应枚举值不存在。";

        /// <summary>
        /// 验证属性值
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override bool ValidateValue(object value, ValidationContext validationContext)
        {
            try
            {
                if (value == null)
                    return true;

                if (Enum.IsDefined(value.GetType(), (int)value))
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
