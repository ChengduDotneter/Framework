using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Cache.Configuration;
using Common;
using Common.Compute;
using Common.DAL;
using Common.ServiceCommon;
using Microsoft.Extensions.Hosting;
using SqlSugar;

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
        private readonly IComputeFactory m_computeFactory;
        private readonly IMapReduce m_mapReduce;
        private readonly IAsyncMapReduce m_asyncMapReduce;

        public ComputeTestTask(IComputeFactory computeFactory, IMapReduce mapReduce, IAsyncMapReduce asyncMapReduce)
        {
            m_computeFactory = computeFactory;
            m_mapReduce = mapReduce;
            m_asyncMapReduce = asyncMapReduce;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(async () =>
            {
                bool wait = false;

                while (wait)
                    await Task.Delay(1000);

                while (true)
                {
                    Console.WriteLine("start task...");

                    int time = Environment.TickCount;

                    IEnumerable<LogDataResult> result = m_mapReduce.Excute(m_computeFactory.CreateComputeMapReduceTask<LogDataMapReduceTask, string, IEnumerable<LogDataResult>, LogDataSplitParameter, LogDataSplitResult>(), string.Empty);

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

    public class LogDataResult
    {
        public int Count { get; set; }
        public string TimeStamp { get; set; }
    }

    public class LogDataSplitResult
    {
        public IEnumerable<LogDataResult> LogDataResults { get; set; }
        public string IPAddress { get; set; }
    }

    public class LogDataSplitParameter
    {
        public long ID { get; set; }
        public int Limit { get; set; }
    }

    public class LogDataFunc : IComputeFunc<LogDataSplitParameter, LogDataSplitResult>
    {
        private ISearchQuery<LogData> m_searchQuery;

        public LogDataFunc(ISearchQuery<LogData> searchQuery)
        {
            m_searchQuery = searchQuery;
        }

        public LogDataSplitResult Excute(LogDataSplitParameter logDataSplitParameter)
        {
            Console.WriteLine($"id: {logDataSplitParameter.ID}, limit: {logDataSplitParameter.Limit}");

            int time = Environment.TickCount;

            IList<LogDataResult> logDataJobResults = new List<LogDataResult>();

            var result = m_searchQuery.Query(
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
                                ID > @ID
                            LIMIT @LIMIT) A
                        WHERE
                            A.MID = 0
                            AND A.SID = 1
                        GROUP BY
                            A.UNQIUEDATA, A.TIME_STAMP) A
                    GROUP BY
                       TIME_STAMP
                    ORDER BY
                        TIME_STAMP",
                new Dictionary<string, object>()
                {
                    ["@ID"] = logDataSplitParameter.ID,
                    ["@LIMIT"] = logDataSplitParameter.Limit
                });

            foreach (var item in result)
                logDataJobResults.Add(new LogDataResult() { Count = Convert.ToInt32(item["NUM"]), TimeStamp = Convert.ToString(item["TIME_STAMP"]) });

            Console.WriteLine($"job time: {Environment.TickCount - time}");

            return new LogDataSplitResult()
            {
                LogDataResults = logDataJobResults,
                IPAddress = ConfigManager.Configuration["IgniteService:LocalHost"]
            };
        }
    }

    public class LogDataMapReduceTask : IMapReduceTask<string, IEnumerable<LogDataResult>, LogDataSplitParameter, LogDataSplitResult>
    {
        private IComputeFactory m_computeFactory;
        private ISearchQuery<LogData> m_searchQuery;

        public LogDataMapReduceTask(IComputeFactory computeFactory, ISearchQuery<LogData> searchQuery)
        {
            m_computeFactory = computeFactory;
            m_searchQuery = searchQuery;
        }

        public IEnumerable<LogDataResult> Reduce(IEnumerable<LogDataSplitResult> splitResults)
        {
            foreach (var item in splitResults)
            {
                Console.WriteLine(item.IPAddress);
            }

            return splitResults.SelectMany(item => item.LogDataResults).GroupBy(item => item.TimeStamp).Select(item => new LogDataResult() { TimeStamp = item.Key, Count = item.Sum(item => item.Count) });
        }

        public IEnumerable<MapReduceSplitJob<LogDataSplitParameter, LogDataSplitResult>> Split(int nodeCount, string parameter)
        {
            int count = m_searchQuery.Count();
            int splitCount = count / nodeCount;

            MapReduceSplitJob<LogDataSplitParameter, LogDataSplitResult>[] mapReduceSplitJobs = new MapReduceSplitJob<LogDataSplitParameter, LogDataSplitResult>[nodeCount];

            for (int i = 0; i < nodeCount; i++)
            {
                mapReduceSplitJobs[i] = new MapReduceSplitJob<LogDataSplitParameter, LogDataSplitResult>();
                long id = m_searchQuery.Search(startIndex: i * splitCount, count: 1).First().ID;
                mapReduceSplitJobs[i].Parameter = new LogDataSplitParameter() { ID = id, Limit = splitCount };
                mapReduceSplitJobs[i].ComputeFunc = m_computeFactory.CreateComputeFunc<LogDataFunc, LogDataSplitParameter, LogDataSplitResult>();
            }

            return mapReduceSplitJobs;
        }
    }
}
