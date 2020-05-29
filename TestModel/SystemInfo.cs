using Common;
using Common.Model;
using Common.Validation;
using SqlSugar;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace TestModel
{
    /// <summary>
    /// AppSecret
    /// </summary>
    [LinqSearch(typeof(SystemInfo), nameof(GetSearchLinq))]
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


        private static Func<SystemInfo, Expression<Func<SystemInfo, bool>>> GetSearchLinq()
        {
            return parameter =>
            {
                Expression<Func<SystemInfo, bool>> linq = item => true;

                if (!string.IsNullOrWhiteSpace(parameter?.SystemName))
                    linq = linq.And(item => item.SystemName.Contains(parameter.SystemName));
                return linq;
            };
        }
    }
}
