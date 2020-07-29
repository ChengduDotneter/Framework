using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Common.DAL.ETL
{
    /// <summary>
    /// 关系型数据库对Nosql的转换帮助类
    /// </summary>
    public class ETLHelper
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="modelTypes"></param>
        /// <param name="sourceSearchQueryFactory"></param>
        /// <param name="destEditQueryFactory"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static ETLTask Transform(IEnumerable<Type> modelTypes, Func<Type, object> sourceSearchQueryFactory, Func<Type, object> destEditQueryFactory, int pageSize = 1024)
        {
            IList<ETLTable> complatedTables = new List<ETLTable>();

            ETLTask etlTask = new ETLTask(modelTypes, complatedTables);

            etlTask.Task = Task.Factory.StartNew(() =>
            {
                foreach (Type modelType in modelTypes)
                {
                    ETLTable etlTable = new ETLTable(modelType);
                    etlTask.RunningTable = etlTable;
                    Transform(etlTable, pageSize, sourceSearchQueryFactory, destEditQueryFactory);
                    complatedTables.Add(etlTable);
                }
            });

            return etlTask;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="etlTable"></param>
        /// <param name="pageSize"></param>
        /// <param name="sourceSearchQueryFactory"></param>
        /// <param name="destEditQueryFactory"></param>
        private static void Transform(ETLTable etlTable, int pageSize, Func<Type, object> sourceSearchQueryFactory, Func<Type, object> destEditQueryFactory)
        {
            object searchQuery = sourceSearchQueryFactory.Invoke(etlTable.TableType);
            object editQuery = destEditQueryFactory.Invoke(etlTable.TableType);

            Type searchQueryType = typeof(ISearchQuery<>).MakeGenericType(etlTable.TableType);
            Type editQueryType = typeof(IEditQuery<>).MakeGenericType(etlTable.TableType);

            if (searchQuery.GetType().GetInterface(searchQueryType.Name) == null ||
                editQuery.GetType().GetInterface(editQueryType.Name) == null)
                throw new NotSupportedException();

            Type predicateType = typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(etlTable.TableType, typeof(bool)));
            Type queryOrderBiesType = typeof(IEnumerable<>).MakeGenericType(typeof(QueryOrderBy<>).MakeGenericType(etlTable.TableType));
            etlTable.DataCount = (int)searchQueryType.GetMethod("Count", new Type[] { predicateType }).Invoke(searchQuery, new object[] { null, null });
            IList<object> preperInsertDatas = new List<object>();

            while (etlTable.ComplatedCount < etlTable.DataCount)
            {
                int residueCount = etlTable.DataCount - etlTable.ComplatedCount;
                int size = residueCount < pageSize ? residueCount : pageSize;
                preperInsertDatas.Clear();

                IEnumerable<object> query = (IEnumerable<object>)searchQueryType.GetMethod("Search", new Type[] { predicateType, queryOrderBiesType, typeof(int), typeof(int) }).Invoke(searchQuery, new object[] { null, null, etlTable.ComplatedCount, size, null });

                foreach (object data in query)
                    preperInsertDatas.Add(data);

                object datas = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray)).MakeGenericMethod(etlTable.TableType).Invoke(null, new object[] { typeof(Enumerable).GetMethod(nameof(Enumerable.Cast)).MakeGenericMethod(etlTable.TableType).Invoke(null, new object[] { preperInsertDatas }) });
                editQueryType.GetMethod("Merge").Invoke(editQuery, new object[] { null, datas });
                etlTable.ComplatedCount += preperInsertDatas.Count;
            }
        }
    }
}