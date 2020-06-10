using Apache.Ignite.Core.Cache.Configuration;
using Common;
using Common.DAL;
using Common.Model;
using Common.ServiceCommon;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System;

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

        public TestConcurrentPostController(IEditQuery<ConcurrentModel> editQuery, ISearchQuery<ConcurrentModel> searchQuery, ISSOUserService ssoUserService) : base(editQuery, ssoUserService)
        {
            m_editQuery = editQuery;
            m_searchQuery = searchQuery;
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
                        //System.Threading.Thread.Sleep(10000);

                        m_editQuery.FilterIsDeleted().Insert(concurrentModel);

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
}
