using HeadQuartersERP.DAL;
using HeadQuartersERP.Model.Enums;
using HeadQuartersERP.Validation;
using SqlSugar;
using System;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace HeadQuartersERP.Model.Company
{
    /// <summary>
    /// 公司-业务类型
    /// </summary>
    [LinqSearch(typeof(CompanyBussinessTypeLink), nameof(GetSearchLinq))]
    public class CompanyBussinessTypeLink : ViewModelBase
    {
        [SugarColumn(IsNullable = false, ColumnDescription = "公司ID")]
        [NotNull]
        [ForeignKey(typeof(CompanyInfo), nameof(IEntity.ID))]
        [Foreign(nameof(CompanyInfo), nameof(IEntity.ID))]
        [JsonConverter(typeof(ObjectIdNullableConverter))]
        public long? CompanyID { get; set; }

        [SugarColumn(IsNullable = false, ColumnDescription = "公司业务类型")]
        [NotNull]
        [EnumValueExist]
        public CompanyBussinessTypeEnum? CompanyBussinessTypeEnum { get; set; }


        private static Func<CompanyBussinessTypeLink, Expression<Func<CompanyBussinessTypeLink, bool>>> GetSearchLinq()
        {
            return parameter =>
            {
                if (parameter?.CompanyID.HasValue ?? false)
                    return companyBussinessTypeLink => companyBussinessTypeLink.CompanyID == parameter.CompanyID.Value;

                else
                    return companyInfo => true;
            };
        }
    }
}
