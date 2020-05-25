using Common.Model;
using Common.Validation;
using SqlSugar;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TestModel
{
    /// <summary>
    /// AppSecret
    /// </summary>
    [SqlSearch(typeof(SystemInfo), nameof(GetSearchSql))]
    public class SystemInfo : ViewModelBase
    {
        /// <summary>
        /// 系统名称
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "系统名称")]
        [NotNull]
        [Unique]
        [StringMaxLength(50)]
        [Display(Name = "系统名称")]
        public string SystemName { get; set; }

        /// <summary>
        /// AppSecret
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "AppSecret")]
        [NotNull]
        [Unique]
        [StringMaxLength(50)]
        [Display(Name = "AppSecret")]
        public string AppSecret { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "是否启用")]
        [NotNull]
        [Display(Name = "是否启用")]
        public bool? IsActive { get; set; }

        /// <summary>
        /// 用户状态
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        [Display(Name = "用户状态")]
        public bool? UserStatus { get; set; }

        private static Func<SystemInfo, string> GetSearchSql()
        {
            return parameter =>
            {
                StringBuilder queryString = new StringBuilder();

                queryString.Append(" 1 = 1 ");

                if (!string.IsNullOrWhiteSpace(parameter?.SystemName))
                    queryString.Append($" AND {nameof(SystemName)} LIKE '%{parameter.SystemName.Trim()}%' ");

                return queryString.ToString();
            };
        }
    }
}
