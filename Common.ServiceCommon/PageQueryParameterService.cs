using Common.Model;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Common.ServiceCommon
{
    /// <summary>
    /// 分页接口
    /// </summary>
    public interface IPageQueryParameterService
    {
        /// <summary>
        /// 泛型返回分页参数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        PageQuery<T> GetQueryParameter<T>() where T : new();
    }

    /// <summary>
    /// 分页实现
    /// </summary>
    public class HttpContextQueryStringPageQueryParameterService : IPageQueryParameterService
    {
        private IHttpContextAccessor m_httpContextAccessor;

        /// <summary>
        ///
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        public HttpContextQueryStringPageQueryParameterService(IHttpContextAccessor httpContextAccessor) => m_httpContextAccessor = httpContextAccessor;

        /// <summary>
        /// 泛型返回分页参数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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

            foreach (var item in query)
            {
                // ReSharper disable once StringIndexOfIsCultureSpecific.1
                if (item.Key.IndexOf(conditionPropertyName) == 0 &&
                    item.Value.Count > 0 &&
                    !string.IsNullOrWhiteSpace(item.Value[0]))
                {
                    string propertyName = JsonUtils.PropertyNameToCSharpStyle(item.Key.Replace(conditionPropertyName, string.Empty));

                    PropertyInfo propertyInfo = typeof(T).GetProperty(propertyName);

                    if (propertyInfo == null)
                        continue;

                    Type underlyingType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

                    if (underlyingType.Name != typeof(IEnumerable<>).Name &&
                        ((!underlyingType.IsValueType && underlyingType != typeof(string)) ||
                        underlyingType.GetInterface(typeof(IConvertible).FullName) == null))
                        continue;

                    try
                    {
                        if (underlyingType.IsEnum)
                        {
                            propertyInfo.SetValue(pageQuery.Condition, Enum.Parse(underlyingType, item.Value[0]));
                        }
                        else if (underlyingType.Name == typeof(IEnumerable<>).Name)
                        {
                            string[] values = item.Value.SelectMany(item => item.Split(",")).ToArray();
                            Type elementType = underlyingType.GenericTypeArguments[0];

                            object enumerableValue = null;

                            if (elementType.IsEnum)
                                enumerableValue = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(elementType).
                                  Invoke(null, new object[] { values.Select(item => Enum.Parse(elementType, item)) });
                            else
                                enumerableValue = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(elementType).
                                  Invoke(null, new object[] { values.Select(item => Convert.ChangeType(item, elementType)) });

                            propertyInfo.SetValue(pageQuery.Condition, enumerableValue);
                        }
                        else
                        {
                            propertyInfo.SetValue(pageQuery.Condition, Convert.ChangeType(item.Value[0].Trim(), underlyingType));
                        }
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