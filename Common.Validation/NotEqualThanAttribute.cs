using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    public class NotEqualThanAttribute : ValidationBaseAttribute
    {
        private object m_notEqualThanValue;

        public NotEqualThanAttribute(object notEqualThanValue) => m_notEqualThanValue = notEqualThanValue;

        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}必须不等于{GetNotEqualThanText(validationContext, m_notEqualThanValue)}。";

        protected override bool ValidateValue(object value, ValidationContext validationContext)
        {
            if (value == null)
                return false;

            return (int)typeof(CompareHelper).GetMethod(nameof(CompareHelper.Compare)).MakeGenericMethod(value.GetType()).
                Invoke(null, new object[] { value, GetNotEqualThanValue(validationContext, m_notEqualThanValue) }) != 0;
        }

        protected virtual string GetNotEqualThanText(ValidationContext validationContext, object m_notEqualThanValue)
        {
            return m_notEqualThanValue.ToString();
        }

        protected virtual object GetNotEqualThanValue(ValidationContext validationContext, object m_notEqualThanValue)
        {
            return m_notEqualThanValue;
        }
    }
}
