using HeadQuartersERP.DAL;
using HeadQuartersERP.Validation;
using SqlSugar;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HeadQuartersERP.Model.Commodity
{
    /// <summary>
    /// SKU商品列表
    /// </summary>
    public class SKUInfo : ViewModelBase
    {
        /// <summary>
        /// SKU自编码
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "SKU自编码")]
        [NotNull]
        [StringMaxLength(50)]
        [Unique]
        [Display(Name = "SKU自编码")]
        public string SKUCode { get; set; }

        /// <summary>
        /// 国际条码
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "国际条码")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "国际条码")]
        public string BarCode { get; set; }

        /// <summary>
        /// 保质期（天）
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "保质期（天）")]
        [Display(Name = "保质期（天）")]
        public int? QualityGuaranteeDate { get; set; }

        /// <summary>
        /// 生产日期
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "生产日期")]
        [Display(Name = "生产日期")]
        public DateTime? ProductionDate { get; set; }

        /// <summary>
        /// 重量
        /// </summary>
        [SugarColumn(DecimalDigits = 4, IsNullable = true, ColumnDescription = "重量")]
        [NumberDecimal(4)]
        [Display(Name = "重量")]
        public decimal? Weight { get; set; }

        /// <summary>
        /// 体积
        /// </summary>
        [SugarColumn(DecimalDigits = 4, IsNullable = true, ColumnDescription = "体积")]
        [NumberDecimal(4)]
        [Display(Name = "体积")]
        public decimal? Volume { get; set; }

        /// <summary>
        /// 市场价
        /// </summary>
        [SugarColumn(DecimalDigits = 4, IsNullable = true, ColumnDescription = "市场价")]
        [NumberDecimal(4)]
        [Display(Name = "市场价")]
        public decimal? MarketPrice { get; set; }

        /// <summary>
        /// 零售价
        /// </summary>
        [SugarColumn(DecimalDigits = 4, IsNullable = true, ColumnDescription = "零售价")]
        [NumberDecimal(4)]
        [Display(Name = "零售价")]
        public decimal? RetailPrice { get; set; }

        /// <summary>
        /// 成本价
        /// </summary>
        [SugarColumn(DecimalDigits = 4, IsNullable = true, ColumnDescription = "成本价")]
        [NumberDecimal(4)]
        [Display(Name = "成本价")]
        public decimal? CostPrice { get; set; }

        /// <summary>
        /// 会员价
        /// </summary>
        [SugarColumn(DecimalDigits = 4, IsNullable = true, ColumnDescription = "会员价")]
        [NumberDecimal(4)]
        [Display(Name = "会员价")]
        public decimal? MenberPrice { get; set; }

        /// <summary>
        /// 税
        /// </summary>
        [SugarColumn(DecimalDigits = 4, IsNullable = true, ColumnDescription = "税")]
        [NumberDecimal(4)]
        [Display(Name = "税")]
        public decimal? Tax { get; set; }

        /// <summary>
        /// 单位Id
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "单位Id")]
        [NotNull]
        [Display(Name = "单位Id")]
        [ForeignKey(typeof(UnitInfo), nameof(IEntity.ID))]
        [Foreign(nameof(UnitInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long UnitId { get; set; }

        /// <summary>
        /// 商品ID
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "商品ID")]
        [NotNull]
        [Display(Name = "商品ID")]
        [ForeignKey(typeof(CommodityInfo), nameof(IEntity.ID))]
        [Foreign(nameof(CommodityInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long CommodityID { get; set; }
    }
}
