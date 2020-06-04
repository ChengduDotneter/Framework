using Common;
using Common.DAL;
using Common.ServiceCommon;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
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
                    systemInfo.IsActive = 1;

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
                systemInfo.IsActive = 1;

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

    public class YJ
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    [ApiController]
    [Route("testdd")]
    public class DDController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            var array = new JArray();
            array.Add(1);

            return Ok(array);
        }

        [HttpGet("/a")]
        public IActionResult Get1()
        {
            return Ok(new YJ()
            {
                Name = Guid.NewGuid().ToString(),
                Count = Environment.TickCount
            });
        }

        [HttpGet("/c")]
        public IActionResult Get2()
        {
            return Ok(new JObject()
            {
                ["Name"] = "中文",
                ["Count"] = Environment.TickCount
            });
        }

        [HttpGet("/d")]
        public IActionResult Get3()
        {
            return Ok("123");
        }
    }
}
