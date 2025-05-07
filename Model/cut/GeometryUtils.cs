using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut
{
    public class GeometryUtils
    {
        /// <summary>
        /// 计算水平直线与旋转矩形的交点（线段的起始点在结束点的左侧）
        /// </summary>
        /// <param name="rect">原始矩形</param>
        /// <param name="rotationCenter">旋转中心点</param>
        /// <param name="angleDegrees">旋转角度（度，顺时针为正）</param>
        /// <param name="lineY">水平直线的Y坐标</param>
        /// <returns>如果没有交点或不相交则返回null</returns>
        public static LineSegment? CalculateRectangleIntersectionLine(DataPoint<float> rotationCenter, DataRectangleF rect, float angleDegrees, float lineY)
        {
            List<DataPoint<float>> intersections = new List<DataPoint<float>>();
            // 转换为弧度
            float angleRad = angleDegrees * (float)Math.PI / 180f;
            float cos = (float)Math.Cos(angleRad);
            float sin = (float)Math.Sin(angleRad);
            // 获取旋转后的顶点
            DataPoint<float>[] rotatedVertices = GetRotatedCorners(rect, rotationCenter, angleDegrees);
            
            // 检查每条边
            for (int i = 0; i < 4; i++)
            {
                DataPoint<float> p1 = rotatedVertices[i];
                DataPoint<float> p2 = rotatedVertices[(i + 1) % 4];

                // 检查边是否跨越水平线
                if ((p1.Y <= lineY && p2.Y >= lineY) || (p1.Y >= lineY && p2.Y <= lineY))
                {
                    // 避免除以零（水平边）
                    if (p1.Y == p2.Y) continue;

                    float t = (lineY - p1.Y) / (float)(p2.Y - p1.Y);
                    if (t >= 0 && t <= 1)
                    {
                        float x = p1.X + (int)(t * (p2.X - p1.X));
                        intersections.Add(new DataPoint<float>(x, lineY));
                    }
                }
            }
            // 移除可能的重复点（当水平线通过顶点时）
            List<DataPoint<float>> duplicatePoints = RemoveDuplicatePoints(intersections);
            LineSegment? lineSegment = null;
            if (duplicatePoints.Count == 1)
            {
                lineSegment = new LineSegment(duplicatePoints[0].X, lineY, duplicatePoints[0].X, lineY);
            }
            else if (duplicatePoints.Count == 2)
            {
                lineSegment = new LineSegment(duplicatePoints[0].X, lineY, duplicatePoints[1].X, lineY);
            }
            if (lineSegment != null)
            {
                // 确保线段的起始点在结束点的左侧
                if (lineSegment.StartPoint.X > lineSegment.EndPoint.X)
                {
                    (lineSegment.EndPoint, lineSegment.StartPoint) = (lineSegment.StartPoint, lineSegment.EndPoint);
                }
            }
            return lineSegment;
        }

        private static List<DataPoint<float>> RemoveDuplicatePoints(List<DataPoint<float>> points)
        {
            List<DataPoint<float>> result = new List<DataPoint<float>>();
            foreach (DataPoint<float> p in points)
            {
                if (!result.Exists(pt => pt.X == p.X && pt.Y == p.Y))
                {
                    result.Add(p);
                }
            }
            return result;
        }

        /// <summary>
        /// 计算水平直线与旋转半圆的交点
        /// </summary>
        /// <param name="pivot">旋转中心点</param>
        /// <param name="centerOffset">圆心相对于旋转中心的偏移</param>
        /// <param name="radius">半圆半径</param>
        /// <param name="rotationAngle">顺时针旋转角度（弧度）</param>
        /// <param name="y">水平直线的Y坐标</param>
        /// <returns>交点列表（可能包含0-3个点）</returns>
        public static LineSegment? CalculateSemicircleIntersectionLine(DataPoint<float> pivot, DataPoint<float> centerOffset, float radius, float rotationAngle, float y)
        {
            // 1. 计算旋转后的圆心位置
            DataPoint<float> center = RotatePoint(
                new DataPoint<float>(pivot.X + centerOffset.X, pivot.Y + centerOffset.Y),
                pivot,
                rotationAngle);

            // 2. 计算不考虑旋转时的交点（完整圆）
            List<DataPoint<float>> intersections = new List<DataPoint<float>>();
            float dy = y - center.Y;

            if (Math.Abs(dy) <= radius)
            {
                // 计算x坐标偏移量
                float dx = (float)Math.Sqrt(radius * radius - dy * dy);

                // 不考虑旋转时的两个交点（圆上的点）
                DataPoint<float> p1 = new DataPoint<float>(center.X - dx, y);
                DataPoint<float> p2 = new DataPoint<float>(center.X + dx, y);

                // 3. 检查这些点是否在半圆内（考虑旋转）
                foreach (DataPoint<float> p in new[] { p1, p2 })
                {
                    if (IsPointInRotatedSemicircle(p, center, pivot, centerOffset, radius, rotationAngle))
                    {
                        intersections.Add(p);
                    }
                }
            }

            // 4. 检查直线与直径的交点
            // 直径的初始位置是从(center.X - radius, center.Y)到(center.X + radius, center.Y)
            DataPoint<float> diameterStart = new DataPoint<float>(center.X - radius, center.Y);
            DataPoint<float> diameterEnd = new DataPoint<float>(center.X + radius, center.Y);

            // 旋转直径端点（圆心已经旋转，直径需要相对于圆心旋转）
            DataPoint<float> rotatedStart = RotatePointAroundCenter(diameterStart, center, rotationAngle);
            DataPoint<float> rotatedEnd = RotatePointAroundCenter(diameterEnd, center, rotationAngle);

            // 计算直线与直径的交点
            if (Math.Abs(rotatedStart.Y - rotatedEnd.Y) > float.Epsilon)
            {
                // 直径不再水平，计算交点
                float t = (y - rotatedStart.Y) / (rotatedEnd.Y - rotatedStart.Y);
                if (t >= 0 && t <= 1)
                {
                    float x = rotatedStart.X + t * (rotatedEnd.X - rotatedStart.X);
                    DataPoint<float> diameterIntersection = new DataPoint<float>(x, y);

                    // 检查交点是否在直径线段上
                    if (IsPointOnDiameter(diameterIntersection, rotatedStart, rotatedEnd))
                    {
                        intersections.Add(diameterIntersection);
                    }
                }
            }
            else if (Math.Abs(rotatedStart.Y - y) < float.Epsilon)
            {
                // 直径与直线重合，添加两个端点
                intersections.Add(rotatedStart);
                intersections.Add(rotatedEnd);
            }

            // 5. 去重（考虑浮点误差）
            List<DataPoint<float>> points = RemoveDuplicatePoints(intersections, 0.0001f);

            if (points.Count == 0)
            {
                // 如果没有交点，则返回null
                return null;
            }
            else if (points.Count == 1)
            {
                // 如果只有一个交点，则返回null
                return new LineSegment(points[0], points[0]);
            }
            else if (points.Count == 2)
            {
                // 如果有两个交点，则返回这两个交点
                // 确保起始点的x坐标小于结束点的x坐标
                DataPoint<float> p1 = points[0];
                DataPoint<float> p2 = points[1];
                if (p1.X < p2.X)
                {
                    return new LineSegment(p1, p2);
                }
                return new LineSegment(p2, p1);
            }
            else
            {
                // 如果有三个交点，则返回同一水平线的两个交点
                var tuple = GetSameHorizontalLine(points[0], points[1], points[2]);
                // 确保起始点的x坐标小于结束点的x坐标
                DataPoint<float> p1 = tuple.Item1;
                DataPoint<float> p2 = tuple.Item2;
                if (p1.X < p2.X)
                {
                    return new LineSegment(p1, p2);
                }
                return new LineSegment(p2, p1);
            }
        }

        /// <summary>
        /// 点绕圆心旋转（顺时针）
        /// </summary>
        private static DataPoint<float> RotatePointAroundCenter(DataPoint<float> point, DataPoint<float> center, float angle)
        {
            // 转换为弧度
            float theta = angle * MathF.PI / 180f;
            float x = point.X - center.X;
            float y = point.Y - center.Y;

            // 顺时针旋转矩阵
            float newX = x * (float)Math.Cos(theta) + y * (float)Math.Sin(theta);
            float newY = -x * (float)Math.Sin(theta) + y * (float)Math.Cos(theta);

            return new DataPoint<float>(newX + center.X, newY + center.Y);
        }

        /// <summary>
        /// 检查点是否在旋转后的半圆内
        /// </summary>
        private static bool IsPointInRotatedSemicircle(
            DataPoint<float> point, DataPoint<float> center, DataPoint<float> pivot,
            DataPoint<float> centerOffset, float radius, float rotationAngle)
        {
            // 转换为弧度
            float theta = rotationAngle * MathF.PI / 180f;
            // 将点转换到以圆心为原点的坐标系
            float localX = point.X - center.X;
            float localY = point.Y - center.Y;

            // 应用逆时针旋转（相当于将半圆转回初始位置）
            float unrotatedX = localX * (float)Math.Cos(theta) + localY * (float)Math.Sin(theta);
            float unrotatedY = -localX * (float)Math.Sin(theta) + localY * (float)Math.Cos(theta);

            // 在半圆的局部坐标系中，初始半圆是下半圆，所以需要 unrotatedY >= 0
            return unrotatedY >= 0;
        }

        /// <summary>
        /// 检查点是否在直径线段上
        /// </summary>
        private static bool IsPointOnDiameter(DataPoint<float> point, DataPoint<float> start, DataPoint<float> end)
        {
            // 检查点是否在线段的包围盒内
            float minX = Math.Min(start.X, end.X);
            float maxX = Math.Max(start.X, end.X);
            float minY = Math.Min(start.Y, end.Y);
            float maxY = Math.Max(start.Y, end.Y);

            return point.X >= minX && point.X <= maxX &&
                   point.Y >= minY && point.Y <= maxY;
        }

        /// <summary>
        /// 去除重复点（考虑浮点误差）
        /// </summary>
        private static List<DataPoint<float>> RemoveDuplicatePoints(List<DataPoint<float>> points, float epsilon)
        {
            List<DataPoint<float>> result = new List<DataPoint<float>>();

            foreach (DataPoint<float> p in points)
            {
                bool isDuplicate = false;
                foreach (DataPoint<float> existing in result)
                {
                    if (Math.Abs(p.X - existing.X) < epsilon &&
                        Math.Abs(p.Y - existing.Y) < epsilon)
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (!isDuplicate)
                {
                    result.Add(p);
                }
            }

            return result;
        }

        public static Tuple<DataPoint<float>, DataPoint<float>> GetSameHorizontalLine(DataPoint<float> point1, DataPoint<float> point2, DataPoint<float> point3)
        {
            // 计算两两之间的绝对差值
            float diffXY = Math.Abs(point1.Y - point2.Y);
            float diffXZ = Math.Abs(point1.Y - point3.Y);
            float diffYZ = Math.Abs(point2.Y - point3.Y);

            // 找出最小的差值
            float minDiff = Math.Min(diffXY, Math.Min(diffXZ, diffYZ));

            // 返回差值最小的两个数
            if (minDiff == diffXY)
                return Tuple.Create(point1, point2);
            else if (minDiff == diffXZ)
                return Tuple.Create(point1, point3);
            else
                return Tuple.Create(point2, point3);
        }

        public static DataPoint<float> RotatePoint(DataPoint<float> point, DataPoint<float> center, float rotationAngle)
        {
            // 转换为弧度
            float theta = rotationAngle * MathF.PI / 180f;
            // 平移至原点
            float x = point.X - center.X;
            float y = point.Y - center.Y;

            // 旋转
            float newX = x * MathF.Cos(theta) - y * MathF.Sin(theta);
            float newY = x * MathF.Sin(theta) + y * MathF.Cos(theta);

            // 平移回原坐标系
            return new DataPoint<float>(newX + center.X, newY + center.Y);
        }

        /// <summary>
        /// 计算旋转半圆的最下方水平切线Y坐标
        /// </summary>
        /// <param name="pivot">旋转中心点</param>
        /// <param name="center">半圆圆心</param>
        /// <param name="radius">半径</param>
        /// <param name="angleDegrees">旋转角度(度，逆时针为正)</param>
        /// <returns>最下方水平切线的Y坐标</returns>
        public static float FindBottomTangentY(DataPoint<float> pivot, DataPoint<float> center, float radius, float rotationAngle)
        {
            // 计算旋转后的圆心
            DataPoint<float> rotatedCenter = RotatePoint(center, pivot, rotationAngle);

            // 计算旋转后的直径端点
            DataPoint<float> diameterStart = new DataPoint<float>(center.X - radius, center.Y);
            DataPoint<float> diameterEnd = new DataPoint<float>(center.X + radius, center.Y);

            DataPoint<float> rotatedStart = RotatePoint(diameterStart, pivot, rotationAngle);
            DataPoint<float> rotatedEnd = RotatePoint(diameterEnd, pivot, rotationAngle);

            // 计算圆弧最低点
            float arcBottomY = rotatedCenter.Y + radius;

            // 找出所有点中的最大Y值（Y轴向下为正方向）
            float minY = Math.Max(rotatedStart.Y, rotatedEnd.Y);
            minY = Math.Max(minY, arcBottomY);

            return minY;
        }

        /// <summary>
        /// 计算旋转矩形的最下方水平切线Y坐标
        /// </summary>
        /// <param name="pivot">旋转中心点</param>
        /// <param name="rect">原始矩形区域</param>
        /// <param name="angleDegrees">旋转角度(度，逆时针为正)</param>
        /// <returns>最下方水平切线的Y坐标</returns>
        public static float FindBottomTangentY(DataPoint<float> pivot, DataRectangleF rect, float angleDegrees)
        {
            // 获取旋转后的四个顶点
            DataPoint<float>[] rotatedCorners = GetRotatedCorners(rect, pivot, angleDegrees);

            // 找出所有顶点中的最大Y值（Y轴向下为正）
            float maxY = rotatedCorners.Max(p => p.Y);

            return maxY;
        }

        /// <summary>
        /// 获取旋转后的矩形四个顶点
        /// </summary>
        private static DataPoint<float>[] GetRotatedCorners(DataRectangleF rect, DataPoint<float> pivot, float angleDegrees)
        {
            // 转换为弧度
            float angleRad = angleDegrees * (float)(Math.PI / 180);
            float cos = (float)Math.Cos(angleRad);
            float sin = (float)Math.Sin(angleRad);

            // 原始四个顶点
            DataPoint<float>[] corners = new DataPoint<float>[]
            {
            new DataPoint<float>(rect.Left, rect.Top),     // 左上
            new DataPoint<float>(rect.Right, rect.Top),    // 右上
            new DataPoint<float>(rect.Right, rect.Bottom), // 右下
            new DataPoint<float>(rect.Left, rect.Bottom)   // 左下
            };

            // 旋转每个顶点
            for (int i = 0; i < 4; i++)
            {
                // 转换为相对于旋转中心的坐标
                float x = corners[i].X - pivot.X;
                float y = corners[i].Y - pivot.Y;

                // 应用旋转
                float rotatedX = x * cos - y * sin;
                float rotatedY = x * sin + y * cos;

                // 转换回绝对坐标
                corners[i] = new DataPoint<float>(rotatedX + pivot.X, rotatedY + pivot.Y);
            }

            return corners;
        }
    }
}
