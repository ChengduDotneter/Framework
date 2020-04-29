using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    public class GreaterThanAttribute : ValidationBaseAttribute
    {
        private object m_greaterThanValue;

        public GreaterThanAttribute(object greaterThanValue) => m_greaterThanValue = greaterThanValue;

        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}必须大于{GetGreaterThanText(validationContext, m_greaterThanValue)}。";

        protected override bool ValidateValue(object value, ValidationContext validationContext)
        {
            if (value == null)
                return false;

            return (int)typeof(CompareHelper).GetMethod(nameof(CompareHelper.Compare)).MakeGenericMethod(value.GetType()).
                Invoke(null, new object[] { value, GetMoreThanValue(validationContext, m_greaterThanValue) }) > 0;
        }

        protected virtual string GetGreaterThanText(ValidationContext validationContext, object greaterThanValue)
        {
            return greaterThanValue.ToString();
        }

        protected virtual object GetMoreThanValue(ValidationContext validationContext, object greaterThanValue)
        {
            return greaterThanValue;
        }
    }
}
