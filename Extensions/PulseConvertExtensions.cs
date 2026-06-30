using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NPOI.OpenXmlFormats.Spreadsheet;
using 精密切割系统.Driver;
using 精密切割系统.Helpers.GTN;

namespace 精密切割系统.Extensions
{
    public static class PulseConvertExtensions
    {
        public static double PulseToMM(this double curLocation, AxisType axisType)
        {
            return axisType switch
            {
                //AxisType.X => curLocation / GsneConfig.Instance.X_PulsePerMM,                    
               AxisType.X => curLocation / GsneConfig.Instance.X.PulsePerMM,                   // X轴：齿轮比转换
                AxisType.Y => curLocation / GsneConfig.Instance.Y.PulsePerMM,                  // Y轴：齿轮比转换
               AxisType.Z1 => curLocation / GsneConfig.Instance.Z1.PulsePerMM,                 // Z1轴：齿轮比转换
               AxisType.Z2 => curLocation / GsneConfig.Instance.Z2.PulsePerMM,                 // Z2轴：齿轮比转换
               AxisType.Theta => curLocation / GsneConfig.Instance.Theta.PulsePerMM,           // DD轴：齿轮比转换
                _ => curLocation                              // 其他轴：默认返回
            };
        }
        public static int MMToPulse(this double curLocation, AxisType axisType)
        {
            return axisType switch
            {
                AxisType.X => (int)(curLocation * GsneConfig.Instance.X.PulsePerMM),                    // X轴：齿轮比转换
                AxisType.Y => (int)(curLocation * GsneConfig.Instance.Y.PulsePerMM),                    // Y轴：齿轮比转换
                AxisType.Z1 => (int)(curLocation * GsneConfig.Instance.Z1.PulsePerMM),                 // Z1轴：齿轮比转换
                AxisType.Z2 => (int)(curLocation * GsneConfig.Instance.Z2.PulsePerMM),                   // Z2轴：齿轮比转换
                AxisType.Theta => (int)(curLocation * GsneConfig.Instance.Theta.PulsePerMM),           // DD轴：齿轮比转换
                _ => (int)curLocation                              // 其他轴：默认返回
            };
        }

        public static float PulseToMMF(this float curLocation, AxisType axisType)
        {
            return axisType switch
            {
                AxisType.X => (float)(curLocation / GsneConfig.Instance.X.PulsePerMM),                    // X轴：齿轮比转换
                AxisType.Y => (float)(curLocation / GsneConfig.Instance.Y.PulsePerMM),                    // Y轴：齿轮比转换
                AxisType.Z1 => (float)(curLocation / GsneConfig.Instance.Z1.PulsePerMM),                 // Z1轴：齿轮比转换
                AxisType.Z2 => (float)(curLocation / GsneConfig.Instance.Z2.PulsePerMM),                   // Z2轴：齿轮比转换
                AxisType.Theta => (float)(curLocation / GsneConfig.Instance.Theta.PulsePerMM),           // DD轴：齿轮比转换
                _ => (float)curLocation                              // 其他轴：默认返回
            };
        }

