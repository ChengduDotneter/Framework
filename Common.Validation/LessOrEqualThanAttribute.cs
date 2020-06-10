using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    /// <summary>
    /// 小于等于指定值的验证器特性类
    /// </summary>
    public class LessOrEqualThanAttribute : ValidationBaseAttribute
    {
        private object m_lessOrEqualThanValue;

        /// <summary>
        /// 验证器特性构造函数
        /// </summary>
        /// <param name="lessOrEqualThanValue">小于等于的指定值</param>
        public LessOrEqualThanAttribute(object lessOrEqualThanValue) => m_lessOrEqualThanValue = lessOrEqualThanValue;

        /// <summary>
        /// 获取验证失败的错误信息
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="propertyName">属性名</param>
        /// <returns></returns>
        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}必须小于或等于{GetLessOrEqualThanText(validationContext, m_lessOrEqualThanValue)}。";

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
                Invoke(null, new object[] { value, GetLessOrEqualThanValue(validationContext, m_lessOrEqualThanValue) }) <= 0;
        }

        /// <summary>
        /// 获取小于等于的指定值的文本格式
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="lessOrEqualThanValue">小于等于的指定值的文本形式</param>
        /// <returns></returns>
        protected virtual string GetLessOrEqualThanText(ValidationContext validationContext, object lessOrEqualThanValue)
        {
            return lessOrEqualThanValue.ToString();
        }

        /// <summary>
        /// 获取小于等于的指定值
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="lessOrEqualThanValue">小于等于的指定值</param>
        /// <returns></returns>
        protected virtual object GetLessOrEqualThanValue(ValidationContext validationContext, object lessOrEqualThanValue)
        {
            return lessOrEqualThanValue;
        }
    }
}