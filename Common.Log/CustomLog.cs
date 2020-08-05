using Apache.Ignite.Core.Cache.Configuration;
using Common.Validation;
using SqlSugar;

namespace Common.Log
{
    public class CustomLog : LogViewModelBase
    {
        /// <summary>
        /// 是否错误日志
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "是否错误日志")]
        [QuerySqlField(NotNull = true)]
        public bool IsError { get; set; }

        /// <summary>
        /// 日志
        /// </summary>
        [StringMaxLength(1000)]
        [SugarColumn(IsNullable = false, ColumnDescription = "日志")]
        [QuerySqlField(NotNull = true)]
        public string Message { get; set; }
    }
}
