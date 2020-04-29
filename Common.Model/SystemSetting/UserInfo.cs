using HeadQuartersERP.DAL;
using HeadQuartersERP.Validation;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HeadQuartersERP.Model.SystemSetting
{
    /// <summary>
    /// 总部ERP系统登录用户实体表
    /// </summary>
    public class UserInfo : ViewModelBase
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "用户名")]
        [NotNull]
        [Unique]
        [StringMaxLength(50)]
        [Display(Name = "用户名")]
        public string UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "密码")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "密码")]
        public string Password { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "姓名")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "姓名")]
        public string PersonName { get; set; }

        /// <summary>
        /// 手机号码
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "手机号码")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "手机号码")]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// 是否禁用
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "是否禁用")]
        [NotNull]
        [Display(Name = "是否禁用")]
        public bool? IsForbidden { get; set; }

        /// <summary>
        /// 所属组织ID
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "所属组织")]
        [NotNull]
        [Display(Name = "所属组织")]
        [ForeignKey(typeof(OrganizationInfo), nameof(IEntity.ID))]
        [Foreign(nameof(OrganizationInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long OrganizationID { get; set; }

        /// <summary>
        /// 所属角色ID
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "所属角色")]
        [NotNull]
        [Display(Name = "所属角色")]
        [ForeignKey(typeof(RoleInfo), nameof(IEntity.ID))]
        [Foreign(nameof(RoleInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long RoleID { get; set; }
    }
}
