//using HeadQuartersERP.DAL;
//using HeadQuartersERP.Model.Enums;
//using HeadQuartersERP.Validation;
//using SqlSugar;
//using System.ComponentModel.DataAnnotations;
//using System.Text.Json.Serialization;

//namespace HeadQuartersERP.Model.Order.PayDetail
//{
//    /// <summary>
//    /// 供应链订单付款详情
//    /// </summary>
//    public class SupplyChainPaydetail : ViewModelBase
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
//        /// 供应链订单ID
//        /// </summary>
//        [SugarColumn(IsNullable = false, ColumnDescription = "供应链订单ID")]
//        [NotNull]
//        [Display(Name = "供应链订单ID")]
//        [ForeignKey(typeof(SupplyChainOrder), nameof(IEntity.ID))]
//        [Foreign(nameof(SupplyChainOrder), nameof(IEntity.ID))]
//        [JsonConverter(typeof(ObjectIdConverter))]
//        public long SupplyChainOrderID { get; set; }
//    }
//}
