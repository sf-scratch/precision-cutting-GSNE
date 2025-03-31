using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.position.Bayesian
{
    internal class GaussianProcess
    {
        private double sigmaF; // 核函数尺度
        private double sigmaN; // 噪声方差

        public GaussianProcess(double sigmaF, double sigmaN)
        {
            this.sigmaF = sigmaF;
            this.sigmaN = sigmaN;
        }

        // RBF 核函数
        public double RbfKernel(double[] x1, double[] x2)
        {
            if (x1.Length != x2.Length)
                throw new ArgumentException("x1 和 x2 的长度不一致");
            double sum = 0;
            for (int i = 0; i < x1.Length; i++)
            {
                sum += Math.Pow(x1[i] - x2[i], 2);
            }
            return sigmaF * Math.Exp(-0.5 * sum);
        }

        // 核矩阵的计算
        public double[,] ComputeKernelMatrix(double[][] X)
        {
            int n = X.Length;
            double[,] K = new double[n, n];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    K[i, j] = RbfKernel(X[i], X[j]);
                }
            }
            // 加上噪声方差，防止矩阵不可逆
            for (int i = 0; i < n; i++)
            {
                K[i, i] += sigmaN;
            }
            return K;
        }

        // 高斯过程的预测
        public double Predict(double[] xStar, double[][] X, double[] y)
        {
            var K = ComputeKernelMatrix(X);
            var K_star = new double[X.Length];
            for (int i = 0; i < X.Length; i++)
            {
                K_star[i] = RbfKernel(X[i], xStar);
            }
            var K_inv = MathNet.Numerics.LinearAlgebra.Double.DenseMatrix.OfArray(K).Inverse();
            double mean = 0;
            for (int i = 0; i < X.Length; i++)
            {
                mean += K_inv[i, 0] * (y[i] - mean);
            }
            return mean;
        }
    }
}
