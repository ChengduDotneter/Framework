//using HeadQuartersERP.DAL;
//using HeadQuartersERP.Model.Commodity;
//using HeadQuartersERP.Model.Order.PayDetail;
//using HeadQuartersERP.Validation;
//using SqlSugar;
//using System.ComponentModel.DataAnnotations;
//using System.Text.Json.Serialization;

//namespace HeadQuartersERP.Model.Order
//{
//    /// <summary>
//    /// 大客户订单商品实体
//    /// </summary>
//    public class KeyAccountOrderCommodity : ViewModelBase
//    {
//        /// <summary>
//        /// 商品ID
//        /// </summary>
//        [SugarColumn(IsNullable = false, ColumnDescription = "商品ID")]
//        [NotNull]
//        [Display(Name = "商品ID")]
//        [ForeignKey(typeof(CommodityInfo), nameof(IEntity.ID))]
//        [Foreign(nameof(CommodityInfo), nameof(IEntity.ID))]
//        [JsonConverter(typeof(ObjectIdConverter))]
//        public long CommodityID { get; set; }

//        /// <summary>
//        /// 大客户订单ID
//        /// </summary>
//        [SugarColumn(IsNullable = false, ColumnDescription = "大客户订单ID")]
//        [NotNull]
//        [Display(Name = "大客户订单ID")]
//        [ForeignKey(typeof(KeyAccountOrderPaydetail), nameof(IEntity.ID))]
//        [Foreign(nameof(KeyAccountOrderPaydetail), nameof(IEntity.ID))]
//        public long KeyAccountOrderID { get; set; }

//        /// <summary>
//        /// 商品数量
//        /// </summary>
//        [SugarColumn(IsNullable = false, ColumnDescription = "商品数量")]
//        [NotNull]
//        [NumberDecimal(4)]
//        [Display(Name = "商品数量")]
//        public decimal? CommodityCount { get; set; }

//        /// <summary>
//        /// 商品单价
//        /// </summary>
//        [SugarColumn(IsNullable = false, ColumnDescription = "商品单价")]
//        [NotNull]
//        [NumberDecimal(4)]
//        [Display(Name = "商品单价")]
//        public decimal? CommodityPrice { get; set; }
//    }
//}
