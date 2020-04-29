using HeadQuartersERP.DAL;
using HeadQuartersERP.Validation;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HeadQuartersERP.Model.Commodity.Group
{
    public class GroupCommodityLink : ViewModelBase
    {
        [SugarColumn(IsNullable = false, ColumnDescription = "商品ID")]
        [NotNull]
        [Display(Name = "商品ID")]
        [ForeignKey(typeof(CommodityInfo), nameof(IEntity.ID))]
        [Foreign(nameof(CommodityInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long CommodityID { get; set; }

        [SugarColumn(IsNullable = false, ColumnDescription = "商品组ID")]
        [NotNull]
        [Display(Name = "商品组ID")]
        [ForeignKey(typeof(CommodityGroupInfo), nameof(IEntity.ID))]
        [Foreign(nameof(CommodityGroupInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long CommodityGroupID { get; set; }

    }
}
