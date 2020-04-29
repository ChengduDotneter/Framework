using System;

namespace Common.Validation
{
    public static class CompareHelper
    {
        private static T MakeType<T>(object value)
        {
            if (value == null)
                return default;

            return (T)Convert.ChangeType(value, typeof(T));
        }

        public static int Compare<T>(T a, object b)
        {
            if (a is IComparable comparable)
                return comparable.CompareTo(MakeType<T>(b));

            throw new NotSupportedException($"{typeof(T)}需要继承{nameof(IComparable)}");
        }
    }
}
