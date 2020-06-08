using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.DAL.ETL
{
    public class ETLTable
    {
        public Type TableType { get; }
        public long DataCount { get; internal set; }
        public long ComplatedCount { get; internal set; }

        public ETLTable(Type tableType)
        {
            TableType = tableType;
        }
    }

    public class ETLTask
    {
        public IEnumerable<Type> SourceTables { get; }
        public IEnumerable<ETLTable> ComplatedTables { get; }
        public ETLTable RunningTable { get; internal set; }
        public Task Task { get; internal set; }

        internal ETLTask(IEnumerable<Type> sourceTables, IEnumerable<ETLTable> complatedTables)
        {
            SourceTables = sourceTables;
            ComplatedTables = complatedTables;
        }
    }
}
