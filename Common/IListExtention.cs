using System;
using System.Collections.Generic;

namespace Common
{
    public static class IListExtention
    {
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

        public static void AddRange<T>(this IList<T> instance, IEnumerable<T> other)
        {
            if (other != null)
                foreach (T item in other)
                    instance.Add(item);
        }

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
