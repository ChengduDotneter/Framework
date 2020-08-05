using Apache.Ignite.Core.Cache.Configuration;
using Common.Model;
using Common.Validation;
using SqlSugar;

namespace Common.Log
{
    [IgnoreBuildController(true, true, true, true, true)]
    public class ErrorLog : LogViewModelBase
    {
        /// <summary>
        /// 请求路径
        /// </summary>
        [StringMaxLength(500)]
        [SugarColumn(IsNullable = false, ColumnDescription = "请求路径")]
        [QuerySqlField(NotNull = true)]
        public string Path { get; set; }

        /// <summary>
        /// 请求方式
        /// </summary>
        [StringMaxLength(50)]
        [SugarColumn(IsNullable = false, ColumnDescription = "请求方式")]
        [QuerySqlField(NotNull = true)]
        public string Methed { get; set; }

        /// <summary>
        /// 请求参数
        /// </summary>
        [StringMaxLength(500)]
        [SugarColumn(IsNullable = true, ColumnDescription = "请求参数")]
        [QuerySqlField]
        public string Parameters { get; set; }

        /// <summary>
        /// 报错信息
        /// </summary>
        [StringMaxLength(500)]
        [SugarColumn(IsNullable = true, ColumnDescription = "报错信息")]
        [QuerySqlField]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 接口组名称
        /// </summary>
        [StringMaxLength(100)]
        [SugarColumn(IsNullable = false, ColumnDescription = "接口组名称")]
        [QuerySqlField(NotNull = true)]
        public string ControllerName { get; set; }

        /// <summary>
        /// 接口状态编码
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "接口状态编码")]
        [QuerySqlField(NotNull = true)]
        public int StatusCode { get; set; }
    }
}
