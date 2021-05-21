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
            IQueryCollection query = m_httpContextAccessor.HttpContext.Request.Query;//获取请求的参数
            PageQuery<T> pageQuery = new PageQuery<T>() { Condition = new T() };//实例化

            string pageIndexPropertyName = JsonUtils.PropertyNameToJavaScriptStyle(nameof(PageQuery<T>.PageIndex));//转为首字符小写
            string pageCountPropertyName = JsonUtils.PropertyNameToJavaScriptStyle(nameof(PageQuery<T>.PageCount));//转为首字符小写
            string conditionPropertyName = $"{JsonUtils.PropertyNameToJavaScriptStyle(nameof(PageQuery<T>.Condition))}.";//转为首字符小写

            if (query.ContainsKey(pageIndexPropertyName) &&//如果存在PageIndex且PageCount大于0 且PageIndex为数字则赋值
                query[pageIndexPropertyName].Count > 0 &&
                int.TryParse(query[pageIndexPropertyName][0], out int pageIndex))
                pageQuery.PageIndex = pageIndex;

            if (query.ContainsKey(pageCountPropertyName) &&
                query[pageCountPropertyName].Count > 0 &&
                int.TryParse(query[pageCountPropertyName][0], out int pageCount))
                pageQuery.PageCount = pageCount;//赋值分页条数

            foreach (var item in query)
            {
                // ReSharper disable once StringIndexOfIsCultureSpecific.1
                if (item.Key.IndexOf(conditionPropertyName) == 0 &&//值得key下标为0 且值不为空
                    item.Value.Count > 0 &&
                    !string.IsNullOrWhiteSpace(item.Value[0]))
                {
                    string propertyName = JsonUtils.PropertyNameToCSharpStyle(item.Key.Replace(conditionPropertyName, string.Empty));//替换为空后转为大写

                    PropertyInfo propertyInfo = typeof(T).GetProperty(propertyName);//反射t的属性 propertyName

                    if (propertyInfo == null)
                        continue;
                      //获取这个属性的类型
                    Type underlyingType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

                    if (underlyingType.Name != typeof(IEnumerable<>).Name &&//判断类型是否为IEnumerable 是否包含IConvertible这个借口 并且参数类型不为string
                        ((!underlyingType.IsValueType && underlyingType != typeof(string)) ||
                        underlyingType.GetInterface(typeof(IConvertible).FullName) == null))
                        continue;

                    try
                    {
                        if (underlyingType.IsEnum)//判断是不是枚举类型
                        {
                            propertyInfo.SetValue(pageQuery.Condition, Enum.Parse(underlyingType, item.Value[0]));//给这个参数设置新的属性值
                        }
                        else if (underlyingType.Name == typeof(IEnumerable<>).Name)//是不是IEnumerable
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
                        else//其他类型
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