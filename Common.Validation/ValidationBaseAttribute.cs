using System;
using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    /// <summary>
    /// 验证器特性基类
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class ValidationBaseAttribute : ValidationAttribute
    {
        /// <summary>
        /// 获取验证失败的错误信息
        /// </summary>
        /// <param name="validationContext">验证上下文</param>
        /// <param name="propertyName">属性名</param>
        /// <returns></returns>
        protected abstract string GetErrorMessage(ValidationContext validationContext, string propertyName);

        /// <summary>
        /// 验证属性值
        /// </summary>
        /// <param name="value">属性值</param>
        /// <param name="validationContext">验证上下文</param>
        /// <returns></returns>
        protected abstract bool ValidateValue(object value, ValidationContext validationContext);

        /// <summary>
        /// 是否验证
        /// </summary>
        /// <param name="value">属性值</param>
        /// <param name="validationContext">验证上下文</param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (!ValidateValue(value, validationContext))
            {
                return new ValidationResult(GetErrorMessage(validationContext, validationContext.DisplayName));
            }

            return ValidationResult.Success;
        }
    }
}