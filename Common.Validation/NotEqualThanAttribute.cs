using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    /// <summary>
    /// 不等于值验证器特性类
    /// </summary>
    public class NotEqualThanAttribute : ValidationBaseAttribute
    {
        private object m_notEqualThanValue;

        /// <summary>
        /// 验证器特性构造函数
        /// </summary>
        /// <param name="notEqualThanValue">不等于的指定值</param>
        /// <param name="ignorePredeciteFunction"></param>
        public NotEqualThanAttribute(object notEqualThanValue, string ignorePredeciteFunction = null) : base(ignorePredeciteFunction) => m_notEqualThanValue = notEqualThanValue;

        /// <summary>
        /// 获取验证失败的错误信息
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="propertyName">属性值</param>
        /// <returns></returns>
        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}必须不等于{GetNotEqualThanText(validationContext, m_notEqualThanValue)}。";

        /// <summary>
        /// 验证属性值
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override bool ValidateValue(object value, ValidationContext validationContext)
        {
            if (value == null)
                return false;

            return (int)typeof(CompareHelper).GetMethod(nameof(CompareHelper.Compare)).MakeGenericMethod(value.GetType()).
                Invoke(null, new object[] { value, GetNotEqualThanValue(validationContext, m_notEqualThanValue) }) != 0;
        }

        /// <summary>
        /// 获取不等于的指定值的文本形式
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="notEqualThanValue">指定值</param>
        /// <returns></returns>
        protected virtual string GetNotEqualThanText(ValidationContext validationContext, object notEqualThanValue)
        {
            return m_notEqualThanValue.ToString();
        }

        /// <summary>
        /// 获取不等于的指定值
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="notEqualThanValue">指定值</param>
        /// <returns></returns>
        protected virtual object GetNotEqualThanValue(ValidationContext validationContext, object notEqualThanValue)
        {
            return notEqualThanValue;
        }
    }
}