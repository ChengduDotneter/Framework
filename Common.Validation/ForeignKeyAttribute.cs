using System;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Common.DAL;

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

                ParameterExpression parameter = Expression.Parameter(m_foreignTableType, "item");
                Expression equal = Expression.Equal(Expression.Property(parameter, m_foreignColumn), Expression.Constant(value));
                Expression isDeleted = Expression.Equal(Expression.Property(parameter, "IsDeleted"), Expression.Constant(false));

                if (m_filterIsDeleted)
                    equal = Expression.And(isDeleted, equal);

                Expression predicate = Expression.Lambda(equal, parameter);

                return (int)typeof(ISearchQuery<>).MakeGenericType(m_foreignTableType).GetMethod("Count", new Type[] { typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(m_foreignTableType, typeof(bool))), typeof(ITransaction) }).Invoke(searchQuery, new object[] { predicate, null }) > 0;
            }

            throw new NotImplementedException();
        }
    }
}