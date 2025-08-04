using System;
using System.Collections.Generic;

namespace 精密切割系统.Helpers
{
    public class ThetaRotationCenterCalculator
    {
        // 容差值，用于处理浮点数精度问题
        public const double Tolerance = 1e-3;

        /// <summary>
        /// 根据直线的角度（度数）和直线上的一点，返回一般式方程的系数 (A, B, C)
        /// </summary>
        /// <param name="angleDegrees">直线与x轴的夹角（度数，0~360）</param>
        /// <param name="x0">直线上已知点的x坐标</param>
        /// <param name="y0">直线上已知点的y坐标</param>
        /// <returns>(A: x系数, B: y系数, C: 常数项)</returns>
        public static (double A, double B, double C) GetLineCoefficients(double angleDegrees, double x0, double y0)
        {
            // 规范化角度到 [0, 360)
            double normalizedAngle = angleDegrees % 360;
            if (normalizedAngle < 0) normalizedAngle += 360;

            // 处理垂直直线 (x = x0)
            if (normalizedAngle % 180 == 90)
            {
                return (A: 1, B: 0, C: -x0);
            }

            // 处理水平直线 (y = y0)
            if (normalizedAngle % 180 == 0)
            {
                return (A: 0, B: 1, C: -y0);
            }

            // 其他情况：计算斜率和截距
            double angleRadians = normalizedAngle * Math.PI / 180;
            double k = Math.Tan(angleRadians);
            double b = y0 - k * x0;

            // 转换为一般式: kx - y + b = 0 ⇒ A=k, B=-1, C=b
            return (A: k, B: -1, C: b);
        }


        /// <summary>
        /// 计算两条直线之间的旋转中心
        /// </summary>
        /// <param name="a1">直线a的x系数</param>
        /// <param name="b1">直线a的y系数</param>
        /// <param name="c1">直线a的常数项</param>
        /// <param name="a2">直线b的x系数</param>
        /// <param name="b2">直线b的y系数</param>
        /// <param name="c2">直线b的常数项</param>
        /// <param name="thetaDegrees">旋转角度（度）</param>
        /// <returns>旋转中心列表（可能为空或多个解）</returns>
        public static List<PointD> FindRotationCenter(
            double a1, double b1, double c1,
            double a2, double b2, double c2,
            double thetaDegrees)
        {
            // 处理零角度旋转的特殊情况
            if (Math.Abs(thetaDegrees) < Tolerance || Math.Abs(thetaDegrees - 360) < Tolerance)
            {
                return HandleZeroRotation(a1, b1, c1, a2, b2, c2);
            }

            // 处理180度旋转的特殊情况
            if (Math.Abs(thetaDegrees - 180) < Tolerance)
            {
                return Handle180Rotation(a1, b1, c1, a2, b2, c2);
            }

            // 转换为弧度
            double theta = thetaDegrees * Math.PI / 180.0;
            double cosTheta = Math.Cos(theta);
            double sinTheta = Math.Sin(theta);

            // 计算比例系数k
            double k;
            if (Math.Abs(a1) > Tolerance)
            {
                k = (a2 * cosTheta + b2 * sinTheta) / a1;
            }
            else if (Math.Abs(b1) > Tolerance)
            {
                k = (-a2 * sinTheta + b2 * cosTheta) / b1;
            }
            else
            {
                throw new ArgumentException("直线a退化（系数全为零）");
            }

            // 构建第一个约束方程
            double E1 = a2 * (1 - cosTheta) - b2 * sinTheta;
            double F1 = a2 * sinTheta + b2 * (1 - cosTheta);
            double G1 = k * c1 - c2;

            // 计算直线长度的比例因子
            double norm1 = Math.Sqrt(a1 * a1 + b1 * b1);
            double norm2 = Math.Sqrt(a2 * a2 + b2 * b2);
            double K = norm1 / norm2;

            List<PointD> solutions = new List<PointD>();

            // 处理两个分支（距离相等约束）
            // 分支1: d1 = K * d2
            double E2 = a1 - K * a2;
            double F2 = b1 - K * b2;
            double G2 = K * c2 - c1;
            SolveLinearSystem(E1, F1, G1, E2, F2, G2, solutions, a1, b1, c1, a2, b2, c2, theta);

            // 分支2: d1 = -K * d2
            double E3 = a1 + K * a2;
            double F3 = b1 + K * b2;
            double G3 = -K * c2 - c1;
            SolveLinearSystem(E1, F1, G1, E3, F3, G3, solutions, a1, b1, c1, a2, b2, c2, theta);

            return solutions;
        }

        /// <summary>
        /// 解决线性系统并验证解
        /// </summary>
        private static void SolveLinearSystem(
            double E1, double F1, double G1,
            double E2, double F2, double G2,
            List<PointD> solutions,
            double a1, double b1, double c1,
            double a2, double b2, double c2,
            double theta)
        {
            try
            {
                // 求解线性方程组
                double det = E1 * F2 - E2 * F1;

                // 处理行列式为零的情况
                if (Math.Abs(det) < Tolerance)
                {
                    // 检查是否无穷解
                    if (Math.Abs(E1 * G2 - E2 * G1) < Tolerance &&
                        Math.Abs(F1 * G2 - F2 * G1) < Tolerance)
                    {
                        // 无穷解的情况（例如90度旋转垂直线）
                        HandleInfiniteSolutions(solutions, E1, F1, G1);
                    }
                    return;
                }

                double x0 = (F2 * G1 - F1 * G2) / det;
                double y0 = (E1 * G2 - E2 * G1) / det;
                var point = new PointD(x0, y0);

                // 验证解
                if (ValidateSolution(point, a1, b1, c1, a2, b2, c2, theta) &&
                    !solutions.Contains(point))
                {
                    solutions.Add(point);
                }
            }
            catch
            {
                // 忽略数值错误
            }
        }

