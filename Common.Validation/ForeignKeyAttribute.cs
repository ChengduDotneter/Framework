using Common.DAL;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    public class ForeignKeyAttribute : ValidationBaseAttribute
    {
        private readonly Type m_foreignTableType;
        private readonly string m_foreignColumn;
        private readonly bool m_filterIsDeleted;

        public ForeignKeyAttribute(Type foreignTableType, string foreignColumn, bool filterIsDeleted = true)
        {
            m_foreignTableType = foreignTableType;
            m_foreignColumn = foreignColumn;
            m_filterIsDeleted = filterIsDeleted;
        }

        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) =>
            $"{m_foreignTableType.Name}中{m_foreignColumn}为{validationContext.ObjectType.GetProperty(validationContext.MemberName).GetValue(validationContext.ObjectInstance)}的数据不存在。";

        //TODO: 外键表Insert时处理方式
        protected override bool ValidateValue(object value, ValidationContext validationContext)
        {
            if (validationContext.ObjectInstance is IEntity)
            {
                if (value == null)
                    return true;

                Type queryType = typeof(ISearchQuery<>).MakeGenericType(m_foreignTableType);
                object searchQuery = validationContext.GetService(queryType);
                string sql = string.Format("{0} = @{0} {1}", m_foreignColumn,
                                                              m_filterIsDeleted ? " AND IsDeleted = 0 " : string.Empty);

                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters.Add($"@{m_foreignColumn}", value);

                return (int)queryType.GetMethod("Count", new Type[] { typeof(string),typeof(Dictionary<string, object>) }).
                    Invoke(searchQuery, new object[] { sql, parameters }) > 0;
            }

            throw new NotImplementedException();
        }
    }
}
