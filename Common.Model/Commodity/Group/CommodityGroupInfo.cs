using HeadQuartersERP.Validation;
using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace HeadQuartersERP.Model.Commodity.Group
{
    public class CommodityGroupInfo : ViewModelBase
    {
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "商品组名称")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "商品组名称")]
        [Unique]
        public string GroupName { get; set; }

        [SugarColumn(Length = 200, IsNullable = true, ColumnDescription = "备注")]
        [StringMaxLength(200)]
        [Display(Name = "备注")]
        public string Remark { get; set; }

        [SugarColumn(IsNullable = false, ColumnDescription = "是否禁用")]
        [NotNull]
        [Display(Name = "是否禁用")]
        public bool IsForbidden { get; set; }


        [SugarColumn(IsNullable = false, DecimalDigits = 4, ColumnDescription = "商品数量")]
        [NotNull]
        [NumberDecimal(4)]
        [Display(Name = "商品数量")]
        public decimal? GroupCommodityNum { get; set; }
    }
}
