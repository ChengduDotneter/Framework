using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.DAL.ETL
{
    public class ETLHelper
    {
        public static ETLTask Transform(IEnumerable<Type> modelTypes, Func<Type, object> sourceSearchQueryFactory, Func<Type, object> destEditQueryFactory, int pageSize = 1024)
        {
            IList<Type> sourceTables = new List<Type>();
            IList<ETLTable> complatedTables = new List<ETLTable>();

            ETLTask etlTask = new ETLTask(sourceTables, complatedTables);
            etlTask.Task = Task.CompletedTask;

            foreach (Type modelType in modelTypes)
            {
                ETLTable etlTable = new ETLTable(modelType);
                sourceTables.Add(modelType);

                etlTask.Task = etlTask.Task.ContinueWith((task) =>
                {
                    etlTask.RunningTable = etlTable;
                    Transform(etlTable, pageSize, sourceSearchQueryFactory, destEditQueryFactory);
                    complatedTables.Add(etlTable);
                });
            }

            etlTask.Task.Start();
            return etlTask;
        }

        private static void Transform(ETLTable etlTable, int pageSize, Func<Type, object> sourceSearchQueryFactory, Func<Type, object> destEditQueryFactory)
        {
            dynamic searchQuery = sourceSearchQueryFactory.Invoke(etlTable.TableType);
            dynamic editQuery = destEditQueryFactory.Invoke(etlTable.TableType);

            Type searchQueryType = typeof(ISearchQuery<>).MakeGenericType(etlTable.TableType);
            Type editQueryType = typeof(IEditQuery<>).MakeGenericType(etlTable.TableType);

            if (searchQuery.GetType().GetInterface(searchQueryType.Name) == null ||
                editQuery.GetType().GetInterface(editQueryType.Name) == null)
                throw new NotSupportedException();

            etlTable.DataCount = searchQuery.Count();
            IList<object> preperInsertDatas = new List<object>();

            while (etlTable.ComplatedCount < etlTable.DataCount)
            {
                long residueCount = etlTable.DataCount - etlTable.ComplatedCount;
                long size = residueCount < pageSize ? residueCount : pageSize;
                preperInsertDatas.Clear();

                foreach (object data in searchQuery.Search(startIndex: etlTable.ComplatedCount, count: size))
                    preperInsertDatas.Add(data);

                dynamic datas = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast)).MakeGenericMethod(etlTable.TableType).Invoke(null, new object[] { preperInsertDatas });
                editQuery.Merge(datas);

                etlTable.ComplatedCount += preperInsertDatas.Count;
            }
        }
    }
}
