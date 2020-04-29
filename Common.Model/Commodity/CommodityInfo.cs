using HeadQuartersERP.DAL;
using HeadQuartersERP.Model.Commodity.Brand;
using HeadQuartersERP.Model.Enums;
using HeadQuartersERP.Validation;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HeadQuartersERP.Model.Commodity
{
    /// <summary>
    /// 商品
    /// </summary>
    public class CommodityInfo : ViewModelBase
    {
        /// <summary>
        /// 商品名称
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "商品名称")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "商品名称")]
        public string CommodityName { set; get; }

        /// <summary>
        /// 描述
        /// </summary>
        [SugarColumn(Length = 200, IsNullable = false, ColumnDescription = "描述")]
        [NotNull]
        [StringMaxLength(200)]
        [Display(Name = "描述")]
        public string Description { set; get; }

        /// <summary>
        /// 是否上架
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "是否上架")]
        [NotNull]
        [Display(Name = "是否上架")]
        public bool? IsLoaded { set; get; }

        /// <summary>
        /// 简称
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "简称")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "简称")]
        public string Abbreviation { set; get; }

        /// <summary>
        /// 商品类型
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "商品类型")]
        [NotNull]
        [EnumValueExist]
        [Display(Name = "商品类型")]
        public SkuCommodifyTypeEnum? CommodityType { get; set; }

        /// <summary>
        /// 分类ID
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "分类ID")]
        [NotNull]
        [Display(Name = "分类ID")]
        [ForeignKey(typeof(ClassifyInfo), nameof(IEntity.ID))]
        [Foreign(nameof(ClassifyInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long ClassificationID { set; get; }

        /// <summary>
        /// 产地
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "产地ID")]
        [NotNull]
        [Display(Name = "产地ID")]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long CountryID { get; set; }

        /// <summary>
        /// 品牌
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "品牌ID")]
        [NotNull]
        [Display(Name = "品牌ID")]
        [ForeignKey(typeof(BrandInfo), nameof(IEntity.ID))]
        [Foreign(nameof(BrandInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long BrandId { get; set; }
    }
}
