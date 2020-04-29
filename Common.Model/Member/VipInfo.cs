using HeadQuartersERP.DAL;
using HeadQuartersERP.Model.Company;
using HeadQuartersERP.Model.Enums;
using HeadQuartersERP.Validation;
using SqlSugar;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HeadQuartersERP.Model.Member
{
    /// <summary>
    /// 会员实体类
    /// </summary>
    public class VipInfo : ViewModelBase
    {
        /// <summary>
        /// 会员手机号
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "会员手机号")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "会员手机号")]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// 会员名称
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "会员名称")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "会员名称")]
        public string MemberName { get; set; }

        /// <summary>
        /// 会员身份证号
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "会员身份证号")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "会员身份证号")]
        public string MemberID { get; set; }

        /// <summary>
        /// 会员性别
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "会员性别")]
        [NotNull]
        [EnumValueExist]
        [Display(Name = "会员性别")]
        public SexEnum? Sex { get; set; }

        /// <summary>
        /// 会员积分
        /// </summary>
        [SugarColumn(DecimalDigits = 4, IsNullable = false, ColumnDescription = "会员积分")]
        [NotNull]
        [NumberDecimal(4)]
        [Display(Name = "会员积分")]
        public decimal? Integral { get; set; }

        /// <summary>
        /// 会员余额
        /// </summary>
        [SugarColumn(DecimalDigits = 4, IsNullable = false, ColumnDescription = "会员余额")]
        [NotNull]
        [NumberDecimal(4)]
        [Display(Name = "会员余额")]
        public decimal? Balance { get; set; }

        /// <summary>
        /// 会员生日
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "会员生日")]
        [NotNull]
        [Display(Name = "会员生日")]
        public DateTime? MemberBirthDay { get; set; }

        /// <summary>
        /// 注册时间
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "注册时间")]
        [NotNull]
        [Display(Name = "注册时间")]
        public DateTime? RegistrationTime { get; set; }

        /// <summary>
        /// 到期时间
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "到期时间")]
        [NotNull]
        [Display(Name = "到期时间")]
        public DateTime? DueTime { get; set; }

        /// <summary>
        /// 是否禁用
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "是否禁用")]
        [NotNull]
        [Display(Name = "是否禁用")]
        public bool? IsForbidden { get; set; }

        /// <summary>
        /// 会员来源,所属公司
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "会员所属公司")]
        [NotNull]
        [Display(Name = "会员所属公司")]
        [ForeignKey(typeof(CompanyInfo), nameof(IEntity.ID))]
        [Foreign(nameof(CompanyInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long CompanyID { get; set; }

        /// <summary>
        /// 省CODE
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "省CODE")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "省CODE")]
        public string ProvinceCode { get; set; }

        /// <summary>
        /// 市CODE
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "市CODE")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "市CODE")]
        public string CityCode { get; set; }

        /// <summary>
        /// 区CODE
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "区CODE")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "区CODE")]
        public string AreaCode { get; set; }

    }
}
