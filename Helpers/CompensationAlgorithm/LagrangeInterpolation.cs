using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace 精密切割系统.Helpers.CompensationAlgorithm
{
    public static class LagrangeInterpolation
    {
        /// <summary>
        /// 拉格朗日插值（严格通过所有已知点）
        /// </summary>
        /// <param name="x">已知位置数据</param>
        /// <param name="y">已知误差数据</param>
        /// <param name="targetX">要插值的 x 值</param>
        /// <returns>插值结果（误差值）</returns>
        public static float Interpolate(float[] x, float[] y, float targetX)
        {
            if (x.Length != y.Length)
                throw new ArgumentException("x 和 y 数组长度必须相等");
            if (x.Length == 0)
                return 0;

            float result = 0;
            int n = x.Length;

            for (int i = 0; i < n; i++)
            {
                float term = y[i];
                for (int j = 0; j < n; j++)
                {
                    if (j != i)
                    {
                        term *= (targetX - x[j]) / (x[i] - x[j]);
                    }
                }
                result += term;
            }

            return result;
        }

        /// <summary>
        /// 拉格朗日插值法进行位置补偿（输入位置，返回补偿后的位置）
        /// </summary>
        /// <param name="x">已知位置数据（测量点）</param>
        /// <param name="y">已知误差数据（测量点处的误差值）</param>
        /// <param name="inputPosition">输入的目标位置</param>
        /// <returns>补偿后的位置</returns>
        public static float Compensate(float[] x, float[] y, float inputPosition)
        {
            float error = Interpolate(x, y, inputPosition);
            return inputPosition + error;
        }

        /// <summary>
        /// 批量补偿：对一系列位置进行补偿
        /// </summary>
        /// <param name="x">已知位置数据（测量点）</param>
        /// <param name="y">已知误差数据（测量点处的误差值）</param>
        /// <param name="inputPositions">要补偿的位置数组</param>
        /// <returns>补偿后的位置数组</returns>
        public static float[] CompensateBatch(float[] x, float[] y, float[] inputPositions)
        {
            float[] compensated = new float[inputPositions.Length];
            for (int i = 0; i < inputPositions.Length; i++)
            {
                compensated[i] = Compensate(x, y, inputPositions[i]);
            }
            return compensated;
        }

        /// <summary>
        /// 三次样条插值（自然边界条件，两端平顺）
        /// </summary>
        public static float Interpolate3(float[] x, float[] y, float targetX)
        {
            if (x.Length != y.Length)
                throw new ArgumentException("x 和 y 数组长度必须相等");
            if (x.Length < 2)
                throw new ArgumentException("至少需要2个点");

            int n = x.Length;
            float[] a = (float[])y.Clone();
            float[] h = new float[n - 1];
            for (int i = 0; i < n - 1; i++)
                h[i] = x[i + 1] - x[i];

            // 构建三对角矩阵
            float[] alpha = new float[n];
            for (int i = 1; i < n - 1; i++)
                alpha[i] = 3.0f / h[i] * (a[i + 1] - a[i]) - 3.0f / h[i - 1] * (a[i] - a[i - 1]);

            float[] l = new float[n];
            float[] mu = new float[n];
            float[] z = new float[n];
            l[0] = 1; mu[0] = 0; z[0] = 0;

            for (int i = 1; i < n - 1; i++)
            {
                l[i] = 2 * (x[i + 1] - x[i - 1]) - h[i - 1] * mu[i - 1];
                mu[i] = h[i] / l[i];
                z[i] = (alpha[i] - h[i - 1] * z[i - 1]) / l[i];
            }

            l[n - 1] = 1; z[n - 1] = 0;
            float[] c = new float[n];
            float[] b = new float[n - 1];
            float[] d = new float[n - 1];

            for (int i = n - 2; i >= 0; i--)
            {
                c[i] = z[i] - mu[i] * c[i + 1];
                b[i] = (a[i + 1] - a[i]) / h[i] - h[i] * (c[i + 1] + 2 * c[i]) / 3;
                d[i] = (c[i + 1] - c[i]) / (3 * h[i]);
            }

            // 查找区间
            int index = FindInterval(x, targetX);
            float dx = targetX - x[index];
            return a[index] + b[index] * dx + c[index] * dx * dx + d[index] * dx * dx * dx;
        }

        /// <summary>
        /// 三次样条插值法进行位置补偿（输入位置，返回补偿后的位置）
        /// </summary>
        /// <param name="x">已知位置数据（测量点）</param>
        /// <param name="y">已知误差数据（测量点处的误差值）</param>
        /// <param name="inputPosition">输入的目标位置</param>
        /// <returns>补偿后的位置</returns>
        public static float Compensate3(float[] x, float[] y, float inputPosition)
        {
            float error = Interpolate3(x, y, inputPosition);
            return inputPosition + error;
        }

        private static int FindInterval(float[] x, float targetX)
        {
            if (targetX <= x[0]) return 0;
            if (targetX >= x[^1]) return x.Length - 2;

            for (int i = 0; i < x.Length - 1; i++)
                if (targetX >= x[i] && targetX <= x[i + 1])
                    return i;
            return 0;
        }

        /// <summary>
        /// 批量插值：根据已知点，生成一组平滑曲线点
        /// </summary>
        /// <param name="x">已知位置数据</param>
        /// <param name="y">已知误差数据</param>
        /// <param name="numPoints">要生成的曲线点数量</param>
        /// <returns>曲线点数组 (x, y)</returns>
        public static (float[] X, float[] Y) GenerateCurve(float[] x, float[] y, int numPoints = 100)
        {
            if (x.Length != y.Length)
                throw new ArgumentException("x 和 y 数组长度必须相等");
            if (x.Length == 0)
                return (Array.Empty<float>(), Array.Empty<float>());

            float minX = x.Min();
            float maxX = x.Max();
            float step = (maxX - minX) / (numPoints - 1);

            float[] curveX = new float[numPoints];
            float[] curveY = new float[numPoints];

            for (int i = 0; i < numPoints; i++)
            {
                curveX[i] = minX + i * step;
                curveY[i] = Interpolate3(x, y, curveX[i]);
            }

            return (curveX, curveY);
        }

        /// <summary>
        /// 计算拉格朗日插值多项式的系数表达式（用于调试/输出）
        /// </summary>
        public static string GetPolynomialExpression(float[] x, float[] y)
        {
            int n = x.Length;
            string expression = "L(x) = ";

            for (int i = 0; i < n; i++)
            {
                string numerator = $"{y[i]:F6}";
                string denominator = "1";
                string factors = "";

                for (int j = 0; j < n; j++)
                {
                    if (j != i)
                    {
                        numerator += $" * (x - {x[j]:F3})";
                        denominator += $" * ({x[i]:F3} - {x[j]:F3})";
                    }
                }

                expression += i > 0 ? " + " : "";
                expression += $"({numerator}) / ({denominator})";
            }

            return expression;
        }
    }
}