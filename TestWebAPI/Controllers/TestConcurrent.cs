using Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;

namespace TestWebAPI.Controllers
{
    [Route("test")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public string TestApi()
        {
            return "奥斯卡大家埃里克森大家啊就开始对按时到南昌将扩展性能都";

            //return Ok(new { msg = "奥斯卡大家埃里克森大家啊就开始对按时到南昌将扩展性能都", error = "12313" });
            //throw new DealException("奥斯卡大家埃里克森大家啊就开始对按时到南昌将扩展性能都");
            //throw new ResourceException("奥斯卡大家埃里克森大家啊就开始对按时到南昌将扩展性能都");
            //throw new Exception("奥斯卡大家埃里克森大家啊就开始对按时到南昌将扩展性能都");+

            //HttpContext.Response.ContentType = "text/html; charset=utf-8";
            //HttpResponseWritingExtensions.WriteAsync(HttpContext.Response, "奥斯卡大家埃里克森大家啊就开始对按时到南昌将扩展性能都", Encoding.UTF8).Wait();
        }
    }
}