        public static int MMToPulseF(this float curLocation, AxisType axisType)
        {
            return axisType switch
            {
                AxisType.X => (int)(curLocation * GsneConfig.Instance.X.PulsePerMM),                    // X轴：齿轮比转换
                AxisType.Y => (int)(curLocation * GsneConfig.Instance.Y.PulsePerMM),                    // Y轴：齿轮比转换
                AxisType.Z1 => (int)(curLocation * GsneConfig.Instance.Z1.PulsePerMM),                 // Z1轴：齿轮比转换
                AxisType.Z2 => (int)(curLocation * GsneConfig.Instance.Z2.PulsePerMM),                   // Z2轴：齿轮比转换
                AxisType.Theta => (int)(curLocation * GsneConfig.Instance.Theta.PulsePerMM),           // DD轴：齿轮比转换
                _ => (int)curLocation                              // 其他轴：默认返回
            };
        }
        /// <summary>
        /// pulse/ms-转换mm/s速度
        /// </summary>
        /// <param name="speed"></param>
        /// <param name="axisType"></param>
        /// <returns></returns>
        public static double PulsePerMsToMmPerSec(this double speed, AxisType axisType)
        {
            return axisType switch
            {
                AxisType.X => speed / GsneConfig.Instance.X.PulsePerMM * 1000,                    // X轴：齿轮比转换
                AxisType.Y => speed / GsneConfig.Instance.Y.PulsePerMM * 1000,                    // Y轴：齿轮比转换
                AxisType.Z1 => speed / GsneConfig.Instance.Z1.PulsePerMM * 1000,                 // Z1轴：齿轮比转换
                AxisType.Z2 => speed / GsneConfig.Instance.Z2.PulsePerMM * 1000,                   // Z2轴：齿轮比转换
                AxisType.Theta => speed / GsneConfig.Instance.Theta.PulsePerMM * 1000,           // DD轴：齿轮比转换
                _ => speed                              // 其他轴：默认返回
            };
        }
        /// <summary>
        /// 转换mm/s速度-pulse/ms
        /// </summary>
        /// <param name="speed"></param>
        /// <param name="axisType"></param>
        /// <returns></returns>
        public static int MmPerSecToPulsePerMs(this double speed, AxisType axisType)
        {
            return axisType switch
            {
                AxisType.X => (int)(speed * GsneConfig.Instance.X.PulsePerMM / 1000),                    // X轴：齿轮比转换
                AxisType.Y => (int)(speed * GsneConfig.Instance.Y.PulsePerMM / 1000),                    // Y轴：齿轮比转换
                AxisType.Z1 => (int)(speed * GsneConfig.Instance.Z1.PulsePerMM / 1000),                 // Z1轴：齿轮比转换
                AxisType.Z2 => (int)(speed * GsneConfig.Instance.Z2.PulsePerMM / 1000),                   // Z2轴：齿轮比转换
                AxisType.Theta => (int)(speed * GsneConfig.Instance.Theta.PulsePerMM / 1000),           // DD轴：齿轮比转换
                _ => (int)speed                              // 其他轴：默认返回
            };
        }

        public static float PulsePerMsToMmPerSec(this float curLocation, AxisType axisType)
        {
            return axisType switch
            {
                AxisType.X => (float)(curLocation / GsneConfig.Instance.X.PulsePerMM * 1000),                    // X轴：齿轮比转换
                AxisType.Y => (float)(curLocation / GsneConfig.Instance.Y.PulsePerMM * 1000),                    // Y轴：齿轮比转换
                AxisType.Z1 => (float)(curLocation / GsneConfig.Instance.Z1.PulsePerMM * 1000),                 // Z1轴：齿轮比转换
                AxisType.Z2 => (float)(curLocation / GsneConfig.Instance.Z2.PulsePerMM * 1000),                   // Z2轴：齿轮比转换
                AxisType.Theta => (float)(curLocation / GsneConfig.Instance.Theta.PulsePerMM * 1000),           // DD轴：齿轮比转换
                _ => (float)curLocation                              // 其他轴：默认返回
            };
        }

        public static int MmPerSecToPulsePerMs(this float curLocation, AxisType axisType)
        {
            return axisType switch
            {
                AxisType.X => (int)(curLocation * GsneConfig.Instance.X.PulsePerMM / 1000),                    // X轴：齿轮比转换
                AxisType.Y => (int)(curLocation * GsneConfig.Instance.Y.PulsePerMM / 1000),                    // Y轴：齿轮比转换
                AxisType.Z1 => (int)(curLocation * GsneConfig.Instance.Z1.PulsePerMM / 1000),                 // Z1轴：齿轮比转换
                AxisType.Z2 => (int)(curLocation * GsneConfig.Instance.Z2.PulsePerMM / 1000),                   // Z2轴：齿轮比转换
                AxisType.Theta => (int)(curLocation * GsneConfig.Instance.Theta.PulsePerMM / 1000),           // DD轴：齿轮比转换
                _ => (int)curLocation                              // 其他轴：默认返回
            };
        }
    }
}
