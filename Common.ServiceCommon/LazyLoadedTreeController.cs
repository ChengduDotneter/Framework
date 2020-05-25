using Common.DAL;
using Common.Model;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Common.ServiceCommon
{
    [Route("lazyloadedtree")]
    [ApiController]
    public class LazyLoadedTreeController : ControllerBase
    {
        private IServiceProvider m_serviceProvider;
        public LazyLoadedTreeController(IServiceProvider serviceProvider) => m_serviceProvider = serviceProvider;

        [HttpGet]
        public object Get(string objectName, string parentID)
        {
            bool queryChildren = !string.IsNullOrWhiteSpace(parentID);

            //根据ObjectName获取实体类型
            Type objectType = GetTypeByObjectName(JsonUtils.PropertyNameToCSharpStyle(objectName));

            //根据获取的实体类型获取该实体类型的LazyLoadedTree特性
            LazyLoadedTreeAttribute lazyLoadedTree = GetLazyLoadedTreeByType(objectType);
            string childForeignKeyPropertyName = lazyLoadedTree.ForeignKeyPropertyName;
            bool isSelf = lazyLoadedTree.ChildModelType == null || lazyLoadedTree.ChildModelType == objectType;

            if (string.IsNullOrWhiteSpace(childForeignKeyPropertyName))
                throw new NullReferenceException();

            if (queryChildren)
            {
                objectType = GetChildType(objectType);
                lazyLoadedTree = GetLazyLoadedTreeByType(objectType);
            }

            //根据获取该实体的查询器类型
            Type queryType = typeof(ISearchQuery<>).MakeGenericType(objectType);

            //根据查询器的类型获取该查询器
            object searchQuery = m_serviceProvider.GetService(queryType);

            string sql = " IsDeleted = 0 ";

            //如果该实体的LazyLoadedTree特性定义外键字段，则向sql中添加外键筛选条件
            if (queryChildren)
            {
                if (isSelf)
                    sql += $" AND {lazyLoadedTree.ForeignKeyPropertyName} = '{parentID}' ";
                else
                    sql += ($" AND {childForeignKeyPropertyName} = '{parentID}' " +
                        (lazyLoadedTree != null && (lazyLoadedTree.ChildModelType == null || lazyLoadedTree.ChildModelType == objectType) ? $" AND {lazyLoadedTree.ForeignKeyPropertyName} IS NULL " : ""));
            }
            else
                sql += isSelf ? $" AND {lazyLoadedTree.ForeignKeyPropertyName} IS NULL " : "";

            //反射执行查询方法
            var objects = queryType.GetMethod("Search", new Type[] { typeof(string), typeof(Dictionary<string, object>), typeof(string), typeof(int), typeof(int) }).
                   Invoke(searchQuery, new object[] { sql, null, null, 0, int.MaxValue });

            //如果该实体Label与Value字段，并且该实体的LazyLoadedTree特性定义Label与Value字段名，则向Label与Value中赋值
            if (objectType.GetProperty("Label") != null &&
                objectType.GetProperty("Value") != null &&
                !string.IsNullOrWhiteSpace(lazyLoadedTree.LabelPropertyName) &&
                !string.IsNullOrWhiteSpace(lazyLoadedTree.ValuePropertyName))
            {
                foreach (object item in (object[])objects)
                {
                    objectType.GetProperty("Label").SetValue(item, objectType.GetProperty($"{lazyLoadedTree.LabelPropertyName}").GetValue(item));
                    objectType.GetProperty("Value").SetValue(item, objectType.GetProperty($"{lazyLoadedTree.ValuePropertyName}").GetValue(item));
                }
            }

            return objects;
        }

        private static Type GetTypeByObjectName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                throw new NullReferenceException();

            Type objectType = TypeReflector.ReflectType(item => item.GetInterface(typeof(IEntity).FullName) != null && item.IsClass && !item.IsAbstract && item.Name == objectName).FirstOrDefault();

            if (objectType == null)
                throw new NullReferenceException();

            return objectType;
        }

        private static Type GetChildType(Type type)
        {
            if (type == null)
                throw new NullReferenceException();

            return GetLazyLoadedTreeByType(type)?.ChildModelType ?? null;
        }

        private static LazyLoadedTreeAttribute GetLazyLoadedTreeByType(Type type)
        {
            if (type == null)
                throw new NullReferenceException();

            return type.GetCustomAttribute<LazyLoadedTreeAttribute>() ?? null;
        }
    }
}
