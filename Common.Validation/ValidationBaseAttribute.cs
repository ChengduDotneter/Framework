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
        private readonly string m_ignorePredeciteFunction;

        protected ValidationBaseAttribute(string ignorePredeciteFunction = null)
        {
            m_ignorePredeciteFunction = ignorePredeciteFunction;
        }

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
            bool ignore = false;

            if (!string.IsNullOrWhiteSpace(m_ignorePredeciteFunction))
            {
                ignore = (bool)validationContext.ObjectType.GetMethod(m_ignorePredeciteFunction, System.Reflection.BindingFlags.Instance |
                                                                                                 System.Reflection.BindingFlags.Static |
                                                                                                 System.Reflection.BindingFlags.Public |
                                                                                                 System.Reflection.BindingFlags.NonPublic).Invoke(null, new object[] { validationContext.ObjectInstance });
            }

            if (!ignore && !ValidateValue(value, validationContext))
            {
                return new ValidationResult(GetErrorMessage(validationContext, validationContext.DisplayName));
            }

            return ValidationResult.Success;
        }
    }
}