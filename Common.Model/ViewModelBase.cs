using System;
using System.Text.Json.Serialization;
using Apache.Ignite.Core.Cache.Configuration;
using Common.DAL;
using Common.Validation;
using SqlSugar;

namespace Common.Model
{
    public abstract class ViewModelBase : IEntity
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [SugarColumn(IsNullable = false, IsPrimaryKey = true, ColumnDescription = "主键ID")]
        [QuerySqlField(IsIndexed = true, NotNull = true)]
        [JsonConverter(typeof(ObjectIdConverter))]
        [NotNull]
        public long ID { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [SugarColumn(IsNullable = false, IsOnlyIgnoreUpdate = true, ColumnDescription = "创建时间")]
        [QuerySqlField(NotNull = true)]
        public DateTime CreateTime { set; get; }

        /// <summary>
        /// 创建人ID
        /// </summary>
        [SugarColumn(IsNullable = false, IsOnlyIgnoreUpdate = true, ColumnDescription = "创建人ID")]
        [QuerySqlField(NotNull = true)]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long CreateUserID { set; get; }

        /// <summary>
        /// 修改时间
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "修改时间")]
        [QuerySqlField]
        public DateTime? UpdateTime { set; get; }

        /// <summary>
        /// 修改人ID
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "修改人ID")]
        [QuerySqlField]
        [JsonConverter(typeof(ObjectIdNullableConverter))]
        public long? UpdateUserID { set; get; }

        /// <summary>
        /// 是否删除
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "是否删除",IndexGroupNameList = new string[] { "INDEX_ISDELETED" })]
        [QuerySqlField]
        public bool IsDeleted { set; get; }

        public ViewModelBase()
        {
            CreateTime = DateTime.Now;
        }
    }
}
