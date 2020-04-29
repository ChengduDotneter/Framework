using HeadQuartersERP.DAL;
using HeadQuartersERP.Model.Commodity;
using HeadQuartersERP.Validation;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HeadQuartersERP.Model.Company
{
    /// <summary>
    /// 公司-商品授权
    /// </summary>
    public class CompanyCommodityLink : ViewModelBase
    {
        [SugarColumn(IsNullable = false, ColumnDescription = "商品ID")]
        [NotNull]
        [Display(Name = "商品ID")]
        [ForeignKey(typeof(CommodityInfo), nameof(IEntity.ID))]
        [Foreign(nameof(CommodityInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long CommodityID { get; set; }

        [SugarColumn(IsNullable = false, ColumnDescription = "公司ID")]
        [NotNull]
        [Display(Name = "公司ID")]
        [ForeignKey(typeof(CompanyInfo), nameof(IEntity.ID))]
        [Foreign(nameof(CompanyInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long CompanyID { get; set; }

        [SugarColumn(DecimalDigits = 4, IsNullable = false, ColumnDescription = "结算价")]
        [NotNull]
        [GreaterThan(0)]
        [NumberDecimal(4)]
        [Display(Name = "结算价")]
        public decimal? SettlementPrice { get; set; }
    }
}
