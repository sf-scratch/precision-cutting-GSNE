using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.ViewModel;

namespace 精密切割系统.Model.plc
{
    internal static class DeviceKey
    {
        // θ轴名称
        public static string thetaName = "Theta轴";

        // X轴名称
        public static string xName = "X轴";

        // Y轴名称
        public static string yName = "Y轴";

        // Z1轴名称
        public static string z1Name = "Z1轴";

        // Z2轴名称
        public static string z2Name = "Z2轴";

        // 刀片维护
        public static string bladeMaintenance = "刀片维护";

        // 系统
        public static string systemName = "整机";

        // 校准
        public static string alignName = "校准";

        // 校准
        public static string cutName = "切割";

        // 校准
        public static string enterElectricalName = "电火花修刀";

        public static string curLocationKey = "X轴当前位置";
        public static string curSpeedKey = "X轴当前速度";
        public static string jogSpeedKey = "X轴点动速度";
        public static string curStatusKey = "X轴当前状态";
        public static string curMotionStatusKey = "X轴当前电机状态";
        public static string runTypeKey = "X轴运行类型";
        public static string jogStartKey = "X轴正转开始";
        public static string jogAntiStartKey = "X轴反转开始";
        public static string jogRelativeSpeedKey = "X点动和相对运动速度";
        public static string relativeDistanceKey = "X轴相对运动距离";
        public static string absoluteStartKey = "X轴绝对运动开始";
        public static string absoluteSpeedKey = "X轴绝对运动速度";
        public static string absoluteLocationKey = "X轴绝对运动目标位置";
        public static string highSpeedKey = "X轴高速运动";
        public static string softUpperLimitKey = "X轴软正限位";
        public static string softLowerLimitKey = "X轴软负限位";

        public static string yCurLocationKey = "Y轴当前位置";
        public static string yGratingRulerCurLocationKey = "Y轴光栅尺当前位置";
        public static string yCurMotionStatusKey = "Y轴当前电机状态";
        public static string yCurSpeedKey = "Y轴当前速度";
        public static string yJogSpeedKey = "Y轴点动速度";
        public static string yCurStatusKey = "Y轴当前状态";
        public static string yRunTypeKey = "Y轴运行类型";
        public static string yJogStartKey = "Y轴正转开始";
        public static string yJogAntiStartKey = "Y轴反转开始";
        public static string yJogRelativeSpeedKey = "Y点动和相对运动速度";
        public static string yRelativeDistanceKey = "Y轴相对运动距离";
        public static string yAbsoluteStartKey = "Y轴绝对运动开始";
        public static string yAbsoluteSpeedKey = "Y轴绝对运动速度";
        public static string yAbsoluteLocationKey = "Y轴绝对运动目标位置";
        public static string yHighSpeedKey = "Y轴高速运动";
        public static string ySoftUpperLimitKey = "Y轴软正限位";
        public static string ySoftLowerLimitKey = "Y轴软负限位";

        public static string z1CurLocationKey = "Z1轴当前位置";
        public static string z1GratingRulerCurLocationKey = "Z1轴光栅尺当前位置";
        public static string z1CurMotionStatusKey = "Z1轴当前电机状态";
        public static string z1CurSpeedKey = "Z1轴当前速度";
        public static string z1JogSpeedKey = "Z1轴点动速度";
        public static string z1CurStatusKey = "Z1轴当前状态";
        public static string z1RunTypeKey = "Z1轴运行类型";
        public static string z1JogStartKey = "Z1轴正转开始";
        public static string z1JogAntiStartKey = "Z1轴反转开始";
        public static string z1JogRelativeSpeedKey = "Z1点动和相对运动速度";
        public static string z1RelativeDistanceKey = "Z1轴相对运动距离";
        public static string z1AbsoluteStartKey = "Z1轴绝对运动开始";
        public static string z1AbsoluteSpeedKey = "Z1轴绝对运动速度";
        public static string z1AbsoluteLocationKey = "Z1轴绝对运动目标位置";
        public static string z1HighSpeedKey = "Z1轴高速运动";
        public static string z1SoftUpperLimitKey = "Z1轴软正限位";
        public static string z1SoftLowerLimitKey = "Z1轴软负限位";

