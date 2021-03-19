using System;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Common;
using Common.DAL;
using Common.Model;
using Common.Validation;

namespace TestWebAPI
{
    /// <summary>
    /// 第三方供应商商品
    /// </summary>
    [LinqSearch(typeof(SupplierCommoditySearch), nameof(GetSearchLinq))]
    [IgnoreBuildController(ignoreGet: true, ignoreDelete: true, ignorePost: true, ignorePut: true, ignoreSearch: true)]
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class SupplierCommodity : ViewModelBase
    {
        /// <summary>
        /// 关联商品ID
        /// </summary>
        [Display(Name = "关联商品ID")]
        [NotNull]
        [JsonConverter(typeof(ObjectIdNullableConverter))]
        public long? LinkCommodityID { get; set; }

        /// <summary>
        /// 关联商品SKUCODE
        /// </summary>
        [StringMaxLength(50)]
        [Display(Name = "关联商品SKUCODE")]
        public string LinkCommoditySkuCode { get; set; }

        /// <summary>
        /// 上下架状态
        /// </summary>
        [Display(Name = "上下架状态")]
        public SaleTypeEnum? SaleType { get; set; }

        /// <summary>
        /// 供应商商品唯一键
        /// </summary>
        [StringMaxLength(50)]
        [Display(Name = "供应商商品唯一键")]
        public string SupplierCommodityKey { get; set; }

        /// <summary>
        /// 供应商商品名
        /// </summary>
        [StringMaxLength(250)]
        [Display(Name = "供应商商品名")]
        public string SupplierCommodityName { get; set; }

        /// <summary>
        /// 供应商商品SKU
        /// </summary>
        [StringMaxLength(50)]
        [Display(Name = "供应商商品SKU")]
        public string SupplierCommoditySkuCode { get; set; }

        /// <summary>
        /// 供应商
        /// </summary>
        [JsonConverter(typeof(ObjectIdNullableConverter))]
        [Display(Name = "供应商")]
        public long? SupplierID { get; set; }

        /// <summary>
        /// 供应商名称
        /// </summary>
        [Display(Name = "供应商名称")]
        public string SupplierName { get; set; }

        /// <summary>
        /// 供应商编号
        /// </summary>
        [Display(Name = "供应商编号")]
        public string SupplierCode { get; set; }

        /// <summary>
        /// 商品数据
        /// </summary>
        [Display(Name = "商品数据")]
        public string SupplierCommodityData { get; set; }

        /// <summary>
        /// 商品图片
        /// </summary>
        [Display(Name = "商品图片")]
        public string SupplierCommodityImageUrl { get; set; }

        /// <summary>
        /// 供应商商品类型
        /// </summary>
        [Display(Name = "供应商商品类型")]
        public int? SupplierCommodityType { get; set; }

        /// <summary>
        /// 供应商商品国际条码
        /// </summary>
        [StringMaxLength(50)]
        [Display(Name = "供应商商品国际条码")]
        public string SupplierCommodityInternationBarCode { get; set; }

        /// <summary>
        /// 供应商商品产地
        /// </summary>
        [StringMaxLength(50)]
        [Display(Name = "供应商商品产地")]
        public string SupplierCommodityOriginPlace { get; set; }

        /// <summary>
        /// 供应商商品分类
        /// </summary>
        [StringMaxLength(50)]
        [Display(Name = "供应商商品分类")]
        public string SupplierCommodityClassify { get; set; }

        /// <summary>
        /// 供应商商品品牌
        /// </summary>
        [StringMaxLength(50)]
        [Display(Name = "供应商商品品牌")]
        public string SupplierCommodityBrand { get; set; }

        /// <summary>
        /// 供应商商品单位
        /// </summary>
        [StringMaxLength(50)]
        [Display(Name = "供应商商品单位")]
        public string SupplierCommodityUinit { get; set; }

        /// <summary>
        /// 商品描述
        /// </summary>
        [Display(Name = "商品描述")]
        public string SupplierCommodityDescription { get; set; }

        //[MongoDB.Bson.Serialization.Attributes.BsonIgnore]
        //public IEnumerable<StockInfo> StockInfos { get; set; }

        /// <summary>
        /// 市场价
        /// </summary>
        [NumberDecimal(4)]
        [Display(Name = "市场价")]
        [JsonConverter(typeof(DecimalNullableConverter))]
        public decimal? MarketPrice { get; set; }

        /// <summary>
        /// 零售价
        /// </summary>
        [NumberDecimal(4)]
        [Display(Name = "零售价")]
        [JsonConverter(typeof(DecimalNullableConverter))]
        public decimal? RetailPrice { get; set; }

        /// <summary>
        /// 会员价
        /// </summary>
        [NumberDecimal(4)]
        [Display(Name = "会员价")]
        [JsonConverter(typeof(DecimalNullableConverter))]
        public decimal? MemberPrice { get; set; }

        private static Func<SupplierCommoditySearch, Expression<Func<SupplierCommodity, bool>>> GetSearchLinq()
        {
            return parameter =>
            {
                Expression<Func<SupplierCommodity, bool>> linq = supplierCommodity => true;

                if (!string.IsNullOrWhiteSpace(parameter?.SupplierCommodityName))
                    linq = linq.And(supplierCommodity => supplierCommodity.SupplierCommodityName.Contains(parameter.SupplierCommodityName));

                if (!string.IsNullOrWhiteSpace(parameter?.SupplierCommoditySkuCode))
                    linq = linq.And(supplierCommodity => supplierCommodity.SupplierCommoditySkuCode.Contains(parameter.SupplierCommoditySkuCode));

                if (!string.IsNullOrWhiteSpace(parameter?.SupplierCommodityKey))
                    linq = linq.And(supplierCommodity => supplierCommodity.SupplierCommodityKey.Contains(parameter.SupplierCommodityKey));

                if (parameter?.SupplierID.HasValue ?? false)
                    linq = linq.And(supplierCommodity => supplierCommodity.SupplierID == parameter.SupplierID.Value);

                if (parameter?.IsConnectSku.HasValue ?? false)
                {
                    if (parameter.IsConnectSku.Value)
                        linq = linq.And(supplierCommodity => supplierCommodity.LinkCommodityID != null);
                    else
                        linq = linq.And(supplierCommodity => supplierCommodity.LinkCommodityID == null);
                }

                if (!string.IsNullOrWhiteSpace(parameter?.SupplierCommodityInternationBarCode))
                    linq = linq.And(supplierCommodity => supplierCommodity.SupplierCommodityInternationBarCode.Contains(parameter.SupplierCommodityInternationBarCode));

                return linq;
            };
        }
    }

    /// <summary>
    /// 供应商商品查询
    /// </summary>
    [IgnoreTable]
    [IgnoreBuildController(ignoreGet: true, ignoreDelete: true, ignorePost: true, ignorePut: true, ignoreSearch: true)]
    public class SupplierCommoditySearch : ViewModelBase
    {
        /// <summary>
        /// 供应商品名
        /// </summary>
        public string SupplierCommodityName { get; set; }

        /// <summary>
        /// 供应商商品SKU
        /// </summary>
        public string SupplierCommoditySkuCode { get; set; }

        /// <summary>
        /// 供应商品编码
        /// </summary>
        public string SupplierCommodityKey { get; set; }

        /// <summary>
        /// 供应商ID
        /// </summary>
        [JsonConverter(typeof(ObjectIdNullableConverter))]
        public long? SupplierID { get; set; }

        /// <summary>
        /// 国际条码
        /// </summary>
        public string SupplierCommodityInternationBarCode { get; set; }

        /// <summary>
        /// 国际条码
        /// </summary>
        public bool? IsConnectSku { get; set; }
    }

    public enum SaleTypeEnum
    {
        A,
        B
    }
}