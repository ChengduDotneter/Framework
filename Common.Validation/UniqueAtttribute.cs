using Common.DAL;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    public class UniqueAttribute : ValidationBaseAttribute
    {
        private readonly bool m_filterIsDeleted;

        public UniqueAttribute(bool filterIsDeleted = true) => m_filterIsDeleted = filterIsDeleted;

        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}不能重复。";

        protected override bool ValidateValue(object value, ValidationContext validationContext)
        {
            if (validationContext.ObjectInstance is IEntity entity)
            {
                Type queryType = typeof(ISearchQuery<>).MakeGenericType(validationContext.ObjectType);
                object searchQuery = validationContext.GetService(queryType);
                string sql = string.Format("{0} = @{0} AND {1} <> @{1} {2}", validationContext.MemberName,
                                                                               nameof(IEntity.ID),
                                                                               m_filterIsDeleted ? " AND IsDeleted = 0 " : string.Empty);

                Dictionary<string, object> parameters = new Dictionary<string, object>();
                parameters.Add(validationContext.MemberName, value);
                parameters.Add(nameof(IEntity.ID), typeof(IEntity).GetProperty(nameof(IEntity.ID)).GetValue(entity));

                return (int)queryType.GetMethod("Count", new Type[] { typeof(string) }).
                        Invoke(searchQuery, new object[] { sql, parameters }) == 0;
            }

            throw new NotSupportedException();
        }
    }
}
