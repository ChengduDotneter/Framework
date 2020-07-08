using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Cache.Configuration;
using Apache.Ignite.Core.Cache.Query;
using Apache.Ignite.Linq;
using Common.Compute;
using Common.DAL;
using Microsoft.Extensions.Hosting;
using SqlSugar;
using ICompute = Common.Compute.ICompute;

namespace TestConsole
{
    public class LogData : IEntity
    {
        [SugarColumn(IsNullable = false, IsPrimaryKey = true, ColumnName = "id")]
        [QuerySqlField(NotNull = true)]
        public long ID { get; set; }

        [SugarColumn(IsNullable = false, ColumnName = "cd")]
        [QuerySqlField]
        public int CD { get; set; }

        [SugarColumn(IsNullable = false, ColumnName = "sid")]
        [QuerySqlField]
        public byte SID { get; set; }

        [SugarColumn(IsNullable = false, ColumnName = "mid")]
        [QuerySqlField]
        public int MID { get; set; }

        [SugarColumn(IsNullable = false, Length = 255, ColumnName = "ip")]
        [QuerySqlField]
        public string IP { get; set; }

        [SugarColumn(IsNullable = false, Length = 255, ColumnName = "mac")]
        [QuerySqlField]
        public string MAC { get; set; }

        [SugarColumn(IsNullable = false, Length = 15, ColumnName = "client")]
        [QuerySqlField]
        public string Client { get; set; }

        [SugarColumn(IsNullable = false, Length = 255, ColumnName = "controller")]
        [QuerySqlField]
        public string Controller { get; set; }

        [SugarColumn(IsNullable = false, Length = 255, ColumnName = "method")]
        [QuerySqlField]
        public string Method { get; set; }

        [SugarColumn(IsNullable = false, ColumnDataType = "text", ColumnName = "param")]
        [QuerySqlField]
        public string Param { get; set; }

        [SugarColumn(IsNullable = false, Length = 32, ColumnName = "unique_data")]
        [QuerySqlField]
        public string UnqiueData { get; set; }

        [SugarColumn(IsNullable = false, ColumnName = "type")]
        [QuerySqlField]
        public int Type { get; set; }

        [SugarColumn(IsNullable = false, ColumnName = "type_id")]
        [QuerySqlField]
        public int TypeID { get; set; }
    }

    public class ComputeTestTask : IHostedService
    {
        private readonly ISearchQuery<LogData> m_logDataSearchQuery;
        private readonly ICompute m_compute;
        private readonly IMapReduce m_mapReduce;
        private readonly IAsyncMapReduce m_asyncMapReduce;

