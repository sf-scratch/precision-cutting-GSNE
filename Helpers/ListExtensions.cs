using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Helpers
{
    public static class ListExtensions
    {
        public static bool TryGetMiddleElement<T>(this List<T> list, out T result)
        {
            if (list == null || list.Count == 0)
            {
                result = default;
                return false;
            }
            result = list[list.Count / 2];
            return true;
        }
    }
}
