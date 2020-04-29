using HeadQuartersERP.Validation;
using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace HeadQuartersERP.Model.SystemSetting
{
    /// <summary>
    /// 组织实体表
    /// </summary>
    public class OrganizationInfo : ViewModelBase
    {
        /// <summary>
        /// 组织名称
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "组织名称")]
        [NotNull]
        [Unique]
        [StringMaxLength(50)]
        [Display(Name = "组织名称")]
        public string OrganizationName { get; set; }

        /// <summary>
        /// 上级组织名称
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "上级组织名称")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "上级组织名称")]
        public string UpLevelName { get; set; }
    }
}
