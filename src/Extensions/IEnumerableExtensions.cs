using System.Linq;

namespace System.Collections.Generic
{
    internal static class IEnumerableExtensions
    {
        // Reference: https://stackoverflow.com/a/24058279
        public static IEnumerable<T> TopologicalSort<T>(this IEnumerable<T> nodes, Func<T, IEnumerable<T>> connected)
        {
            var elems = nodes.ToDictionary(node => node,
                node => new HashSet<T>(connected(node)));
            while (elems.Count > 0)
            {
                var elem = elems.FirstOrDefault(x => x.Value.Count == 0);
                if (elem.Key == null)
                {
                    throw new ArgumentException("Cyclic connections are not allowed");
                }

                elems.Remove(elem.Key);
                foreach (var kvp in elems)
                {
                    kvp.Value.Remove(elem.Key);
                }

                yield return elem.Key;
            }
        }
    }
}