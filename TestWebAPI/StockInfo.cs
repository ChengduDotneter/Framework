using Common.DAL;
using Common.Model;
using Common.Validation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MicroService.StorageService.Model
{
    /// <summary>
    /// 库存模型
    /// </summary>
    [IgnoreBuildController(ignoreGet: true, ignoreDelete: true, ignorePost: true, ignorePut: true, ignoreSearch: true)]
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class StockInfo : ViewModelBase
    {
        /// <summary>
        /// 关联仓库ID
        /// </summary>
        [NotNull]
        [ForeignKey(typeof(WarehouseInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdNullableConverter))]
        [Display(Name = "仓库ID")]
        public long? WarehouseID { get; set; }

        /// <summary>
        /// 关联仓库实体
        /// </summary>
        //[MongoDB.Bson.Serialization.Attributes.BsonIgnore]
        //public WarehouseInfo Warehouse { get; set; }

        /// <summary>
        /// 关联商品实体
        /// </summary>
        //[MongoDB.Bson.Serialization.Attributes.BsonIgnore]
        //public SupplierCommodity SupplierCommodity { get; set; }

        /// <summary>
        /// 库存商品ID
        /// </summary>
        [ForeignKey(typeof(SupplierCommodity), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdNullableConverter))]
        [Display(Name = "库存商品ID")]
        public long? SupplierCommodityID { get; set; }

        /// <summary>
        /// 批次号
        /// </summary>
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "批次号")]
        public string BatchNo { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        [NumberDecimal(2)]
        [NotNull]
        [GreaterOrEqualThan(0)]
        [Display(Name = "数量")]
        public decimal? Number { get; set; }

        /// <summary>
        /// 库存规格列表
        /// </summary>
        //[MongoDB.Bson.Serialization.Attributes.BsonIgnore]
        //public IEnumerable<StockSpecification> StockSpecifications { get; set; }
    }

    [IgnoreBuildController(ignoreGet: true, ignoreDelete: true, ignorePost: true, ignorePut: true, ignoreSearch: true)]
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class StockSpecification : ViewModelBase
    {
        public string Data { get; set; }
    }

    /// <summary>
    /// 库存查询
    /// </summary>
    [IgnoreTable]
    [IgnoreBuildController(ignoreGet: true, ignoreDelete: true, ignorePost: true, ignorePut: true, ignoreSearch: true)]
    public class StockInfoSearch : ViewModelBase
    {
        /// <summary>
        /// 供应商品ID
        /// </summary>
        [JsonConverter(typeof(ObjectIdNullableConverter))]
        public long? SupplierCommodityID { get; set; }

        /// <summary>
        /// 供应商品名称
        /// </summary>
        public string SupplierCommodityName { get; set; }

        /// <summary>
        /// 供应商品编码
        /// </summary>
        public string SupplierCommodityKey { get; set; }

        /// <summary>
        /// 仓库ID
        /// </summary>
        [JsonConverter(typeof(ObjectIdNullableConverter))]
        public long? WarehouseID { get; set; }

        /// <summary>
        /// 批次号
        /// </summary>
        public string BatchNo { get; set; }

        /// <summary>
        /// 仓库类型
        /// </summary>
        public WarehouseTypeEnum? WarehouseType { get; set; }

        /// <summary>
        /// 国际条码
        /// </summary>
        public string SupplierCommodityInternationBarCode { get; set; }

        /// <summary>
        /// sku编码
        /// </summary>
        public string SupplierCommoditySkuCode { get; set; }

        /// <summary>
        /// 供应商
        /// </summary>
        public long? SupplierID { get; set; }
    }

    public enum WarehouseTypeEnum
    {
        A,
        B
    }
}