        public static string z2CurLocationKey = "Z2轴当前位置";
        public static string z2CurSpeedKey = "Z2轴当前速度";
        public static string z2JogSpeedKey = "Z2轴点动速度";
        public static string z2CurStatusKey = "Z2轴当前状态";
        public static string z2CurMotionStatusKey = "Z2轴当前电机状态";
        public static string z2RunTypeKey = "Z2轴运行类型";
        public static string z2JogStartKey = "Z2轴正转开始";
        public static string z2JogAntiStartKey = "Z2轴反转开始";
        public static string z2JogRelativeSpeedKey = "Z2点动和相对运动速度";
        public static string z2RelativeDistanceKey = "Z2轴相对运动距离";
        public static string z2AbsoluteStartKey = "Z2轴绝对运动开始";
        public static string z2AbsoluteSpeedKey = "Z2轴绝对运动速度";
        public static string z2AbsoluteLocationKey = "Z2轴绝对运动目标位置";
        public static string z2HighSpeedKey = "Z2轴高速运动";

        public static string thetaCurLocationKey = "Theta轴当前位置";
        public static string thetaCurSpeedKey = "Theta轴当前速度";
        public static string thetaJogSpeedKey = "Theta轴点动速度";
        public static string thetaCurStatusKey = "Theta轴当前状态";
        public static string thetaCurMotionStatusKey = "Theta轴当前电机状态";
        public static string thetaRunTypeKey = "Theta轴运行类型";
        public static string thetaJogStartKey = "Theta轴正转开始";
        public static string thetaJogAntiStartKey = "Theta轴反转开始";
        public static string thetaJogRelativeSpeedKey = "Theta点动和相对运动速度";
        public static string thetaRelativeDistanceKey = "Theta轴相对运动距离";
        public static string thetaAbsoluteStartKey = "Theta轴绝对运动开始";
        public static string thetaAbsoluteSpeedKey = "Theta轴绝对运动速度";
        public static string thetaAbsoluteLocationKey = "Theta轴绝对运动目标位置";
        public static string thetaHighSpeedKey = "Theta轴高速运动";

        public static string initReplaceLocationKey = "进入换刀画面";
        public static string NoContactHeightMeasurementKey = "是否开启非接触测高";
        public static string HeightMeasurementCompletedKey = "测高完成";
        public static string xReplaceLocationKey = "x轴刀片换刀位置";
        public static string bladeMantanceStatusKey = "刀片维护准备OK";
        public static string yReplaceLocationKey = "Y轴刀片换刀位置";
        public static string xHeightPos = "x轴测高位置设置";
        public static string yHeightPos = "Y轴测高位置设置";
        public static string xBaseMeasurePos = "x轴基准线测量位置设置";
        public static string yBaseMeasurePos = "Y轴基准线测量位置设置";
        public static string bladeSetupKey = "进入测高画面";

        public static string systemInitKey = "整机零点标定";
        public static string systemResetKey = "整机复位";
        public static string systemInitStatusKey = "整机零点标定完成";
        public static string systemStopKey = "停止";
        public static string systemErrorClearKey = "轴错误清除";
        public static string systemErrorResetKey = "轴错误报警已解除";
        public static string securityDoor1StatusKey = "安全门1状态";
        public static string securityDoor2StatusKey = "安全门2状态";
        public static string panelStatusKey = "面板按钮有效";
        public static string bunkeringNumKey = "加油泵计数";

        public static string setupValueKey = "刀片高度数据读取";
        public static string setupNumberKey = "刀片测高计数";
        public static string setupStartKey = "测高开始";

        // 真空
        public static string vacuumStateKey = "真空状态";

        public static string vacuumSwitchStatusKey = "真空打开状态";

