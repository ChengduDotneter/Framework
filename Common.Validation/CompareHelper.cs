using System;

namespace Common.Validation
{
    /// <summary>
    /// 对象值比较Helper类
    /// </summary>
    public static class CompareHelper
    {
        /// <summary>
        /// 将参数转换为指定泛型
        /// </summary>
        /// <typeparam name="T">指定泛型</typeparam>
        /// <param name="value">参数值</param>
        /// <returns></returns>
        private static T MakeType<T>(object value)
        {
            if (value == null)
                return default;

            return (T)Convert.ChangeType(value, typeof(T));
        }

        /// <summary>
        /// 比较ab参数是否相等
        /// </summary>
        /// <typeparam name="T">指定泛型</typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>1：a大于b, 0:a等于b, -1:a小于b </returns>
        public static int Compare<T>(T a, object b)
        {
            if (a is IComparable comparable)
                return comparable.CompareTo(MakeType<T>(b));

            throw new NotSupportedException($"{typeof(T)}需要继承{nameof(IComparable)}");
        }
    }
}
