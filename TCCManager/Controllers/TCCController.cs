using Common;
using Common.Log;
using Common.Model;
using Common.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace TCCManager.Controllers
{
    public class TCCModel
    {
        public long ID { get; set; }
        public int TimeOut { get; set; }

        [NotNull]
        public IEnumerable<TCCNodeModel> TCCNodes { get; set; }
    }

    public class TCCNodeModel
    {
        public string Url { get; set; }
        public string TryUrl { get; set; }
        public string CancelUrl { get; set; }
        public string CommitUrl { get; set; }

        [NotNull]
        [JsonConverter(typeof(JObjectConverter))]
        public JObject TryContent { get; set; }
    }

    public class NodeResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    [Route("tcc")]
    [ApiController]
    public class TCCController : ControllerBase
    {
        private const int TASK_TIME_OUT = 500;
        private const int TCCTASK_SCHEDULER_COUNT = 40;
        private const string TRY = "try";
        private const string CANCEL = "cancel";
        private const string COMMIT = "commit";
        private readonly static int MIN_TIMEOUT;
        private readonly static ILogHelper m_logHelper;
        private readonly static BlockingCollection<TCCTaskScheduler> m_tccTaskSchedulers;
        private IHttpContextAccessor m_httpContextAccessor;

        public TCCController(IHttpContextAccessor httpContextAccessor)
        {
            m_httpContextAccessor = httpContextAccessor;
        }

        private static Dictionary<long, CancellationTokenSource> a = new Dictionary<long, CancellationTokenSource>();

        [HttpPost]
        public long Post(TCCModel tccModel)
        {
            TCCTaskScheduler tccTaskScheduler;
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(TASK_TIME_OUT));

            try
            {
                tccTaskScheduler = m_tccTaskSchedulers.Take(cancellationTokenSource.Token);
            }
            catch
            {
                throw new DealException("TCC事务繁忙，请稍后再试。");
            }

            tccModel.TimeOut = Math.Max(tccModel.TimeOut, MIN_TIMEOUT);
            tccModel.ID = IDGenerator.NextID();

            IEnumerable<NodeResult> errorNodeResults;
            IList<Task<NodeResult>> tryTasks = new List<Task<NodeResult>>();
            IList<Task<NodeResult>> cancelTasks = new List<Task<NodeResult>>();
            IList<Task<NodeResult>> commitTasks = new List<Task<NodeResult>>();

            m_logHelper.TCCServer(tccModel.ID, "启动TCC事务");

            try
            {
                a.Add(tccModel.ID, new CancellationTokenSource());
                Task.Factory.StartNew((state) =>
                {
                    try
                    {
                        Task.Delay(TimeSpan.FromSeconds(10), a[tccModel.ID].Token).Wait();
                        Console.WriteLine(tccModel.ID);
                    }
                    catch
                    {

                    }
                }, null, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                //TODO: TCC节点日志
                foreach (TCCNodeModel tccNode in tccModel.TCCNodes)
                {
                    if (string.IsNullOrWhiteSpace(tccNode.Url) &&
                       (string.IsNullOrWhiteSpace(tccNode.TryUrl) || string.IsNullOrWhiteSpace(tccNode.CancelUrl) || string.IsNullOrWhiteSpace(tccNode.CommitUrl)))
                    {
                        throw new DealException("当Url为空时，TryUrl，CancelUrl，CommintUrl不能为空。");
                    }

                    string tryUrl = string.IsNullOrWhiteSpace(tccNode.TryUrl) ? GetTryUrl(tccNode.Url) : tccNode.TryUrl;
                    string cancelUrl = string.IsNullOrWhiteSpace(tccNode.CancelUrl) ? GetCancelUrl(tccNode.Url) : tccNode.CancelUrl;
                    string commitUrl = string.IsNullOrWhiteSpace(tccNode.CommitUrl) ? GetCommitUrl(tccNode.Url) : tccNode.CommitUrl;

                    tryTasks.Add(new Task<NodeResult>(() =>
                    {
                        HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpPostByAbsoluteUri(
                                    $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}/{tryUrl}/{tccModel.ID}/{tccModel.TimeOut}",
                                    tccNode.TryContent,
                                    m_httpContextAccessor?.HttpContext?.Request.Headers["Authorization"]);

                        NodeResult nodeResult = new NodeResult() { Success = true };

                        if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
                        {
                            nodeResult.Success = false;
                            nodeResult.ErrorMessage = $"{tryUrl} Try失败{Environment.NewLine}详细信息：{httpResponseMessage.Content.ReadAsStringAsync().Result}{Environment.NewLine}Data：{tccNode.TryContent}。";
                        }

                        return nodeResult;
                    }));

                    cancelTasks.Add(new Task<NodeResult>(() =>
                    {
                        HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpPostByAbsoluteUri(
                                    $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}/{cancelUrl}/{tccModel.ID}",
                                    null,
                                    bearerToken: m_httpContextAccessor?.HttpContext?.Request.Headers["Authorization"]);

                        a[tccModel.ID].Cancel();

                        NodeResult nodeResult = new NodeResult() { Success = true };

                        if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
                        {
                            nodeResult.Success = false;
                            nodeResult.ErrorMessage = $"{cancelUrl} Cancel失败{Environment.NewLine}详细信息：{httpResponseMessage.Content.ReadAsStringAsync().Result}。";
                        }

                        return nodeResult;
                    }));

                    commitTasks.Add(new Task<NodeResult>(() =>
                    {
                        HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpPostByAbsoluteUri(
                                    $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}/{commitUrl}/{tccModel.ID}",
                                    null,
                                    m_httpContextAccessor?.HttpContext?.Request.Headers["Authorization"]);

                        a[tccModel.ID].Cancel();

                        NodeResult nodeResult = new NodeResult() { Success = true };

                        if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
                        {
                            nodeResult.Success = false;
                            nodeResult.ErrorMessage = $"{commitUrl} Commit失败{Environment.NewLine}详细信息：{httpResponseMessage.Content.ReadAsStringAsync().Result}。";
                        }

                        return nodeResult;
                    }));
                }

                tryTasks.ForEach(tryTask => tryTask.Start(tccTaskScheduler));
                errorNodeResults = Task.Factory.ContinueWhenAll(tryTasks.ToArray(),
                                                                       GetErrorResult,
                                                                       CancellationToken.None,
                                                                       TaskContinuationOptions.None,
                                                                       tccTaskScheduler).Result;

                if (errorNodeResults != null && errorNodeResults.Count() > 0)
                {
                    cancelTasks.ForEach(cancelTask => cancelTask.Start(tccTaskScheduler));
                    errorNodeResults = errorNodeResults.Concat(Task.Factory.ContinueWhenAll(cancelTasks.ToArray(),
                                                                                                   GetErrorResult,
                                                                                                   CancellationToken.None,
                                                                                                   TaskContinuationOptions.None,
                                                                                                   tccTaskScheduler).Result);

                    string errorText = $"{tccModel.ID}请求失败{Environment.NewLine}{string.Join(Environment.NewLine, errorNodeResults.Select(errorNodeResult => errorNodeResult.ErrorMessage))}";

                    //TODO: 监控中心日志
                    m_logHelper.TCCNode(tccModel.ID, false, errorText);
                    throw new DealException(errorText);
                }

                commitTasks.ForEach(commitTask => commitTask.Start(tccTaskScheduler));
                errorNodeResults = Task.Factory.ContinueWhenAll(commitTasks.ToArray(),
                                                                      GetErrorResult,
                                                                      CancellationToken.None,
                                                                      TaskContinuationOptions.None,
                                                                      tccTaskScheduler).Result;

                if (errorNodeResults != null && errorNodeResults.Count() > 0)
                {
                    string errorText = $"{tccModel.ID}请求失败{Environment.NewLine}{string.Join(Environment.NewLine, errorNodeResults.Select(errorNodeResult => errorNodeResult.ErrorMessage))}";

                    //TODO: 监控中心日志
                    m_logHelper.TCCNode(tccModel.ID, false, errorText);
                    throw new DealException(errorText);
                }

                return tccModel.ID;
            }
            catch
            {
                throw;
            }
            finally
            {
                m_tccTaskSchedulers.Add(tccTaskScheduler);
            }
        }

        private static IEnumerable<NodeResult> GetErrorResult(IEnumerable<Task<NodeResult>> tasks)
        {
            return tasks.Where(task => !task.Result.Success).Select(task => task.Result);
        }

        private static string GetTryUrl(string url)
        {
            return $"{url}/{TRY}";
        }

        private static string GetCancelUrl(string url)
        {
            return $"{url}/{CANCEL}";
        }

        private static string GetCommitUrl(string url)
        {
            return $"{url}/{COMMIT}";
        }

        static TCCController()
        {
            m_logHelper = LogHelperFactory.GetKafkaLogHelper();
            MIN_TIMEOUT = Convert.ToInt32(ConfigManager.Configuration["MinTimeOut"]);
            m_tccTaskSchedulers = new BlockingCollection<TCCTaskScheduler>();

            for (int i = 0; i < TCCTASK_SCHEDULER_COUNT; i++)
                m_tccTaskSchedulers.Add(new TCCTaskScheduler(i));
        }
    }

    internal class TCCTaskScheduler : TaskScheduler
    {
        private BlockingCollection<Task> m_tasks;
        private Thread m_thread;
        private CancellationTokenSource m_cancellationTokenSource;

        public int TaskSchedulerID { get; }

        private void DoWork()
        {
            while (true)
            {
                try
                {
                    Task task = m_tasks.Take(m_cancellationTokenSource.Token);
                    TryExecuteTask(task);
                }
                catch
                {
                    continue;
                }
            }
        }

        public TCCTaskScheduler(int taskSchedulerID)
        {
            TaskSchedulerID = taskSchedulerID;
            m_cancellationTokenSource = new CancellationTokenSource();
            m_tasks = new BlockingCollection<Task>();

            m_thread = new Thread(DoWork);
            m_thread.IsBackground = true;
            m_thread.Name = $"TCC_THREAD_{taskSchedulerID}";
            m_thread.Start();
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return m_tasks;
        }

        protected override void QueueTask(Task task)
        {
            m_tasks.Add(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }
    }
}
