using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    public class NotEqualAttribute : ValidationBaseAttribute
    {
        private object m_notEqualValue;

        public NotEqualAttribute(object notEqualValue) => m_notEqualValue = notEqualValue;

        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}不能为{m_notEqualValue}。";

        protected override bool ValidateValue(object value, ValidationContext validationContext)
        {
            return (int)typeof(CompareHelper).GetMethod(nameof(CompareHelper.Compare)).MakeGenericMethod(value.GetType()).Invoke(null, new object[] { value, m_notEqualValue }) != 0;
        }
    }
}
