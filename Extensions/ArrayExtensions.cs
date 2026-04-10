using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Extensions
{
    public static class ArrayExtensions
    {
        public enum TrendType
        {
            Increasing,      // 递增
            Decreasing,      // 递减
            Constant,        // 恒定
            Unsorted         // 无序
        }

        public static TrendType GetTrend<T>(this T[] array) where T : IComparable<T>
        {
            if (array == null || array.Length <= 1)
                return TrendType.Constant;

            bool? isIncreasing = null;

            for (int i = 1; i < array.Length; i++)
            {
                int cmp = array[i].CompareTo(array[i - 1]);

                if (cmp > 0)
                {
                    if (isIncreasing == false) return TrendType.Unsorted;
                    isIncreasing = true;
                }
                else if (cmp < 0)
                {
                    if (isIncreasing == true) return TrendType.Unsorted;
                    isIncreasing = false;
                }
                // 相等时继续，不影响判断
            }

            return isIncreasing switch
            {
                true => TrendType.Increasing,
                false => TrendType.Decreasing,
                null => TrendType.Constant
            };
        }
    }
}