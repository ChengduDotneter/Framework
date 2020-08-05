using Apache.Ignite.Core.Cache.Configuration;
using Common.DAL;
using Common.MessageQueueClient;
using Common.Model;
using Common.Validation;
using SqlSugar;
using System;
using System.Text.Json.Serialization;

namespace Common.Log
{
    public abstract class LogViewModelBase : IEntity, IMQData
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
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 节点ID
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "节点ID")]
        [QuerySqlField(NotNull = true)]
        public int Node { get; set; }

        /// <summary>
        /// 节点类型
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "节点类型")]
        [QuerySqlField(NotNull = true)]
        public int NodeType { get; set; }

        /// <summary>
        /// 调用堆栈
        /// </summary>
        [StringMaxLength(1000)]
        [SugarColumn(IsNullable = true, ColumnDescription = "调用堆栈")]
        [QuerySqlField]
        public string StackTrace { get; set; }

        public LogViewModelBase()
        {
            ID = IDGenerator.NextID();
            CreateTime = DateTime.Now;
        }
    }
}
