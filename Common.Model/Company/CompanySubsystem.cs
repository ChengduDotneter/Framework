using HeadQuartersERP.DAL;
using HeadQuartersERP.Model.Enums;
using HeadQuartersERP.Validation;
using SqlSugar;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace HeadQuartersERP.Model.Company
{
    /// <summary>
    /// 公司子系统信息实体类
    /// </summary>
    [LinqSearch(typeof(CompanySubsystem), nameof(GetSearchLinq))]
    public class CompanySubsystem : ViewModelBase
    {
        /// <summary>
        /// 子系统APPID
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "子系统APPID")]
        [NotNull]
        [Unique]
        [Display(Name = "子系统APPID")]
        public string AppID { get; set; }

        /// <summary>
        /// 子系统调用地址
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "子系统调用地址")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "子系统调用地址")]
        public string AppUrl { get; set; }

        /// <summary>
        /// 子系统秘钥
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "子系统秘钥")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "子系统秘钥")]
        public string AppSecret { get; set; }

        /// <summary>
        /// 子系统类型
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "子系统类型")]
        [NotNull]
        [EnumValueExist]
        [Display(Name = "子系统类型")]
        public CompanyBussinessTypeEnum? BussinessType { get; set; }

        /// <summary>
        /// 公司ID
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "公司外键")]
        [Display(Name = "公司外键")]
        [ForeignKey(typeof(CompanyInfo), nameof(IEntity.ID))]
        [Foreign(nameof(CompanyInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdNullableConverter))]
        public long? CompanyID { get; set; }

        private static Func<CompanySubsystem, Expression<Func<CompanySubsystem, bool>>> GetSearchLinq()
        {
            return parameter =>
            {
                if (parameter?.CompanyID.HasValue ?? false)
                    return companySubsystem => companySubsystem.CompanyID == parameter.CompanyID.Value;

                else
                    return companyInfo => true;
            };
        }
    }
}
