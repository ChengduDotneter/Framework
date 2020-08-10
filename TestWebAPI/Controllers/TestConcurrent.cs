using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Apache.Ignite.Core.Cache.Configuration;
using Common;
using Common.DAL;
using Common.Model;
using Common.ServiceCommon;
using Common.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace TestWebAPI.Controllers
{
    [IgnoreBuildController(ignoreGet: true, ignoreDelete: true, ignorePost: true, ignorePut: true, ignoreSearch: true)]
    public class ConcurrentModel : ViewModelBase
    {
        [SugarColumn(IsNullable = false, ColumnDescription = "账号")]
        [QuerySqlField(NotNull = true)]
        public string UserAccount { get; set; }

        [SugarColumn(IsNullable = false, ColumnDescription = "密码")]
        [QuerySqlField(NotNull = true)]
        public string Password { get; set; }
    }

    [Route("testconcurrent")]
    public class TestConcurrentPostController : GenericPostController<ConcurrentModel>
    {
        private readonly ISearchQuery<ConcurrentModel> m_searchQuery;
        private readonly IEditQuery<ConcurrentModel> m_editQuery;
        private readonly ISearchQuery<WarehouseInfo> m_warehouseInfoSearchQuery;


        public TestConcurrentPostController(
            IEditQuery<ConcurrentModel> editQuery,
            ISearchQuery<ConcurrentModel> searchQuery,
            ISearchQuery<WarehouseInfo> warehouseInfoSearchQuery,
            ISSOUserService ssoUserService) : base(editQuery, ssoUserService)
        {
            m_editQuery = editQuery;
            m_searchQuery = searchQuery;
            m_warehouseInfoSearchQuery = warehouseInfoSearchQuery;
        }

        protected override void DoPost(long id, ConcurrentModel concurrentModel)
        {
            using (ITransaction transaction = m_editQuery.FilterIsDeleted().BeginTransaction(20))
            {
                try
                {
                    if (m_searchQuery.FilterIsDeleted().Count(item => item.UserAccount == concurrentModel.UserAccount, transaction) == 0)
                    {
                        int time = Environment.TickCount;

                        IEnumerable<WarehouseInfo> warehouseInfos = m_warehouseInfoSearchQuery.FilterIsDeleted().Search(transaction: transaction);

                        m_editQuery.FilterIsDeleted().Insert(transaction, concurrentModel);

                        transaction.Submit();
                    }
                    else
                    {
                        throw new DealException("账号已存在");
                    }
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
    [ApiController]
    [Route("tt")]
    public class testController : GenericGetController<ConcurrentModel>
    {
        private readonly ISearchQuery<ConcurrentModel> m_searchQuery;
        public testController(ISearchQuery<ConcurrentModel> searchQuery) : base(searchQuery)
        {
            m_searchQuery = searchQuery;
        }

        protected override ConcurrentModel DoGet(long id)
        {
            string sql = "SELECT * FROM ConcurrentModel WHERE ";

            return MapperModelHelper<ConcurrentModel>.ReadModel(m_searchQuery.Query(sql)).FirstOrDefault();
        }
    }


    [Route("testconcurrent2")]
    public class TestConcurrentLockPostController : GenericPostController<WarehouseInfo>
    {
        private readonly ISearchQuery<WarehouseInfo> m_warehouseInfoSearchQuery;
        private readonly IEditQuery<WarehouseInfo> m_warehouseInfoEditQuery;
        private readonly ISearchQuery<ConcurrentModel> m_concurrentModelSearchQuery;

        public TestConcurrentLockPostController(
            ISearchQuery<WarehouseInfo> warehouseInfoSearchQuery,
            IEditQuery<WarehouseInfo> warehouseInfoEditQuery,
            ISearchQuery<ConcurrentModel> concurrentModelSearchQuery,
            ISSOUserService ssoUserService) : base(warehouseInfoEditQuery, ssoUserService)
        {
            m_warehouseInfoSearchQuery = warehouseInfoSearchQuery;
            m_warehouseInfoEditQuery = warehouseInfoEditQuery;
            m_concurrentModelSearchQuery = concurrentModelSearchQuery;
        }

        protected override void DoPost(long id, WarehouseInfo warehouseInfo)
        {
            using (ITransaction transaction = m_warehouseInfoEditQuery.FilterIsDeleted().BeginTransaction(10))
            {
                try
                {
                    m_warehouseInfoSearchQuery.Count(transaction: transaction);

                    IEnumerable<ConcurrentModel> concurrentModels = m_concurrentModelSearchQuery.FilterIsDeleted().Search(transaction: transaction);

                    transaction.Submit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }

    public class TCCTestData : ViewModelBase
    {
        [SugarColumn(IsNullable = false, Length = 10)]
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
    public class tccdocontroller : TransactionTCCController<TCCTestData>
    {
        private IEditQuery<TCCTestData> m_tccTestDataeditQuery;
        private ISearchQuery<TCCTestData> m_searchQuery;

        public tccdocontroller(IEditQuery<TCCTestData> tccTestDataeditQuery, ISearchQuery<TCCTestData> searchQuery, IHttpClientFactory httpContextFactory, IHttpContextAccessor httpContextAccessor, ITccTransactionManager tccTransactionManager) : base(tccTestDataeditQuery, httpContextFactory, httpContextAccessor, tccTransactionManager)
        {
            m_tccTestDataeditQuery = tccTestDataeditQuery;
            m_searchQuery = searchQuery;
        }

        protected override async Task DoTry(long tccID, ITransaction transaction, TCCTestData data)
        {
            data.Data = $"{data.Data}:{data.ID}, tccID:{tccID}";
            data.ID = IDGenerator.NextID();
            //m_searchQuery.Count(transaction: transaction);
            await m_tccTestDataeditQuery.InsertAsync(transaction, data);
            //await Task.Delay(1000);
        }
    }
}