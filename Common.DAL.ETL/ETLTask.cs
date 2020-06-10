using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.DAL.ETL
{
    /// <summary>
    /// 转换表实体
    /// </summary>
    public class ETLTable
    {
        /// <summary>
        /// 表类型
        /// </summary>
        public Type TableType { get; }

        /// <summary>
        /// 数据条数
        /// </summary>
        public int DataCount { get; internal set; }

        /// <summary>
        /// 满足条数
        /// </summary>
        public int ComplatedCount { get; internal set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="tableType"></param>
        public ETLTable(Type tableType)
        {
            TableType = tableType;
        }
    }

    /// <summary>
    /// 转换任务类
    /// </summary>
    public class ETLTask
    {
        /// <summary>
        /// 源数据表集合
        /// </summary>
        public IEnumerable<Type> SourceTables { get; }

        /// <summary>
        /// 满足表集合
        /// </summary>
        public IEnumerable<ETLTable> ComplatedTables { get; }

        /// <summary>
        /// 正在转换的表
        /// </summary>
        public ETLTable RunningTable { get; internal set; }

        /// <summary>
        /// 转换的任务
        /// </summary>
        public Task Task { get; internal set; }

        internal ETLTask(IEnumerable<Type> sourceTables, IEnumerable<ETLTable> complatedTables)
        {
            SourceTables = sourceTables;
            ComplatedTables = complatedTables;
        }
    }
}