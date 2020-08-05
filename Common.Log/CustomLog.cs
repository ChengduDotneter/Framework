using Apache.Ignite.Core.Cache.Configuration;
using Common.Model;
using Common.Validation;
using SqlSugar;

namespace Common.Log
{
    [IgnoreBuildController(true, true, true, true, true)]
    public class CustomLog : LogViewModelBase
    {
        /// <summary>
        /// 日志
        /// </summary>
        [StringMaxLength(1000)]
        [SugarColumn(IsNullable = false, ColumnDescription = "日志")]
        [QuerySqlField(NotNull = true)]
        public string Message { get; set; }

        /// <summary>
        /// 自定义日志编码
        /// </summary>
        [StringMaxLength(50)]
        [SugarColumn(IsNullable = false, ColumnDescription = "日志")]
        [QuerySqlField(NotNull = true)]
        public string CustomCode { get; set; }
    }
}
