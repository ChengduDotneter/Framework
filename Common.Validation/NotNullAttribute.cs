using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    /// <summary>
    /// 不为空验证器特性类
    /// </summary>
    public class NotNullAttribute : ValidationBaseAttribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public NotNullAttribute() { }

        /// <summary>
        /// 获取验证失败的错误信息
        /// </summary>
        /// <param name="validationContext"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}不能为空。";

        /// <summary>
        /// 验证属性值
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override bool ValidateValue(object value, ValidationContext validationContext)
        {
            if (value != null && value is string stringValue)
                return !string.IsNullOrWhiteSpace(stringValue);

            return value != null;
        }
    }
}
