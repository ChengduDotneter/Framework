using Common.DAL;
using System;
using System.Text.Json.Serialization;

namespace Common.Model
{
    public abstract class ViewModelBase : IEntity
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [LinqToDB.Mapping.PrimaryKey, LinqToDB.Mapping.NotNull]
        [JsonConverter(typeof(ObjectIdConverter))]
        [Validation.NotNull]
        public long ID { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [LinqToDB.Mapping.NotNull, LinqToDB.Mapping.SkipValuesOnUpdate]
        public DateTime CreateTime { set; get; }

        /// <summary>
        /// 创建人ID
        /// </summary>
        [LinqToDB.Mapping.NotNull, LinqToDB.Mapping.SkipValuesOnUpdate]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long CreateUserID { set; get; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime? UpdateTime { set; get; }

        /// <summary>
        /// 修改人ID
        /// </summary>
        [JsonConverter(typeof(ObjectIdNullableConverter))]
        public long? UpdateUserID { set; get; }

        /// <summary>
        /// 是否删除
        /// </summary>
        [LinqToDB.Mapping.NotNull]
        public bool IsDeleted { set; get; }

        public ViewModelBase()
        {
            CreateTime = DateTime.Now;
        }
    }
}