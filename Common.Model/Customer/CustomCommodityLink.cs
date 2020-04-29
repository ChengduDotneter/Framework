using HeadQuartersERP.DAL;
using HeadQuartersERP.Model.Commodity;
using HeadQuartersERP.Validation;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HeadQuartersERP.Model.Customer
{
    /// <summary>
    /// 客户-商品授权
    /// </summary>
    public class CustomCommodityLink : ViewModelBase
    {
        [SugarColumn(IsNullable = false, ColumnDescription = "商品ID")]
        [NotNull]
        [Display(Name = "商品ID")]
        [ForeignKey(typeof(CommodityInfo), nameof(IEntity.ID))]
        [Foreign(nameof(CommodityInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long CommodityID { get; set; }

        [SugarColumn(IsNullable = false, ColumnDescription = "客户ID")]
        [NotNull]
        [Display(Name = "客户ID")]
        [ForeignKey(typeof(CustomerInfo), nameof(IEntity.ID))]
        [Foreign(nameof(CustomerInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long CustomerID { get; set; }
    }
}
