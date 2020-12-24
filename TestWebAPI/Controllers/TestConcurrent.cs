using Common;
using Common.Compute;
using Common.DAL;
using Common.Model;
using Common.ServiceCommon;
using MicroService.StorageService.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TestRedis;

namespace TestWebAPI.Controllers
{
    public class TCCTestData : ViewModelBase
    {
        public string Data { get; set; }
    }

    [Route("abc")]
    [ApiController]
    public class CCC : ControllerBase
    {
        private ISearchQuery<Left> m_searchQuery;
        private IEditQuery<Left> m_editQuery;

        public CCC(ISearchQuery<Left> searchQuery, IEditQuery<Left> editQuery)
        {
            m_searchQuery = searchQuery;
            m_editQuery = editQuery;
        }

        [HttpGet]
        public async Task<Left> Get()
        {
            Left data = null;

            var datas = m_searchQuery.FilterIsDeleted().Search().ToArray();

            Random random = new Random();

            long id = datas[random.Next(0, datas.Length - 1)].ID;


            using (ITransaction transaction = m_editQuery.BeginTransaction())
            {
                try
                {
                    data = m_searchQuery.FilterIsDeleted().Get(id, transaction);

                    if (data == null)
                        return data;

                    var datas2 = m_searchQuery.FilterIsDeleted().Search(transaction, item => item.ClassID == data.ClassID);

                    data.UpdateUserID = random.Next(1000, 9999);
                    data.UpdateTime = DateTime.Now;

                    m_editQuery.Update(data, transaction);

                    m_editQuery.FilterIsDeleted().Delete(transaction, data.ID);

                    m_editQuery.Update(item => item.ID == data.ID, new Dictionary<string, object>() { [nameof(Left.IsDeleted)] = false },transaction);

                    transaction.Submit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }

            return data;
        }

        private void Sleep()
        {
            Thread.Sleep(5000);
        }
    }

    [Route("aaaaa")]
    [ApiController]
    public class AAAAA : ControllerBase
    {
        private ISearchQuery<Left> m_searchQuery;
        private IEditQuery<Left> m_editQuery;

        public AAAAA(ISearchQuery<Left> searchQuery, IEditQuery<Left> editQuery)
        {
            m_searchQuery = searchQuery;
            m_editQuery = editQuery;
        }

        [HttpGet]
        public async Task<IEnumerable<Left>> Get()
        {
            Console.WriteLine("controller start");

            var data = m_searchQuery.RedisCache().Search();

            return data;
        }
    }

    [Route("insert")]
    [ApiController]
    public class INSERT : ControllerBase
    {
        private ISearchQuery<Left> m_searchQuery;
        private IEditQuery<Left> m_editQuery;
        private ISSOUserService m_ssouserservice;

        public INSERT(ISearchQuery<Left> searchQuery, IEditQuery<Left> editQuery, ISSOUserService ssouserservice)
        {
            m_searchQuery = searchQuery;
            m_editQuery = editQuery;
            m_ssouserservice = ssouserservice;
        }

        [HttpGet]
        public async Task<Left> Get()
        {
            Console.WriteLine("controller start");

            Random random = new Random();

            Left data = new Left()
            {
                StudentName = $"studentName_{random.Next(1, 10000)}",
                ClassID = 0,
            }.GenerateInitialization(m_ssouserservice);

            m_editQuery.RedisCache().Insert(null, data);
            return data;
        }
    }

    [Route("test")]
    [ApiController]
    public class TEST : ControllerBase
    {
        private ISearchQuery<Left> m_searchQuery;
        private IEditQuery<Left> m_editQuery;

        public TEST(ISearchQuery<Left> searchQuery, IEditQuery<Left> editQuery)
        {
            m_searchQuery = searchQuery;
            m_editQuery = editQuery;
        }

