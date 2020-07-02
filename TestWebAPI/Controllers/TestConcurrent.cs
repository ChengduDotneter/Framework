using Apache.Ignite.Core.Cache.Configuration;
using Common;
using Common.DAL;
using Common.Model;
using Common.ServiceCommon;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
            using (ITransaction transaction = m_editQuery.FilterIsDeleted().BeginTransaction())
            {
                try
                {
                    if (m_searchQuery.FilterIsDeleted().Count(item => item.UserAccount == concurrentModel.UserAccount) == 0)
                    {
                        int time = Environment.TickCount;

                        IEnumerable<WarehouseInfo> warehouseInfos = m_warehouseInfoSearchQuery.FilterIsDeleted().Search();

                        //Console.WriteLine(Environment.TickCount - time);
                        time = Environment.TickCount;

                        m_editQuery.FilterIsDeleted().Insert(concurrentModel);

                        //Console.WriteLine(Environment.TickCount - time);
                        time = Environment.TickCount;

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

            using (ITransaction transaction = m_warehouseInfoEditQuery.FilterIsDeleted().BeginTransaction(5))
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

    [ApiController]
    [Route("tttt")]
    public class testcontroller : ControllerBase
    {
        private readonly IEditQuery<WarehouseInfo> m_warehouseInfoEditQuery;
        private readonly IEditQuery<ConcurrentModel> m_concurrentModelEditQuery;
        private readonly ISearchQuery<ConcurrentModel> m_searchQuery;

        public testcontroller(IEditQuery<WarehouseInfo> warehouseInfoEditQuery, IEditQuery<ConcurrentModel> concurrentModelEditQuery, ISearchQuery<ConcurrentModel> searchQuery)
        {
            m_warehouseInfoEditQuery = warehouseInfoEditQuery;
            m_concurrentModelEditQuery = concurrentModelEditQuery;
            m_searchQuery = searchQuery;
        }

        [HttpGet]
        [Route("1")]
        public void data()
        {
            var client = getclient();

            client.BeginTran();
            try
            {
                var datas = getclient(true).Queryable<ConcurrentModel>();

                getclient().Updateable<ConcurrentModel>().Where(item => true).SetColumns(item => item.Password == "12311").ExecuteCommand();
                getclient().Insertable(new ConcurrentModel { CreateTime = DateTime.Now, CreateUserID = 0, ID = IDGenerator.NextID(), Password = "11", UserAccount = "123" }).ExecuteCommand();
                client.CommitTran();
            }
            catch
            {
                client.RollbackTran();
            }


        }

        [HttpGet]
        [Route("2")]
        public void data111()
        {
            using (ITransaction transaction = m_concurrentModelEditQuery.BeginTransaction())
            {

                try
                {
                    var datas = m_searchQuery.FilterIsDeleted().Search();

                    m_concurrentModelEditQuery.FilterIsDeleted().Update(item => true, item => item.Password == "12311");
                    m_concurrentModelEditQuery.FilterIsDeleted().Insert(new ConcurrentModel { CreateTime = DateTime.Now, CreateUserID = 0, ID = IDGenerator.NextID(), Password = "11", UserAccount = "123" });
                    datas = m_searchQuery.FilterIsDeleted().Search();
                    //int i = 0;
                    //int c = 5 / i;

                    transaction.Submit();
                }
                catch
                {
                    transaction.Rollback();
                }

                //using (ITransaction transaction2 = m_concurrentModelEditQuery.BeginTransaction())
                //{

                //    try
                //    {
                //        var datas = m_searchQuery.FilterIsDeleted().Search();

                //        m_concurrentModelEditQuery.FilterIsDeleted().Update(item => true, item => item.Password == "123112222");
                //        m_concurrentModelEditQuery.FilterIsDeleted().Insert(new ConcurrentModel { CreateTime = DateTime.Now, CreateUserID = 0, ID = IDGenerator.NextID(), Password = "11", UserAccount = "123" });
                //        datas = m_searchQuery.FilterIsDeleted().Search();
                //        int i = 0;
                //        int c = 5 / i;

                //        transaction2.Submit();
                //    }
                //    catch
                //    {
                //        transaction2.Rollback();
                //    }
                //}
            }
        }

        private SqlSugarClient getclient(bool isa = true)
        {
            return new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString = ConfigManager.Configuration.GetConnectionString("MasterConnection"),
                DbType = DbType.MySql,
                InitKeyType = InitKeyType.Attribute,
                IsAutoCloseConnection = isa,
                //标记该数据库链接是否为线程共享
                IsShardSameThread = isa
            });
        }
    }

}