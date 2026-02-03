using NPOI.OpenXmlFormats.Dml;
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.ViewModel;

namespace 精密切割系统.Driver
{
    /// <summary>
    /// 公共状态检查
    /// </summary>
    internal class CommonCheck
    {
        /// <summary>
        /// 检查全局运行状态
        /// </summary>
        /// <returns></returns>
        public static bool CheckGlobalRunStatus()
        {
            return GlobalParams.globalRunFlag;
        }

        /// <summary>
        /// 轴运动状态检查 1、全局运行状态 2 轴busy状态 3、报警状态
        /// </summary>
        /// <returns></returns>
        public static bool AxisRunStatusCheck()
        {
            if (!CheckGlobalRunStatus() || AlarmConfig.Instance.HasActiveErrorAlarm())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 校准模式状态校验
        /// </summary>
        /// <returns></returns>
        public static bool MlignStatusCheck()
        {
            return true;
        }

        /// <summary>
        /// 自动聚焦限制
        /// </summary>
        /// <returns></returns>
        public static bool FocusStatsCheck()
        {
            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public static bool CheckDoor2()
        {
            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public static bool CheckDoor1()
        {
            return true;
        }

        /// <summary>
        /// 校验是否有配置信息
        /// </summary>
        /// <returns></returns>
        public static bool CheckConfig()
        {
            long deviceDataId = CurrentUtils.GetCurrentConfiguration().DeviceDataId;
            if (deviceDataId == 0)
            {
                MaterialSnack("请先选择配置文件！", SnackType.WARNING);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 校验是否有测高数据
        /// </summary>
        /// <returns></returns>
        public static bool CheckBladeHeight()
        {
            string bladeHeight = CurrentUtils.GetBladeHeightModel().BladeHeight;
            bool flag = bladeHeight != null && !"".Equals(bladeHeight) && !"0".Equals(bladeHeight);
            if (!flag)
            {
                MaterialSnack("请先测高！", SnackType.WARNING);
                return false;
            }
            return true;
        }
    }
}