        [HttpGet]
        public async Task<Left> Get()
        {
            m_searchQuery.RedisCache().Search();
            m_searchQuery.RedisCache().Search(item => !item.IsDeleted);
            m_searchQuery.RedisCache().Search(item => !item.IsDeleted, startIndex: 0);
            m_searchQuery.RedisCache().Search(item => !item.IsDeleted, startIndex: 0, count: 100);
            m_searchQuery.RedisCache().Search(item => !item.IsDeleted, new List<QueryOrderBy<Left>> { new QueryOrderBy<Left>(item => item.ID, OrderByType.Desc) }, startIndex: 0, count: 100);

            m_searchQuery.RedisCache().Count();
            m_searchQuery.RedisCache().Count(item => !item.IsDeleted);

            var data = m_searchQuery.RedisCache().Get(238335829955582145);

            data.StudentName = "456789";
            //m_editQuery.RedisCache().Update(data);

            //m_editQuery.RedisCache().Insert(null, new Left() { ID = IDGenerator.NextID(), CreateTime = DateTime.Now, CreateUserID = 1002, StudentName = "123456" });

            return (await m_searchQuery.RedisCache().SearchAsync(count: 1)).FirstOrDefault();
        }
    }

    [Route("update")]
    [ApiController]
    public class UPDATE : ControllerBase
    {
        private ISearchQuery<Left> m_searchQuery;
        private IEditQuery<Left> m_editQuery;

        public UPDATE(ISearchQuery<Left> searchQuery, IEditQuery<Left> editQuery)
        {
            m_searchQuery = searchQuery;
            m_editQuery = editQuery;
        }

        [HttpGet]
        public async Task<Left> Get()
        {
            var data = m_searchQuery.RedisCache().Get(238335829955582145);
            Random random = new Random();
            data.StudentName = $"studentName_{random.Next(1, 10000)}";

            m_editQuery.RedisCache().Update(data);

            return data;
        }
    }

    //[Route("tccdo")]
    //public class tccdocontroller : TransactionTCCController<TCCTestData, TCCTestData>
    //{
    //    private IEditQuery<TCCTestData> m_tccTestDataeditQuery;
    //    private ISearchQuery<TCCTestData> m_searchQuery;

    //    public tccdocontroller(IEditQuery<TCCTestData> tccTestDataeditQuery, ISearchQuery<TCCTestData> searchQuery, IHttpClientFactory httpContextFactory, IHttpContextAccessor httpContextAccessor, ITccTransactionManager tccTransactionManager) : base(tccTestDataeditQuery, httpContextFactory, httpContextAccessor, tccTransactionManager)
    //    {
    //        m_tccTestDataeditQuery = tccTestDataeditQuery;
    //        m_searchQuery = searchQuery;
    //    }

    //    protected override async Task<object> DoTry(long tccID, ITransaction transaction, TCCTestData data)
    //    {
    //        data.Data = $"{data.Data}:{data.ID}, tccID:{tccID}";
    //        data.ID = IDGenerator.NextID();
    //        //m_searchQuery.Count(transaction: transaction);
    //        await m_tccTestDataeditQuery.InsertAsync(transaction, data);
    //        //await Task.Delay(1000);

    //        return data;
    //    }
    //}
    //[ApiController]
    //[Route("tttessst")]
    //public class TestController : ControllerBase
    //{
    //    [HttpGet]
    //    public string ttt()
    //    {
    //        throw new DealException("123456");
    //    }
    //}

    public class TestService : IHostedService
    {
        private IComputeFactory m_computeFactory;
        private ICompute m_compute;
        private IMapReduce m_mapReduce;
        private IAsyncMapReduce m_asyncMapReduce;

        public TestService(IComputeFactory computeFactory, ICompute compute, IMapReduce mapReduce, IAsyncMapReduce asyncMapReduce)
        {
            m_computeFactory = computeFactory;
            m_compute = compute;
            m_mapReduce = mapReduce;
            m_asyncMapReduce = asyncMapReduce;
        }

