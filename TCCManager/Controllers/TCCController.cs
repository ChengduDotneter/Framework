using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Common;
using Common.Lock;
using Common.Log;
using Common.Model;
using Common.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace TCCManager.Controllers
{
    /// <summary>
    /// TCCModel
    /// </summary>
    public class TCCModel
    {
        /// <summary>
        /// ID
        /// </summary>
        public long ID { get; set; }

        /// <summary>
        /// TimeOut
        /// </summary>
        public int TimeOut { get; set; }

        /// <summary>
        /// DeadLockFlag
        /// </summary>
        public string DeadLockFlag { get; set; }

        /// <summary>
        /// TccNodes
        /// </summary>
        [NotNull]
        public IEnumerable<TCCNodeModel> TCCNodes { get; set; }
    }

    /// <summary>
    /// TCCNodeModel
    /// </summary>
    public class TCCNodeModel
    {
        /// <summary>
        /// Url
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// TryUrl
        /// </summary>
        public string TryUrl { get; set; }

        /// <summary>
        /// CancelUrl
        /// </summary>
        public string CancelUrl { get; set; }

        /// <summary>
        /// CommitUrl
        /// </summary>
        public string CommitUrl { get; set; }

        /// <summary>
        /// TryContent
        /// </summary>
        [NotNull]
        [JsonConverter(typeof(JObjectConverter))]
        public JObject TryContent { get; set; }
    }

    /// <summary>
    /// NodeResult
    /// </summary>
    public class NodeResult
    {
        /// <summary>
        /// Success
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// ErrorMessage
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// TCCController
    /// </summary>
    [Route("tcc")]
    [ApiController]
    public class TCCController : ControllerBase
    {
        private const string TRY = "try";
        private const string CANCEL = "cancel";
        private const string COMMIT = "commit";
        private readonly static int MIN_TIMEOUT;
        private readonly static ILogHelper m_logHelper;
        private IHttpContextAccessor m_httpContextAccessor;
        private IHttpClientFactory m_httpClientFactory;
        private ILock m_lock;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        /// <param name="httpClientFactory"></param>
        /// <param name="lock"></param>
        public TCCController(IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory, ILock @lock)
        {
            m_httpContextAccessor = httpContextAccessor;
            m_httpClientFactory = httpClientFactory;
            m_lock = @lock;
        }

        /// <summary>
        /// Post
        /// </summary>
        /// <param name="tccModel"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<long> Post(TCCModel tccModel)
        {
            tccModel.TimeOut = Math.Max(tccModel.TimeOut, MIN_TIMEOUT);
            tccModel.ID = IDGenerator.NextID();

            await m_lock.AcquireAsync(tccModel.DeadLockFlag, tccModel.ID.ToString(), 0, tccModel.TimeOut);

            IEnumerable<NodeResult> errorNodeResults;
            IList<Func<Task<NodeResult>>> tryTasks = new List<Func<Task<NodeResult>>>();
            IList<Func<Task<NodeResult>>> cancelTasks = new List<Func<Task<NodeResult>>>();
            IList<Func<Task<NodeResult>>> commitTasks = new List<Func<Task<NodeResult>>>();

            await m_logHelper.TCCServer(tccModel.ID, "启动TCC事务");

            try
            {
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

                    tryTasks.Add(async () =>
                    {
                        HttpResponseMessage httpResponseMessage = await HttpJsonHelper.HttpPostByAbsoluteUriAsync(
                                    m_httpClientFactory,
                                    $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}/{tryUrl}/{tccModel.ID}/{tccModel.TimeOut}",
                                    tccNode.TryContent,
                                    m_httpContextAccessor?.HttpContext?.Request.Headers["Authorization"]);

                        NodeResult nodeResult = new NodeResult() { Success = true };

                        if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
                        {
                            nodeResult.Success = false;
                            nodeResult.ErrorMessage = $"{tryUrl} Try失败{Environment.NewLine}详细信息：{await httpResponseMessage.Content.ReadAsStringAsync()}{Environment.NewLine}Data：{tccNode.TryContent}。";
                        }

                        return nodeResult;
                    });

                    cancelTasks.Add(async () =>
                    {
                        HttpResponseMessage httpResponseMessage = await HttpJsonHelper.HttpPostByAbsoluteUriAsync(
                                    m_httpClientFactory,
                                    $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}/{cancelUrl}/{tccModel.ID}",
                                    null,
                                    bearerToken: m_httpContextAccessor?.HttpContext?.Request.Headers["Authorization"]);

                        NodeResult nodeResult = new NodeResult() { Success = true };

                        if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
                        {
                            nodeResult.Success = false;
                            nodeResult.ErrorMessage = $"{cancelUrl} Cancel失败{Environment.NewLine}详细信息：{await httpResponseMessage.Content.ReadAsStringAsync()}。";
                        }

                        return nodeResult;
                    });

                    commitTasks.Add(async () =>
                    {
                        HttpResponseMessage httpResponseMessage = await HttpJsonHelper.HttpPostByAbsoluteUriAsync(
                                    m_httpClientFactory,
                                    $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}/{commitUrl}/{tccModel.ID}",
                                    null,
                                    m_httpContextAccessor?.HttpContext?.Request.Headers["Authorization"]);

                        NodeResult nodeResult = new NodeResult() { Success = true };

                        if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
                        {
                            nodeResult.Success = false;
                            nodeResult.ErrorMessage = $"{commitUrl} Commit失败{Environment.NewLine}详细信息：{await httpResponseMessage.Content.ReadAsStringAsync()}。";
                        }

                        return nodeResult;
                    });
                }

                errorNodeResults = await await Task.Factory.ContinueWhenAll(tryTasks.Select(tryTask => Task.Factory.StartNew(tryTask, TaskCreationOptions.LongRunning)).ToArray(), GetErrorResult);

                if (errorNodeResults != null && errorNodeResults.Count() > 0)
                {
                    errorNodeResults = errorNodeResults.Concat(await await Task.Factory.ContinueWhenAll(cancelTasks.Select(cancelTask => Task.Factory.StartNew(cancelTask, TaskCreationOptions.LongRunning)).ToArray(), GetErrorResult));

                    string errorText = $"{tccModel.ID}请求失败{Environment.NewLine}{string.Join(Environment.NewLine, errorNodeResults.Select(errorNodeResult => errorNodeResult.ErrorMessage))}";
                    await m_logHelper.TCCNode(tccModel.ID, false, errorText);
                    throw new DealException(errorText);
                }

                errorNodeResults = await await Task.Factory.ContinueWhenAll(commitTasks.Select(commitTask => Task.Factory.StartNew(commitTask, TaskCreationOptions.LongRunning)).ToArray(), GetErrorResult);

                if (errorNodeResults != null && errorNodeResults.Count() > 0)
                {
                    string errorText = $"{tccModel.ID}请求失败{Environment.NewLine}{string.Join(Environment.NewLine, errorNodeResults.Select(errorNodeResult => errorNodeResult.ErrorMessage))}";
                    await m_logHelper.TCCNode(tccModel.ID, false, errorText);
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
                await m_lock.ReleaseAsync(tccModel.ID.ToString());
            }
        }

        private static async Task<IEnumerable<NodeResult>> GetErrorResult(IEnumerable<Task<Task<NodeResult>>> tasks)
        {
            NodeResult[] nodeResults = await Task.WhenAll(tasks.Select(task => task.Result));
            return nodeResults.Where(nodeResult => !nodeResult.Success);
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
        }
    }
}
