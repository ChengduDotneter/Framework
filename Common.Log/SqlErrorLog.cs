using Apache.Ignite.Core.Cache.Configuration;
using Common.Model;
using Common.Validation;
using SqlSugar;

namespace Common.Log
{
    [IgnoreBuildController(true, true, true, true, true)]
    public class SqlErrorLog : LogViewModelBase
    {
        /// <summary>
        /// Sql执行语句
        /// </summary>
        [StringMaxLength(500)]
        [SugarColumn(IsNullable = false, ColumnDescription = "Sql执行语句")]
        [QuerySqlField(NotNull = true)]
        public string Sql { get; set; }

        /// <summary>
        /// Sql执行参数
        /// </summary>
        [StringMaxLength(500)]
        [SugarColumn(IsNullable = true, ColumnDescription = "Sql执行参数")]
        [QuerySqlField]
        public string Parameters { get; set; }

        /// <summary>
        /// Sql执行报错信息
        /// </summary>
        [StringMaxLength(1000)]
        [SugarColumn(IsNullable = false, ColumnDescription = "调用堆栈")]
        [QuerySqlField(NotNull = true)]
        public string Message { get; set; }
    }
}
