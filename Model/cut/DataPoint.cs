using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut
{
    /// <summary>
    /// 表示一个二维数据点
    /// </summary>
    /// <typeparam name="T">坐标值的类型</typeparam>
    public class DataPoint<T> where T : struct
    {
        /// <summary>
        /// X 轴坐标值
        /// </summary>
        public T X { get; set; }

        /// <summary>
        /// Y 轴坐标值
        /// </summary>
        public T Y { get; set; }

        /// <summary>
        /// 初始化一个数据点
        /// </summary>
        public DataPoint() { }

        /// <summary>
        /// 使用指定坐标初始化数据点
        /// </summary>
        /// <param name="x">X 轴坐标</param>
        /// <param name="y">Y 轴坐标</param>
        public DataPoint(T x, T y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// 返回点的字符串表示形式
        /// </summary>
        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
