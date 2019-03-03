using System.Collections.Generic;
using System.Linq;

namespace Lokad.ILPack.IL
{
    public class ArrayMapper<T>
    {
        private readonly Dictionary<T, int> _dic;

        public ArrayMapper()
        {
            _dic = new Dictionary<T, int>();
        }

        public int Add(T val)
        {
            if (_dic.TryGetValue(val, out var idx))
            {
                return idx;
            }

            var count = _dic.Count;
            _dic.Add(val, count);

            return count;
        }

        public T[] ToArray()
        {
            return _dic.OrderBy(tu => tu.Value).Select(tu => tu.Key).ToArray();
        }
    }
}