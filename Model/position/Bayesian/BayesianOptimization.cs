using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Model.position.Bayesian;

namespace 精密切割系统.Model.position.新文件夹
{
    internal class BayesianOptimization
    {
        private GaussianProcess gp;
        private AcquisitionFunction acq;
        private double[][] X;  // 训练数据点
        private double[] y;    // 对应的目标值

        public BayesianOptimization(double[][] X, double[] y, double sigmaF, double sigmaN)
        {
            this.X = X;
            this.y = y;
            gp = new GaussianProcess(sigmaF, sigmaN);
            acq = new AcquisitionFunction(y);
        }

        public void Optimize(int numIterations)
        {
            for (int i = 0; i < numIterations; i++)
            {
                // 计算高斯过程的预测和协方差
                double[,] K = gp.ComputeKernelMatrix(X);
                double[] mean = new double[X.Length];
                double[] covariance = new double[X.Length];
                for (int j = 0; j < X.Length; j++)
                {
                    mean[j] = gp.Predict(X[j], X, y);
                    covariance[j] = K[j, j];
                }

                // 使用采集函数计算EI（Expected Improvement）
                double[] ei = new double[X.Length];
                for (int j = 0; j < X.Length; j++)
                {
                    ei[j] = acq.ExpectedImprovement(X[j], mean, K, y);
                }

                // 选择具有最大EI的点进行采样
                int nextIndex = Array.IndexOf(ei, ei.Max());
                double[] nextSample = X[nextIndex];
                double nextY = NextMeasurement(nextSample);

                // 更新数据点和目标值
                X = (double[][])X.Append(nextSample);
                y = (double[])y.Append(nextY);
            }
        }

        // 目标函数的测量
        public double NextMeasurement(double[] sample)
        {
            // 在此可以调用目标函数来获取下一个测量值
            return Math.Sin(sample[0]);  // 例如，目标函数为 sin(x)
        }

        // 预测补偿值
        public double PredictCompensation(double position)
        {
            double[] positionArray = new double[] { position };  // 将位置传入模型
            return gp.Predict(positionArray, X, y);  // 使用高斯过程进行预测
        }
    }
}
