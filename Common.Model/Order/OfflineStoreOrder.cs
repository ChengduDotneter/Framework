//using HeadQuartersERP.DAL;
//using HeadQuartersERP.Model.Member;
//using HeadQuartersERP.Validation;
//using SqlSugar;
//using System.ComponentModel.DataAnnotations;
//using System.Text.Json.Serialization;

//namespace HeadQuartersERP.Model.Order
//{
//    /// <summary>
//    /// 线下门店订单实体
//    /// </summary>
//    public class OfflineStoreOrder : BaseOrder
//    {
//        /// <summary>
//        /// 小票单号
//        /// </summary>
//        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "小票单号")]
//        [NotNull]
//        [StringMaxLength(50)]
//        [Display(Name = "小票单号")]
//        public string ReceiptsCode { get; set; }

//        /// <summary>
//        /// 下单会员ID
//        /// </summary>
//        [SugarColumn(IsNullable = true, ColumnDescription = "下单会员ID")]
//        [NotNull]
//        [Display(Name = "会员ID")]
//        [ForeignKey(typeof(VipInfo), nameof(IEntity.ID))]
//        [Foreign(nameof(VipInfo), nameof(IEntity.ID))]
//        [JsonConverter(typeof(ObjectIdConverter))]
//        public long VipInfoID { get; set; }

//    }
//}
