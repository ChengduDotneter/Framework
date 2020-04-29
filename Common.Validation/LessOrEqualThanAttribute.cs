using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    public class LessOrEqualThanAttribute : ValidationBaseAttribute
    {
        private object m_lessThanValue;

        public LessOrEqualThanAttribute(object lessThanValue) => m_lessThanValue = lessThanValue;

        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}必须小于或等于{GetLessThanText(validationContext, m_lessThanValue)}。";

        protected override bool ValidateValue(object value, ValidationContext validationContext)
        {
            if (value == null)
                return true;

            return (int)typeof(CompareHelper).GetMethod(nameof(CompareHelper.Compare)).MakeGenericMethod(value.GetType()).
                Invoke(null, new object[] { value, GetLessThanValue(validationContext, m_lessThanValue) }) <= 0;
        }

        protected virtual string GetLessThanText(ValidationContext validationContext, object lessThanValue)
        {
            return lessThanValue.ToString();
        }

        protected virtual object GetLessThanValue(ValidationContext validationContext, object lessThanValue)
        {
            return lessThanValue;
        }
    }
}
