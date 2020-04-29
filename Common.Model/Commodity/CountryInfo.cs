using HeadQuartersERP.Validation;
using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace HeadQuartersERP.Model.Commodity
{
    /// <summary>
    /// 国家
    /// </summary>
    public class CountryInfo : ViewModelBase
    {
        /// <summary>
        /// 国家名称
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "国家名称")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "国家名称")]
        [Unique]
        public string CountryName { get; set; }

        /// <summary>
        /// 国家简称
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "国家简称")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "国家简称")]
        [Unique]
        public string CountryAbbreviation { get; set; }

        /// <summary>
        /// 排序号
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "排序号")]
        [Display(Name = "排序号")]
        public int? OrderInt { get; set; }
    }
}
