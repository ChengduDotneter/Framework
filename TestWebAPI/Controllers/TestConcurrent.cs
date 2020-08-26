using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Common;
using Common.DAL;
using Common.Model;
using Common.ServiceCommon;
using Common.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TestWebAPI.Controllers
{
    public class TCCTestData : ViewModelBase
    {
        [NotNull]
        [StringMaxLength(100)]
        public string Data { get; set; }
    }

    [Route("abc")]
    [ApiController]
    public class CCC : ControllerBase
    {
        private ISearchQuery<TCCTestData> m_searchQuery;

        public CCC(ISearchQuery<TCCTestData> searchQuery)
        {
            m_searchQuery = searchQuery;
        }

        [HttpGet]
        public async Task<TCCTestData> Get()
        {
            return (await m_searchQuery.SearchAsync(count: 1)).FirstOrDefault();
        }
    }

    [Route("tccdo")]
    public class tccdocontroller : TransactionTCCController<TCCTestData, TCCTestData>
    {
        private IEditQuery<TCCTestData> m_tccTestDataeditQuery;
        private ISearchQuery<TCCTestData> m_searchQuery;

        public tccdocontroller(IEditQuery<TCCTestData> tccTestDataeditQuery, ISearchQuery<TCCTestData> searchQuery, IHttpClientFactory httpContextFactory, IHttpContextAccessor httpContextAccessor, ITccTransactionManager tccTransactionManager) : base(tccTestDataeditQuery, httpContextFactory, httpContextAccessor, tccTransactionManager)
        {
            m_tccTestDataeditQuery = tccTestDataeditQuery;
            m_searchQuery = searchQuery;
        }

        protected override async Task<object> DoTry(long tccID, ITransaction transaction, TCCTestData data)
        {
            data.Data = $"{data.Data}:{data.ID}, tccID:{tccID}";
            data.ID = IDGenerator.NextID();
            //m_searchQuery.Count(transaction: transaction);
            await m_tccTestDataeditQuery.InsertAsync(transaction, data);
            //await Task.Delay(1000);

            return data;
        }
    }
    [ApiController]
    [Route("tttessst")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public string ttt()
        {
            throw new DealException("123456");
        }
    }
}