        public ComputeTestTask(ISearchQuery<LogData> logDataSearchQuery, IMapReduce mapReduce, IAsyncMapReduce asyncMapReduce, ICompute compute)
        {
            m_logDataSearchQuery = logDataSearchQuery;
            m_mapReduce = mapReduce;
            m_asyncMapReduce = asyncMapReduce;
            m_compute = compute;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(async () =>
            {
                bool wait = true;

                while (wait)
                    await Task.Delay(1000);

                while (true)
                {
                    Console.WriteLine("start task...");

                    int time = Environment.TickCount;

                    IEnumerable<LogDataJobResult> result = m_mapReduce.Excute(new LogDataMapReduceTask(), string.Empty);

                    //foreach (LogDataJobResult item in result)
                    //    Console.WriteLine($"timestamp: {item.TimeStamp}, count: {item.Count}");

                    Console.WriteLine($"task done, total time: {Environment.TickCount - time}");

                    await Task.Delay(3000);
                }
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (m_mapReduce != null)
                m_mapReduce.Dispose();

            if (m_asyncMapReduce != null && m_asyncMapReduce.Running)
                m_asyncMapReduce.Cancel();

            if (m_asyncMapReduce != null)
                m_asyncMapReduce.Dispose();

            return Task.CompletedTask;
        }
    }

    public class LogDataJobResult
    {
        public int Count { get; set; }
        public string TimeStamp { get; set; }
        public string NodeID { get; set; }
    }

    public class LogDataSplitParameter
    {
        public long ID { get; set; }
        public int Limit { get; set; }
    }

    public class LogDataFunc : IComputeFunc<LogDataSplitParameter, IEnumerable<LogDataJobResult>>
    {
        public IEnumerable<LogDataJobResult> Excute(LogDataSplitParameter logDataSplitParameter)
        {
            Console.WriteLine($"id: {logDataSplitParameter.ID}, limit: {logDataSplitParameter.Limit}");

            int time = Environment.TickCount;

            IList<LogDataJobResult> logDataJobResults = new List<LogDataJobResult>();

            var result = Ignition.GetIgnite().GetCache<long, LogData>("LogData").Query(new SqlFieldsQuery(
                $@"SELECT
	                    COUNT(*) AS NUM,
	                    TIME_STAMP
                    FROM
	                    (
	                    SELECT
		                    A.UNQIUEDATA, A.TIME_STAMP
	                    FROM
		                    (
		                    SELECT
			                    UNQIUEDATA, SID, MID, FORMATDATETIME (DATEADD('SECOND', CD, '1970-1-1'), 'yyyy-MM-dd HH') AS TIME_STAMP
		                    FROM LOGDATA
                            WHERE
                                ID > {logDataSplitParameter.ID}
                            LIMIT {logDataSplitParameter.Limit}) A
                        WHERE
                            A.MID = 0
                            AND A.SID = 1
                        GROUP BY
                            A.UNQIUEDATA, A.TIME_STAMP) A
                    GROUP BY
                       TIME_STAMP
                    ORDER BY
                        TIME_STAMP", true));

            foreach (var item in result)
                logDataJobResults.Add(new LogDataJobResult() { Count = Convert.ToInt32(item[0]), TimeStamp = Convert.ToString(item[1]), NodeID = Ignition.GetIgnite().GetCluster().GetLocalNode().ConsistentId.ToString() });

            Console.WriteLine($"job time: {Environment.TickCount - time}");

            return logDataJobResults;

            //var data = Ignition.GetIgnite().GetCache<long, LogData>("LogData").GroupBy(item => new DateTime(1970, 1, 1).AddSeconds(item.Value.CD).ToString("yyyy-MM-dd HH")).Select(item => new LogDataJobResult() { TimeStamp = item.Key, Count = item.Count() });

            //return data;
        }
    }

    public class LogDataMapReduceTask : IMapReduceTask<string, IEnumerable<LogDataJobResult>, LogDataSplitParameter, IEnumerable<LogDataJobResult>>
    {
        public IEnumerable<LogDataJobResult> Reduce(IEnumerable<IEnumerable<LogDataJobResult>> splitResults)
        {
            foreach (var item in splitResults)
            {
                Console.WriteLine(item.First().NodeID);
            }

            return splitResults.SelectMany(item => item).GroupBy(item => item.TimeStamp).Select(item => new LogDataJobResult() { TimeStamp = item.Key, Count = item.Sum(item => item.Count) });
        }

        public IEnumerable<MapReduceSplitJob<LogDataSplitParameter, IEnumerable<LogDataJobResult>>> Split(int nodeCount, string parameter)
        {
            int count = Ignition.GetIgnite().GetCache<long, LogData>("LogData").AsCacheQueryable().Count();
            int splitCount = count / nodeCount;

            MapReduceSplitJob<LogDataSplitParameter, IEnumerable<LogDataJobResult>>[] mapReduceSplitJobs = new MapReduceSplitJob<LogDataSplitParameter, IEnumerable<LogDataJobResult>>[nodeCount];

            for (int i = 0; i < nodeCount; i++)
            {
                mapReduceSplitJobs[i] = new MapReduceSplitJob<LogDataSplitParameter, IEnumerable<LogDataJobResult>>();
                long id = (long)Ignition.GetIgnite().GetCache<long, LogData>("LogData").Query(new SqlFieldsQuery($"SELECT ID FROM LOGDATA LIMIT 1 OFFSET {i * splitCount}")).ToArray()[0].ToArray()[0];
                mapReduceSplitJobs[i].Parameter = new LogDataSplitParameter() { ID = id, Limit = splitCount };
                mapReduceSplitJobs[i].ComputeFunc = new LogDataFunc();
            }

            return mapReduceSplitJobs;
        }
    }
}
