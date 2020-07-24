using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Common;
using Common.Model;
using Common.Validation;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

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

    public class TccTryContent
    {
        public long TCCID { get; set; }
        public JObject Content { get; set; }
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
        private const string TRY = "Try";
        private const string CANCEL = "Cancel";
        private const string COMMIT = "Commit";
        private readonly static int MIN_TIMEOUT;
        private readonly static ILog m_transactionsLog;
        private readonly static ILog m_detailsLog;
        private IHttpContextAccessor m_httpContextAccessor;

        public TCCController(IHttpContextAccessor httpContextAccessor)
        {
            m_httpContextAccessor = httpContextAccessor;
        }

        [HttpPost]
        public async Task<long> Post(TCCModel tccModel)
        {
            tccModel.TimeOut = Math.Max(tccModel.TimeOut, MIN_TIMEOUT);
            tccModel.ID = IDGenerator.NextID();

            IEnumerable<NodeResult> errorNodeResults;
            IList<Task<NodeResult>> tryTasks = new List<Task<NodeResult>>();
            IList<Task<NodeResult>> cancelTasks = new List<Task<NodeResult>>();
            IList<Task<NodeResult>> commitTasks = new List<Task<NodeResult>>();

            m_transactionsLog.Info($"启动TCC事务，ID：{tccModel.ID}");

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
                                $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}/{tryUrl}",
                                new TccTryContent() { TCCID = tccModel.ID, Content = tccNode.TryContent },
                                m_httpContextAccessor?.HttpContext?.Request.Headers["Authorization"]);

                    NodeResult nodeResult = new NodeResult() { Success = true };

                    if (httpResponseMessage.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        nodeResult.Success = false;
                        nodeResult.ErrorMessage = $"{tryUrl} Try失败{Environment.NewLine}详细信息：{httpResponseMessage.Content.ReadAsStringAsync().Result}{Environment.NewLine}Data：{tccNode.TryContent}。";
                    }

                    return nodeResult;
                }));

                cancelTasks.Add(new Task<NodeResult>(() =>
                {
                    HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpPostByAbsoluteUri(
                                $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}/{cancelUrl}",
                                tccModel.ID,
                                m_httpContextAccessor?.HttpContext?.Request.Headers["Authorization"]);

                    NodeResult nodeResult = new NodeResult() { Success = true };

                    if (httpResponseMessage.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        nodeResult.Success = false;
                        nodeResult.ErrorMessage = $"{cancelUrl} Cancel失败{Environment.NewLine}详细信息：{httpResponseMessage.Content.ReadAsStringAsync().Result}。";
                    }

                    return nodeResult;
                }));

                commitTasks.Add(new Task<NodeResult>(() =>
                {
                    HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpPostByAbsoluteUri(
                                $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}/{commitUrl}",
                                tccModel.ID,
                                m_httpContextAccessor?.HttpContext?.Request.Headers["Authorization"]);

                    NodeResult nodeResult = new NodeResult() { Success = true };

                    if (httpResponseMessage.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        nodeResult.Success = false;
                        nodeResult.ErrorMessage = $"{commitUrl} Commit失败{Environment.NewLine}详细信息：{httpResponseMessage.Content.ReadAsStringAsync().Result}。";
                    }

                    return nodeResult;
                }));
            }

            tryTasks.ForEach(tryTask => tryTask.Start());
            errorNodeResults = (await Task.WhenAll(tryTasks)).Where(nodeResult => !nodeResult.Success);

            if (errorNodeResults != null && errorNodeResults.Count() > 0)
            {
                cancelTasks.ForEach(cancelTask => cancelTask.Start());
                errorNodeResults = errorNodeResults.Concat((await Task.WhenAll(cancelTasks)).Where(nodeResult => !nodeResult.Success));
                string errorText = $"{tccModel.ID}请求失败{Environment.NewLine}{string.Join(Environment.NewLine, errorNodeResults.Select(errorNodeResult => errorNodeResult.ErrorMessage))}";

                //TODO: 监控中心日志
                m_detailsLog.Error(errorText);
                throw new DealException(errorText);
            }

            commitTasks.ForEach(commitTask => commitTask.Start());
            errorNodeResults = (await Task.WhenAll(commitTasks)).Where(nodeResult => !nodeResult.Success);

            if (errorNodeResults != null && errorNodeResults.Count() > 0)
            {
                string errorText = $"{tccModel.ID}请求失败{Environment.NewLine}{string.Join(Environment.NewLine, errorNodeResults.Select(errorNodeResult => errorNodeResult.ErrorMessage))}";

                //TODO: 监控中心日志
                m_detailsLog.Error(errorText);
                throw new DealException(errorText);
            }

            return tccModel.ID;
        }

        private static string GetTryUrl(string url)
        {
            return $"{url}{TRY}";
        }

        private static string GetCancelUrl(string url)
        {
            return $"{url}{CANCEL}";
        }

        private static string GetCommitUrl(string url)
        {
            return $"{url}{COMMIT}";
        }

        static TCCController()
        {
            m_transactionsLog = LogHelper.CreateLog("TCC", "TCC", "TCCTransactions");
            m_detailsLog = LogHelper.CreateLog("TCC", "TCC", "TCCDetails");
            MIN_TIMEOUT = Convert.ToInt32(ConfigManager.Configuration["MinTimeOut"]);
        }
    }
}
