using Common;
using Common.DAL;
using Common.ServiceCommon;
using System;
using TestModel;

namespace TestWebAPI.Controllers
{
    /// <summary>
    /// 测试接口
    /// </summary>
    [DynamicRoute("testpost")]
    public class TestController : MultipleGenericPostController<SystemInfo, bool>
    {
        private ISSOUserService m_ssoUserService;
        private IEditQuery<SystemInfo> m_systemInfoEditQuery;
        private ISearchQuery<SystemInfo> m_systemInfoSearchQuery;

        public TestController(
            IEditQuery<SystemInfo> systemInfoEditQuery,
            ISSOUserService ssoUserService,
            ISearchQuery<SystemInfo> systemInfoSearchQuery) : base(ssoUserService)
        {
            m_systemInfoEditQuery = systemInfoEditQuery;
            m_ssoUserService = ssoUserService;
            m_systemInfoSearchQuery = systemInfoSearchQuery;
        }

        protected override void DoPost(SystemInfo systemInfo, bool isActive)
        {
            using (ITransaction trans = m_systemInfoEditQuery.FilterIsDeleted().BeginTransaction())
            {
                try
                {
                    systemInfo.UpdateUserID = m_ssoUserService.GetUser().ID;
                    systemInfo.UpdateTime = DateTime.Now;
                    systemInfo.IsActive = isActive;

                    systemInfo.ID = IDGenerator.NextID();
                    m_systemInfoEditQuery.FilterIsDeleted().Insert(systemInfo);

                    trans.Submit();

                    var s = m_systemInfoSearchQuery.FilterIsDeleted().Count(item => !string.IsNullOrEmpty(item.SystemName));
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }

            m_systemInfoSearchQuery.Search();


            using ITransaction trans2 = m_systemInfoEditQuery.FilterIsDeleted().BeginTransaction();
            try
            {
                systemInfo.UpdateUserID = m_ssoUserService.GetUser().ID;
                systemInfo.UpdateTime = DateTime.Now;
                systemInfo.IsActive = isActive;

                systemInfo.ID = IDGenerator.NextID();
                m_systemInfoEditQuery.FilterIsDeleted().Insert(systemInfo);

                trans2.Submit();

                var s = m_systemInfoSearchQuery.FilterIsDeleted().Count(item => !string.IsNullOrEmpty(item.SystemName));
            }
            catch
            {
                trans2.Rollback();
                throw;
            }

            m_systemInfoSearchQuery.Search();

        }
    }
}
