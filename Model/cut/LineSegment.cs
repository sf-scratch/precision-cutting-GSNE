using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut
{
    /// <summary>
    /// 表示二维空间中的线段
    /// </summary>
    public class LineSegment
    {
        /// <summary>
        /// 线段起点
        /// </summary>
        public DataPoint<float> StartPoint { get; set; }

        /// <summary>
        /// 线段终点
        /// </summary>
        public DataPoint<float> EndPoint { get; set; }

        /// <summary>
        /// 使用起点和终点初始化线段
        /// </summary>
        public LineSegment(DataPoint<float> start, DataPoint<float> end)
        {
            StartPoint = start;
            EndPoint = end;
        }

        /// <summary>
        /// 使用坐标值初始化线段
        /// </summary>
        public LineSegment(float startX, float startY, float endX, float endY)
        {
            StartPoint = new DataPoint<float>(startX, startY);
            EndPoint = new DataPoint<float>(endX, endY);
        }

        public void ExchangePointsStartAndEnd()
        {
            var temp = StartPoint;
            StartPoint = EndPoint;
            EndPoint = temp;
        }

        /// <summary>
        /// 返回线段的字符串表示
        /// </summary>
        public override string ToString()
        {
            return $"Line from {StartPoint} to {EndPoint}";
        }
    }

}
