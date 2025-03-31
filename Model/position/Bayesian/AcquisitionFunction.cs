using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.position.Bayesian
{
    internal class AcquisitionFunction
    {
        private double[] yMax;

        public AcquisitionFunction(double[] yMax)
        {
            this.yMax = yMax;
        }

        // 计算EI（Expected Improvement）
        public double ExpectedImprovement(double[] x, double[] mean, double[,] covariance, double[] y)
        {
            double meanX = mean[Array.IndexOf(x, x)];
            double varX = covariance[Array.IndexOf(x, x), Array.IndexOf(x, x)];

            double improvement = meanX - yMax[0];
            if (improvement > 0)
                return improvement;
            else
                return 0;
        }
    }
}
