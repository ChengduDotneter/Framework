using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    /// <summary>
    /// 大于指定值验证器特性类
    /// </summary>
    public class GreaterThanAttribute : ValidationBaseAttribute
    {
        private object m_greaterThanValue;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="greaterThanValue">大于的特性值</param>
        public GreaterThanAttribute(object greaterThanValue) => m_greaterThanValue = greaterThanValue;

        /// <summary>
        /// 获取验证失败的错误信息
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="propertyName">属性名</param>
        /// <returns></returns>
        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}必须大于{GetGreaterThanText(validationContext, m_greaterThanValue)}。";

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

            return (int)typeof(CompareHelper).GetMethod(nameof(CompareHelper.Compare)).MakeGenericMethod(value.GetType()).
                Invoke(null, new object[] { value, GetGreaterThanValue(validationContext, m_greaterThanValue) }) > 0;
        }

        /// <summary>
        /// 获取大于的指定值的文本形式
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="greaterThanValue">指定值</param>
        /// <returns></returns>
        protected virtual string GetGreaterThanText(ValidationContext validationContext, object greaterThanValue) => greaterThanValue.ToString();

        /// <summary>
        /// 获取大于的指定值
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="greaterThanValue">指定值</param>
        /// <returns></returns>
        protected virtual object GetGreaterThanValue(ValidationContext validationContext, object greaterThanValue) => greaterThanValue;
    }
}