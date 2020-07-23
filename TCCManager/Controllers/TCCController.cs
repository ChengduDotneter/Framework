using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Common.Validation;
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
        public JObject TryContent { get; set; }
    }

    [Route("tcc")]
    [ApiController]
    public class TCCController : ControllerBase
    {
        private readonly static int MIN_TIMEOUT;

        [HttpPost]
        public async Task<long> Post(TCCModel tccModel)
        {
            tccModel.TimeOut = Math.Max(tccModel.TimeOut, MIN_TIMEOUT);
            IList<Task> tasks = new List<Task>();

            foreach (TCCNodeModel tCCNode in tccModel.TCCNodes)
            {
                if (string.IsNullOrWhiteSpace(tCCNode.Url) &&
                   (string.IsNullOrWhiteSpace(tCCNode.TryUrl) || string.IsNullOrWhiteSpace(tCCNode.CancelUrl) || string.IsNullOrWhiteSpace(tCCNode.CommitUrl)))
                {
                    throw new DealException("当Url为空时，TryUrl，CancelUrl，CommintUrl不能为空。");
                }

                //tasks.Add(new Task());
            }

            //Common.ServiceCommon.MicroServiceHelper.SendMicroServicePost<>()



            await Task.WhenAll(tasks);

            return 0;
        }

        static TCCController()
        {
            MIN_TIMEOUT = Convert.ToInt32(ConfigManager.Configuration["MinTimeOut"]);
        }
    }
}
