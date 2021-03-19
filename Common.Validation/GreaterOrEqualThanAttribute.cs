using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    /// <summary>
    /// 大于等于指定值的验证器特性类
    /// </summary>
    public class GreaterOrEqualThanAttribute : ValidationBaseAttribute
    {
        private object m_greaterOrEqualThanValue;

        /// <summary>
        /// 验证器构造函数
        /// </summary>
        /// <param name="greaterOrEqualThanValue">大于等于的指定值</param>
        /// <param name="ignorePredeciteFunction"></param>
        public GreaterOrEqualThanAttribute(object greaterOrEqualThanValue, string ignorePredeciteFunction = null) : base(ignorePredeciteFunction) => m_greaterOrEqualThanValue = greaterOrEqualThanValue;

        /// <summary>
        /// 获取验证失败的错误信息
        /// </summary>
        /// <param name="validationContext">验证器赏析文</param>
        /// <param name="propertyName">属性名</param>
        /// <returns></returns>
        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}必须大于或等于{GetGreaterOrEqualThanText(validationContext, m_greaterOrEqualThanValue)}。";

        /// <summary>
        /// 验证属性值
        /// </summary>
        /// <param name="value">属性值</param>
        /// <param name="validationContext">验证器上下文</param>
        /// <returns></returns>
        protected override bool ValidateValue(object value, ValidationContext validationContext)
        {
            if (value == null)
                return true;

            return (int)typeof(CompareHelper).GetMethod(nameof(CompareHelper.Compare)).MakeGenericMethod(value.GetType()).
                Invoke(null, new object[] { value, GetGreaterOrEqualThanValue(validationContext, m_greaterOrEqualThanValue) }) >= 0;
        }

        /// <summary>
        /// 获取大于等于的指定值的文本形式
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="greaterOrEqualThanValue">大于等于的指定值</param>
        /// <returns></returns>
        protected virtual string GetGreaterOrEqualThanText(ValidationContext validationContext, object greaterOrEqualThanValue)
        {
            return greaterOrEqualThanValue.ToString();
        }

        /// <summary>
        /// 获取大于等于的指定值
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="greaterOrEqualThanValue">大于等于的指定值</param>
        /// <returns></returns>
        protected virtual object GetGreaterOrEqualThanValue(ValidationContext validationContext, object greaterOrEqualThanValue)
        {
            return greaterOrEqualThanValue;
        }
    }
}