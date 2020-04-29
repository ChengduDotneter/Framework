using HeadQuartersERP.Model.Enums;
using HeadQuartersERP.Validation;
using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace HeadQuartersERP.Model.SystemSetting
{
    /// <summary>
    /// 批量导入记录实体
    /// </summary>
    public class DataImportInfo : ViewModelBase
    {
        /// <summary>
        /// 导入文件存放位置
        /// </summary>
        [SugarColumn(Length = 200, IsNullable = true, ColumnDescription = "导入文件存放位置")]
        [StringMaxLength(200)]
        [Display(Name = "导入文件存放位置")]
        public string ImportFilePathUrl { get; set; }

        /// <summary>
        /// 总共条数
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "总共条数")]
        [NotNull]
        [Display(Name = "总共条数")]
        public int TotalCount { get; set; }

        /// <summary>
        /// 成功导入条数
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "成功导入条数")]
        [NotNull]
        [Display(Name = "成功导入条数")]
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败条数
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "失败条数")]
        [NotNull]
        [Display(Name = "失败条数")]
        public int FailCount { get; set; }

        /// <summary>
        /// 失败文件存放位置
        /// </summary>
        [SugarColumn(Length = 200, IsNullable = true, ColumnDescription = "失败文件存放位置")]
        [StringMaxLength(200)]
        [Display(Name = "失败文件存放位置")]
        public string FailFilePathUrl { get; set; }

        /// <summary>
        /// 导入类型
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "导入类型")]
        [NotNull]
        [Display(Name = "导入类型")]
        [EnumValueExist]
        public ImportDataTypeEnum? ImportDataType { get; set; }
    }
}
