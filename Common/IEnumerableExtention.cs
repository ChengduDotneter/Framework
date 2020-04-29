using System;
using System.Collections.Generic;

namespace Common
{
    public static class IEnumerableExtention
    {
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
    }
}
