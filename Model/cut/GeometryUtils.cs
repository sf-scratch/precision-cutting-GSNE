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
        /// 计算水平直线与旋转半圆的交点（线段的起始点在结束点的左侧）
        /// </summary>
        /// <param name="center">圆心坐标</param>
        /// <param name="radius">半径</param>
        /// <param name="rotationAngle">旋转角度（度）</param>
        /// <param name="horizontalLineY">水平直线y坐标</param>
        /// <returns>两个交点，如果没有交点或不相交则返回null</returns>
        public static LineSegment? CalculateSemicircleIntersectionLine(DataPoint<float> center, float radius, float rotationAngle, float horizontalLineY)
        {
            // 转换为弧度
            float theta = rotationAngle * MathF.PI / 180f;
            var points = new List<DataPoint<float>>();
            float cTranslated = horizontalLineY - center.Y; // 平移后的水平直线 y' = c - y0

            // 预计算三角函数
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);

            // (1) 计算旋转后的半圆部分交点
            float term1 = 4 * cTranslated * cTranslated * sinTheta * sinTheta;
            float term2 = 4 * (cTranslated * cTranslated - radius * radius * cosTheta * cosTheta);
            float delta = term1 - term2;

            if (delta >= 0)
            {
                float sqrtDelta = MathF.Sqrt(delta);
                float x1 = (2 * cTranslated * sinTheta + sqrtDelta) / 2;
                float x2 = (2 * cTranslated * sinTheta - sqrtDelta) / 2;

                foreach (float x in new[] { x1, x2 })
                {
                    if (MathF.Abs(x) <= radius)
                    {
                        float y = MathF.Sqrt(radius * radius - x * x); // 下半圆 y ≥ 0
                                                                       // 反向旋转并平移回原坐标系
                        float xRotated = center.X + x * cosTheta - y * sinTheta;
                        float yRotated = center.Y + x * sinTheta + y * cosTheta;
                        points.Add(new DataPoint<float>(xRotated, yRotated));
                    }
                }
            }

            // (2) 计算旋转后的直径部分交点
            if (MathF.Abs(sinTheta) > 1e-6f)
            {
                float xDiameter = cTranslated / sinTheta;
                if (MathF.Abs(xDiameter) <= radius)
                {
                    float xRotated = center.X + xDiameter * cosTheta;
                    float yRotated = center.Y + xDiameter * sinTheta;
                    points.Add(new DataPoint<float>(xRotated, yRotated));
                }
            }
            else if (MathF.Abs(cTranslated) < 1e-6f) // 水平直线与直径重合
            {
                points.Add(new DataPoint<float>(
                    center.X + radius * cosTheta,
                    center.Y + radius * sinTheta
                ));
                points.Add(new DataPoint<float>(
                    center.X - radius * cosTheta,
                    center.Y - radius * sinTheta
                ));
            }

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
