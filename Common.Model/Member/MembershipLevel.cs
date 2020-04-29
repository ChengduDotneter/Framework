using HeadQuartersERP.DAL;
using HeadQuartersERP.Validation;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HeadQuartersERP.Model.Member
{
    /// <summary>
    /// 会员等级实体类
    /// </summary>
    public class MembershipLevel
    {
        /// <summary>
        /// 会员等级
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "会员等级")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "会员等级")]
        public string Level { get; set; }

        /// <summary>
        /// 会员等级ID
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "会员ID")]
        [NotNull]
        [Display(Name = "会员ID")]
        [ForeignKey(typeof(VipInfo), nameof(IEntity.ID))]
        [Foreign(nameof(VipInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long MemberLevelID { get; set; }
    }
}
