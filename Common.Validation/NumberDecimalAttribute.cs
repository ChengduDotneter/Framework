using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    /// <summary>
    /// 小数精度验证器特性类
    /// </summary>
    public class NumberDecimalAttribute : ValidationBaseAttribute
    {
        private int m_decimal;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="decimal">精度</param>
        public NumberDecimalAttribute(int @decimal) => m_decimal = @decimal;

        /// <summary>
        /// 获取验证失败的错误信息
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="propertyName">属性名</param>
        /// <returns></returns>
        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}超出数值精度，数值精度为{m_decimal}位小数。";

        /// <summary>
        /// 验证属性值
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override bool ValidateValue(object value, ValidationContext validationContext)
        {
            if (value == null)
                return true;

            string stringValue = value.ToString();
            var decimalsLength = stringValue.IndexOf(".") > -1 ? stringValue.Length - stringValue.IndexOf(".") - 1 : 0;
            return decimalsLength <= m_decimal;
        }
    }
}
