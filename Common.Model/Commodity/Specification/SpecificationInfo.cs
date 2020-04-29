using HeadQuartersERP.DAL;
using HeadQuartersERP.Validation;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HeadQuartersERP.Model.Commodity.Specification
{
    /// <summary>
    /// 规格
    /// </summary>
    public class SpecificationInfo : ViewModelBase
    {
        /// <summary>
        /// 规格名称
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "规格名称")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "规格名称")]
        public string Title { get; set; }

        /// <summary>
        /// 规格值
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "规格值")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "规格值")]
        public string Value { get; set; }

        /// <summary>
        /// 所属分类
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "所属分类ID")]
        [NotNull]
        [Display(Name = "所属分类ID")]
        [ForeignKey(typeof(ClassifyInfo), nameof(IEntity.ID))]
        [Foreign(nameof(ClassifyInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long ClassificationId { get; set; }
    }
}
