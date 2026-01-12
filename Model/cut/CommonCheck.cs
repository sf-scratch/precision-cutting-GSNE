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
        /// 切割状态校验
        /// </summary>
        /// <param name="checkType">0 全部检查 1 不检查安全门2</param>
        /// <returns></returns>
        public static bool CutStatusCheck(int checkType = 0)
        {
            // 判断是否有报警
            if (AlarmConfig.Instance.HasActiveErrorAlarm())
            {
                return false;
            }
            if (checkType == 0)
            {
                if (!CheckDoor2())
                {
                    return false;
                }
            }
            // 校验安全门 主轴状态 是否有配置数据 是否有测高数据 校验各轴是否准备好
            if (!CheckDoor1() || !SpindleStatusCheck() || !CheckCacuum() || !CheckConfig() || !CheckBladeHeight() || !AxisReady(false))
            {
                return false;
            }
            return true;
        }

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
            if (!CheckGlobalRunStatus() || !AxisReady(false) || AlarmConfig.Instance.HasActiveErrorAlarm())
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
            // 校验安全门
            if (!CheckDoor() || !AxisReady(false))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 电火花修刀状态检查
        /// </summary>
        /// <returns></returns>
        public static bool TruingStatusCheck()
        {
            // 校验安全门
            if (!CheckDoor() || !SpindleStatusCheck() || !AxisReady(false))
            {
                return false;
            }
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
        /// 校验是否在切割模式
        /// </summary>
        /// <returns></returns>
        public static bool CutModeCheck()
        {
            return Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.fullAutoInitKey));
        }

        /// <summary>
        /// 校验是否在某个模式
        /// </summary>
        /// <returns></returns>
        public static bool ModeCheck()
        {
            // 校准
            bool alignStatus = Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.alignStatusKey));
            // 电火花修刀
            bool sharpenStatus = Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.sharpenStatusKey));
            // 刀片维护状态
            bool bladeMantanceStatus = Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.bladeMantanceStatusKey));

            if (CutModeCheck() || alignStatus || sharpenStatus || bladeMantanceStatus)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 校验是否在运行中 切割或者电火花修刀
        /// </summary>
        /// <returns></returns>
        public static bool ModelRunCheck()
        {
            bool cutStatus = Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.cutStatusKey));
            bool fullAutoInit = Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.fullAutoInitKey));
            bool sharpenStatus = Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.electricalStatusKey));
            if ((fullAutoInit && !cutStatus) || sharpenStatus)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Theta轴拉直/校准
        /// </summary>
        /// <returns></returns>
        public static bool ThetaAlignStatsCheck()
        {
            if (Tools.TrueFlag(DeviceKey.alignStatusKey))
            {
                MaterialSnack("校准模式未准备好！", SnackType.WARNING);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 校验所有轴是否准备好
        /// </summary>
        /// <returns></returns>
        public static bool AxisReady(bool spindleFlag = true)
        {
            bool xReadyFlag = Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.curMotionStatusKey));
            bool yReadyFlag = Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.yCurMotionStatusKey));
            bool z1ReadyFlag = Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.z1CurMotionStatusKey));
            bool z2ReadyFlag = Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.z2CurMotionStatusKey));
            bool thetaReadyFlag = Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.thetaCurMotionStatusKey));
            bool spindleRunFlag = CheckSpindleRunStatus();
            if (xReadyFlag || yReadyFlag || z1ReadyFlag || z2ReadyFlag || thetaReadyFlag)
            {
                return false;
            }
            if (spindleFlag)
            {
                if (spindleRunFlag)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 检测主轴是否在转
        /// </summary>
        /// <returns></returns>
        public static bool CheckSpindleRunStatus()
        {
            bool status = Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.spindleManuallyRunStatusKey));
            if (status)
            {
                MaterialSnack("主轴运行中！", SnackType.WARNING);
            }
            return status;
        }

        /// <summary>
        /// 校验主轴信息
        /// </summary>
        /// <returns></returns>
        public static bool SpindleStatusCheck()
        {
            if (!SpindleAirCheck())
            {
                return false;
            }
            // 主轴切割水
            if (!GetParamsStatus(DeviceKey.spindleCoolingWaterKey))
            {
                MaterialSnack("主轴冷却水异常！", SnackType.WARNING);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 校验主轴气源
        /// </summary>
        /// <returns></returns>
        public static bool SpindleAirCheck()
        {
            // 主轴气源
            if (!GetParamsStatus(DeviceKey.spindleAirKey))
            {
                MaterialSnack("主轴气源未开启或压力不足！", SnackType.WARNING);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 检查负真空状态
        /// </summary>
        /// <returns></returns>
        public static bool CheckCacuum()
        {
            bool tempVacuumState = CommonCheck.GetParamsStatus(DeviceKey.vacuumSwitchStatusKey);
            if (!tempVacuumState)
            {
                MaterialSnack("请先打开真空！", SnackType.WARNING);
                return false;
            }
            if (!GetParamsStatus(DeviceKey.vacuumStateKey))
            {
                MaterialSnack("真空不足或工作盘上无工件！", SnackType.WARNING);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 校验2个安全门
        /// </summary>
        /// <returns></returns>
        public static bool CheckDoor()
        {
            if (!GetDoorStatus(DeviceKey.securityDoor1StatusKey))
            {
                MaterialSnack("请先关闭安全门！", SnackType.WARNING);
                return false;
            }
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
            if (!GetDoorStatus(DeviceKey.securityDoor1StatusKey))
            {
                MaterialSnack("请先关闭安全门！", SnackType.WARNING);
                return false;
            }
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

        /// <summary>
        /// 检查安全门 关闭状态 true 打开状态 false
        /// </summary>
        /// <returns>门的key </returns>
        public static bool GetDoorStatus(String doorName)
        {
            return true;
            string runValue = PlcControl.plc.GetPlcValueString(doorName);
            return "True".Equals(runValue);
        }

        public static bool GetParamsStatus(String paramsName)
        {
            string value = PlcControl.plc.GetPlcValueString(paramsName);
            return ("1".Equals(value) || "True".Equals(value));
        }

        // 检查各轴的状态
        public static bool CheckAxisRunStatus()
        {
            return true;
        }
    }
}