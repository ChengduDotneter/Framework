//using HeadQuartersERP.DAL;
//using HeadQuartersERP.Model.Enums;
//using HeadQuartersERP.Validation;
//using SqlSugar;
//using System.ComponentModel.DataAnnotations;
//using System.Text.Json.Serialization;

//namespace HeadQuartersERP.Model.Order.PayDetail
//{
//    /// <summary>
//    /// 线下门店订单付款详情
//    /// </summary>
//    public class OfflineStorePaydetail : ViewModelBase
//    {
//        /// <summary>
//        /// 支付金额
//        /// </summary>
//        [SugarColumn(DecimalDigits = 4, IsNullable = false, ColumnDescription = "支付金额")]
//        [NotNull]
//        [NumberDecimal(4)]
//        [Display(Name = "支付金额")]
//        public decimal? PayMoney { get; set; }

//        /// <summary>
//        /// 支付方式
//        /// </summary>
//        [SugarColumn(IsNullable = false, ColumnDescription = "支付方式")]
//        [NotNull]
//        [EnumValueExist]
//        [Display(Name = "支付方式")]
//        public PaymentMethodEnum? PaymentMethod { get; set; }

//        /// <summary>
//        /// 线下门店订单ID
//        /// </summary>
//        [SugarColumn(IsNullable = false, ColumnDescription = "线下门店订单ID")]
//        [NotNull]
//        [Display(Name = "线下门店订单ID")]
//        [ForeignKey(typeof(OfflineStoreOrder), nameof(IEntity.ID))]
//        [Foreign(nameof(OfflineStoreOrder), nameof(IEntity.ID))]
//        [JsonConverter(typeof(ObjectIdConverter))]
//        public long OfflineStoreOrderID { get; set; }
//    }
//}
