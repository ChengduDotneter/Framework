//using HeadQuartersERP.DAL;
//using HeadQuartersERP.Model.Customer;
//using HeadQuartersERP.Model.Enums;
//using HeadQuartersERP.Validation;
//using SqlSugar;
//using System.ComponentModel.DataAnnotations;
//using System.Text.Json.Serialization;

//namespace HeadQuartersERP.Model.Order
//{
//    /// <summary>
//    /// 供应链公司订单实体
//    /// </summary>
//    public class SupplyChainOrder : BaseOrder
//    {
//        /// <summary>
//        /// 客户ID,关联客户对象
//        /// </summary>
//        [SugarColumn(IsNullable = false, ColumnDescription = "客户")]
//        [NotNull]
//        [Display(Name = "客户")]
//        [ForeignKey(typeof(CustomerInfo), nameof(IEntity.ID))]
//        [Foreign(nameof(CustomerInfo), nameof(IEntity.ID))]
//        [JsonConverter(typeof(ObjectIdConverter))]
//        public long CustomerInfoID { get; set; }

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

//    }
//}
