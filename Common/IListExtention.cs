using System;
using System.Collections.Generic;

namespace Common
{
    /// <summary>
    /// IList扩展类
    /// </summary>
    public static class IListExtention
    {
        /// <summary>
        /// 移除集合中的所有元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="match"></param>
        public static void RemoveAll<T>(this IList<T> instance, Predicate<T> match)
        {
            int index = instance.Count - 1;

            while (index >= 0)
            {
                if (match(instance[index]))
                    instance.RemoveAt(index);

                index--;
            }
        }

        /// <summary>
        /// 向指定集合添加集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="other"></param>
        public static void AddRange<T>(this IList<T> instance, IEnumerable<T> other)
        {
            if (other != null)
                foreach (T item in other)
                    instance.Add(item);
        }

        /// <summary>
        /// 从指定起始位置移除剩余所有元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="startIndex"></param>
        public static void RemoveFrom<T>(this IList<T> instance, int startIndex)
        {
            int index = instance.Count - 1;

            while (index >= startIndex)
            {
                instance.RemoveAt(index);
                index--;
            }
        }
    }
}