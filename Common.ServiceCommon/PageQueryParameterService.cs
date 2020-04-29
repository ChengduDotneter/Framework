using Common.DAL;
using Common.Model;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Reflection;

namespace Common.ServiceCommon
{
    public interface IPageQueryParameterService
    {
        PageQuery<T> GetQueryParameter<T>() where T : new();
    }

    public class HttpContextQueryStringPageQueryParameterService : IPageQueryParameterService
    {
        private IHttpContextAccessor m_httpContextAccessor;

        public HttpContextQueryStringPageQueryParameterService(IHttpContextAccessor httpContextAccessor) => m_httpContextAccessor = httpContextAccessor;

        public PageQuery<T> GetQueryParameter<T>() where T : new()
        {
            IQueryCollection query = m_httpContextAccessor.HttpContext.Request.Query;
            PageQuery<T> pageQuery = new PageQuery<T>() { Condition = new T() };

            string pageIndexPropertyName = JsonUtils.PropertyNameToJavaScriptStyle(nameof(PageQuery<T>.PageIndex));
            string pageCountPropertyName = JsonUtils.PropertyNameToJavaScriptStyle(nameof(PageQuery<T>.PageCount));
            string conditionPropertyName = $"{JsonUtils.PropertyNameToJavaScriptStyle(nameof(PageQuery<T>.Condition))}.";

            if (query.ContainsKey(pageIndexPropertyName) &&
                query[pageIndexPropertyName].Count > 0 &&
                int.TryParse(query[pageIndexPropertyName][0], out int pageIndex))
                pageQuery.PageIndex = pageIndex;

            if (query.ContainsKey(pageCountPropertyName) &&
                query[pageCountPropertyName].Count > 0 &&
                int.TryParse(query[pageCountPropertyName][0], out int pageCount))
                pageQuery.PageCount = pageCount;

            //TODO: 是否存在Sql注入
            foreach (var item in query)
            {
                if (item.Key.IndexOf(conditionPropertyName) == 0 &&
                    item.Value.Count > 0 &&
                    !string.IsNullOrWhiteSpace(item.Value[0]))
                {
                    string propertyName = JsonUtils.PropertyNameToCSharpStyle(item.Key.Replace(conditionPropertyName, string.Empty));

                    PropertyInfo propertyInfo = typeof(T).GetProperty(propertyName);

                    Type underlyingType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

                    if (propertyInfo == null ||
                        (!underlyingType.IsValueType && underlyingType != typeof(string)) ||
                        underlyingType.GetInterface(typeof(IConvertible).FullName) == null)
                        continue;

                    try
                    {
                        if (underlyingType.IsEnum)
                            propertyInfo.SetValue(pageQuery.Condition, Enum.Parse(underlyingType, item.Value[0]));
                        else
                            propertyInfo.SetValue(pageQuery.Condition, Convert.ChangeType(item.Value[0].Trim(), underlyingType));
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            return pageQuery;
        }
    }
}
