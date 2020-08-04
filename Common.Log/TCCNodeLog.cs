using Apache.Ignite.Core.Cache.Configuration;
using Common.Model;
using Common.Validation;
using SqlSugar;
using System.Text.Json.Serialization;

namespace Common.Log
{
    public class TCCNodeLog : LogViewModelBase
    {
        /// <summary>
        /// TCC事务ID
        /// </summary>
        [JsonConverter(typeof(ObjectIdConverter))]
        [SugarColumn(IsNullable = false, ColumnDescription = "TCC事务ID")]
        [QuerySqlField(NotNull = true)]
        public long TranscationID { get; set; }

        /// <summary>
        /// 是否出现错误
        /// </summary>
        [JsonConverter(typeof(BoolNullableConverter))]
        [SugarColumn(IsNullable = true, ColumnDescription = "Sql执行语句")]
        [QuerySqlField]
        public bool? IsError { get; set; }

        /// <summary>
        /// 日志
        /// </summary>
        [StringMaxLength(1000)]
        [SugarColumn(IsNullable = true, ColumnDescription = "日志")]
        [QuerySqlField]
        public string Message { get; set; }
    }
}
