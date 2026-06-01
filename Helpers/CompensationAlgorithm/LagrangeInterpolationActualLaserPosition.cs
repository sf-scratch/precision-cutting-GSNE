using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Helpers.CompensationAlgorithm
{
    internal class LagrangeInterpolationActualLaserPosition
    {
        /// <summary>
        /// 拉格朗日插值（严格通过所有已知点）
        /// </summary>
        /// <param name="x">已知指令位置数据（测量点）</param>
        /// <param name="y">已知误差数据（测量点处的误差值 = 激光实测值 - 指令值）</param>
        /// <param name="targetX">要插值的指令位置</param>
        /// <returns>插值结果（预估误差值）</returns>
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
        /// 拉格朗日插值法进行位置补偿（输入指令位置，返回补偿后的指令位置）
        /// 补偿后位置 = 指令位置 + 预估误差
        /// </summary>
        /// <param name="x">已知指令位置数据（测量点）</param>
        /// <param name="y">已知误差数据（测量点处的误差值）</param>
        /// <param name="commandPosition">输入的指令位置</param>
        /// <returns>补偿后的指令位置</returns>
        public static float Compensate(float[] x, float[] y, float commandPosition)
        {
            float error = Interpolate(x, y, commandPosition);
            return commandPosition + error;
        }

        /// <summary>
        /// 根据指令位置，获取激光实际测量位置（实测值 = 指令值 + 误差）
        /// </summary>
        /// <param name="x">已知指令位置数据（测量点）</param>
        /// <param name="y">已知误差数据（测量点处的误差值）</param>
        /// <param name="commandPosition">输入的指令位置</param>
        /// <returns>激光实际测量位置</returns>
        public static float GetActualPosition(float[] x, float[] y, float commandPosition)
        {
            float error = Interpolate(x, y, commandPosition);
            return commandPosition + error;
        }

        /// <summary>
        /// 批量获取激光实际位置
        /// </summary>
        /// <param name="x">已知指令位置数据（测量点）</param>
        /// <param name="y">已知误差数据（测量点处的误差值）</param>
        /// <param name="commandPositions">指令位置数组</param>
        /// <returns>激光实际测量位置数组</returns>
        public static float[] GetActualPositions(float[] x, float[] y, float[] commandPositions)
        {
            float[] actualPositions = new float[commandPositions.Length];
            for (int i = 0; i < commandPositions.Length; i++)
            {
                actualPositions[i] = GetActualPosition(x, y, commandPositions[i]);
            }
            return actualPositions;
        }

        /// <summary>
        /// 根据期望的激光实际位置，反推需要的指令位置（逆向补偿）
        /// 指令位置 = 实际位置 - 误差
        /// </summary>
        /// <param name="x">已知指令位置数据（测量点）</param>
        /// <param name="y">已知误差数据（测量点处的误差值）</param>
        /// <param name="desiredPosition">期望的激光实际位置</param>
        /// <returns>需要发送的指令位置</returns>
        public static float GetCommandPosition(float[] x, float[] y, float desiredPosition)
        {
            // 由于误差是位置相关的函数，需要迭代求解
            // 简单方案：使用二分法或直接假设误差变化不大
            float estimatedCommand = desiredPosition;
            for (int i = 0; i < 5; i++) // 迭代5次提高精度
            {
                float error = Interpolate(x, y, estimatedCommand);
                estimatedCommand = desiredPosition - error;
            }
            return estimatedCommand;
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
        }/// <summary>

         /// 三次样条插值（自然边界条件，两端平顺）
         /// </summary>
        public static float Interpolate3(float[] x, float[] y, float targetX)
        {
            if (x.Length != y.Length)
                throw new ArgumentException("x 和 y 数组长度必须相等");
            if (x.Length < 2)
                throw new ArgumentException("至少需要2个点");

            // 添加：检查是否有重复的 X 值
            for (int i = 0; i < x.Length - 1; i++)
            {
                if (Math.Abs(x[i] - x[i + 1]) < 1e-7f)
                {
                    // 如果重复，使用拉格朗日插值作为降级方案
                    return Interpolate(x, y, targetX);
                }
            }

            // 添加：边界值处理（超出范围时外推或返回边界值）
            if (targetX <= x[0])
            {
                // 线性外推或返回第一个点的值
                if (x.Length == 1) return y[0];
                float slope = (y[1] - y[0]) / (x[1] - x[0]);
                return y[0] + slope * (targetX - x[0]);
            }
            if (targetX >= x[^1])
            {
                if (x.Length == 1) return y[^1];
                float slope = (y[^1] - y[^2]) / (x[^1] - x[^2]);
                return y[^1] + slope * (targetX - x[^1]);
            }

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
                // 添加：防止除零
                float denominator = 2 * (x[i + 1] - x[i - 1]) - h[i - 1] * mu[i - 1];
                if (Math.Abs(denominator) < 1e-7f)
                {
                    // 降级使用拉格朗日插值
                    return Interpolate(x, y, targetX);
                }

                l[i] = denominator;
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

                // 添加：检查计算结果是否为 NaN
                if (float.IsNaN(b[i]) || float.IsNaN(c[i]) || float.IsNaN(d[i]))
                {
                    return Interpolate(x, y, targetX);
                }
            }

            // 查找区间
            int index = FindInterval(x, targetX);
            float dx = targetX - x[index];
            float result = a[index] + b[index] * dx + c[index] * dx * dx + d[index] * dx * dx * dx;

            // 添加：最终结果检查
            return float.IsNaN(result) ? Interpolate(x, y, targetX) : result;
        }

        /// <summary>
        /// 改进的区间查找（添加边界保护）
        /// </summary>
        private static int FindInterval(float[] x, float targetX)
        {
            if (targetX <= x[0]) return 0;
            if (targetX >= x[^1]) return x.Length - 2;

            for (int i = 0; i < x.Length - 1; i++)
            {
                if (targetX >= x[i] && targetX <= x[i + 1])
                    return i;
            }
            return 0;
        }

        /// <summary>
        /// 安全的 Compensation3 方法（带降级处理）
        /// </summary>
        public static float Compensate3(float[] x, float[] y, float inputPosition)
        {
            try
            {
                float error = Interpolate3(x, y, inputPosition);

                // 检查 error 是否有效
                if (float.IsNaN(error) || float.IsInfinity(error))
                {
                    // 降级使用拉格朗日插值
                    error = Interpolate(x, y, inputPosition);
                }

                return inputPosition + error;
            }
            catch (Exception)
            {
                // 异常时降级使用拉格朗日插值
                float error = Interpolate(x, y, inputPosition);
                return inputPosition + error;
            }
        }

        /// <summary>
        /// 根据指令位置和实际测量位置，进行三次样条补偿
        /// </summary>
        /// <param name="commandPositions">已知指令位置</param>
        /// <param name="actualPositions">已知实际测量位置</param>
        /// <param name="targetCommand">目标指令位置</param>
        /// <returns>补偿后的指令位置</returns>
        public static float Compensate3FromActual(float[] commandPositions, float[] actualPositions, float targetCommand)
        {
            if (commandPositions.Length != actualPositions.Length)
                throw new ArgumentException("数组长度必须相等");

            // 内部转换为误差
            float[] errors = new float[commandPositions.Length];
            for (int i = 0; i < commandPositions.Length; i++)
            {
                errors[i] = actualPositions[i] - commandPositions[i];
            }

            // 使用现有方法
            float error = Interpolate3(commandPositions, errors, targetCommand);
            return targetCommand + error;
        }

        /// <summary>
        /// 根据指令位置和实际测量位置，获取激光实际位置（直接插值）
        /// </summary>
        public static float GetActualPositionFromActual(float[] commandPositions, float[] actualPositions, float targetCommand)
        {
            // 直接对实际位置进行插值（更简单，推荐！）
            return Interpolate3(commandPositions, actualPositions, targetCommand);
        }

        /// <summary>
        /// 批量插值：根据已知点，生成一组平滑曲线点
        /// </summary>
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