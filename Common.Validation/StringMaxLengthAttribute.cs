using System;
using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    /// <summary>
    /// 字符串最长验证器特性类
    /// </summary>
    public class StringMaxLengthAttribute : ValidationBaseAttribute
    {
        private int m_maxLength;

        /// <summary>
        /// 验证器特性构造函数
        /// </summary>
        /// <param name="maxLength">最大长度</param>
        /// <param name="ignorePredeciteFunction"></param>
        public StringMaxLengthAttribute(int maxLength, string ignorePredeciteFunction = null) : base(ignorePredeciteFunction) => m_maxLength = maxLength;

        /// <summary>
        /// 获取验证失败的错误信息
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="propertyName">属性名</param>
        /// <returns></returns>
        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}长度必须短于{m_maxLength}。";

        /// <summary>
        /// 验证属性值
        /// </summary>
        /// <param name="value">属性值</param>
        /// <param name="validationContext">验证器上下文</param>
        /// <returns></returns>
        protected override bool ValidateValue(object value, ValidationContext validationContext)
        {
            if (value is string stringValue && !string.IsNullOrEmpty(stringValue))
                return stringValue.Length <= m_maxLength;

            return true;
        }
    }
}