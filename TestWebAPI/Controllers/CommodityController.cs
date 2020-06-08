using Apache.Ignite.Core.Cache.Configuration;
using Common.DAL;
using Common.Model;
using Common.Validation;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Common;
using Microsoft.AspNetCore.Mvc;
using Common.ServiceCommon;

namespace TestWebAPI.Controllers
{
    /// <summary>
    /// 商品上下架状态
    /// </summary>
    public enum SaleTpeEnum
    {
        /// <summary>
        /// 下架
        /// </summary>
        [Display(Name = "下架")]
        LowerShelf = 0,

        /// <summary>
        /// 上架
        /// </summary>
        [Display(Name = "上架")]
        UpperShelf = 1,
    }

    public enum WarehouseTypeEnum
    {
        /// <summary>
        /// 普通
        /// </summary>
        [Display(Name = "普通")]
        Ordinary = 0,

        /// <summary>
        /// 第三方
        /// </summary>
        [Display(Name = "第三方仓库")]
        ThirdParty = 1,
    }

    public enum ExamineTypeEnum
    {
        /// <summary>
        /// 新增
        /// </summary>
        [Display(Name = "新增")]
        Insert = -1,

        /// <summary>
        /// 未审核
        /// </summary>
        [Display(Name = "未审核")]
        NotExamine = 0,

        /// <summary>
        /// 审核通过
        /// </summary>
        [Display(Name = "审核通过")]
        ExaminePassed = 1,

        /// <summary>
        /// 审核未通过
        /// </summary>
        [Display(Name = "审核未通过")]
        ExamineFaild = 2,

        /// <summary>
        /// 审核中
        /// </summary>
        [Display(Name = "审核中")]
        Examining = 3
    }

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

