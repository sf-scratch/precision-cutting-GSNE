using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.position.correction
{
    internal class PositionCorrection
    {

        // 定义参数
        const double InitialLaserPosition = 109.7;// 激光干涉仪初始位置(mm
        const double StepDistance = 0.3;//步距(mm)(mm)，即 0.2微米
        const double MaxError = 0.2e-3;//最大误差

        public static void CalculateCompensation()
        {
            // 初始位置
            double laserPosition = InitialLaserPosition;
            double gratingPosition = laserPosition; // 光栅尺初始位置
            double cumulativeError = 0; // 累计误差

            // 输出表头
            Debug.WriteLine("步骤\t激光干涉仪位置 (mm)\t光栅尺位置 (mm)\t补偿因子 (μm)\t单步误差 (μm)\t累计误差 (μm)");

            // 计算每步
            for (int step = 1; step <= 12; step++) // 假设步数为12
            {
                double error = Math.Abs(laserPosition - gratingPosition); // 计算单步误差

                // 计算补偿因子, 保证每步误差不超过最大值
                double compensationFactor = CalculateCompensationFactor(laserPosition, gratingPosition, error);

                // 更新光栅尺位置
                gratingPosition = gratingPosition + compensationFactor;

                // 累计误差
                cumulativeError += error;

                // 输出每步计算的结果
                Debug.WriteLine($"{step}\t{laserPosition:F4}\t\t\t{gratingPosition:F4}\t\t{compensationFactor * 1e3:F2}\t\t\t{error * 1e6:F2}\t\t\t{cumulativeError * 1e6:F2}");

                // 减去步距，模拟激光干涉仪的位置更新
                laserPosition -= StepDistance;
            }
        }

        static double CalculateCompensationFactor(double laserPosition, double gratingPosition, double error)
        {
            double compensationFactor = 0;
            if (error > MaxError)
            {
                compensationFactor = Math.Sign(laserPosition - gratingPosition) * Math.Min(Math.Abs(laserPosition - gratingPosition), MaxError);
            }
            return compensationFactor;
        }
    }
}
