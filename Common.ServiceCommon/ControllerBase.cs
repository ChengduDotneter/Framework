using Common.DAL;
using Common.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Common.ServiceCommon
{
    /// <summary>
    /// ServiceToService 批量查询接口基类
    /// </summary>
    /// <typeparam name="TRequest">请求实体泛型</typeparam>
    /// <typeparam name="TResponse">返回实体泛型</typeparam>
    [ApiController]
    public abstract class BatchGenericSearchController<TRequest, TResponse> : ControllerBase
    {
        [HttpPost]
        public TResponse Get(TRequest request)
        {
            return SearchDatas(request);
        }

        protected abstract TResponse SearchDatas(TRequest request);
    }

    /// <summary>
    /// Service中无相应的实体接受请求参数时使用且请求参数为JObject的接口基类
    /// </summary>
    /// <typeparam name="TResponse">返回结果泛型/typeparam>
    [ApiController]
    public abstract class JObjectGenericPostController<TResponse> : ControllerBase
    {
        [HttpPost]
        public TResponse Post([FromServices]IJObjectSerializeService jObjectSerializeService)
        {
            return DoPost(jObjectSerializeService.GetJObject());
        }

        protected abstract TResponse DoPost(JObject request);
    }

    /// <summary>
    /// Service中无相应的实体接受请求参数时使用且请求参数为JArray的接口基类
    /// </summary>
    /// <typeparam name="TResponse">返回实体泛型</typeparam>
    [ApiController]
    public abstract class JArrayGenericPostController<TResponse> : ControllerBase
    {
        [HttpPost]
        public TResponse Post([FromServices]IJArraySerializeService jArraySerializeService)
        {
            return DoPost(jArraySerializeService.GetJArray());
        }

        protected abstract TResponse DoPost(JArray request);
    }

    /// <summary>
    /// 根据ID查询自定义结果的接口基类
    /// </summary>
    /// <typeparam name="TResponse">返回实体泛型</typeparam>
    public abstract class BatchGenericGetController<TResponse> : ControllerBase
    {
        [HttpGet("{id}")]
        public IActionResult Get(long id)
        {
            TResponse data = DoGet(id);

            if (data == null)
                return NotFound(id);

            return Ok(data);
        }

        protected abstract TResponse DoGet(long id);
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

        [HttpGet("{id}")]
        public IActionResult Get(long id)
        {
            TResponse data = DoGet(id);

            if (data == null)
                return NotFound(id);

            return Ok(data);
        }

        protected virtual TResponse DoGet(long id)
        {
            return m_searchQuery.FilterIsDeleted().Get(id);
        }

        public GenericGetController(ISearchQuery<TResponse> searchQuery)
        {
            m_searchQuery = searchQuery;
        }
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
        private static ISearchQuery<TResponse> m_searchQuery;

        public GenericSearchController(ISearchQuery<TResponse> searchQuery) => m_searchQuery = searchQuery;

        [HttpGet]
        public PageQueryResult<TResponse> Get([FromServices]IPageQueryParameterService pageQueryParameterService)
        {
            Tuple<IEnumerable<TResponse>, int> tupleDatas = SearchDatas(pageQueryParameterService.GetQueryParameter<TRequest>());

            return new PageQueryResult<TResponse>()
            {
                Datas = PreperDatas(tupleDatas?.Item1),
                TotalCount = tupleDatas?.Item2 ?? 0
            };
        }

        /// <summary>
        /// 查询方法
        /// </summary>
        /// <param name="pageQuery">请求参数</param>
        /// <returns></returns>
        protected virtual Tuple<IEnumerable<TResponse>, int> SearchDatas(PageQuery<TRequest> pageQuery)
        {
            Expression<Func<TResponse, bool>> linq = GetBaseLinq(pageQuery.Condition);

            return Tuple.Create(m_searchQuery.FilterIsDeleted().OrderByIDDesc().Search(linq, startIndex: pageQuery.StartIndex, count: pageQuery.PageCount), m_searchQuery.FilterIsDeleted().Count(linq));
        }

        /// <summary>
        /// 查询结果处理方法
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        protected virtual IEnumerable<TResponse> PreperDatas(IEnumerable<TResponse> datas) => datas;

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
                    Func<TRequest, Expression<Func<TResponse, bool>>> predicateLinq = method.Invoke(null, null) as Func<TRequest, Expression<Func<TResponse, bool>>> ?? null;

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
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TSearhEntity"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    [ApiController]
    public abstract class GenericCustomSearchController<TRequest, TSearhEntity, TResponse> : ControllerBase
       where TRequest : ViewModelBase, new()
       where TSearhEntity : ViewModelBase, new()
       where TResponse : new()
    {
        public GenericCustomSearchController(ISearchQuery<TSearhEntity> searchQuery)
        {
        }

        [HttpGet]
        public PageQueryResult<TResponse> Get([FromServices]IPageQueryParameterService pageQueryParameterService)
        {
            Tuple<IEnumerable<TSearhEntity>, int> tupleDatas = Tuple.Create(SearchDatas(pageQueryParameterService.GetQueryParameter<TRequest>()), SearchDatasCount(pageQueryParameterService.GetQueryParameter<TRequest>()));

            return new PageQueryResult<TResponse>()
            {
                Datas = PreperDatas(tupleDatas?.Item1),
                TotalCount = tupleDatas?.Item2 ?? 0
            };
        }

        protected abstract IEnumerable<TSearhEntity> SearchDatas(PageQuery<TRequest> pageQuery);

        protected abstract int SearchDatasCount(PageQuery<TRequest> pageQuery);

        protected abstract IEnumerable<TResponse> PreperDatas(IEnumerable<TSearhEntity> datas);
    }

    [Obsolete("该接口基类即将过期")]
    [ApiController]
    public abstract class GenericSearchWithQueryController<TRequest, TResponse> : ControllerBase
        where TRequest : ViewModelBase, new()
        where TResponse : ViewModelBase, new()
    {
        [HttpGet]
        public PageQueryResult<TResponse> Get([FromServices]IPageQueryParameterService pageQueryParameterService)
        {
            PageQuery<TRequest> pageQuery = pageQueryParameterService.GetQueryParameter<TRequest>();

            return new PageQueryResult<TResponse>()
            {
                TotalCount = GetCount(pageQuery),
                Datas = PreperDatas(DoSearch(pageQuery))
            };
        }

        protected virtual IEnumerable<TResponse> PreperDatas(IEnumerable<TResponse> datas)
        {
            return datas;
        }

        protected virtual IEnumerable<TResponse> DoSearch(PageQuery<TRequest> pageQuery)
        {
            return null;
        }

        protected virtual int GetCount(PageQuery<TRequest> pageQuery)
        {
            return 0;
        }
    }

    /// <summary>
    /// 无查询条件的Get请求接口街垒
    /// </summary>
    /// <typeparam name="TResponse">返回实体参数</typeparam>
    [ApiController]
    public abstract class BatchGenericSearchController<TResponse> : ControllerBase
    {
        [HttpGet]
        public TResponse Get()
        {
            return SearchDatas();
        }

        protected abstract TResponse SearchDatas();
    }

    /// <summary>
    /// 两个实体聚合查询接口基类
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TTable"></typeparam>
    /// <typeparam name="TJoinTable"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    [ApiController]
    public abstract class GenericSearchController<TRequest, TTable, TJoinTable, TResponse> : ControllerBase
     where TRequest : new()
     where TTable : ViewModelBase, new()
     where TJoinTable : ViewModelBase, new()
     where TResponse : ViewModelBase, new()
    {
        private static Func<TRequest, Expression<Func<TResponse, TJoinTable, bool>>> m_predicateJoinTableLinq;
        private ISearchQuery<TResponse> m_searchQuery;

        [HttpGet]
        public PageQueryResult<TResponse> Get([FromServices]IPageQueryParameterService pageQueryParameterService)
        {
            PageQuery<TRequest> pageQuery = pageQueryParameterService.GetQueryParameter<TRequest>();

            if (m_predicateJoinTableLinq != null)
            {
                Expression<Func<TResponse, TJoinTable, bool>> linq = m_predicateJoinTableLinq(pageQuery.Condition);

                return new PageQueryResult<TResponse>()
                {
                    TotalCount = GetCount(linq),
                    Datas = PreperDatas(SearchDatas(linq, pageQuery))
                };
            }
            else
            {
                return new PageQueryResult<TResponse>()
                {
                    TotalCount = m_searchQuery.FilterIsDeleted().Count(),
                    Datas = PreperDatas(SearchDatas(pageQuery))
                };
            }
        }

        protected virtual IEnumerable<TResponse> PreperDatas(IEnumerable<TResponse> datas)
        {
            return datas;
        }

        protected abstract int GetCount(Expression<Func<TResponse, TJoinTable, bool>> linq);

        protected abstract IEnumerable<TResponse> SearchDatas(Expression<Func<TResponse, TJoinTable, bool>> linq, PageQuery<TRequest> pageQuery);

        protected virtual IEnumerable<TResponse> SearchDatas(PageQuery<TRequest> pageQuery)
        {
            return m_searchQuery.FilterIsDeleted().OrderByIDDesc().Search(startIndex: pageQuery.StartIndex, count: pageQuery.PageCount);
        }

        public GenericSearchController(ISearchQuery<TResponse> searchQuery)
        {
            m_searchQuery = searchQuery;
        }

        static GenericSearchController()
        {
            LinqJoinTableSearchAttribute linqJoinTableSearchAttribute = typeof(TResponse).GetCustomAttribute<LinqJoinTableSearchAttribute>();

            if (linqJoinTableSearchAttribute != null && !string.IsNullOrWhiteSpace(linqJoinTableSearchAttribute.GetLinqJoinTableSearchFunctionName))
            {
                MethodInfo method = typeof(TResponse).GetMethod(linqJoinTableSearchAttribute.GetLinqJoinTableSearchFunctionName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (method != null)
                    m_predicateJoinTableLinq = method.Invoke(null, null) as Func<TRequest, Expression<Func<TResponse, TJoinTable, bool>>> ?? null;
            }
        }
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

        [HttpPost]
        public IActionResult Post([FromBody]TRequest request)
        {
            request.ID = IDGenerator.NextID();
            request.CreateUserID = m_ssoUserService.GetUser().ID;
            request.CreateTime = DateTime.Now;
            request.IsDeleted = false;

            DoPost(request.ID, request);

            return Ok(request);
        }

        protected virtual void DoPost(long id, TRequest request)
        {
            m_editQuery.FilterIsDeleted().Insert(request);
        }

        public GenericPostController(IEditQuery<TRequest> editQuery, ISSOUserService ssoUserService)
        {
            m_editQuery = editQuery;
            m_ssoUserService = ssoUserService;
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

        [HttpPut]
        public virtual IActionResult Put([FromBody]TRequest request)
        {
            if (m_searchQuery.FilterIsDeleted().Count(item => item.ID == request.ID) > 0)
            {
                request.UpdateUserID = m_ssoUserService.GetUser().ID;
                request.UpdateTime = DateTime.Now;
                DoPut(request);

                return Ok();
            }
            else
                return NotFound();
        }

        protected virtual void DoPut(TRequest request)
        {
            IgnoreColumnAttribute ignoreColumnAttribute = typeof(TRequest).GetCustomAttribute<IgnoreColumnAttribute>();
            m_editQuery.FilterIsDeleted().Update(request, ignoreColumnAttribute?.IgnoreColumns);
        }

        public GenericPutController(IEditQuery<TRequest> editQuery, ISearchQuery<TRequest> searchQuery, ISSOUserService ssoUserService)
        {
            m_editQuery = editQuery;
            m_searchQuery = searchQuery;
            m_ssoUserService = ssoUserService;
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

        [HttpDelete("{id}")]
        public virtual IActionResult Delete(long id)
        {
            if (m_searchQuery.FilterIsDeleted().Count(item => item.ID == id) > 0)
            {
                DoDelete(id);
                return Ok();
            }
            else
                return NotFound();
        }

        protected virtual void DoDelete(long id)
        {
            m_editQuery.FilterIsDeleted().Delete(id);
        }

        public GenericDeleteController(IEditQuery<TRequest> editQuery, ISearchQuery<TRequest> searchQuery)
        {
            m_editQuery = editQuery;
            m_searchQuery = searchQuery;
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
        [HttpGet("{id}")]
        public IActionResult Get(long id)
        {
            Tuple<TResponse1, TResponse2> data = DoGet(id);

            if (data == null)
                return NotFound(id);

            return Ok(new Dictionary<string, object>
            {
                [typeof(TResponse1).Name] = data.Item1,
                [typeof(TResponse2).Name] = data.Item2
            });
        }

        protected abstract Tuple<TResponse1, TResponse2> DoGet(long id);
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
        [HttpGet("{id}")]
        public IActionResult Get(long id)
        {
            Tuple<TResponse1, TResponse2, TResponse3> data = DoGet(id);

            if (data == null)
                return NotFound(id);

            return Ok(new Dictionary<string, object>
            {
                [typeof(TResponse1).Name] = data.Item1,
                [typeof(TResponse2).Name] = data.Item2,
                [typeof(TResponse2).Name] = data.Item3
            });
        }

        protected abstract Tuple<TResponse1, TResponse2, TResponse3> DoGet(long id);
    }

    /// <summary>
    /// 自定义Controller接口
    /// </summary>
    public interface IDynamicController { }

    /// <summary>
    /// 一个参数的Post请求接口基类
    /// </summary>
    /// <typeparam name="TRequest1">请求实体泛型</typeparam>
    public abstract class MultipleGenericPostController<TRequest1> : IDynamicController
    {
        private ISSOUserService m_ssoUserService;

        public MultipleGenericPostController(ISSOUserService ssoUserService)
        {
            m_ssoUserService = ssoUserService;
        }

        public IActionResult Post(TRequest1 request1)
        {
            DoPost(request1);

            object result = GetReturnValue();

            if (result == null)
                return new OkResult();
            else
                return new OkObjectResult(result);
        }

        /// <summary>
        /// 设置接口返回值
        /// </summary>
        /// <returns></returns>
        public virtual object GetReturnValue() => null;

        protected abstract void DoPost(TRequest1 request1);
    }

    /// <summary>
    /// 两个参数的Post请求接口基类
    /// </summary>
    /// <typeparam name="TRequest1">请求实体泛型</typeparam>
    /// <typeparam name="TRequest2">请求实体泛型</typeparam>
    public abstract class MultipleGenericPostController<TRequest1, TRequest2> : IDynamicController
    {
        private ISSOUserService m_ssoUserService;

        public MultipleGenericPostController(ISSOUserService ssoUserService)
        {
            m_ssoUserService = ssoUserService;
        }

        public IActionResult Post(TRequest1 request1, TRequest2 request2)
        {
            DoPost(request1, request2);

            object result = GetReturnValue();

            if (result == null)
                return new OkResult();
            else
                return new OkObjectResult(result);
        }

        /// <summary>
        /// 设置接口返回值
        /// </summary>
        /// <returns></returns>
        public virtual object GetReturnValue() => null;

        protected abstract void DoPost(TRequest1 request1, TRequest2 request2);
    }

    /// <summary>
    /// 三个参数的Post请求接口基类
    /// </summary>
    /// <typeparam name="TRequest1">请求实体泛型</typeparam>
    /// <typeparam name="TRequest2">请求实体泛型</typeparam>
    /// <typeparam name="TRequest3">请求实体泛型</typeparam>
    public abstract class MultipleGenericPostController<TRequest1, TRequest2, TRequest3> : IDynamicController
    {
        private readonly ISSOUserService m_ssoUserService;

        public MultipleGenericPostController(ISSOUserService ssoUserService)
        {
            m_ssoUserService = ssoUserService;
        }

        public IActionResult Post(TRequest1 request1, TRequest2 request2, TRequest3 request3)
        {
            DoPost(request1, request2, request3);

            object result = GetReturnValue();

            if (result == null)
                return new OkResult();
            else
                return new OkObjectResult(result);
        }

        /// <summary>
        /// 设置接口返回值
        /// </summary>
        /// <returns></returns>
        public virtual object GetReturnValue() => null;

        protected abstract void DoPost(TRequest1 request1, TRequest2 request2, TRequest3 request3);
    }

    /// <summary>
    /// 一个参数的Put请求接口基类
    /// </summary>
    /// <typeparam name="TRequest1">请求实体泛型</typeparam>
    public abstract class MultipleGenericPutController<TRequest1> : IDynamicController
    {
        private ISSOUserService m_ssoUserService;

        public MultipleGenericPutController(ISSOUserService ssoUserService)
        {
            m_ssoUserService = ssoUserService;
        }

        public IActionResult Put(TRequest1 request1)
        {
            DoPut(request1);
            return new OkResult();
        }

        protected abstract void DoPut(TRequest1 request1);
    }

    /// <summary>
    /// 两个参数的Put请求接口基类
    /// </summary>
    /// <typeparam name="TRequest1">请求实体泛型</typeparam>
    /// <typeparam name="TRequest2">请求实体泛型</typeparam>
    public abstract class MultipleGenericPutController<TRequest1, TRequest2> : IDynamicController
    {
        private readonly ISSOUserService m_ssoUserService;

        public MultipleGenericPutController(ISSOUserService ssoUserService)
        {
            m_ssoUserService = ssoUserService;
        }

        public IActionResult Put(TRequest1 request1, TRequest2 request2)
        {
            DoPut(request1, request2);
            return new OkResult();
        }

        protected abstract void DoPut(TRequest1 request1, TRequest2 request2);
    }

    /// <summary>
    /// 三个参数的Put请求接口基类
    /// </summary>
    /// <typeparam name="TRequest1">请求实体泛型</typeparam>
    /// <typeparam name="TRequest2">请求实体泛型</typeparam>
    /// <typeparam name="TRequest3">请求实体泛型</typeparam>
    public abstract class MultipleGenericPutController<TRequest1, TRequest2, TRequest3> : IDynamicController
    {
        private ISSOUserService m_ssoUserService;

        public MultipleGenericPutController(ISSOUserService ssoUserService)
        {
            m_ssoUserService = ssoUserService;
        }

        public IActionResult Put(TRequest1 request1, TRequest2 request2, TRequest3 request3)
        {
            DoPut(request1, request2, request3);
            return new OkResult();
        }

        protected abstract void DoPut(TRequest1 request1, TRequest2 request2, TRequest3 request3);
    }
}