    [LinqSearch(typeof(SupplierCommoditySearch), nameof(GetSearchLinq))]
    public class SupplierCommodity : ViewModelBase
    {
        /// <summary>
        /// 关联商品ID
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "关联商品ID")]
        [QuerySqlField(NotNull = false)]
        [JsonConverter(typeof(ObjectIdNullableConverter))]
        public long? LinkCommodityID { get; set; }

        /// <summary>
        /// 关联商品SKUCODE
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "关联商品SKUCODE")]
        [QuerySqlField(NotNull = false)]
        public string LinkCommoditySkuCode { get; set; }

        /// <summary>
        /// 上下架状态
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "上下架状态", IndexGroupNameList = new string[] { "INDEX_ISDELETED" })]
        [QuerySqlField(NotNull = true)]
        public SaleTpeEnum? SaleTpe { get; set; }

        /// <summary>
        /// 供应商商品唯一键
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "供应商商品唯一键")]
        [QuerySqlField(NotNull = true)]
        public string SupplierCommodityKey { get; set; }

        /// <summary>
        /// 供应商商品名
        /// </summary>
        [SugarColumn(Length = 250, IsNullable = false, ColumnDescription = "供应商商品名")]
        [QuerySqlField(NotNull = true)]
        public string SupplierCommodityName { get; set; }

        /// <summary>
        /// 供应商商品SKU
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "供应商商品SKU")]
        [QuerySqlField(NotNull = false)]
        public string SupplierCommoditySkuCode { get; set; }

        /// <summary>
        /// 供应商
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "供应商")]
        [QuerySqlField(NotNull = false)]
        [JsonConverter(typeof(ObjectIdNullableConverter))]
        public long? SupplierID { get; set; }

        /// <summary>
        /// 供应商名称
        /// </summary>
        [SugarColumn(IsIgnore = true, ColumnDescription = "供应商名称")]
        [QuerySqlField(NotNull = false)]
        public string SupplierName { get; set; }

        /// <summary>
        /// 供应商编号
        /// </summary>
        [SugarColumn(IsIgnore = true, ColumnDescription = "供应商编号")]
        [QuerySqlField(NotNull = false)]
        public string SupplierCode { get; set; }

        /// <summary>
        /// 商品数据
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "商品数据", ColumnDataType = "text")]
        [QuerySqlField(NotNull = false)]
        public string SupplierCommodityData { get; set; }

        /// <summary>
        /// 商品图片
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "商品图片", ColumnDataType = "text")]
        [QuerySqlField(NotNull = false)]
        public string SupplierCommodityImageUrl { get; set; }

        /// <summary>
        /// 供应商商品类型
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "供应商商品类型")]
        [QuerySqlField(NotNull = false)]
        public int? SupplierCommodityType { get; set; }

        /// <summary>
        /// 供应商商品国际条码
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "供应商商品国际条码")]
        [QuerySqlField(NotNull = false)]
        public string SupplierCommodityInternationBarCode { get; set; }

        /// <summary>
        /// 供应商商品产地
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "供应商商品产地")]
        [QuerySqlField(NotNull = false)]
        public string SupplierCommodityOriginPlace { get; set; }

        /// <summary>
        /// 供应商商品分类
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "供应商商品分类")]
        [QuerySqlField(NotNull = false)]
        public string SupplierCommodityClassify { get; set; }

        /// <summary>
        /// 供应商商品品牌
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "供应商商品品牌")]
        [QuerySqlField(NotNull = false)]
        public string SupplierCommodityBrand { get; set; }

        /// <summary>
        /// 供应商商品单位
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "供应商商品单位")]
        [QuerySqlField(NotNull = false)]
        public string SupplierCommodityUinit { get; set; }

        /// <summary>
        /// 商品描述
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "商品描述", ColumnDataType = "text")]
        [QuerySqlField(NotNull = false)]
        public string SupplierCommodityDescription { get; set; }

        /// <summary>
        /// 关联仓库实体
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public IEnumerable<StockInfo> StockInfos { get; set; }

        /// <summary>
        /// 市场价
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        [JsonConverter(typeof(DecimalNullableConverter))]
        public decimal? MarketPrice { get; set; }

        /// <summary>
        /// 零售价
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        [JsonConverter(typeof(DecimalNullableConverter))]
        public decimal? RetailPrice { get; set; }

        /// <summary>
        /// 会员价
        /// </summary>
        [SugarColumn(IsIgnore = true)]
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

    public class StockInfo : ViewModelBase
    {
        /// <summary>
        /// 关联仓库ID
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "仓库ID", IndexGroupNameList = new string[] { "INDEX_ISDELETED" })]
        [JsonConverter(typeof(ObjectIdNullableConverter))]
        [QuerySqlField(NotNull = true)]
        public long? WarehouseID { get; set; }

        /// <summary>
        /// 关联仓库实体
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public WarehouseInfo Warehouse { get; set; }

        /// <summary>
        /// 关联商品实体
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public SupplierCommodity SupplierCommodity { get; set; }

        /// <summary>
        /// 库存商品ID
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "库存商品ID", IndexGroupNameList = new string[] { "INDEX_ISDELETED" })]
        [QuerySqlField(NotNull = true)]
        [JsonConverter(typeof(ObjectIdNullableConverter))]
        public long? SupplierCommodityID { get; set; }

        /// <summary>
        /// 批次号
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "批次号")]
        [QuerySqlField(NotNull = true)]
        public string BatchNo { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        [SugarColumn(IsNullable = false, DecimalDigits = 2, ColumnDescription = "数量")]
        [QuerySqlField(NotNull = false)]
        public decimal? Number { get; set; }

        /// <summary>
        /// 成本价
        /// </summary>
        [SugarColumn(IsNullable = true, DecimalDigits = 2, ColumnDescription = "成本价")]
        [QuerySqlField(NotNull = false)]
        public decimal? CostPrice { get; set; }

        /// <summary>
        /// 保质期开始时间
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "保质期开始时间")]
        [QuerySqlField(NotNull = false)]
        [JsonConverter(typeof(DateTimeNullableConverter))]
        public DateTime? ShelfLifeStartTime { get; set; }

        /// <summary>
        /// 保质期结束时间
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "保质期结束时间")]
        [QuerySqlField(NotNull = false)]
        [JsonConverter(typeof(DateTimeNullableConverter))]
        public DateTime? ShelfLifeEndTime { get; set; }
    }

    public class WarehouseInfo : ViewModelBase
    {
        /// <summary>
        /// 仓库名称
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "仓库名称")]
        [QuerySqlField(NotNull = true)]
        public string WarehouseName { get; set; }

        /// <summary>
        /// 仓库编号
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "仓库编号")]
        [QuerySqlField(NotNull = false)]
        public string WarehouseCode { get; set; }

        /// <summary>
        /// 供应商名称
        /// </summary>
        [SugarColumn(IsIgnore = true, ColumnDescription = "供应商名称")]
        [QuerySqlField(NotNull = false)]
        public string SupplierName { get; set; }

        /// <summary>
        /// 供应商编号
        /// </summary>
        [SugarColumn(IsIgnore = true, ColumnDescription = "供应商编号")]
        [QuerySqlField(NotNull = false)]
        public string SupplierCode { get; set; }

        /// <summary>
        /// 供应商ID
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "供应商ID")]
        [QuerySqlField(NotNull = true)]
        [JsonConverter(typeof(ObjectIdNullableConverter))]
        public long? SupplierID { get; set; }

        /// <summary>
        /// 仓库状态，正常为true
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "仓库状态")]
        [QuerySqlField(NotNull = true)]
        public bool WarehouseState { get; set; }

        /// <summary>
        /// 审核状态
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "审核状态")]
        [QuerySqlField(NotNull = true)]
        public ExamineTypeEnum? ExamineState { get; set; }

        /// <summary>
        /// 仓库类型
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "仓库类型", IndexGroupNameList = new string[] { "INDEX_ISDELETED" })]
        [QuerySqlField(NotNull = true)]
        public WarehouseTypeEnum? WarehouseType { get; set; }

        /// <summary>
        /// 仓库账号
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "仓库账号")]
        [QuerySqlField(NotNull = false)]
        public string WarehouseAccount { get; set; }

        /// <summary>
        /// 仓库动态Dll名称
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "仓库动态Dll名称")]
        [QuerySqlField(NotNull = false)]
        public string WarehouseDllName { get; set; }
    }

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

    /// <summary>
    /// 库存列表查询
    /// </summary>
    [Route("stockinfo")]
    public class StockInfoSearchControlller : GenericSearchController<StockInfoSearch, StockInfo>
    {
        private readonly ISearchQuery<SupplierCommodity> m_supplierCommoditySearchQuery;
        private readonly ISearchQuery<StockInfo> m_stockInfoSearchQuery;
        private readonly ISearchQuery<WarehouseInfo> m_warehouseInfoSearchQuery;

        public StockInfoSearchControlller(
           ISearchQuery<SupplierCommodity> supplierCommoditySearchQuery,
           ISearchQuery<StockInfo> stockInfoSearchQuery,
           ISearchQuery<WarehouseInfo> warehouseInfoSearchQuery
           ) : base(stockInfoSearchQuery)
        {
            m_supplierCommoditySearchQuery = supplierCommoditySearchQuery;
            m_stockInfoSearchQuery = stockInfoSearchQuery;
            m_warehouseInfoSearchQuery = warehouseInfoSearchQuery;
        }

        protected override Tuple<IEnumerable<StockInfo>, int> SearchDatas(PageQuery<StockInfoSearch> pageQuery)
        {
            StockInfoSearch stockInfoSearch = pageQuery.Condition;

            Dictionary<string, object> parameters = new Dictionary<string, object>();

            string sql = @"SELECT
                                 DISTINCT
                                   STOCKINFO.*
                               FROM
                                   STOCKINFO
                                   JOIN SUPPLIERCOMMODITY ON STOCKINFO.SUPPLIERCOMMODITYID = SUPPLIERCOMMODITY.ID
                                   JOIN WAREHOUSEINFO ON STOCKINFO.WAREHOUSEID = WAREHOUSEINFO.ID
                               WHERE
                                   STOCKINFO.ISDELETED = 0 AND SUPPLIERCOMMODITY.ISDELETED = 0 AND WAREHOUSEINFO.ISDELETED = 0 ";

            string countSql = @" SELECT
                                  DISTINCT
                                   COUNT(1) AS COUNT
                               FROM
                                   STOCKINFO
                                   JOIN SUPPLIERCOMMODITY ON STOCKINFO.SUPPLIERCOMMODITYID = SUPPLIERCOMMODITY.ID
                                   JOIN WAREHOUSEINFO ON STOCKINFO.WAREHOUSEID = WAREHOUSEINFO.ID
                               WHERE
                                    STOCKINFO.ISDELETED = 0 AND SUPPLIERCOMMODITY.ISDELETED = 0 AND WAREHOUSEINFO.ISDELETED = 0 ";

            string whereCondition = string.Empty;

            if (stockInfoSearch != null)
            {
                if (stockInfoSearch?.SupplierCommodityID.HasValue ?? false)
                {
                    whereCondition += $" AND {nameof(StockInfo)}.{nameof(StockInfo.SupplierCommodityID)} = @{nameof(StockInfo.SupplierCommodityID)}";

                    parameters.Add($"@{nameof(StockInfo.SupplierCommodityID)}", stockInfoSearch.SupplierCommodityID.Value);
                }

                if (!string.IsNullOrWhiteSpace(stockInfoSearch?.SupplierCommodityName))
                {
                    whereCondition += $" AND {nameof(SupplierCommodity)}.{nameof(SupplierCommodity.SupplierCommodityName)} LIKE CONCAT('%',@{nameof(SupplierCommodity.SupplierCommodityName)},'%')";

                    parameters.Add($"@{nameof(SupplierCommodity.SupplierCommodityName)}", stockInfoSearch.SupplierCommodityName);
                }

                if (!string.IsNullOrWhiteSpace(stockInfoSearch?.SupplierCommodityKey))
                {
                    whereCondition += $" AND {nameof(SupplierCommodity)}.{nameof(SupplierCommodity.SupplierCommodityKey)} LIKE CONCAT('%',@{nameof(SupplierCommodity.SupplierCommodityKey)},'%')";
                    countSql += $" AND {nameof(SupplierCommodity)}.{nameof(SupplierCommodity.SupplierCommodityKey)} LIKE CONCAT('%',@{nameof(SupplierCommodity.SupplierCommodityKey)},'%')";

                    parameters.Add($"@{nameof(SupplierCommodity.SupplierCommodityKey)}", stockInfoSearch.SupplierCommodityKey);
                }

                if (stockInfoSearch?.WarehouseID.HasValue ?? false)
                {
                    whereCondition += $" AND {nameof(StockInfo)}.{nameof(StockInfo.WarehouseID)} = @{nameof(StockInfo.WarehouseID)}";
                    countSql += $" AND {nameof(StockInfo)}.{nameof(StockInfo.WarehouseID)} = @{nameof(StockInfo.WarehouseID)}";

                    parameters.Add($"@{nameof(StockInfo.WarehouseID)}", stockInfoSearch.WarehouseID);
                }

                if (!string.IsNullOrWhiteSpace(stockInfoSearch?.BatchNo))
                {
                    whereCondition += $" AND {nameof(StockInfo)}.{nameof(StockInfo.BatchNo)} LIKE CONCAT('%',@{nameof(StockInfo.BatchNo)},'%')";
                    countSql += $" AND {nameof(StockInfo)}.{nameof(StockInfo.BatchNo)} LIKE CONCAT('%',@{nameof(StockInfo.BatchNo)},'%')";

                    parameters.Add($"@{nameof(StockInfo.BatchNo)}", stockInfoSearch.BatchNo);
                }

                if (stockInfoSearch?.SupplierID.HasValue ?? false)
                {
                    whereCondition += $" AND {nameof(SupplierCommodity)}.{nameof(SupplierCommodity.SupplierID)} = @{nameof(SupplierCommodity.SupplierID)}";
                    countSql += $" AND {nameof(SupplierCommodity)}.{nameof(SupplierCommodity.SupplierID)} = @{nameof(SupplierCommodity.SupplierID)}";

                    parameters.Add($"@{nameof(SupplierCommodity.SupplierID)}", stockInfoSearch.SupplierID);
                }

                if (!string.IsNullOrWhiteSpace(stockInfoSearch?.SupplierCommodityInternationBarCode))
                {
                    whereCondition += $" AND {nameof(SupplierCommodity)}.{nameof(SupplierCommodity.SupplierCommodityInternationBarCode)} LIKE CONCAT('%',@{nameof(SupplierCommodity.SupplierCommodityInternationBarCode)},'%')";

                    parameters.Add($"@{nameof(SupplierCommodity.SupplierCommodityInternationBarCode)}", stockInfoSearch.SupplierCommodityInternationBarCode);
                }

                if (!string.IsNullOrWhiteSpace(stockInfoSearch?.SupplierCommoditySkuCode))
                {
                    whereCondition += $" AND {nameof(SupplierCommodity)}.{nameof(SupplierCommodity.SupplierCommoditySkuCode)} LIKE CONCAT('%',@{nameof(SupplierCommodity.SupplierCommoditySkuCode)},'%')";

                    parameters.Add($"@{nameof(SupplierCommodity.SupplierCommoditySkuCode)}", stockInfoSearch.SupplierCommoditySkuCode);
                }

                if (stockInfoSearch?.WarehouseType.HasValue ?? false)
                {
                    whereCondition += $" AND {nameof(WarehouseInfo)}.{nameof(WarehouseInfo.WarehouseType)} = @{nameof(WarehouseInfo.WarehouseType)}";

                    parameters.Add($"@{nameof(WarehouseInfo.WarehouseType)}", stockInfoSearch.WarehouseType);
                }
            }
            sql += whereCondition;
            countSql += whereCondition;

            sql += $" ORDER BY STOCKINFO.ID DESC LIMIT {pageQuery.PageCount} OFFSET { pageQuery.StartIndex} ";

            return Tuple.Create(MapperModelHelper<StockInfo>.ReadModel(m_stockInfoSearchQuery.Query(sql.ToString(), parameters)),
               Convert.ToInt32(m_stockInfoSearchQuery.Query(countSql.ToString(), parameters).First()["COUNT"]));
        }

        protected override IEnumerable<StockInfo> PreperDatas(IEnumerable<StockInfo> stockInfos)
        {
            stockInfos = stockInfos.ToList();

            if (stockInfos.Count() > 0)
            {
                IEnumerable<long> supplierCommodityIDs = stockInfos.Select(sku => sku.SupplierCommodityID.GetValueOrDefault()).Distinct();

                IEnumerable<SupplierCommodity> supplierCommoditys = m_supplierCommoditySearchQuery.FilterIsDeleted().Search(item => supplierCommodityIDs.Contains(item.ID));

                IEnumerable<long> warehouseIDs = stockInfos.Select(sku => sku.WarehouseID.GetValueOrDefault()).Distinct();
                IEnumerable<WarehouseInfo> warehouseInfos = m_warehouseInfoSearchQuery.FilterIsDeleted().Search(item => warehouseIDs.Contains(item.ID));

                foreach (StockInfo stockInfo in stockInfos)
                {
                    stockInfo.SupplierCommodity = supplierCommoditys.Where(item => item.ID == stockInfo.SupplierCommodityID).FirstOrDefault();

                    stockInfo.Warehouse = warehouseInfos.Where(item => item.ID == stockInfo.WarehouseID).FirstOrDefault();
                }
            }

            return stockInfos;
        }
    }
}
