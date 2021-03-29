using Common.DAL;
using Common.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Common.ServiceCommon
{
    public static class ValidSystem
    {
        internal const string SYSTEMID = "systemID";

        public static bool CheckSystem(this HttpContext httpContext, bool splitSystem)
        {
            if (splitSystem)
            {
                if (string.IsNullOrWhiteSpace(httpContext.Request.Headers[SYSTEMID].FirstOrDefault()))
                    throw new DealException($"请传入{SYSTEMID}。");

                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Service中无相应的实体接受请求参数时使用且请求参数为JObject的接口基类
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    [ApiController]
    public abstract class JObjectGenericPostController<TResponse> : ControllerBase
    {
        /// <summary>
        /// Post
        /// </summary>
        /// <param name="jObjectSerializeService"></param>
        /// <returns></returns>
        [HttpPost]
        public Task<TResponse> Post([FromServices] IJObjectSerializeService jObjectSerializeService)
        {
            return DoPost(jObjectSerializeService.GetJObject());
        }

        /// <summary>
        /// 获取结果
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected abstract Task<TResponse> DoPost(JObject request);
    }

    /// <summary>
    /// Service中无相应的实体接受请求参数时使用且请求参数为JArray的接口基类
    /// </summary>
    /// <typeparam name="TResponse">返回实体泛型</typeparam>
    [ApiController]
    public abstract class JArrayGenericPostController<TResponse> : ControllerBase
    {
        /// <summary>
        /// Post
        /// </summary>
        /// <param name="jArraySerializeService"></param>
        /// <returns></returns>
        [HttpPost]
        public Task<TResponse> Post([FromServices] IJArraySerializeService jArraySerializeService)
        {
            return DoPost(jArraySerializeService.GetJArray());
        }

        /// <summary>
        /// 获取结果
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected abstract Task<TResponse> DoPost(JArray request);
    }

    /// <summary>
    /// 根据ID查询自定义结果的接口基类
    /// </summary>
    /// <typeparam name="TResponse">返回实体泛型</typeparam>
    public abstract class CustomGetController<TResponse> : ControllerBase
    {
        /// <summary>
        /// Get
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            TResponse data = await DoGet(id);

            if (data == null)
                return NotFound(id);

            return Ok(data);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected abstract Task<TResponse> DoGet(long id);
    }

    /// <summary>
    /// 无查询条件的Get请求接口基类
    /// </summary>
    /// <typeparam name="TResponse">返回实体参数</typeparam>
    [ApiController]
    public abstract class CustomGetWithoutParameterController<TResponse> : ControllerBase
    {
        /// <summary>
        /// Get
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public Task<TResponse> Get()
        {
            return SearchDatas();
        }

        /// <summary>
        /// 获取结果
        /// </summary>
        /// <returns></returns>
        protected abstract Task<TResponse> SearchDatas();
    }

    /// <summary>
    /// 根据ID获取继承于ViewModelBase的实体的接口基类
    /// </summary>
    /// <typeparam name="TResponse">返回实体泛型，继承于ViewModelBase</typeparam>
    [ApiController]
    public abstract class GenericGetController<TResponse> : ControllerBase
        where TResponse : ViewModelBase, new()
    {
        private ISearchQuery<TResponse> m_searchQuery;
        private readonly bool m_splitSystem;

        /// <summary>
        /// Get
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            HttpContext.CheckSystem(m_splitSystem);

            TResponse data = await DoGet(id);

            if (data == null)
                return NotFound(id);

            return Ok(data);
        }

        /// <summary>
        /// 获取结果
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected virtual Task<TResponse> DoGet(long id)
        {
            if (HttpContext.CheckSystem(m_splitSystem))
                return m_searchQuery.SplitBySystemID(HttpContext).FilterIsDeleted().GetAsync(id);
            else
                return m_searchQuery.FilterIsDeleted().GetAsync(id);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="searchQuery"></param>
        /// <param name="splitSystem"></param>
        public GenericGetController(ISearchQuery<TResponse> searchQuery, bool splitSystem = true)
        {
            m_searchQuery = searchQuery;
            m_splitSystem = splitSystem;
        }
    }

    /// <summary>
    /// ServiceToService 批量查询接口基类
    /// </summary>
    /// <typeparam name="TRequest">请求实体泛型</typeparam>
    /// <typeparam name="TResponse">返回实体泛型</typeparam>
    [ApiController]
    public abstract class BatchGenericSearchController<TRequest, TResponse> : ControllerBase
    {
        /// <summary>
        /// Get
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public Task<TResponse> Get(TRequest request)
        {
            return SearchDatas(request);
        }

        /// <summary>
        /// 获取结果
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected abstract Task<TResponse> SearchDatas(TRequest request);
    }

    /// <summary>
    /// 根据筛选条件查询列表且请求，返回实体皆继承于ViewModelBase的列表查询接口基类
    /// </summary>
    /// <typeparam name="TRequest">请求实体泛型，继承于ViewModelBase</typeparam>
    /// <typeparam name="TResponse">结果实体泛型，继承于ViewModelBase</typeparam>
    [ApiController]
    public abstract class GenericSearchController<TRequest, TResponse> : ControllerBase
        where TRequest : ViewModelBase, new()
        where TResponse : ViewModelBase, new()
    {
        private ISearchQuery<TResponse> m_searchQuery;
        private readonly bool m_splitSystem;
        private readonly IDBResourceContent m_dbResourceContent;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="searchQuery"></param>
        /// <param name="splitSystem"></param>
        /// <param name="dbResourceContent"></param>
        public GenericSearchController(ISearchQuery<TResponse> searchQuery, bool splitSystem = true, IDBResourceContent dbResourceContent = null)
        {
            m_searchQuery = searchQuery;
            m_splitSystem = splitSystem;
            m_dbResourceContent = dbResourceContent;
        }

        /// <summary>
        /// Get
        /// </summary>
        /// <param name="pageQueryParameterService"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<PageQueryResult<TResponse>> Get([FromServices] IPageQueryParameterService pageQueryParameterService)
        {
            HttpContext.CheckSystem(m_splitSystem); 

            Tuple<IEnumerable<TResponse>, int> tupleDatas = await SearchDatas(pageQueryParameterService.GetQueryParameter<TRequest>());

            return new PageQueryResult<TResponse>()
            {
                Datas = await PreperDatas(tupleDatas?.Item1),
                TotalCount = tupleDatas?.Item2 ?? 0
            };
        }

        /// <summary>
        /// 查询方法
        /// </summary>
        /// <param name="pageQuery">请求参数</param>
        /// <returns></returns>
        protected virtual async Task<Tuple<IEnumerable<TResponse>, int>> SearchDatas(PageQuery<TRequest> pageQuery)
        {
            Expression<Func<TResponse, bool>> linq = GetBaseLinq(pageQuery.Condition);
            IDBResourceContent dbResourceContent = m_dbResourceContent ?? HttpContext.RequestServices.GetService<IDBResourceContent>();

            if (HttpContext.CheckSystem(m_splitSystem))
                return Tuple.Create(await m_searchQuery.SplitBySystemID(HttpContext).
                                                        FilterIsDeleted().
                                                        OrderByIDDesc().
                                                        SearchAsync(linq, startIndex: pageQuery.StartIndex, count: pageQuery.PageCount,
                                                                    dbResourceContent: dbResourceContent),
                                    await m_searchQuery.SplitBySystemID(HttpContext).
                                                        FilterIsDeleted().
                                                        CountAsync(linq, dbResourceContent: m_dbResourceContent));
            else
                return Tuple.Create(await m_searchQuery.FilterIsDeleted().
                                                        OrderByIDDesc().
                                                        SearchAsync(linq, startIndex: pageQuery.StartIndex, count: pageQuery.PageCount,
                                                                    dbResourceContent: dbResourceContent),
                                    await m_searchQuery.FilterIsDeleted().
                                                        CountAsync(linq, dbResourceContent: m_dbResourceContent));
        }

        /// <summary>
        /// 查询结果处理方法
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        protected virtual Task<IEnumerable<TResponse>> PreperDatas(IEnumerable<TResponse> datas)
        {
            return Task.FromResult(datas);
        }

        /// <summary>
        /// 获取LinqSearchAttribute特性指定的Linq
        /// </summary>
        /// <param name="queryCondition">查询条件实体</param>
        /// <returns></returns>
        protected virtual Expression<Func<TResponse, bool>> GetBaseLinq(TRequest queryCondition)
        {
            if (queryCondition == null)
                return item => true;

            LinqSearchAttribute linqSearchAttribute = typeof(TResponse).GetCustomAttribute<LinqSearchAttribute>();

            if (linqSearchAttribute != null && !string.IsNullOrWhiteSpace(linqSearchAttribute.GetLinqFunctionName))
            {
                MethodInfo method = typeof(TResponse).GetMethod(linqSearchAttribute.GetLinqFunctionName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (method != null)
                {
                    Func<TRequest, Expression<Func<TResponse, bool>>> predicateLinq = method.Invoke(null, null) as Func<TRequest, Expression<Func<TResponse, bool>>>;

                    if (predicateLinq != null)
                        return predicateLinq(queryCondition);
                }
            }

            return item => true;
        }
    }

    /// <summary>
    /// 自定义返回值，根据查询出的实体对返回值进行拼装
    /// </summary>
    /// <typeparam name="TRequest">请求实体参数，继承于ViewModelBase</typeparam>
    [ApiController]
    public abstract class GenericCustomSearchController<TRequest> : ControllerBase
        where TRequest : ViewModelBase, new()
    {
        /// <summary>
        /// Get
        /// </summary>
        /// <param name="pageQueryParameterService"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<JObject> Get([FromServices] IPageQueryParameterService pageQueryParameterService)
        {
            return new JObject()
            {
                ["Datas"] = await SearchDatas(pageQueryParameterService.GetQueryParameter<TRequest>()),
                ["TotalCount"] = await SearchDatasCount(pageQueryParameterService.GetQueryParameter<TRequest>())
            };
        }

        /// <summary>
        /// 返回查询条件总条数
        /// </summary>
        /// <param name="pageQuery"></param>
        /// <returns></returns>
        protected abstract Task<int> SearchDatasCount(PageQuery<TRequest> pageQuery);

        /// <summary>
        /// 查询结果
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        protected abstract Task<JArray> SearchDatas(PageQuery<TRequest> datas);
    }

    /// <summary>
    /// 新增接口基类
    /// </summary>
    /// <typeparam name="TRequest">请求实体泛型，继承于ViewModelBase</typeparam>
    [ApiController]
    public abstract class GenericPostController<TRequest> : ControllerBase where TRequest : ViewModelBase, new()
    {
        private IEditQuery<TRequest> m_editQuery;
        private ISSOUserService m_ssoUserService;
        private readonly bool m_splitSystem;

        /// <summary>
        /// Post
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TRequest request)
        {
            HttpContext.CheckSystem(m_splitSystem);

            request.ID = IDGenerator.NextID();
            request.AddCreateUser(m_ssoUserService);
            request.IsDeleted = false;

            await DoPost(request.ID, request);

            return Ok(request);
        }

        /// <summary>
        /// 获取结果
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        protected virtual async Task DoPost(long id, TRequest request)
        {
            if (HttpContext.CheckSystem(m_splitSystem))
                await m_editQuery.SplitBySystemID(HttpContext).FilterIsDeleted().InsertAsync(datas: request);
            else
                await m_editQuery.FilterIsDeleted().InsertAsync(datas: request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="editQuery"></param>
        /// <param name="ssoUserService"></param>
        /// <param name="splitSystem"></param>
        public GenericPostController(IEditQuery<TRequest> editQuery, ISSOUserService ssoUserService, bool splitSystem = true)
        {
            m_editQuery = editQuery;
            m_ssoUserService = ssoUserService;
            m_splitSystem = splitSystem;
        }
    }

    /// <summary>
    /// 修改接口基类
    /// </summary>
    /// <typeparam name="TRequest">请求实体泛型，继承于ViewModelBase</typeparam>
    [ApiController]
    public abstract class GenericPutController<TRequest> : ControllerBase
        where TRequest : ViewModelBase, new()
    {
        private IEditQuery<TRequest> m_editQuery;
        private ISearchQuery<TRequest> m_searchQuery;
        private ISSOUserService m_ssoUserService;
        private readonly bool m_splitSystem;

        /// <summary>
        /// Put
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut]
        public virtual async Task<IActionResult> Put([FromBody] TRequest request)
        {
            int count = HttpContext.CheckSystem(m_splitSystem)
                            ? m_searchQuery.SplitBySystemID(HttpContext).FilterIsDeleted().Count(item => item.ID == request.ID)
                            : m_searchQuery.FilterIsDeleted().Count(item => item.ID == request.ID);

            if (count > 0)
            {
                request.AddUpdateUser(m_ssoUserService);
                await DoPut(request);

                return Ok();
            }
            else
                return NotFound();
        }

        /// <summary>
        /// 获取结果
        /// </summary>
        /// <param name="request"></param>
        protected virtual async Task DoPut(TRequest request)
        {
            if (HttpContext.CheckSystem(m_splitSystem))
                await m_editQuery.SplitBySystemID(HttpContext).FilterIsDeleted().UpdateAsync(request);
            else
                await m_editQuery.FilterIsDeleted().UpdateAsync(request);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="editQuery"></param>
        /// <param name="searchQuery"></param>
        /// <param name="ssoUserService"></param>
        /// <param name="splitSystem"></param>
        public GenericPutController(IEditQuery<TRequest> editQuery, ISearchQuery<TRequest> searchQuery, ISSOUserService ssoUserService, bool splitSystem)
        {
            m_editQuery = editQuery;
            m_searchQuery = searchQuery;
            m_ssoUserService = ssoUserService;
            m_splitSystem = splitSystem;
        }
    }

    /// <summary>
    /// 删除接口基类
    /// </summary>
    /// <typeparam name="TRequest">请求实体泛型，继承于ViewModelBase</typeparam>
    [ApiController]
    public abstract class GenericDeleteController<TRequest> : ControllerBase
        where TRequest : ViewModelBase, new()
    {
        private IEditQuery<TRequest> m_editQuery;
        private ISearchQuery<TRequest> m_searchQuery;
        private readonly bool m_splitSystem;

        /// <summary>
        /// Delete
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public virtual async Task<IActionResult> Delete(long id)
        {
            int count = HttpContext.CheckSystem(m_splitSystem)
                            ? await m_searchQuery.SplitBySystemID(HttpContext).FilterIsDeleted().CountAsync(item => item.ID == id)
                            : await m_searchQuery.FilterIsDeleted().CountAsync(item => item.ID == id);

            if (count > 0)
            {
                await DoDelete(id);
                return Ok();
            }
            else
                return NotFound();
        }

        /// <summary>
        /// 获取结果
        /// </summary>
        /// <param name="id"></param>
        protected virtual async Task DoDelete(long id)
        {
            if (HttpContext.CheckSystem(m_splitSystem))
                await m_editQuery.SplitBySystemID(HttpContext).FilterIsDeleted().DeleteAsync(ids: id);
            else
                await m_editQuery.FilterIsDeleted().DeleteAsync(ids: id);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="editQuery"></param>
        /// <param name="searchQuery"></param>
        /// <param name="splitSystem"></param>
        public GenericDeleteController(IEditQuery<TRequest> editQuery, ISearchQuery<TRequest> searchQuery, bool splitSystem)
        {
            m_editQuery = editQuery;
            m_searchQuery = searchQuery;
            m_splitSystem = splitSystem;
        }
    }

    /// <summary>
    /// 根据ID返回两个实体且实体继承于ViewModelBase接口基类
    /// </summary>
    /// <typeparam name="TResponse1">请求实体泛型，继承于ViewModelBase</typeparam>
    /// <typeparam name="TResponse2">请求实体泛型，继承于ViewModelBase</typeparam>
    [ApiController]
    public abstract class MultipleGenericGetController<TResponse1, TResponse2> : ControllerBase
        where TResponse1 : ViewModelBase, new()
        where TResponse2 : ViewModelBase, new()
    {
        /// <summary>
        /// get请求入口
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            Tuple<TResponse1, TResponse2> data = await DoGet(id);

            if (data == null)
                return NotFound(id);

            return Ok(new Dictionary<string, object>
            {
                [typeof(TResponse1).Name] = data.Item1,
                [typeof(TResponse2).Name] = data.Item2
            });
        }

        /// <summary>
        /// get请求具体实现方法
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected abstract Task<Tuple<TResponse1, TResponse2>> DoGet(long id);
    }

    /// <summary>
    /// 根据ID返回三个实体且实体继承于ViewModelBase接口基类
    /// </summary>
    /// <typeparam name="TResponse1">请求实体泛型，继承于ViewModelBase</typeparam>
    /// <typeparam name="TResponse2">请求实体泛型，继承于ViewModelBase</typeparam>
    /// <typeparam name="TResponse3">请求实体泛型，继承于ViewModelBase</typeparam>
    [ApiController]
    public abstract class MultipleGenericGetController<TResponse1, TResponse2, TResponse3> : ControllerBase
        where TResponse1 : ViewModelBase, new()
        where TResponse2 : ViewModelBase, new()
        where TResponse3 : ViewModelBase, new()
    {
        /// <summary>
        /// get请求入口
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            Tuple<TResponse1, TResponse2, TResponse3> data = await DoGet(id);

            if (data == null)
                return NotFound(id);

            return Ok(new Dictionary<string, object>
            {
                [typeof(TResponse1).Name] = data.Item1,
                [typeof(TResponse2).Name] = data.Item2,
                [typeof(TResponse2).Name] = data.Item3
            });
        }

        /// <summary>
        /// get请求具体方法实现
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected abstract Task<Tuple<TResponse1, TResponse2, TResponse3>> DoGet(long id);
    }

    /// <summary>
    /// 自定义Controller接口
    /// </summary>
    public interface IDynamicController
    {
    }

    public abstract class MultipleGenericControllerBase
    {
        private readonly IHttpContextAccessor m_httpContextAccessor;
        protected HttpContext HttpContext { get { return m_httpContextAccessor.HttpContext; } }

        public MultipleGenericControllerBase(IHttpContextAccessor httpContextAccessor)
        {
            m_httpContextAccessor = httpContextAccessor;
        }
    }

    /// <summary>
    /// 一个参数的Post请求接口基类
    /// </summary>
    /// <typeparam name="TRequest1">请求实体泛型</typeparam>
    public abstract class MultipleGenericPostController<TRequest1> : MultipleGenericControllerBase, IDynamicController
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        public MultipleGenericPostController(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }

        /// <summary>
        /// post请求入口
        /// </summary>
        /// <param name="request1"></param>
        /// <returns></returns>
        public async Task<IActionResult> Post(TRequest1 request1)
        {
            await DoPost(request1);

            object result = await GetReturnValue();

            if (result == null)
                return new OkResult();
            else
                return new OkObjectResult(result);
        }

        /// <summary>
        /// 设置接口返回值
        /// </summary>
        /// <returns></returns>
        public virtual Task<object> GetReturnValue()
        {
            return Task.FromResult(default(object));
        }

        /// <summary>
        /// post请求具体执行方法
        /// </summary>
        /// <param name="request1"></param>
        protected abstract Task DoPost(TRequest1 request1);
    }

    /// <summary>
    /// 两个参数的Post请求接口基类
    /// </summary>
    /// <typeparam name="TRequest1">请求实体泛型</typeparam>
    /// <typeparam name="TRequest2">请求实体泛型</typeparam>
    public abstract class MultipleGenericPostController<TRequest1, TRequest2> : MultipleGenericControllerBase, IDynamicController
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        public MultipleGenericPostController(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }

        /// <summary>
        /// post请求入口
        /// </summary>
        /// <param name="request1"></param>
        /// <param name="request2"></param>
        /// <returns></returns>
        public async Task<IActionResult> Post(TRequest1 request1, TRequest2 request2)
        {
            await DoPost(request1, request2);

            object result = await GetReturnValue();

            if (result == null)
                return new OkResult();
            else
                return new OkObjectResult(result);
        }

        /// <summary>
        /// 设置接口返回值
        /// </summary>
        /// <returns></returns>
        public virtual Task<object> GetReturnValue()
        {
            return Task.FromResult(default(object));
        }

        /// <summary>
        /// post请求具体执行方法
        /// </summary>
        /// <param name="request1"></param>
        /// <param name="request2"></param>
        protected abstract Task DoPost(TRequest1 request1, TRequest2 request2);
    }

    /// <summary>
    /// 三个参数的Post请求接口基类
    /// </summary>
    /// <typeparam name="TRequest1">请求实体泛型</typeparam>
    /// <typeparam name="TRequest2">请求实体泛型</typeparam>
    /// <typeparam name="TRequest3">请求实体泛型</typeparam>
    public abstract class MultipleGenericPostController<TRequest1, TRequest2, TRequest3> : MultipleGenericControllerBase, IDynamicController
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        public MultipleGenericPostController(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }

        /// <summary>
        /// post请求
        /// </summary>
        /// <param name="request1"></param>
        /// <param name="request2"></param>
        /// <param name="request3"></param>
        /// <returns></returns>
        public async Task<IActionResult> Post(TRequest1 request1, TRequest2 request2, TRequest3 request3)
        {
            await DoPost(request1, request2, request3);

            object result = await GetReturnValue();

            if (result == null)
                return new OkResult();
            else
                return new OkObjectResult(result);
        }

        /// <summary>
        /// 设置接口返回值
        /// </summary>
        /// <returns></returns>
        public virtual Task<object> GetReturnValue()
        {
            return Task.FromResult(default(object));
        }

        /// <summary>
        /// post请求具体实现方法
        /// </summary>
        /// <param name="request1"></param>
        /// <param name="request2"></param>
        /// <param name="request3"></param>
        protected abstract Task DoPost(TRequest1 request1, TRequest2 request2, TRequest3 request3);
    }

    /// <summary>
    /// 一个参数的Put请求接口基类
    /// </summary>
    /// <typeparam name="TRequest1">请求实体泛型</typeparam>
    public abstract class MultipleGenericPutController<TRequest1> : MultipleGenericControllerBase, IDynamicController
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        public MultipleGenericPutController(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }

        /// <summary>
        /// Put请求入口
        /// </summary>
        /// <param name="request1"></param>
        /// <returns></returns>
        public async Task<IActionResult> Put(TRequest1 request1)
        {
            await DoPut(request1);
            return new OkResult();
        }

        /// <summary>
        /// put请求具体方法实现
        /// </summary>
        /// <param name="request1"></param>
        protected abstract Task DoPut(TRequest1 request1);
    }

    /// <summary>
    /// 两个参数的Put请求接口基类
    /// </summary>
    /// <typeparam name="TRequest1">请求实体泛型</typeparam>
    /// <typeparam name="TRequest2">请求实体泛型</typeparam>
    public abstract class MultipleGenericPutController<TRequest1, TRequest2> : MultipleGenericControllerBase, IDynamicController
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        public MultipleGenericPutController(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }

        /// <summary>
        /// put请求入口
        /// </summary>
        /// <param name="request1"></param>
        /// <param name="request2"></param>
        /// <returns></returns>
        public async Task<IActionResult> Put(TRequest1 request1, TRequest2 request2)
        {
            await DoPut(request1, request2);
            return new OkResult();
        }

        /// <summary>
        /// put请求具体执行方法
        /// </summary>
        /// <param name="request1"></param>
        /// <param name="request2"></param>
        protected abstract Task DoPut(TRequest1 request1, TRequest2 request2);
    }

    /// <summary>
    /// 三个参数的Put请求接口基类
    /// </summary>
    /// <typeparam name="TRequest1">请求实体泛型</typeparam>
    /// <typeparam name="TRequest2">请求实体泛型</typeparam>
    /// <typeparam name="TRequest3">请求实体泛型</typeparam>
    public abstract class MultipleGenericPutController<TRequest1, TRequest2, TRequest3> : MultipleGenericControllerBase, IDynamicController
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        public MultipleGenericPutController(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }

        /// <summary>
        /// put请求入口
        /// </summary>
        /// <param name="request1"></param>
        /// <param name="request2"></param>
        /// <param name="request3"></param>
        /// <returns></returns>
        public async Task<IActionResult> Put(TRequest1 request1, TRequest2 request2, TRequest3 request3)
        {
            await DoPut(request1, request2, request3);
            return new OkResult();
        }

        /// <summary>
        /// put请求具体执行方法
        /// </summary>
        /// <param name="request1"></param>
        /// <param name="request2"></param>
        /// <param name="request3"></param>
        protected abstract Task DoPut(TRequest1 request1, TRequest2 request2, TRequest3 request3);
    }

    /// <summary>
    /// 动态请求类参数验证文本特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class DynamicDisplayAttribute : Attribute
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public string ParameterName { get; set; }

        /// <summary>
        /// 显示内容
        /// </summary>
        public string DisplayText { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <param name="displayText">显示内容</param>
        public DynamicDisplayAttribute(string parameterName, string displayText)
        {
            ParameterName = parameterName;
            DisplayText = displayText;
        }
    }
}