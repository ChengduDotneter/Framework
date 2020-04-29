using HeadQuartersERP.Validation;
using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace HeadQuartersERP.Model.SystemSetting
{
    /// <summary>
    /// 角色实体表
    /// </summary>
    public class RoleInfo : ViewModelBase
    {
        /// <summary>
        /// 角色名称
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "角色名称")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "角色名称")]
        public string RoleName { get; set; }

        /// <summary>
        /// 是否禁用
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "是否禁用")]
        [NotNull]
        [Display(Name = "是否禁用")]
        public bool? IsForbidden { get; set; }

        /// <summary>
        /// 权限名称
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "权限名称")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "权限名称")]
        public string PowerName { get; set; }
    }
}
