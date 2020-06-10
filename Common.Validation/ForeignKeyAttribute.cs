using Common.DAL;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    /// <summary>
    /// 外键验证器特性类
    /// </summary>
    public class ForeignKeyAttribute : ValidationBaseAttribute
    {
        private readonly Type m_foreignTableType;
        private readonly string m_foreignColumn;
        private readonly bool m_filterIsDeleted;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="foreignTableType">外键指向表的实体Type</param>
        /// <param name="foreignColumn">外键所指向的表的指定列</param>
        /// <param name="filterIsDeleted"></param>
        public ForeignKeyAttribute(Type foreignTableType, string foreignColumn, bool filterIsDeleted = true)
        {
            m_foreignTableType = foreignTableType;
            m_foreignColumn = foreignColumn;
            m_filterIsDeleted = filterIsDeleted;
        }

        /// <summary>
        /// 获取验证失败的错误信息
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="propertyName">属性名</param>
        /// <returns></returns>
        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) =>
            $"{m_foreignTableType.Name}中{m_foreignColumn}为{validationContext.ObjectType.GetProperty(validationContext.MemberName).GetValue(validationContext.ObjectInstance)}的数据不存在。";

        //TODO: 外键表Insert时处理方式
        /// <summary>
        /// 验证属性值
        /// </summary>
        /// <param name="value">属性值</param>
        /// <param name="validationContext">验证器上下文</param>
        /// <returns></returns>
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
