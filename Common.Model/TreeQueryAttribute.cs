using System;

namespace Common.Model
{
    [AttributeUsage(AttributeTargets.Class)]
    public class LazyLoadedTreeAttribute : Attribute
    {
        /// <summary>
        /// 子节点类型
        /// </summary>
        public Type ChildModelType { get; set; }

        /// <summary>
        /// 展示字段名
        /// </summary>
        public string LabelPropertyName { get; set; }

        /// <summary>
        /// Key值字段名
        /// </summary>
        public string ValuePropertyName { get; set; }

        /// <summary>
        /// 外键字段名
        /// </summary>
        public string ForeignKeyPropertyName { get; set; }

        /// <summary>
        /// 特性构造函数
        /// </summary>
        /// <param name="labelPropertyName"><展示字段名/param>
        /// <param name="valuePropertyName">Key值字段名</param>
        /// <param name="foreignKeyPropertyName">外键字段名</param>
        /// <param name="childModelType">子节点类型</param>
        public LazyLoadedTreeAttribute(string foreignKeyPropertyName, string labelPropertyName = "", string valuePropertyName = "", Type childModelType = null)
        {
            ChildModelType = childModelType;
            LabelPropertyName = labelPropertyName;
            ValuePropertyName = valuePropertyName;
            ForeignKeyPropertyName = foreignKeyPropertyName;
        }
    }
}