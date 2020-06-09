using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    /// <summary>
    /// 字符串最短验证器特性类
    /// </summary>
    public class StringMinLengthAttribute : ValidationBaseAttribute
    {
        private int m_minLength;

        /// <summary>
        /// 验证器特性构造函数
        /// </summary>
        /// <param name="minLength">最小长度</param>
        public StringMinLengthAttribute(int minLength) => m_minLength = minLength;

        /// <summary>
        /// 获取验证错误的信息
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="propertyName">属性名</param>
        /// <returns></returns>
        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}长度必须长于{m_minLength}。";

        /// <summary>
        /// 验证属性值
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override bool ValidateValue(object value, ValidationContext validationContext)
        {
            if (value is string stringValue)
            {
                if (!string.IsNullOrEmpty(stringValue))
                    return stringValue.Length >= m_minLength;

                return false;
            }

            return true;
        }
    }
}
