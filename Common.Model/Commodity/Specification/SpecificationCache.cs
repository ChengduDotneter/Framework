using HeadQuartersERP.DAL;
using HeadQuartersERP.Model.Commodity.Brand;
using HeadQuartersERP.Validation;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HeadQuartersERP.Model.Commodity.Specification
{
    /// <summary>
    /// 规格缓存
    /// </summary>
    public class SpecificationCache : ViewModelBase
    {
        [SugarColumn(IsNullable = false, ColumnDescription = "品牌ID")]
        [NotNull]
        [Display(Name = "品牌ID")]
        [ForeignKey(typeof(BrandInfo), nameof(IEntity.ID))]
        [Foreign(nameof(BrandInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long BrandID { get; set; }

        [SugarColumn(IsNullable = false, ColumnDescription = "分类ID")]
        [NotNull]
        [Display(Name = "分类ID")]
        [ForeignKey(typeof(ClassifyInfo), nameof(IEntity.ID))]
        [Foreign(nameof(ClassifyInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long ClassificationID { get; set; }

        [SugarColumn(IsNullable = false, ColumnDescription = "规格ID")]
        [NotNull]
        [Display(Name = "规格ID")]
        [ForeignKey(typeof(SpecificationInfo), nameof(IEntity.ID))]
        [Foreign(nameof(SpecificationInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long SpecificationID { get; set; }

        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "缓存值")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "缓存值")]
        public string Value { get; set; }
    }
}