        /// <summary>
        /// 验证旋转中心是否有效
        /// </summary>
        private static bool ValidateSolution(
            PointD center,
            double a1, double b1, double c1,
            double a2, double b2, double c2,
            double theta)
        {
            // 在直线a上找一个点
            PointD pointOnA;
            if (Math.Abs(b1) > Tolerance)
            {
                pointOnA = new PointD(0, -c1 / b1); // x=0时的点
            }
            else if (Math.Abs(a1) > Tolerance)
            {
                pointOnA = new PointD(-c1 / a1, 0); // y=0时的点
            }
            else
            {
                return false; // 无效直线
            }

            // 将点绕中心旋转
            PointD rotatedPoint = RotatePoint(pointOnA, center, theta);

            // 检查旋转后的点是否在直线b上
            double distance = Math.Abs(a2 * rotatedPoint.X + b2 * rotatedPoint.Y + c2) /
                             Math.Sqrt(a2 * a2 + b2 * b2);

            return distance < Tolerance;
        }

        /// <summary>
        /// 将点绕中心旋转指定角度
        /// </summary>
        private static PointD RotatePoint(PointD point, PointD center, double thetaRadians)
        {
            double dx = point.X - center.X;
            double dy = point.Y - center.Y;

            double cosTheta = Math.Cos(thetaRadians);
            double sinTheta = Math.Sin(thetaRadians);

            double xRot = center.X + dx * cosTheta - dy * sinTheta;
            double yRot = center.Y + dx * sinTheta + dy * cosTheta;

            return new PointD(xRot, yRot);
        }

        /// <summary>
        /// 处理零角度旋转的情况
        /// </summary>
        private static List<PointD> HandleZeroRotation(
            double a1, double b1, double c1,
            double a2, double b2, double c2)
        {
            List<PointD> solutions = new List<PointD>();

            // 检查直线是否相同
            double ratio = 0;
            if (Math.Abs(a1) > Tolerance)
            {
                ratio = a2 / a1;
            }
            else if (Math.Abs(b1) > Tolerance)
            {
                ratio = b2 / b1;
            }
            else if (Math.Abs(c1) > Tolerance)
            {
                ratio = c2 / c1;
            }

            // 如果直线相同，则任意点都可以是旋转中心
            if (Math.Abs(a2 - ratio * a1) < Tolerance &&
                Math.Abs(b2 - ratio * b1) < Tolerance &&
                Math.Abs(c2 - ratio * c1) < Tolerance)
            {
                // 返回一个特殊点表示无穷解
                solutions.Add(new PointD(double.PositiveInfinity, double.PositiveInfinity));
            }

            return solutions;
        }

        /// <summary>
        /// 处理180度旋转的情况
        /// </summary>
        private static List<PointD> Handle180Rotation(
            double a1, double b1, double c1,
            double a2, double b2, double c2)
        {
            List<PointD> solutions = new List<PointD>();

            // 检查直线是否相同
            double ratio = 0;
            if (Math.Abs(a1) > Tolerance)
            {
                ratio = a2 / a1;
            }
            else if (Math.Abs(b1) > Tolerance)
            {
                ratio = b2 / b1;
            }
            else if (Math.Abs(c1) > Tolerance)
            {
                ratio = c2 / c1;
            }

            if (Math.Abs(a2 - ratio * a1) < Tolerance &&
                Math.Abs(b2 - ratio * b1) < Tolerance &&
                Math.Abs(c2 - ratio * c1) < Tolerance)
            {
                // 对于180度旋转，所有点都是旋转中心
                solutions.Add(new PointD(double.PositiveInfinity, double.PositiveInfinity));
            }
            else
            {
                // 当直线不同时，旋转中心在它们的对称轴上
                // 构建中点方程：(a1+a2)x + (b1+b2)y + (c1+c2) = 0
                double A = a1 + a2;
                double B = b1 + b2;
                double C = c1 + c2;

                // 如果对称轴是直线，返回线上一点
                if (Math.Abs(A) > Tolerance || Math.Abs(B) > Tolerance)
                {
                    solutions.Add(new PointD(-C / A, 0));
                }
            }

            return solutions;
        }

        /// <summary>
        /// 处理无穷解的情况
        /// </summary>
        private static void HandleInfiniteSolutions(
            List<PointD> solutions,
            double E1, double F1, double G1)
        {
            // 如果系数不全为零，添加直线上的一点
            if (Math.Abs(E1) > Tolerance || Math.Abs(F1) > Tolerance)
            {
                double x0, y0;
                if (Math.Abs(F1) > Tolerance)
                {
                    x0 = 0;
                    y0 = -G1 / F1;
                }
                else
                {
                    x0 = -G1 / E1;
                    y0 = 0;
                }
                solutions.Add(new PointD(x0, y0));
            }
        }
    }

    /// <summary>
    /// 表示二维点的简单结构
    /// </summary>
    public struct PointD : IEquatable<PointD>
    {
        public double X { get; }
        public double Y { get; }

        public PointD(double x, double y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(PointD other)
        {
            return Math.Abs(X - other.X) < ThetaRotationCenterCalculator.Tolerance &&
                   Math.Abs(Y - other.Y) < ThetaRotationCenterCalculator.Tolerance;
        }

        public override string ToString()
        {
            if (double.IsInfinity(X) && double.IsInfinity(Y))
                return "Infinite solutions";

            return $"({X:F4}, {Y:F4})";
        }
    }
}
