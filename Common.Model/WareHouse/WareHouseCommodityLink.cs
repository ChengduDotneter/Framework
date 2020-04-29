using HeadQuartersERP.DAL;
using HeadQuartersERP.Model.Commodity;
using HeadQuartersERP.Validation;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HeadQuartersERP.Model.WareHouse
{
    /// <summary>
    /// 仓库-商品
    /// </summary>
    public class WareHouseCommodityLink : ViewModelBase
    {
        /// <summary>
        /// 商品ID
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "商品ID")]
        [NotNull]
        [Display(Name = "商品ID")]
        [ForeignKey(typeof(CommodityInfo), nameof(IEntity.ID))]
        [Foreign(nameof(CommodityInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long CommodityID { get; set; }

        /// <summary>
        /// 仓库ID
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "仓库ID")]
        [ForeignKey(typeof(WarehouseInfo), nameof(IEntity.ID))]
        [Foreign(nameof(WarehouseInfo), nameof(IEntity.ID))]
        [NotNull]
        [Display(Name = "仓库ID")]
        [JsonConverter(typeof(ObjectIdConverter))]
        public long WareHouseID { get; set; }

        /// <summary>
        /// 实际库存
        /// </summary>
        [SugarColumn(IsNullable = false, DecimalDigits = 4, ColumnDescription = "实际库存")]
        [NotNull]
        [NumberDecimal(4)]
        [Display(Name = "实际库存")]
        public decimal? ActualStock { get; set; }

        /// <summary>
        /// 占用库存
        /// </summary>
        [SugarColumn(IsNullable = false, DecimalDigits = 4, ColumnDescription = "占用库存")]
        [NotNull]
        [NumberDecimal(4)]
        [Display(Name = "占用库存")]
        public decimal? TakeupStock { get; set; }

        /// <summary>
        /// 可用库存
        /// </summary>
        [SugarColumn(IsNullable = false, DecimalDigits = 4, ColumnDescription = "可用库存")]
        [NotNull]
        [NumberDecimal(4)]
        [Display(Name = "可用库存")]
        public decimal? AvailableStock { get; set; }
    }
}
