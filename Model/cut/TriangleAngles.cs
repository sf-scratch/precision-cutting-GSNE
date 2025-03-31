using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Utils
{
    internal class TriangleAngles
    {
        // 定义一个方法来计算两点之间的距离
        private static double Distance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        // 定义一个方法来计算夹角
        private static double CalculateAngle(double a, double b, double c)
        {
            return Math.Acos((b * b + c * c - a * a) / (2 * b * c)) * (180.0 / Math.PI);
        }

        public static double GetTriangleAngles(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            // 计算三条边的长度
            double a = Distance(x2, y2, x3, y3);
            double b = Distance(x1, y1, x3, y3);
            double c = Distance(x1, y1, x2, y2);

            // 计算三个角的角度
            double angleA = CalculateAngle(a, b, c);
            return Math.Round(angleA, 5);
            //double angleB = CalculateAngle(b, a, c);
            //double angleC = CalculateAngle(c, a, b);

          
        }

        // 计算V槽深度的方法
        public static double CalculateDepth(double angle, double width)
        {
            // 将角度转为弧度
            double angleInRadians = (angle / 2) * (Math.PI / 180.0);

            // 计算深度
            double depth = (width / 2) * Math.Tan(angleInRadians);

            return depth;
        }

        /// <summary>
        /// 根据A点的坐标、角度和B点的y坐标，计算b点的坐标
        /// </summary>
        /// <param name="zA"></param>
        /// <param name="yA"></param>
        /// <param name="yB"></param>
        /// <param name="theta"></param>
        /// <returns></returns>
        public static double CalculateBXCoordinate(double zA, double yA, double yB, double theta)
        {
            // 计算 y 方向的变化量 delta_y
            double deltaY = yB - yA;

            // 将角度转换为弧度，并计算 z 方向的变化量 delta_x
            double thetaRad = DegreesToRadians(theta);
            double deltaZ = deltaY / Math.Tan(thetaRad);

            // 计算 B 点的 z 坐标
            return zA + deltaZ;
        }

        static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }


        /// <summary>
        /// 根据A点和B点的坐标加角度，算出C点的坐标，是等腰三角形
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static double[] CalculatePointC(double[] A, double[] B, double angle)
        {
            // 中点M
            double[] M = { (A[0] + B[0]) / 2, (A[1] + B[1]) / 2 };

            // 根据角度计算C的高度
            double height = (Distance(A, B) / 2) * Math.Tan(DegreeToRadian(angle));

            // C点的坐标
            return new double[] { M[0], M[1] + height };
        }

        static double Distance(double[] A, double[] B)
        {
            return Math.Sqrt(Math.Pow(B[0] - A[0], 2) + Math.Pow(B[1] - A[1], 2));
        }

        static double DegreeToRadian(double degree)
        {
            return degree * (Math.PI / 180);
        }

        /// <summary>
        /// 算点绕圆心旋转后的坐标
        /// </summary>
        /// <param name="Ax"></param>
        /// <param name="Ay"></param>
        /// <param name="Cx"></param>
        /// <param name="Cy"></param>
        /// <param name="angleDeg"></param>
        /// <returns></returns>
        public static Tuple<double, double> RotatePoint(double Ax, double Ay, double Cx, double Cy, double angleDeg)
        {
            // 将角度转换为弧度
            double angleRad = angleDeg * Math.PI / 180.0;

            // 计算旋转后的新坐标
            double AxNew = Cx + (Ax - Cx) * Math.Cos(angleRad) - (Ay - Cy) * Math.Sin(angleRad);
            double AyNew = Cy + (Ax - Cx) * Math.Sin(angleRad) + (Ay - Cy) * Math.Cos(angleRad);

            return Tuple.Create(AxNew, AyNew);
        }

        public struct Point
        {
            public double X;
            public double Y;

            public Point(double x, double y)
            {
                X = x;
                Y = y;
            }
        }

        // 将点转换为极坐标
        public static (double r, double theta) ToPolar(double x, double y)
        {
            double r = Math.Sqrt(x * x + y * y);
            double theta = Math.Atan2(y, x);
            return (r, theta);
        }

        // 将极坐标转换回笛卡尔坐标
        public static Point ToCartesian(double r, double theta)
        {
            double x = r * Math.Cos(theta);
            double y = r * Math.Sin(theta);
            return new Point(x, y);
        }

        // 公共方法：旋转直线并返回旋转后的新端点
        public static (Point A_rotated, Point B_rotated) RotateLine(Point A, Point B, double angleDegrees, Point center)
        {
            // 角度转换为弧度
            double thetaRotation = angleDegrees * (Math.PI / 180);

            // 平移直线到圆心 (将圆心变为原点)
            Point A_translated = new Point(A.X - center.X, A.Y - center.Y);
            Point B_translated = new Point(B.X - center.X, B.Y - center.Y);

            // 获取初始端点的极坐标
            var (r1, theta1) = ToPolar(A_translated.X, A_translated.Y);
            var (r2, theta2) = ToPolar(B_translated.X, B_translated.Y);

            // 应用旋转
            double theta1Rotated = theta1 + thetaRotation;
            double theta2Rotated = theta2 + thetaRotation;

            // 将旋转后的极坐标转换回笛卡尔坐标
            Point A_rotated = ToCartesian(r1, theta1Rotated);
            Point B_rotated = ToCartesian(r2, theta2Rotated);

            // 将旋转后的点平移回原始坐标系
            A_rotated = new Point(A_rotated.X + center.X, A_rotated.Y + center.Y);
            B_rotated = new Point(B_rotated.X + center.X, B_rotated.Y + center.Y);

            return (A_rotated, B_rotated);
        }

        private const double ReferenceThickness = 1.503; // mm
        private const double ReferenceFocusPosition = 36.25; // mm

        public static double CalculateFocusPosition(double thickness)
        {
            return (thickness * ReferenceFocusPosition) / ReferenceThickness;
        }

    }
}
