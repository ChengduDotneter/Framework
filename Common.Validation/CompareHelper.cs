using System;

namespace Common.Validation
{
    /// <summary>
    /// 比较帮助类
    /// </summary>
    public static class CompareHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
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
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int Compare<T>(T a, object b)
        {
            if (a is IComparable comparable)
                return comparable.CompareTo(MakeType<T>(b));

            throw new NotSupportedException($"{typeof(T)}需要继承{nameof(IComparable)}");
        }
    }
}
