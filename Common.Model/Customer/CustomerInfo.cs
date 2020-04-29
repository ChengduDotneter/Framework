using HeadQuartersERP.DAL;
using HeadQuartersERP.Model.Company;
using HeadQuartersERP.Model.Enums;
using HeadQuartersERP.Validation;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HeadQuartersERP.Model.Customer
{
    /// <summary>
    /// 客户实体类
    /// </summary>
    public class CustomerInfo : ViewModelBase
    {
        /// <summary>
        /// 客户名称
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "客户名称")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "客户名称")]
        public string CustomerName { get; set; }

        /// <summary>
        /// 客户代码
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "客户代码")]
        [NotNull]
        [StringMaxLength(50)]
        [Unique]
        [Display(Name = "客户代码")]
        public string CustomerCode { get; set; }

        /// <summary>
        /// 授权商品数量
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "授权商品数量")]
        [NotNull]
        [Display(Name = "授权商品数量")]
        public int? EmpowerWaresCount { get; set; }

        /// <summary>
        /// 业务员
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "业务员")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "业务员")]
        public string Salesman { get; set; }

        /// <summary>
        /// 客户类型
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "客户类型")]
        [NotNull]
        [EnumValueExist]
        [Display(Name = "客户类型")]
        public CustomerTypeEnum? CustomerType { get; set; }

        /// <summary>
        /// 客户公司
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "客户公司")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "客户公司")]
        public string CustomerCompany { get; set; }

        /// <summary>
        /// 联系人
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "联系人")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "联系人")]
        public string ContactName { get; set; }

        /// <summary>
        /// 联系人电话
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "联系人电话")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "联系人电话")]
        public string ContactPhone { get; set; }

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

        /// <summary>
        /// 地址
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "地址")]
        [StringMaxLength(50)]
        [Display(Name = "区CODE")]
        public string Address { get; set; }

        /// <summary>
        /// 是否禁用
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "是否禁用")]
        [NotNull]
        [Display(Name = "是否禁用")]
        public bool? IsForbidden { get; set; }

        /// <summary>
        /// 所属公司ID
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "所属公司")]
        [NotNull]
        [Display(Name = "所属公司ID")]
        [ForeignKey(typeof(CompanyInfo), nameof(IEntity.ID))]
        [Foreign(nameof(CompanyInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long AffiliatedCompanyID { get; set; }

    }
}
