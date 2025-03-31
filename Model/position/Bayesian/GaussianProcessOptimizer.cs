using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Double;

namespace 精密切割系统.Model.position.Bayesian
{
    internal class GaussianProcessOptimizer
    {
        private double[][] X;  // 训练数据（光栅尺位置）
        private double[] y;    // 目标值（激光测量的实际值）
        private double sigmaF; // 核函数尺度
        private double sigmaN; // 噪声方差

        public GaussianProcessOptimizer(double[][] X, double[] y)
        {
            this.X = X;
            this.y = y;
            this.sigmaF = 1.0; // 初始猜测
            this.sigmaN = 1e-2; // 初始猜测
        }

        // 计算边际似然（Marginal Likelihood）
        private double ComputeMarginalLikelihood(double sigmaF, double sigmaN)
        {
            int n = X.Length;
            var K = ComputeKernelMatrix(X, sigmaF, sigmaN);
            var K_inv = Matrix<double>.Build.DenseOfArray(K).Inverse();
            var yVector = Vector<double>.Build.Dense(y); // 将y转换为Vector<double>

            var term1 = 0.5 * y.Length * Math.Log(2 * Math.PI);
            var term2 = 0.5 * yVector.DotProduct(K_inv * yVector);  // 使用DotProduct计算向量积
            var term3 = 0.5 * Math.Log(Matrix<double>.Build.DenseOfArray(K).Determinant());
            return term1 + term2 + term3;
        }

        // 计算RBF核矩阵
        private double[,] ComputeKernelMatrix(double[][] X, double sigmaF, double sigmaN)
        {
            int n = X.Length;
            double[,] K = new double[n, n];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    K[i, j] = RbfKernel(X[i], X[j], sigmaF);
                }
            }

            // 加上噪声方差
            for (int i = 0; i < n; i++)
            {
                K[i, i] += sigmaN;
            }

            return K;
        }

        // RBF核函数
        private double RbfKernel(double[] x1, double[] x2, double sigmaF)
        {
            double sum = 0;
            for (int i = 0; i < x1.Length; i++)
            {
                sum += Math.Pow(x1[i] - x2[i], 2);
            }
            return sigmaF * Math.Exp(-0.5 * sum);
        }

        // 网格搜索来寻找最佳的sigmaF和sigmaN
        public void Optimize()
        {
            double bestSigmaF = sigmaF;
            double bestSigmaN = sigmaN;
            double bestMarginalLikelihood = ComputeMarginalLikelihood(sigmaF, sigmaN);

            // 设置网格搜索的参数范围
            double[] sigmaFTrials = new double[] { 0.1, 0.5, 1.0, 2.0, 5.0 };
            double[] sigmaNTrials = new double[] { 1e-5, 1e-3, 1e-2, 1e-1, 1.0 };

            // 网格搜索优化sigmaF和sigmaN
            foreach (var sigmaFTrial in sigmaFTrials)
            {
                foreach (var sigmaNTrial in sigmaNTrials)
                {
                    double marginalLikelihood = ComputeMarginalLikelihood(sigmaFTrial, sigmaNTrial);
                    Console.WriteLine($"sigmaF: {sigmaFTrial}, sigmaN: {sigmaNTrial}, Marginal Likelihood: {marginalLikelihood}");

                    if (marginalLikelihood > bestMarginalLikelihood)
                    {
                        bestSigmaF = sigmaFTrial;
                        bestSigmaN = sigmaNTrial;
                        bestMarginalLikelihood = marginalLikelihood;
                    }
                }
            }

            // 输出最佳超参数
            Console.WriteLine($"最佳sigmaF: {bestSigmaF}, 最佳sigmaN: {bestSigmaN}, 最大边际似然: {bestMarginalLikelihood}");
        }

        // 使用优化后的模型进行预测
        public double Predict(double[] x)
        {
            var K = ComputeKernelMatrix(X, sigmaF, sigmaN);
            var K_inv = Matrix<double>.Build.DenseOfArray(K).Inverse();
            var K_star = new double[X.Length];
            for (int i = 0; i < X.Length; i++)
            {
                K_star[i] = RbfKernel(X[i], x, sigmaF);
            }

            var K_starVector = Vector<double>.Build.Dense(K_star);
            var mean = K_starVector.DotProduct(K_inv * Vector<double>.Build.Dense(y));
            return mean;
        }
    }

    /*public class Program
    {
        public static void Main()
        {
            // 假设已有光栅尺位置和激光测量数据
            // 位置数据（光栅尺的测量值）
            double[][] positionNumbers = new double[][]
            {
            new double[] { 110 },
            new double[] { 109.7 },
            new double[] { 109.4 },
            new double[] { 109.1 },
            new double[] { 108.8 }
            };

            // 补偿数据（激光干涉仪的实际值）
            double[] compensateNumbers = new double[] { 110.1, 109.75, 109.45, 109.05, 108.85 };

            // 创建高斯过程优化器对象
            var optimizer = new GaussianProcessOptimizer(positionNumbers, compensateNumbers);

            // 进行超参数优化
            optimizer.Optimize();

            // 使用优化后的模型进行预测
            double[] testPosition = new double[] { 109.2 };
            double predictedCompensation = optimizer.Predict(testPosition);
            Console.WriteLine($"预测位置 109.2mm 的补偿值为: {predictedCompensation}");
        }
    }*/
}