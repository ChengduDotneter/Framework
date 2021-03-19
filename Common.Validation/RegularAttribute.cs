using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Common.Validation
{
    /// <summary>
    /// 正则验证器特性类
    /// </summary>
    public class RegexAttribute : ValidationBaseAttribute
    {
        private string m_regexValue;

        /// <summary>
        /// 正则验证器特性构造函数
        /// </summary>
        /// <param name="regexValue">正则表达式</param>
        /// <param name="ignorePredeciteFunction"></param>
        public RegexAttribute(string regexValue, string ignorePredeciteFunction = null) : base(ignorePredeciteFunction) => m_regexValue = regexValue;

        /// <summary>
        /// 获取验证失败的错误信息
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="propertyName">属性名</param>
        /// <returns></returns>
        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}必须满足正则表达式（{ m_regexValue}）。";

        /// <summary>
        /// 验证属性值
        /// </summary>
        /// <param name="value">属性值</param>
        /// <param name="validationContext">验证器上下文</param>
        /// <returns></returns>
        protected override bool ValidateValue(object value, ValidationContext validationContext)
        {
            if (value == null)
                return false;

            return Regex.IsMatch(value.ToString(), m_regexValue);
        }
    }
}