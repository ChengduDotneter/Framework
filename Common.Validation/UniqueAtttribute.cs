//using Common.DAL;
//using System;
//using System.ComponentModel.DataAnnotations;
//using System.Linq.Expressions;

//TODO:验证器有错误
//namespace Common.Validation
//{
//    /// <summary>
//    /// 唯一键验证器特性
//    /// </summary>
//    public class UniqueAttribute : ValidationBaseAttribute
//    {
//        private readonly bool m_filterIsDeleted;

//        /// <summary>
//        /// 验证器特性构造方法
//        /// </summary>
//        /// <param name="filterIsDeleted"></param>
//        public UniqueAttribute(bool filterIsDeleted = true) => m_filterIsDeleted = filterIsDeleted;

//        /// <summary>
//        /// 获取验证失败的错误信息
//        /// </summary>
//        /// <param name="validationContext">验证器上下文</param>
//        /// <param name="propertyName">属性名</param>
//        /// <returns></returns>
//        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}不能重复。";

//        /// <summary>
//        /// 验证属性值
//        /// </summary>
//        /// <param name="value">属性值</param>
//        /// <param name="validationContext">验证器上下文</param>
//        /// <returns></returns>
//        protected override bool ValidateValue(object value, ValidationContext validationContext)
//        {
//            if (validationContext.ObjectInstance is IEntity entity)
//            {
//                if (value == null)
//                    return true;

//                Type queryType = typeof(ISearchQuery<>).MakeGenericType(validationContext.ObjectType);
//                object searchQuery = validationContext.GetService(queryType);

//                ParameterExpression parameter = Expression.Parameter(validationContext.ObjectType, "item");
//                Expression equal = Expression.Equal(Expression.Property(parameter, validationContext.MemberName), Expression.Constant(value));
//                Expression notThis = Expression.NotEqual(Expression.Property(parameter, nameof(IEntity.ID)), Expression.Constant(entity.ID));
//                Expression unique = Expression.And(equal, notThis);
//                Expression isDeleted = Expression.Equal(Expression.Property(parameter, "IsDeleted"), Expression.Constant(false));

//                if (m_filterIsDeleted)
//                    unique = Expression.And(isDeleted, unique);

//                Expression predicate = Expression.Lambda(unique, parameter);

//                return (int)typeof(ISearchQuery<>).MakeGenericType(validationContext.ObjectType).GetMethod("Count", new Type[] { typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(validationContext.ObjectType, typeof(bool))), typeof(IDBResourceContent) }).Invoke(searchQuery, new object[] { predicate, null }) == 0;
//            }

//            throw new NotSupportedException();
//        }
//    }
//}