using Common;
using Common.Model;
using Common.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace MicroService.StorageService.Model
{
    /// <summary>
    /// 仓库实体
    /// </summary>
    [IgnoreBuildController(ignoreGet: true, ignoreDelete: true, ignorePost: true, ignorePut: true, ignoreSearch: true)]
    [LinqSearch(typeof(WarehouseInfo), nameof(GetSearchLinq))]
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class WarehouseInfo : ViewModelBase
    {
        /// <summary>
        /// 仓库名称
        /// </summary>
        [Display(Name = "仓库名称")]
        [Unique]
        public string WarehouseName { get; set; }

        /// <summary>
        /// 仓库编号
        /// </summary>
        [Unique]
        [Display(Name = "仓库编号")]
        public string WarehouseCode { get; set; }

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
        /// 供应商ID
        /// </summary>
        [Display(Name = "供应商ID")]
        [NotNull]
        [JsonConverter(typeof(ObjectIdNullableConverter))]
        public long? SupplierID { get; set; }

        /// <summary>
        /// 审核状态
        /// </summary>
        [Display(Name = "审核状态")]
        public ExamineTypeEnum? ExamineState { get; set; }

        /// <summary>
        /// 仓库类型
        /// </summary>
        [Display(Name = "仓库类型")]
        [EnumValueExist]
        public WarehouseTypeEnum? WarehouseType { get; set; }

        /// <summary>
        /// 仓库账号
        /// </summary>
        [Display(Name = "仓库账号")]
        [StringMaxLength(100)]
        [NotNull]
        public string WarehouseAccount { get; set; }

        /// <summary>
        /// 仓库动态Dll名称
        /// </summary>
        [Display(Name = "仓库动态Dll名称")]
        [StringMaxLength(100)]
        public string WarehouseDllName { get; set; }

        /// <summary>
        /// 货主姓名
        /// </summary>
        [Display(Name = "货主姓名")]
        [StringMaxLength(50)]
        public string CustomerName { get; set; }

        /// <summary>
        /// 固定参数列表
        /// </summary>
        //[MongoDB.Bson.Serialization.Attributes.BsonIgnore]
        //public IEnumerable<WareHouseParameter> WareHouseParameters { get; set; }

        private static Func<WarehouseInfo, Expression<Func<WarehouseInfo, bool>>> GetSearchLinq()
        {
            return parameter =>
            {
                Expression<Func<WarehouseInfo, bool>> linq = warehouseInfo => true;

                if (!string.IsNullOrWhiteSpace(parameter?.WarehouseName))
                    linq = linq.And(warehouseInfo => warehouseInfo.WarehouseName.Contains(parameter.WarehouseName.Trim()));

                if (!string.IsNullOrWhiteSpace(parameter?.WarehouseCode))
                    linq = linq.And(warehouseInfo => warehouseInfo.WarehouseCode.Contains(parameter.WarehouseCode.Trim()));

                if (parameter?.SupplierID.HasValue ?? false)
                    linq = linq.And(warehouseInfo => warehouseInfo.SupplierID == parameter.SupplierID.Value);

                if (parameter?.ExamineState.HasValue ?? false)
                    linq = linq.And(warehouseInfo => warehouseInfo.ExamineState == parameter.ExamineState.Value);

                if (parameter?.WarehouseType.HasValue ?? false)
                    linq = linq.And(warehouseInfo => warehouseInfo.WarehouseType == parameter.WarehouseType.Value);

                return linq;
            };
        }
    }

    public class WareHouseParameter : ViewModelBase
    {
        public string Data { get; set; }
    }

    public enum ExamineTypeEnum
    {
        A,
        B
    }
}