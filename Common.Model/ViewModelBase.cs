using Common.DAL;
using Common.Validation;
using System;
using System.Text.Json.Serialization;

namespace Common.Model
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public abstract class ViewModelBase : IEntity
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [LinqToDB.Mapping.Column(IsPrimaryKey = true)]
        [MongoDB.Bson.Serialization.Attributes.BsonId]
        [JsonConverter(typeof(ObjectIdConverter))]
        [NotNull]
        public long ID { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [LinqToDB.Mapping.Column(SkipOnUpdate = true)]
        [JsonConverter(typeof(DateTimeConverter))]
        public DateTime CreateTime { set; get; }

        /// <summary>
        /// 创建人ID
        /// </summary>
        [LinqToDB.Mapping.Column(SkipOnUpdate = true)]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long CreateUserID { set; get; }

        /// <summary>
        /// 修改时间
        /// </summary>
        [LinqToDB.Mapping.Column(CanBeNull = true)]
        [JsonConverter(typeof(DateTimeNullableConverter))]
        public DateTime? UpdateTime { set; get; }

        /// <summary>
        /// 修改人ID
        /// </summary>
        [JsonConverter(typeof(ObjectIdNullableConverter))]
        [LinqToDB.Mapping.Column(CanBeNull = true)]
        public long? UpdateUserID { set; get; }

        /// <summary>
        /// 是否删除
        /// </summary>
        [LinqToDB.Mapping.Column(CanBeNull = false)]
        public bool IsDeleted { set; get; }
    }
}