using Apache.Ignite.Core.Cache.Configuration;
using Common;
using Common.DAL;
using Common.Model;
using Common.ServiceCommon;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;

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
            m_editQuery.FilterIsDeleted().Insert(null, concurrentModel);
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
            var count = m_searchQuery.Count();

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
}