        private void Test()
        {
            Console.WriteLine("change");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ChangeToken.OnChange(() => ConfigManager.Configuration.GetReloadToken(), Test);

            return Task.Factory.StartNew(async () =>
            {
                await Task.Delay(5000);

                while (true)
                {
                    //IComputeFunc<ComputeParameter, ComputeResult> computeFunc = m_computeFactory.CreateComputeFunc<ComputeFunc, ComputeParameter, ComputeResult>();

                    //foreach (ComputeResult computeResult in m_compute.Bordercast(computeFunc, new ComputeParameter() { RequestData = "ZXY" }))
                    //{
                    //    Console.WriteLine(computeResult.ResponseData);
                    //}

                    ComputeResult computeResult = m_mapReduce.Excute<ComputeSplitFunc, ComputeParameter, ComputeResult, ComputeSplitParameter, ComputeSplitResult>
                        (new ComputeMapReduce(m_computeFactory), new ComputeParameter() { RequestData = "ZXY" });

                    Console.WriteLine(computeResult.ResponseData);

                    await Task.Delay(1000);
                }
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("end");
            return Task.CompletedTask;
        }
    }

    public class ComputeParameter
    {
        public string RequestData { get; set; }
    }

    public class ComputeResult
    {
        public string ResponseData { get; set; }
    }

    public class ComputeSplitParameter
    {
        public string RequestData { get; set; }
    }

    public class ComputeSplitResult
    {
        public string ResponseData { get; set; }
    }

    public class ComputeFunc : IComputeFunc<ComputeParameter, ComputeResult>
    {
        private readonly ISearchQuery<TestData> m_searchQuery;

        public ComputeFunc(ISearchQuery<TestData> searchQuery)
        {
            m_searchQuery = searchQuery;
        }

        public ComputeResult Excute(ComputeParameter parameter)
        {
            Console.WriteLine(parameter.RequestData);
            return new ComputeResult() { ResponseData = DateTime.Now.ToString("g") };
        }
    }

    [Route("ccs")]
    public class CCS : GenericPutController<Left>
    {
        private IEditQuery<Left> m_editQuery;

        public CCS(IEditQuery<Left> editQuery, ISearchQuery<Left> searchQuery, ISSOUserService ssoUserService) : base(editQuery, searchQuery, ssoUserService)
        {
            m_editQuery = editQuery;
        }

        protected override async Task DoPut(Left request)
        {
            using (ITransaction transaction = await m_editQuery.FilterIsDeleted().Cache(HttpContext.RequestServices).BeginTransactionAsync())
            {
                try
                {
                    await m_editQuery.FilterIsDeleted().Cache(HttpContext.RequestServices).UpdateAsync(request, transaction);
                    await transaction.SubmitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                }
            }
        }
    }

    [Route("ccs")]
    public class CCS1 : GenericGetController<Left>
    {
        private ISearchQuery<Left> m_searchQuery;

        public CCS1(ISearchQuery<Left> searchQuery) : base(searchQuery)
        {
            m_searchQuery = searchQuery;
        }

        protected override async Task<Left> DoGet(long id)
        {
            return await m_searchQuery.FilterIsDeleted().KeyCache(HttpContext.RequestServices).GetAsync(id);
        }
    }

    [Route("ccs1")]
    [ApiController]
    public class CCS3 : ControllerBase
    {
        private ISearchQuery<Left> m_searchQuery;

        public CCS3(ISearchQuery<Left> searchQuery)
        {
            m_searchQuery = searchQuery;
        }

        [HttpGet("{name}")]
        public async Task<Left> Get(string name)
        {
            return (await m_searchQuery.FilterIsDeleted().ConditionCache(HttpContext.RequestServices).GetAsync(item => item.StudentName == name)).FirstOrDefault();
        }
    }

    [Route("ccs2")]
    public class CCS2 : GenericGetController<StockInfo>
    {
        private ISearchQuery<StockInfo> m_searchQuery;

        public CCS2(ISearchQuery<StockInfo> searchQuery) : base(searchQuery)
        {
            m_searchQuery = searchQuery;
        }

        protected override async Task<StockInfo> DoGet(long id)
        {
            return await m_searchQuery.FilterIsDeleted().KeyCache(HttpContext.RequestServices).GetAsync(id);
        }
    }

    public class ComputeSplitFunc : IComputeFunc<ComputeSplitParameter, ComputeSplitResult>
    {
        private readonly ISearchQuery<TestData> m_searchQuery;

        public ComputeSplitFunc(ISearchQuery<TestData> searchQuery)
        {
            m_searchQuery = searchQuery;
        }

        public ComputeSplitResult Excute(ComputeSplitParameter parameter)
        {
            Console.WriteLine(parameter.RequestData);
            return new ComputeSplitResult() { ResponseData = DateTime.Now.ToString("g") };
        }
    }

    public class ComputeMapReduce : IMapReduceTask<ComputeParameter, ComputeResult, ComputeSplitParameter, ComputeSplitResult>
    {
        private IComputeFactory m_computeFactory;

        public ComputeMapReduce(IComputeFactory computeFactory)
        {
            m_computeFactory = computeFactory;
        }

        public ComputeResult Reduce(IEnumerable<ComputeSplitResult> splitResults)
        {
            return new ComputeResult() { ResponseData = $"map: {string.Join(",", splitResults.Select(item => item.ResponseData))}" };
        }

        public IEnumerable<MapReduceSplitJob<ComputeSplitParameter, ComputeSplitResult>> Split(int nodeCount, ComputeParameter parameter)
        {
            IList<MapReduceSplitJob<ComputeSplitParameter, ComputeSplitResult>> mapReduceSplitJobs = new List<MapReduceSplitJob<ComputeSplitParameter, ComputeSplitResult>>();

            for (int i = 0; i < nodeCount; i++)
            {
                mapReduceSplitJobs.Add(new MapReduceSplitJob<ComputeSplitParameter, ComputeSplitResult>()
                {
                    ComputeFunc = m_computeFactory.CreateComputeFunc<ComputeSplitFunc, ComputeSplitParameter, ComputeSplitResult>(),
                    Parameter = new ComputeSplitParameter() { RequestData = parameter.RequestData[i % parameter.RequestData.Length].ToString() }
                });
            }

            return mapReduceSplitJobs;
        }
    }

    [Route("cache")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpPost]
        public IActionResult Post()
        {
            return Ok();
        }

        [HttpGet]
        public string Get()
        {
            return DateTime.Now.ToString("yyyyy-MM-dd HH:mm:ss");
        }

        [HttpGet("cross")]
        public async Task<string> Cross([FromServices] IHttpClientFactory httpClientFactory)
        {
            HttpResponseMessage httpResponseMessage = await HttpJsonHelper.HttpGetByAbsoluteUriAsync(httpClientFactory, "http://localhost:5001/testservice/cross", HttpContext.Request.Headers["Authorization"]);

            return await httpResponseMessage.Content.ReadAsStringAsync();
        }
    }

    [Route("cross")]
    [ApiController]
    public class CrossMicroServiceController : ControllerBase
    {
        [HttpPost]
        public IActionResult Post()
        {
            return Ok();
        }

        [HttpGet]
        public string Get([FromServices] ISSOUserService ssoUserService)
        {
            SSOUserInfo ssoUserInfo = ssoUserService.GetUser();
            return $"userName: {ssoUserInfo.UserName}, id: {ssoUserInfo.ID}, phone: {ssoUserInfo.Phone}";
        }
    }

    public class CrossService : IHostedService
    {
        private readonly IHttpClientFactory m_httpClientFactory;

        public CrossService(IHttpClientFactory httpClientFactory)
        {
            m_httpClientFactory = httpClientFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(async () =>
            {
                await Task.Delay(5000);

                while (true)
                {
                    HttpResponseMessage httpResponseMessage = await HttpJsonHelper.HttpGetByAbsoluteUriAsync(m_httpClientFactory, "http://localhost:5001/testservice/cross");
                    Console.WriteLine(await httpResponseMessage.Content.ReadAsStringAsync());
                    await Task.Delay(2000);
                }
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}