//using HeadQuartersERP.Model.Enums;
//using HeadQuartersERP.Validation;
//using SqlSugar;
//using System.ComponentModel.DataAnnotations;

//namespace HeadQuartersERP.Model.Order
//{
//    /// <summary>
//    /// 在线商城订单实体
//    /// </summary>
//    public class OnlineStoreOrder : BaseOrder
//    {
//        /// <summary>
//        /// 下单会员ID
//        /// </summary>
//        [SugarColumn(IsNullable = false, ColumnDescription = "下单会员ID")]
//        [NotNull]
//        [StringMaxLength(50)]
//        [Display(Name = "下单会员ID")]
//        public long VipID { get; set; }

//        /// <summary>
//        /// 收货人
//        /// </summary>
//        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "收货人")]
//        [NotNull]
//        [StringMaxLength(50)]
//        [Display(Name = "收货人")]
//        public string Receiver { get; set; }

//        /// <summary>
//        /// 收货人手机号码
//        /// </summary>
//        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "收货人手机号码")]
//        [NotNull]
//        [StringMaxLength(50)]
//        [Display(Name = "收货人手机号码")]
//        public string ReceiverPhoneNumber { get; set; }

//        /// <summary>
//        /// 收货人身份证号
//        /// </summary>
//        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "收货人身份证号")]
//        [NotNull]
//        [StringMaxLength(50)]
//        [Display(Name = "收货人身份证号")]
//        public string ReceiverCarID { get; set; }

//        /// <summary>
//        /// 收货人真实姓名
//        /// </summary>
//        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "收货人真实姓名")]
//        [NotNull]
//        [StringMaxLength(50)]
//        [Display(Name = "收货人真实姓名")]
//        public string ReceiverRealName { get; set; }

//        /// <summary>
//        /// 订单状态
//        /// </summary>
//        [SugarColumn(IsNullable = false, ColumnDescription = "订单状态")]
//        [NotNull]
//        [Display(Name = "订单状态")]
//        [EnumValueExist]
//        public OrderStateEnum? OrderState { get; set; }

//        /// <summary>
//        /// 结算状态
//        /// </summary>
//        [SugarColumn(IsNullable = false, ColumnDescription = "结算状态")]
//        [NotNull]
//        [Display(Name = "结算状态")]
//        [EnumValueExist]
//        public PayStateEnum? PayState { get; set; }

//        /// <summary>
//        /// 运费
//        /// </summary>
//        [SugarColumn(IsNullable = false, DecimalDigits = 4, ColumnDescription = "运费")]
//        [NotNull]
//        [NumberDecimal(4)]
//        [Display(Name = "运费")]
//        public decimal? Freight { get; set; }

//    }
//}