        // 工作盘真空
        public static string workVacuumSwitchKey = "工作盘真空on/off";

        // 切削水
        public static string spindleCuttingWaterKey = "主轴切割水";

        public static string workpieceBlowingStatusKey = "相机吹气状态";

        // 主轴冷却水
        public static string spindleCoolingWaterKey = "主轴冷却水";

        public static string spindleManuallyRunStatusKey = "主轴运行";

        // 丝杆润源油
        public static string screwOilKey = "丝杆润源油";

        // 主轴气源
        public static string spindleAirKey = "主轴气源";

        // 主轴转速
        public static string spindleSpeedStatusKey = "主轴转速显示";

        public static string refuelingPumpResetKey = "油泵计数清零";
        public static string buzzerKey = "蜂鸣";
        public static string yellowLightFlashKey = "黄灯闪";

        public static string alignInitKey = "进入影像校准画面";
        public static string alignInitXKey = "x轴校准初始设置";
        public static string alignStatusKey = "校准准备OK";
        public static string alignInitYKey = "y轴校准初始设置";
        public static string alignInitZ1Key = "z1轴校准初始设置";
        public static string alignInitZ2Key = "z2轴校准初始设置";

        public static string fullAutoInitKey = "自动切割画面进入";
        public static string semiAutoInitKey = "半自动切割画面进入";
        public static string confirmParamsKey = "切割设置参数确认";
        public static string cutStatusKey = "切割准备完成";
        public static string cutDirectionAgoKey = "切割方向-前切";
        public static string cutDirectionAfterKey = "切割方向-后切";
        public static string cutStartKey = "切割开始";
        public static string cutStopKey = "切割停止";
        public static string CutStopCompletedKey = "切割停止完成";
        public static string fullAutoCutEndKey = "自动切割结束";
        public static string alignmentStatusKey = "自动校准开始";
        public static string xStartPositionKey = "X轴切割开始位置";
        public static string yStartPositionKey = "Y轴切割开始位置";
        public static string z1StartPositionKey = "z1轴切割开始位置";
        public static string spindleRevKey = "主轴速度";
        public static string cutFaceKey = "自动切割面选择";
        public static string cutNumKey = "切割次数";
        public static string cutFaceAngleKey = "当前切割面角度";
        public static string feedSpeedKey = "X切割速度";
        public static string xLengthKey = "X轴切割结束位置";
        public static string xInitPositionKey = "进入切割模式X轴初始位置";
        public static string yInitPositionKey = "进入切割模式Y轴初始位置";
        public static string z1InitPositionKey = "进入切割模式z1轴初始位置";
        public static string xStopLocationKey = "x轴停机检查位置";
        public static string tStopLocationKey = "y轴停机检查位置";
        public static string z2StopLocationKey = "z2轴停机检查位置";

        public static string enterElectricalKey = "进入修刀画面";
        public static string electricalConfirmParamsKey = "修刀参数设置确认";
        public static string sharpenStartKey = "修刀开始";
        public static string sharpenStopKey = "修刀停止";
        public static string sharpenStatusKey = "电火花修刀准备OK";
        public static string z1StartPosKey = "z1轴修刀开始位置";
        public static string yFrontStartPosKey = "Y轴正面修刀开始位置";
        public static string yBackStartPosKey = "Y轴反面修刀开始位置";
        public static string z1EndPosKey = "z1轴修刀结束位置";
        public static string xStartPosKey = "x轴修刀开始位置";
        public static string zStepAmountKey = "z轴每次修刀下降距离";
        public static string repeatCountKey = "重复次数";
        public static string sharpenSpeedKey = "修刀速度";
        public static string spindleSpeedKey = "修刀主轴速度";
        public static string zLimitPosKey = "Z轴极限位置";
        public static string currentCountKey = "当前修刀次数";
        public static string electricalStatusKey = "修刀工作中";

        public static string flangeGrindingReadyKey = "法兰研磨准备好";
        public static string trimmingCurrentCountKey = "当前研磨次数";
    }
}