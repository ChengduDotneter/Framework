using HeadQuartersERP.DAL;
using HeadQuartersERP.Validation;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HeadQuartersERP.Model.Commodity
{
    /// <summary>
    /// 商品分类
    /// </summary>
    public class ClassifyInfo : ViewModelBase
    {
        /// <summary>
        /// 分类编号
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "分类编号")]
        [NotNull]
        [Unique]
        [StringMaxLength(50)]
        [Display(Name = "分类编号")]
        public string ClassificationCode { get; set; }

        /// <summary>
        /// 分类名称
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "分类名称")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "分类名称")]
        public string ClassificationName { get; set; }

        /// <summary>
        /// 父分类ID
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "父分类ID")]
        [Display(Name = "父分类ID")]
        [ForeignKey(typeof(ClassifyInfo), nameof(IEntity.ID))]
        [Foreign(nameof(ClassifyInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long ParentId { get; set; }

        /// <summary>
        /// 是否冻结
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "是否冻结")]
        [NotNull]
        [Display(Name = "是否冻结")]
        public bool? IsForbidden { get; set; }

        /// <summary>
        /// 级别
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "级别")]
        [NotNull]
        [Display(Name = "级别")]
        public int? Level { get; set; }
    }
}
