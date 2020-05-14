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

    [ApiController]
    public abstract class JArrayGenericPostController<TResponse> : ControllerBase
    {
        [HttpPost]
        public TResponse Get([FromServices]IJArraySerializeService jArraySerializeService)
        {
            return DoPost(jArraySerializeService.GetJArray());
        }

        protected abstract TResponse DoPost(JArray request);
    }

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

    [ApiController]
    public abstract class SearchController<TRequest, TResponse> : ControllerBase
        where TRequest : ViewModelBase, new()
        where TResponse : ViewModelBase, new()
    {
        private static string m_sql;
        private static Expression<Func<TResponse, bool>> m_linq;
        private static ISearchQuery<TResponse> m_searchQuery;
        private PageQuery<TRequest> m_pageQuery;

        [HttpGet]
        public PageQueryResult<TResponse> Get([FromServices]IPageQueryParameterService pageQueryParameterService)
        {
            m_pageQuery = pageQueryParameterService.GetQueryParameter<TRequest>();

            if (m_pageQuery.Condition != null)
            {
                m_linq = SetLinq(m_pageQuery.Condition);
                m_sql = SetSql(m_pageQuery.Condition);
            }

            Tuple<IEnumerable<TResponse>, int> tupleDatas = SearchDatas(m_linq, m_sql, m_pageQuery.StartIndex, m_pageQuery.PageCount);

            return new PageQueryResult<TResponse>()
            {
                Datas = PreperDatas(tupleDatas.Item1),
                TotalCount = tupleDatas.Item2
            };
        }

        protected TRequest GetQueryCondition()
        {
            return m_pageQuery.Condition;
        }

        protected virtual IEnumerable<TResponse> PreperDatas(IEnumerable<TResponse> datas) => datas;

        protected virtual Tuple<IEnumerable<TResponse>, int> SearchDatas(Expression<Func<TResponse, bool>> linq, string sql, int startIndex = 0, int count = int.MaxValue)
        {
            if (linq != null)
                return Tuple.Create(m_searchQuery.FilterIsDeleted().OrderByIDDesc().Search(linq, startIndex: startIndex, count: count), m_searchQuery.FilterIsDeleted().Count(linq));

            else if (!string.IsNullOrEmpty(sql))
                return Tuple.Create(m_searchQuery.FilterIsDeleted().OrderByIDDesc().Search(sql, startIndex: startIndex, count: count), m_searchQuery.FilterIsDeleted().Count(sql));

            else if (linq == null && string.IsNullOrEmpty(sql))
                return Tuple.Create(m_searchQuery.FilterIsDeleted().OrderByIDDesc().Search(startIndex: startIndex, count: count), m_searchQuery.FilterIsDeleted().Count());

            throw new NotSupportedException();
        }

        protected abstract Expression<Func<TResponse, bool>> SetLinq(TRequest queryCondition);

        protected abstract string SetSql(TRequest queryCondition);

        public SearchController(ISearchQuery<TResponse> searchQuery) => m_searchQuery = searchQuery;

    }

    [ApiController]
    public abstract class GenericSearchController<TRequest, TResponse> : SearchController<TRequest, TResponse>
        where TRequest : ViewModelBase, new()
        where TResponse : ViewModelBase, new()
    {

        public GenericSearchController(ISearchQuery<TResponse> searchQuery) : base(searchQuery)
        {
        }

        protected override Expression<Func<TResponse, bool>> SetLinq(TRequest queryCondition)
        {
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

            return null;
        }

        protected override string SetSql(TRequest queryCondition)
        {
            SqlSearchAttribute sqlSearchAttribute = typeof(TResponse).GetCustomAttribute<SqlSearchAttribute>();

            if (sqlSearchAttribute != null && !string.IsNullOrWhiteSpace(sqlSearchAttribute.GetSqlFunctionName))
            {
                MethodInfo method = typeof(TResponse).GetMethod(sqlSearchAttribute.GetSqlFunctionName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (method != null)
                {
                    Func<TRequest, string> predicateSql = method.Invoke(null, null) as Func<TRequest, string> ?? null;

                    if (predicateSql != null)
                        return predicateSql(queryCondition);
                }
            }

            return null;
        }
    }

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

    public interface IDynamicController { }

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

        public virtual object GetReturnValue()
        {
            return null;
        }

        protected abstract void DoPost(TRequest1 request1);
    }

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

        public virtual object GetReturnValue()
        {
            return null;
        }

        protected abstract void DoPost(TRequest1 request1, TRequest2 request2);
    }

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
        public virtual object GetReturnValue()
        {
            return null;
        }

        protected abstract void DoPost(TRequest1 request1, TRequest2 request2, TRequest3 request3);
    }

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
