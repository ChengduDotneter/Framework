using System;
using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    /// <summary>
    /// 小于指定值验证器特性类
    /// </summary>
    public class LessThanAttribute : ValidationBaseAttribute
    {
        private object m_lessThanValue;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="lessThanValue">小于的指定值</param>
        /// <param name="ignorePredeciteFunction"></param>
        public LessThanAttribute(object lessThanValue, string ignorePredeciteFunction = null) : base(ignorePredeciteFunction) => m_lessThanValue = lessThanValue;

        /// <summary>
        /// 获取验证失败的错误信息
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="propertyName">属性名</param>
        /// <returns></returns>
        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}必须小于{GetLessThanText(validationContext, m_lessThanValue)}。";

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
                Invoke(null, new object[] { value, GetLessThanValue(validationContext, m_lessThanValue) }) < 0;
        }

        /// <summary>
        /// 获取小于指定值的文本格式
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="lessThanValue">小于的指定值</param>
        /// <returns></returns>
        protected virtual string GetLessThanText(ValidationContext validationContext, object lessThanValue)
        {
            return lessThanValue.ToString();
        }

        /// <summary>
        /// 获取小于的值
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="lessThanValue">小于的指定值</param>
        /// <returns></returns>
        protected virtual object GetLessThanValue(ValidationContext validationContext, object lessThanValue)
        {
            return lessThanValue;
        }
    }
}