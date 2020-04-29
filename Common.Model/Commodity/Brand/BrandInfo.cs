using HeadQuartersERP.DAL;
using HeadQuartersERP.Validation;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HeadQuartersERP.Model.Commodity.Brand
{
    /// <summary>
    /// 品牌
    /// </summary>
    public class BrandInfo : ViewModelBase
    {
        /// <summary>
        /// 品牌名字
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "品牌名字")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "品牌名字")]
        [Unique]
        public string BrandName { get; set; }

        /// <summary>
        /// 简称
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "简称")]
        [StringMaxLength(50)]
        [Display(Name = "简称")]
        public string Abbreviation { get; set; }

        /// <summary>
        /// 国家ID
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "国家ID")]
        [NotNull]
        [Display(Name = "国家ID")]
        [ForeignKey(typeof(CountryInfo), nameof(IEntity.ID))]
        [Foreign(nameof(CountryInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long CountryID { get; set; }

        /// <summary>
        /// 排序号
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "排序号")]
        [Display(Name = "排序号")]
        public int? OrderInt { get; set; }

    }
}
