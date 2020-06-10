using Common.DAL;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    /// <summary>
    /// 唯一键验证器特性
    /// </summary>
    public class UniqueAttribute : ValidationBaseAttribute
    {
        private readonly bool m_filterIsDeleted;

        /// <summary>
        /// 验证器特性构造方法
        /// </summary>
        /// <param name="filterIsDeleted"></param>
        public UniqueAttribute(bool filterIsDeleted = true) => m_filterIsDeleted = filterIsDeleted;

        /// <summary>
        /// 获取验证失败的错误信息
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="propertyName">属性名</param>
        /// <returns></returns>
        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}不能重复。";

        /// <summary>
        /// 验证属性值
        /// </summary>
        /// <param name="value">属性值</param>
        /// <param name="validationContext">验证器上下文</param>
        /// <returns></returns>
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
                parameters.Add($"@{validationContext.MemberName}", value);
                parameters.Add($"@{nameof(IEntity.ID)}", typeof(IEntity).GetProperty(nameof(IEntity.ID)).GetValue(entity));

                return (int)queryType.GetMethod("Count", new Type[] { typeof(string), typeof(Dictionary<string, object>) }).
                        Invoke(searchQuery, new object[] { sql, parameters }) == 0;
            }

            throw new NotSupportedException();
        }
    }
}
