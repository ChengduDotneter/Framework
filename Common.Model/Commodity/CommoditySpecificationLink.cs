using HeadQuartersERP.DAL;
using HeadQuartersERP.Model.Commodity.Specification;
using HeadQuartersERP.Validation;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HeadQuartersERP.Model.Commodity
{
    /// <summary>
    /// SKU商品-规格
    /// </summary>
    public class SKUCommoditySpecificationLink : ViewModelBase
    {
        [SugarColumn(IsNullable = false, ColumnDescription = "SKU商品ID")]
        [NotNull]
        [Display(Name = "SKU商品ID")]
        [ForeignKey(typeof(SKUInfo), nameof(IEntity.ID))]
        [Foreign(nameof(SKUInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long SKUID { get; set; }

        [SugarColumn(IsNullable = false, ColumnDescription = "规格ID")]
        [NotNull]
        [Display(Name = "规格ID")]
        [JsonConverter(typeof(ObjectIdConverter))]
        [ForeignKey(typeof(SpecificationInfo), nameof(IEntity.ID))]
        [Foreign(nameof(SpecificationInfo), nameof(IEntity.ID))]
        public long SpecificationID { get; set; }

        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "值")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "值")]
        public string Value { get; set; }
    }

    /// <summary>
    /// 商品-规格
    /// </summary>
    public class CommoditySpecificationLink : ViewModelBase
    {
        [SugarColumn(IsNullable = false, ColumnDescription = "商品ID")]
        [NotNull]
        [Display(Name = "商品ID")]
        [ForeignKey(typeof(CommodityInfo), nameof(IEntity.ID))]
        [Foreign(nameof(CommodityInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long CommodityID { get; set; }

        [SugarColumn(IsNullable = false, ColumnDescription = "规格ID")]
        [NotNull]
        [Display(Name = "规格ID")]
        [JsonConverter(typeof(ObjectIdConverter))]
        [ForeignKey(typeof(SpecificationInfo), nameof(IEntity.ID))]
        [Foreign(nameof(SpecificationInfo), nameof(IEntity.ID))]
        public long SpecificationID { get; set; }

    }

    public class CommoditySpecificationValue : ViewModelBase
    {
        [SugarColumn(IsNullable = false, ColumnDescription = "商品-规格id")]
        [NotNull]
        [Display(Name = "商品-规格id")]
        [JsonConverter(typeof(ObjectIdConverter))]
        [ForeignKey(typeof(CommoditySpecificationLink), nameof(IEntity.ID))]
        [Foreign(nameof(CommoditySpecificationLink), nameof(IEntity.ID))]
        public long CommoditySpecificationLinkID { get; set; }

        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "值")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "值")]
        public string Value { get; set; }

    }

}
