using HeadQuartersERP.Validation;
using SqlSugar;
using System;
using System.Linq.Expressions;

namespace HeadQuartersERP.Model.Commodity
{
    /// <summary>
    /// 单位
    /// </summary>
    [LinqSearch(typeof(UnitInfo), nameof(GetSearchLinq))]
    public class UnitInfo : ViewModelBase
    {
        /// <summary>
        /// 单位中文名
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "单位中文名")]
        [Unique]
        public string Name { get; set; }

        /// <summary>
        /// 英文名
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "单位英文名")]
        [Unique]
        public string EngName { get; set; }

        /// <summary>
        /// 禁用
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "禁用")]
        public bool? IsForbidden { get; set; }

        private static Func<UnitInfo, Expression<Func<UnitInfo, bool>>> GetSearchLinq()
        {
            return parameter =>
            {
                if (string.IsNullOrWhiteSpace(parameter.Name))
                    return item => true;

                else
                    return item => item.Name.Contains(parameter.Name);
            };
        }
    }
}
