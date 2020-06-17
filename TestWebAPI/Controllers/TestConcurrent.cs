using Apache.Ignite.Core.Cache.Configuration;
using Common;
using Common.DAL;
using Common.Model;
using Common.ServiceCommon;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Threading;

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
        private readonly ISSOUserService m_ssoUserService;
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
            m_ssoUserService = ssoUserService;
        }

        protected override void DoPost(long id, ConcurrentModel concurrentModel)
        {
            using (ITransaction transaction = m_editQuery.FilterIsDeleted().BeginTransaction(20))
            {
                try
                {
                    if (m_searchQuery.FilterIsDeleted().Count(item => item.UserAccount == concurrentModel.UserAccount) == 0)
                    {
                        int time = Environment.TickCount;

                        IEnumerable<WarehouseInfo> warehouseInfos = m_warehouseInfoSearchQuery.FilterIsDeleted().Search();

                        //Console.WriteLine(Environment.TickCount - time);

                        m_editQuery.FilterIsDeleted().Insert(concurrentModel);

                        //Console.WriteLine(Environment.TickCount - time);

                        transaction.Submit();

                        //Console.WriteLine(Environment.TickCount - time);
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
            long time = Environment.TickCount64;

            using (ITransaction transaction = m_warehouseInfoEditQuery.FilterIsDeleted().BeginTransaction(10))
            {
                try
                {
                    m_warehouseInfoSearchQuery.Count();

                    //System.Threading.Thread.Sleep(5000);

                    IEnumerable<ConcurrentModel> concurrentModels = m_concurrentModelSearchQuery.FilterIsDeleted().Search();

                    //System.Threading.Thread.Sleep(5000);

                    transaction.Submit();


                    //Console.WriteLine(Environment.TickCount64 - time);
                    //Console.WriteLine(Environment.StackTrace);
                    //Console.WriteLine(Environment.CurrentDirectory);
                    //Console.WriteLine(Environment.CurrentManagedThreadId);
                    //Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
                    //Console.WriteLine(Environment.TickCount64);
                    //Console.WriteLine(Environment.TickCount);
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }

}