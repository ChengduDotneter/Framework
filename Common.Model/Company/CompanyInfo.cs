using HeadQuartersERP.Model.Enums;
using HeadQuartersERP.Validation;
using SqlSugar;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace HeadQuartersERP.Model.Company
{
    /// <summary>
    /// 公司实体类
    /// </summary>
    [SqlSearch(typeof(CompanyInfo), nameof(GetSearchSql))]
    public class CompanyInfo : ViewModelBase
    {
        /// <summary>
        /// 账号
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "账号")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "账号")]
        [Unique]
        public string RootName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "密码")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "密码")]
        public string RootPassword { get; set; }

        /// <summary>
        /// 公司名
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "公司名")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "公司名")]
        public string CompanyName { get; set; }

        /// <summary>
        /// 公司代码
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "公司代码")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "公司代码")]
        [Unique]
        public string CompanyCode { get; set; }

        /// <summary>
        /// 联系人
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "公司主要联系人")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "公司主要联系人")]
        public string ContactName { get; set; }

        /// <summary>
        /// 联系人电话
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "公司主要联系人电话")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "公司主要联系人电话")]
        public string ContactPhone { get; set; }

        /// <summary>
        /// 公司类型
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "公司类型")]
        [NotNull]
        [EnumValueExist]
        [Display(Name = "公司类型")]
        public CompanyTypeEnum? CompanyType { get; set; }

        /// <summary>
        /// 公司详细地址
        /// </summary>
        [SugarColumn(Length = 254, IsNullable = true, ColumnDescription = "公司详细地址")]
        [StringMaxLength(254)]
        [Display(Name = "公司详细地址")]
        public string DetailAddress { get; set; }

        /// <summary>
        /// 授权商品数量
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "授权商品数量", DecimalDigits = 4)]
        [NumberDecimal(4)]
        [Display(Name = "授权商品数量")]
        public decimal EmpowerWaresCount { get; set; }

        /// <summary>
        /// 是否禁用
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "是否禁用")]
        [NotNull]
        [Display(Name = "是否禁用")]
        public bool? IsForbidden { get; set; }

        /// <summary>
        /// 省CODE
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "省CODE")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "省CODE")]
        public string ProvinceCode { get; set; }

        /// <summary>
        /// 市CODE
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "市CODE")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "市CODE")]
        public string CityCode { get; set; }

        /// <summary>
        /// 区CODE
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "区CODE")]
        [NotNull]
        [StringMaxLength(50)]
        [Display(Name = "区CODE")]
        public string AreaCode { get; set; }

        /// <summary>
        /// 是否是管理员
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "是否是管理员")]
        [Display(Name = "是否是管理员")]
        public bool IsAdmin { get; set; }

        private static Func<CompanyInfo, string> GetSearchSql()
        {
            return parameter =>
            {
                StringBuilder queryString = new StringBuilder();

                queryString.Append(" 1 = 1 ");

                if (!string.IsNullOrWhiteSpace(parameter?.CompanyName))
                    queryString.Append($" AND {nameof(CompanyName)} LIKE '%{parameter.CompanyName.Trim()}%' ");

                if (!string.IsNullOrWhiteSpace(parameter?.CompanyCode))
                    queryString.Append($" AND {nameof(CompanyCode)} LIKE '%{parameter.CompanyCode.Trim()}%' ");

                if (parameter?.CompanyType.HasValue ?? false)
                    queryString.Append($" AND {nameof(CompanyType)} = {parameter.CompanyType} ");

                if (!string.IsNullOrWhiteSpace(parameter.RootName))
                    queryString.Append($" AND {nameof(RootName)} LIKE '%{parameter.RootName.Trim()}%' ");

                if (parameter?.IsForbidden.HasValue ?? false)
                    queryString.Append($" AND {nameof(IsForbidden)} = '{parameter.IsForbidden.Value}' ");

                return queryString.ToString();
            };
        }
    }
}
