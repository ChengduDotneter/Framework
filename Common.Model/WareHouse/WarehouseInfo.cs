using HeadQuartersERP.DAL;
using HeadQuartersERP.Model.Company;
using HeadQuartersERP.Model.Enums;
using HeadQuartersERP.Validation;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HeadQuartersERP.Model.WareHouse
{
    /// <summary>
    /// 仓库实体
    /// </summary>
    public class WarehouseInfo : ViewModelBase
    {
        /// <summary>
        /// 仓库名称
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "仓库名称")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "仓库名称")]
        public string WarehouseName { get; set; }

        /// <summary>
        /// 仓库编码
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "仓库编码")]
        [NotNull]
        [Unique]
        [StringMaxLength(50)]
        [Display(Name = "仓库编码")]
        public string WarehouseCode { get; set; }

        /// <summary>
        /// 仓库类型
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "仓库类型")]
        [NotNull]
        [Display(Name = "仓库类型")]
        [EnumValueExist]
        public WarehouseTypeEnum? WarehouseType { get; set; }

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
        /// 详细地址
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "详细地址")]
        [StringMaxLength(50)]
        [Display(Name = "详细地址")]
        public string DetailAddress { get; set; }

        /// <summary>
        /// 所属公司ID
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "所属公司")]
        [Display(Name = "所属公司")]
        [ForeignKey(typeof(CompanyInfo), nameof(IEntity.ID))]
        [Foreign(nameof(CompanyInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long AffiliatedCompanyID { get; set; }

        /// <summary>
        /// 库存商品总数量
        /// </summary>
        [SugarColumn(IsNullable = false, DecimalDigits = 4, ColumnDescription = "库存商品数量")]
        [NotNull]
        [NumberDecimal(4)]
        [Display(Name = "库存商品数量")]
        public decimal? WareHouseStocks { get; set; }
    }
}
