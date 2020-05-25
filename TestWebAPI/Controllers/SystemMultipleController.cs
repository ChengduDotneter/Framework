using Common;
using Common.DAL;
using Common.ServiceCommon;
using System;
using TestModel;

namespace TestWebAPI.Controllers
{
    /// <summary>
    /// 禁用/启用系统
    /// </summary>
    [DynamicRoute("systemforbidenmultipleput")]
    public class SystemForbidenController : MultipleGenericPutController<SystemInfo, bool>
    {
        private ISSOUserService m_ssoUserService;
        private IEditQuery<SystemInfo> m_systemInfoEditQuery;
        private ISearchQuery<SystemInfo> m_systemInfoSearchQuery;

        public SystemForbidenController(
            IEditQuery<SystemInfo> systemInfoEditQuery,
            ISSOUserService ssoUserService,
            ISearchQuery<SystemInfo> systemInfoSearchQuery) : base(ssoUserService)
        {
            m_systemInfoEditQuery = systemInfoEditQuery;
            m_ssoUserService = ssoUserService;
            m_systemInfoSearchQuery = systemInfoSearchQuery;
        }

        protected override void DoPut(SystemInfo systemInfo, bool isActive)
        {
            using (ITransaction trans = m_systemInfoEditQuery.FilterIsDeleted().BeginTransaction())
            {
                try
                {
                    //if (m_systemInfoSearchQuery.FilterIsDeleted().Count(item => item.ID == systemInfo.ID) < 1)
                    //    throw new DealException("指定数据未找到");

                    systemInfo.UpdateUserID = m_ssoUserService.GetUser().ID;
                    systemInfo.UpdateTime = DateTime.Now;
                    systemInfo.IsActive = isActive;


                    for (int i = 0; i < 10000; i++)
                    {
                        systemInfo.ID = IDGenerator.NextID();
                        m_systemInfoEditQuery.FilterIsDeleted().Insert(systemInfo);
                    }

                    trans.Submit();

                    var s = m_systemInfoSearchQuery.FilterIsDeleted().Count(item => !string.IsNullOrEmpty(item.SystemName));
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }
    }
}
