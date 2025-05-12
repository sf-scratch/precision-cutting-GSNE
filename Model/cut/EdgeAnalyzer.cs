using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut
{
    public class EdgeAnalyzer
    {
        // === 参数配置 ===
        private const double PIXEL_TO_UM = 0.74;          // 每像素对应微米
        private const double ANGLE_THRESHOLD_DEG = 0.3;   // 拟合直线角度大于该值算蛇形
        private const double MAX_DEV_UM_THRESH = 5.0;     // 最大偏差超过该值算蛇形
        private const int SMOOTH_WINDOW = 51;             // 滑动窗口大小
        private const int SMOOTH_POLYORDER = 3;           // Savitzky-Golay滤波阶数

        public class AnalysisResult
        {
            public string File { get; set; }
            public string Status { get; set; }
            public double MaxDevUm { get; set; }
            public double StdDevUm { get; set; }
            public double AngleDeg { get; set; }
            public bool Snake { get; set; }
        }

        public static AnalysisResult AnalyzeEdgeShape(List<(int x, int y)> topPoints, List<(int x, int y)> bottomPoints)
        {
            // 对每个x坐标，找到对应的最高和最低y值
            var allXCoords = topPoints.Select(p => p.x)
                                     .Concat(bottomPoints.Select(p => p.x))
                                     .Distinct()
                                     .OrderBy(x => x)
                                     .ToArray();

            // Process top edge (minimum y for each x)
            var topEdge = new double[allXCoords.Length];
            for (int i = 0; i < allXCoords.Length; i++)
            {
                var x = allXCoords[i];
                var ys = topPoints.Where(p => p.x == x).Select(p => p.y);
                topEdge[i] = ys.Any() ? ys.Min() : double.NaN;
            }

            // Process bottom edge (maximum y for each x)
            var bottomEdge = new double[allXCoords.Length];
            for (int i = 0; i < allXCoords.Length; i++)
            {
                var x = allXCoords[i];
                var ys = bottomPoints.Where(p => p.x == x).Select(p => p.y);
                bottomEdge[i] = ys.Any() ? ys.Max() : double.NaN;
            }

            // 计算中线
            var midLine = new double[allXCoords.Length];
            for (int i = 0; i < allXCoords.Length; i++)
            {
                midLine[i] = (topEdge[i] + bottomEdge[i]) / 2;
            }

            // Get valid indices
            var validIndices = Enumerable.Range(0, midLine.Length)
                                        .Where(i => !double.IsNaN(midLine[i]))
                                        .ToArray();

            if (validIndices.Length < 50)
            {
                return new AnalysisResult
                {
                    Status = "Too few valid points"
                };
            }

            var xVals = validIndices.Select(i => (double)i).ToArray();
            var yVals = validIndices.Select(i => midLine[i]).ToArray();

            // Savitzky-Golay smoothing
            var ySmooth = SavitzkyGolay.Smooth(yVals, SMOOTH_WINDOW, SMOOTH_POLYORDER);

            // 直线拟合
            var fit = Fit.Line(xVals, ySmooth);
            double intercept = fit.Item1;
            double slope = fit.Item2;

            var fitLine = xVals.Select(x => intercept + slope * x).ToArray();
            var deviation = ySmooth.Zip(fitLine, (y, f) => y - f).ToArray();

            // 单位转换
            double maxDevUm = deviation.Select(d => Math.Abs(d)).Max() * PIXEL_TO_UM;
            double stdDevUm = StandardDeviation(deviation) * PIXEL_TO_UM;

            double angleDeg = Math.Atan(slope) * (180.0 / Math.PI);

            bool isSnake = (maxDevUm > MAX_DEV_UM_THRESH) || (Math.Abs(angleDeg) > ANGLE_THRESHOLD_DEG);

            return new AnalysisResult
            {
                MaxDevUm = Math.Round(maxDevUm, 2),
                StdDevUm = Math.Round(stdDevUm, 2),
                AngleDeg = Math.Round(angleDeg, 3),
                Snake = isSnake,
                Status = null
            };
        }
        private static double StandardDeviation(IEnumerable<double> values)
        {
            double avg = values.Average();
            return Math.Sqrt(values.Average(v => Math.Pow(v - avg, 2)));
        }
    }

    // Savitzky-Golay filter implementation for C#
    public static class SavitzkyGolay
    {
        public static double[] Smooth(double[] data, int windowSize, int polynomialOrder)
        {
            if (windowSize % 2 == 0)
                throw new ArgumentException("Window size must be odd");
            if (windowSize < polynomialOrder + 2)
                throw new ArgumentException("Window size is too small for the polynomial order");

            int halfWindow = windowSize / 2;
            var smoothed = new double[data.Length];

            // Compute convolution coefficients
            var coefficients = ComputeCoefficients(halfWindow, polynomialOrder);

            // Handle edges (mirroring)
            for (int i = 0; i < data.Length; i++)
            {
                double sum = 0;
                for (int j = -halfWindow; j <= halfWindow; j++)
                {
                    int index = i + j;
                    if (index < 0) index = -index; // mirror at start
                    if (index >= data.Length) index = 2 * data.Length - index - 2; // mirror at end

                    sum += coefficients[j + halfWindow] * data[index];
                }
                smoothed[i] = sum;
            }

            return smoothed;
        }

        private static double[] ComputeCoefficients(int halfWindow, int polynomialOrder)
        {
            int windowSize = 2 * halfWindow + 1;
            var a = new DenseMatrix(windowSize, polynomialOrder + 1);

            for (int i = 0; i < windowSize; i++)
            {
                int x = i - halfWindow;
                for (int j = 0; j <= polynomialOrder; j++)
                {
                    a[i, j] = Math.Pow(x, j);
                }
            }

            var qr = a.QR();
            var r = qr.R;
            var q = qr.Q;

            // R*c = Q'*b where b is [0,...,0,1,0,...,0] with 1 at position m
            int m = halfWindow;
            var b = new double[windowSize];
            b[m] = 1;

            var qTb = q.TransposeThisAndMultiply(new DenseVector(b));
            var c = new double[polynomialOrder + 1];

            // Back substitution
            for (int k = polynomialOrder; k >= 0; k--)
            {
                c[k] = qTb[k];
                for (int j = k + 1; j <= polynomialOrder; j++)
                {
                    c[k] -= r[k, j] * c[j];
                }
                c[k] /= r[k, k];
            }

            // Compute coefficients
            var coefficients = new double[windowSize];
            for (int i = 0; i < windowSize; i++)
            {
                coefficients[i] = 0;
                int x = i - halfWindow;
                for (int j = 0; j <= polynomialOrder; j++)
                {
                    coefficients[i] += c[j] * Math.Pow(x, j);
                }
            }

            return coefficients;
        }
    }
}
