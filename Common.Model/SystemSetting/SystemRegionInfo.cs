using HeadQuartersERP.Validation;
using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace HeadQuartersERP.Model.SystemSetting
{
    /// <summary>
    /// 区域实体
    /// </summary>
    public class SystemRegionInfo : ViewModelBase
    {
        /// <summary>
        /// 区域名称
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "区域名称")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "区域名称")]
        public string RegionName { get; set; }

        /// <summary>
        /// 区域代码
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "区域代码")]
        [NotNull]
        [StringMaxLength(50)]
        [Unique]
        [Display(Name = "区域代码")]
        public string RegionCode { get; set; }

        /// <summary>
        /// 上级区域
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "上级区域")]
        [StringMaxLength(50)]
        [Display(Name = "上级区域")]
        public string ParentCode { get; set; }

        /// <summary>
        /// 区域等级
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "区域等级")]
        [NotNull]
        [Display(Name = "区域等级")]
        public int ParentLevel { get; set; }

    }
}
