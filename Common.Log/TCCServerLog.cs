using Apache.Ignite.Core.Cache.Configuration;
using Common.Model;
using Common.Validation;
using Newtonsoft.Json;
using SqlSugar;

namespace Common.Log
{
    [IgnoreBuildController(true, true, true, true, true)]
    public class TCCServerLog : LogViewModelBase
    {
        /// <summary>
        /// Sql执行语句
        /// </summary>
        [JsonConverter(typeof(ObjectIdConverter))]
        [SugarColumn(IsNullable = false, ColumnDescription = "Sql执行语句")]
        [QuerySqlField(NotNull = true)]
        public long TranscationID { get; set; }

        /// <summary>
        /// 日志
        /// </summary>
        [StringMaxLength(1000)]
        [SugarColumn(IsNullable = false, ColumnDescription = "日志")]
        [QuerySqlField(NotNull = true)]
        public string Message { get; set; }
    }
}
