using System;
using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    /// <summary>
    /// 不等于验证器特性类
    /// </summary>
    [Obsolete("已过期，请使用NotEqualThanAttribute")]
    public class NotEqualAttribute : ValidationBaseAttribute
    {
        private object m_notEqualValue;

        /// <summary>
        /// 验证器特性构造函数
        /// </summary>
        /// <param name="notEqualValue">不等于的值</param>
        /// <param name="ignorePredeciteFunction"></param>
        public NotEqualAttribute(object notEqualValue, string ignorePredeciteFunction = null) : base(ignorePredeciteFunction) => m_notEqualValue = notEqualValue;

        /// <summary>
        /// 获取验证失败的错误信息
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="propertyName">属性名</param>
        /// <returns></returns>
        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}不能为{m_notEqualValue}。";

        /// <summary>
        /// 验证属性值
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override bool ValidateValue(object value, ValidationContext validationContext)
        {
            return (int)typeof(CompareHelper).GetMethod(nameof(CompareHelper.Compare)).MakeGenericMethod(value.GetType()).Invoke(null, new object[] { value, m_notEqualValue }) != 0;
        }
    }
}