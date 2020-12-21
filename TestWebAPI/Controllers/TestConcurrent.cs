using Common;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using TestWebAPI.Lock;

namespace TestWebAPI.Controllers
{
    public class NeedData
    {
        public string TranscationID { get; set; }
        public int ExpTime { get; set; }

        public int LockType { get; set; }

        public int LockMode { get; set; }

        public string LockTable { get; set; }

        public string[] LockResourceStrings { get; set; }

    }

    [ApiController]
    [Route("test")]
    public class TestController : ControllerBase
    {
        private readonly ILockHelper m_lockHelper;
        public TestController(ILockHelper lockHelper)
        {
            m_lockHelper = lockHelper;
        }

        [HttpPost("apply")]
        public string TestApply(NeedData needData)
        {
            try
            {
                string result = "";

                if (needData.LockType == 0)
                {
                    if (needData.LockMode == 0)
                        result = m_lockHelper.AcquireReadLockWithGroupKey(needData.LockTable, needData.TranscationID, 0, 1000 * 5).ToString();
                    else if (needData.LockMode == 1)
                        result = m_lockHelper.AcquireWriteLockWithGroupKey(needData.LockTable, needData.TranscationID, 0, 1000 * 5).ToString();
                }
                else
                {
                    if (needData.LockMode == 0)
                        result = m_lockHelper.AcquireReadLockWithResourceKeys(needData.LockTable, needData.TranscationID, 0, 1000 * 5, needData.LockResourceStrings).ToString();
                    else if (needData.LockMode == 1)
                        result = m_lockHelper.AcquireWriteLockWithResourceKeys(needData.LockTable, needData.TranscationID, 0, 1000 * 5, needData.LockResourceStrings).ToString();
                }

                return result;
            }
            catch
            {
                return "error";
            }
        }

        [HttpGet("applyone")]
        public string TestApplyOne()
        {
            string result = "";
            try
            {
                result = m_lockHelper.AcquireReadLockWithGroupKey("classinfo", "123", 0, 1000 * 5).ToString();
            }
            catch
            {
                throw new DealException("申请失败");
            }
            return result;
        }

        [HttpGet("applytwo")]
        public string TestApplyTwo()
        {
            string result = "";
            try
            {
                result = m_lockHelper.AcquireReadLockWithGroupKey("classinfo", "123", 0, 1000 * 5).ToString();
                result = m_lockHelper.AcquireWriteLockWithGroupKey("classinfo", "123", 0, 1000 * 5).ToString();
            }
            catch
            {
                throw new DealException("申请失败");
            }
            return result;
        }

        [HttpGet("applythree")]
        public string TestApplyThree()
        {
            string result = "";
            try
            {
                result = m_lockHelper.AcquireReadLockWithResourceKeys("classinfo", "123", 0, 1000 * 5, new string[] { "123", "456" }).ToString();
                result = m_lockHelper.AcquireWriteLockWithGroupKey("classinfo", "123", 0, 1000 * 5).ToString();
            }
            catch
            {
                throw new DealException("申请失败");
            }
            return result;
        }

        [HttpGet("applyfour")]
        public async Task<string> TestApplyFour()
        {
            Random random = new Random();
            string result = "true";
            string id = random.Next(10000, 9000000).ToString();
            try
            {
                string reource = random.Next(10000, 9000000).ToString();
                if (!await m_lockHelper.AcquireReadLockWithResourceKeysAsync("classinfo", id, 0, 1000 * 5, new string[] { id }))
                    throw new DealException("申请读失败");
                if (!await m_lockHelper.AcquireWriteLockWithResourceKeysAsync("classinfo", id, 0, 1000 * 5, new string[] { id }))
                    throw new DealException("申请写失败");

                //if (!await m_lockHelper.AcquireWriteLockWithGroupKeyAsync(id, id, 0, 1000 * 5))
                //    throw new DealException("申请写失败");

                //if (!await m_lockHelper.AcquireReadLockWithGroupKeyAsync(id, id, 0, 1000 * 5))
                //    throw new DealException("申请读失败");


                //await Task.Delay(5000);

                //await m_lockHelper.ReleaseAsync(id);
            }
            catch
            {
                await m_lockHelper.ReleaseAsync(id);
                throw;
            }

            return result;
        }


        [HttpDelete("release/{identity}")]
        public bool TestRelease(string identity)
        {

            try
            {
                m_lockHelper.Release(identity);
                return true;
            }
            catch
            {
                throw new DealException("释放失败");
            }
        }

        [HttpGet("testtt")]
        public void Test()
        {
            ConfigurationOptions configurationOptions = new ConfigurationOptions();
            configurationOptions.EndPoints.Add(ConfigManager.Configuration["RedisEndPoint"]);
            configurationOptions.Password = ConfigManager.Configuration["RedisPassWord"];
            ConnectionMultiplexer m_connectionMultiplexer = ConnectionMultiplexer.Connect(configurationOptions);
            IDatabase m_redisClient = m_connectionMultiplexer.GetDatabase();

            var data = m_redisClient.ScriptEvaluate(RedisLockLuaScript.ACQUIRE_NREAD_NWRITE_LOCK, new RedisKey[] { "123", "1000000", "classinfo_read", "classinfo_write" }, null);

            var data1 = m_redisClient.ScriptEvaluate(RedisLockLuaScript.SAVE_DB_LOCK, new RedisKey[] { "123", "999999", "1000000", "1", "classinfo_write" }, null);

            var data2 = m_redisClient.ScriptEvaluate(RedisLockLuaScript.Release_DB_LOCK, new RedisKey[] { "123", "1", "classinfo_write" }, null);
        }
    }
}