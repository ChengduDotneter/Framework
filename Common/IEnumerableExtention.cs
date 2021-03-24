using System;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    /// <summary>
    /// IEnumerable扩展类
    /// </summary>
    public static class IEnumerableExtention
    {
        /// <summary>
        /// 移除所有元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="match"></param>
        public static void RemoveAll<T>(this IEnumerable<T> instance, Predicate<T> match)
        {
            var list = instance as List<T>;

            int index = list.Count - 1;

            while (index >= 0)
            {
                if (match(list[index]))
                    list.RemoveAt(index);

                index--;
            }
        }

        /// <summary>
        /// 判断数据是否为空
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> instance)
        {
            return instance == null || instance.Count() == 0;
        }

        /// <summary>
        /// 添加元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="parameter"></param>
        public static void Add<T>(this IEnumerable<T> instance, T parameter)
        {
            var list = instance as List<T>;

            list.Add(parameter);
        }
    }
}