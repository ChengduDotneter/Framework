//using HeadQuartersERP.DAL;
//using HeadQuartersERP.Model.Company;
//using HeadQuartersERP.Model.Enums;
//using HeadQuartersERP.Model.WareHouse;
//using HeadQuartersERP.Validation;
//using SqlSugar;
//using System;
//using System.ComponentModel.DataAnnotations;
//using System.Text.Json.Serialization;

//namespace HeadQuartersERP.Model.Order
//{
//    /// <summary>
//    /// 订单基础信息
//    /// </summary>
//    public abstract class BaseOrder : ViewModelBase
//    {
//        /// <summary>
//        /// 订单编号
//        /// </summary>
//        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "订单编号")]
//        [NotNull]
//        [Unique]
//        [StringMaxLength(50)]
//        [Display(Name = "订单编号")]
//        public string OrderCode { get; set; }

//        /// <summary>
//        /// 下单时间
//        /// </summary>
//        [SugarColumn(IsNullable = false, ColumnDescription = "下单时间")]
//        [NotNull]
//        [Display(Name = "下单时间")]
//        public DateTime? OrderTime { get; set; }

//        /// <summary>
//        /// 商品数量
//        /// </summary>
//        [SugarColumn(IsNullable = false, DecimalDigits = 4, ColumnDescription = "商品数量")]
//        [NotNull]
//        [NumberDecimal(4)]
//        [Display(Name = "商品数量")]
//        public decimal? CommodityCount { get; set; }

//        /// <summary>
//        /// 子系统ID
//        /// </summary>
//        [SugarColumn(IsNullable = false, ColumnDescription = "子系统ID")]
//        [NotNull]
//        [Display(Name = "子系统ID")]
//        [ForeignKey(typeof(CompanySubsystem), nameof(IEntity.ID))]
//        [Foreign(nameof(CompanySubsystem), nameof(IEntity.ID))]
//        [JsonConverter(typeof(ObjectIdConverter))]
//        public long CompanySubsystemID { get; set; }

//        /// <summary>
//        /// 发货仓库ID
//        /// </summary>
//        [SugarColumn(IsNullable = false, ColumnDescription = "发货仓库ID")]
//        [NotNull]
//        [Display(Name = "发货仓库ID")]
//        [ForeignKey(typeof(WarehouseInfo), nameof(IEntity.ID))]
//        [Foreign(nameof(WarehouseInfo), nameof(IEntity.ID))]
//        [JsonConverter(typeof(ObjectIdConverter))]
//        public long WarehouseID { get; set; }

//        /// <summary>
//        /// 收款金额
//        /// </summary>
//        [SugarColumn(IsNullable = false, DecimalDigits = 4, ColumnDescription = "收款金额")]
//        [NotNull]
//        [NumberDecimal(4)]
//        [Display(Name = "收款金额")]
//        public decimal? OrderMoney { get; set; }

//        /// <summary>
//        /// 成本价
//        /// </summary>
//        [SugarColumn(IsNullable = false, DecimalDigits = 4, ColumnDescription = "成本价")]
//        [NotNull]
//        [NumberDecimal(4)]
//        [Display(Name = "成本价")]
//        public decimal? OrderCostMoney { get; set; }

//        /// <summary>
//        /// 优惠
//        /// </summary>
//        [SugarColumn(IsNullable = false, DecimalDigits = 4, ColumnDescription = "优惠")]
//        [NotNull]
//        [NumberDecimal(4)]
//        [Display(Name = "优惠")]
//        public decimal? OrderPreferentialMoney { get; set; }

//        /// <summary>
//        /// 销售类型
//        /// </summary>
//        [SugarColumn(IsNullable = false, ColumnDescription = "销售类型")]
//        [NotNull]
//        [Display(Name = "销售类型")]
//        [EnumValueExist]
//        public OrderSaleTypeEnum? OrderSaleType { get; set; }

//        /// <summary>
//        /// 订单备注
//        /// </summary>
//        [SugarColumn(Length = 200, IsNullable = false, ColumnDescription = "订单备注")]
//        [NotNull]
//        [StringMaxLength(200)]
//        [Display(Name = "订单备注")]
//        public string OrderRemark { get; set; }
//    }
//}
