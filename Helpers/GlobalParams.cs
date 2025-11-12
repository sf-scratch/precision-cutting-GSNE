using MathNet.Numerics;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.Driver;
using 精密切割系统.Model.cut;

namespace 精密切割系统.Helpers
{
    public static class GlobalParams
    {
        // true 在线版本 false 0
        public static bool OnlineFlag { get; set; } = false;

        // 是否上传MES
        public static bool OnlineMES { get; set; } = true;

        // 是否带Theta轴
        public static bool HasTheta { get; set; } = true;

        public static string DecimalStringFormat { get; set; } = "F5";

        // 当前页面是否是首页
        public static bool currentPageIsHome = false;

        // 全局运行参数 如果有参数在运行，则其它按钮不能操作
        public static bool globalRunFlag = false;

        // 全局运行中，可以操作的右侧按钮
        public static List<int> globalRunEnableRightBtnCodes = new List<int>();

        // 全局运行中，可以操作的右侧按钮
        public static HashSet<int> globalRunEnableOperateBtnCodes = new HashSet<int>();

        // 全局运行中，可以操作的主菜单按钮
        public static List<int> globalEnableMainBtnCodes = new List<int>();

        // 当前页面操作按钮列表
        public static List<OperateBean> currentOperateBeanList = new List<OperateBean>();

        // 全局参数，当前是否标定
        public static bool systemInitFlag = false;

        // 是否开启运动补偿
        public static bool runCompFlag = true;

        // 全局参数配置 0 设备参数 1 测高参数
        public static int GlobalParamsConfType = 1;

        // 默认光源亮度 第一台 0.8 第二台 0.07
        public static double intensityRatio = 0.95;

        public static double lowIntensityRatio = 0.02;
        public static double RingIntensityRatio = 0.02;

        // 使用的光源通道 第一台 1  第二台 4
        public static int LightIntensityChannel = 3;

        // 低倍光源通道
        public static int LowLightIntensityChannel = 3;

        // 环光光源通道
        public static int RingLightIntensityChannel = 1;

        // 最后一次聚焦位置
        public static float lastFocusZ2Location = 0.85f;

        // 刀片高度 39.9215  40.07  40.052  第二台 48.702  第一台 39.219
        public static float bladeHeight = 0;

        // 最后一次Y轴位置
        public static float lastAlignmentYLocation = 0;

        // 换刀后总共切割刀数
        public static int cutAllNum = 0;

        // 换刀后总共切割距离
        public static float cutAllDistance = 0;

        // 测高后总共切割刀数
        public static int heightCutAllNum = 0;

        // 测高后总共切割距离
        public static float heightCutAllDistance = 0;

        // 相机像素宽高比
        public static double aspectRatio = 1.1953125;

        // 相机图片原始宽度 相机图片尺寸 765 640  真实尺寸 710 848.671875  根据像素换算成真实尺寸 640/
        public static double cameraPixelWidth = 2448;

        // 相机图片原始高度
        public static double cameraPixelHeight = 2048;

        // 最后一次刀痕宽度 cutWdith edgesWidth
        public static float cutWidth = 180;

        // 最后一次崩边宽度
        public static float edgesWidth = 200;

        // 切割深度偏移量 由于测高不准  0.085f
        public static float cutDepthOffset = 0;

        // 校验模式状态超时时间
        public static int checkModelStatusTimeoutSeconds = 15;

        // 切割状态 0 未开始 1 切割中 2 暂停中
        public static int cutStatusInfo = 0;

        //修刀
        // 全局修刀总数
        public static int allDressersNum = 0;

        // 全局修刀总长度
        public static int clearDressersNum = 0;

        // 切割相关
        // 当前CH
        public static string currentCH = CH1;

        // CH1 开始切割位置
        public static float ch1CutStartPosition;

        // CH2 开始切割位置
        public static float ch2CutStartPosition;

        // CH3 开始切割位置
        public static float ch3CutStartPosition;

        // CH4 开始切割位置
        public static float ch4CutStartPosition;

        // Theta轴 拉直角度
        public static float calibrationAngle = 0;

        public const string CH1 = "Ch 1";
        public const string CH2 = "Ch 2";
        public const string CH3 = "Ch 3";
        public const string CH4 = "Ch 4";

        // 修刀位置 Y轴前端：129.985  Y轴后端：128.24  Z轴设定位置：39.005    127.98224  129.30324

        //==========================位置校准相关=======================
        // theta轴切割中心点位置 X轴  184.289 177  第一台  184.289f  第二台 210
        public static float thetaCenterLocationX = 0f;

        // 第二台
        // public static float thetaCenterLocationX = 220f;

        // theta轴中心点位置 - Y轴 49.707   53.73523   62.386
        public static float thetaCenterLocationY = 0f;

        /// <summary>
        /// 正常步进距离
        /// </summary>
        public static readonly float NormalStepDistance = 0.15f;

        /// <summary>
        /// 跳跃步进距离
        /// </summary>
        public static readonly float JumpStepDistance = 0.3f;

        /// <summary>
        /// 在切割几次后检测
        /// </summary>
        public static readonly int CheckMarksCutTimes = 15;

        /// <summary>
        /// 单刀磨损量
        /// </summary>
        public static readonly float SingleBladeWear = 0.01f;

        /// <summary>
        /// 刀片抬起高度
        /// </summary>
        public static readonly float BladeLiftingHeight = 1f;

        /// <summary>
        /// 工件半径
        /// </summary>
        public static readonly float WorkpieceRadius = 75;

        /// <summary>
        /// 工件中心点到theta轴中心点距离
        /// <summary>
        public static readonly float CenterDistance = 2f;

        /// <summary>
        /// 磨刀板尺寸
        /// </summary>
        public static DataRectangleF SharpenRect = new DataRectangleF(Appsettings.ThetaCenterPoint.X - 33, Appsettings.ThetaCenterPoint.Y - 73, 70, 70);

        /// <summary>
        /// 非接触测高位置到工作台的z1轴高度
        /// </summary>
        public static readonly float NonContactHeightMeasurementToWorkbenchZ1 = 8.8f;

        // x轴默认速度
        public const float XDefaultSpeed = 10;

        // y轴默认速度
        public const float YDefaultSpeed = 10;

        // z1轴默认速度
        public const float Z1DefaultSpeed = 3;

        // z2相机轴默认速度
        public const float Z2DefaultSpeed = 2f;

        // thetaθ轴默认速度
        public const float ThetaDefaultSpeed = 20;

        // theta轴相机中心点位置 X轴 28.89837  17.8
        public static float thetaCameraLocationX = 17.8f;

        // theta轴相机中心点位置 Y轴 28.89837 -29
        public static float thetaCameraLocationY = -29f;

        // Z轴切割抬起高度
        public static float zCutRaisedHeight = 1;

        // X轴切割位置和相机位置的偏移量  125
        public static float cameraToCutXOffset = 105f;

        // 相机焦点和切割位置的偏移量
        public static float cameraToCutYOffset = 10f;

        // 切割Z1轴最大安全距离 要根据每台来设置，做成可以配置的参数
        public static float cutZ1MaxLocation = 20f;

        // 相机和切割点的X轴的偏移量
        public static float cameraOffsetX = 10;

        // 相机和切割点的Y轴的偏移量 第一台：8.038814 第二台：4.617499
        public static float cameraOffsetY = 8.163f;

        // 第二台
        // public static float cameraOffsetY = 4.617499f;
        // 聚焦初始位置
        public static double initPosition = 35.5;

        // 工作盘聚焦位置 第一台 37.36  第二台 38.095
        public static double workDiscFocusPosition = 37.36;

        // 第二台
        // public static double workDiscFocusPosition = 38.095;
        // 聚焦比率
        public static float focusRatio = 12;

        // 高速的倍率
        public static double multipleNum = 0.01;

        // 全局高度补偿
        public static float depthComp = 0.000f;

        // Z轴补偿
        public static int zAxisCompNum = 0;

        public static float zAxisCompValue = 0;

        // 静态事件
        public static event EventHandler ValueChanged;

        // 全局-高速还是低速  0 低速 1 高速
        public static int _heightSpeedStatus = 0;

        // 静态属性
        public static int heightSpeedStatus
        {
            get => _heightSpeedStatus;
            set
            {
                if (_heightSpeedStatus != value)
                {
                    _heightSpeedStatus = value;
                    OnValueChanged();
                }
            }
        }

        //==========================校准相关End=======================

        // ===============轴速度设置=================
        // x轴默认速度
        public static string xDefaultSpeed = "10";

        // y轴默认速度
        public static string yDefaultSpeed = "10";

        // z2相机轴默认速度
        public static string z2DefaultSpeed = "2";

        // thetaθ轴默认速度
        public static string thetaDefaultSpeed = "20";

        // X轴扫描速度（mm/s）
        public static string xScanSpeed;

        // X轴扫描距离（mm）
        public static string xScanDistance;

        // Y轴扫描速度（mm/s）
        public static string yScanSpeed;

        // Y轴扫描距离（mm）
        public static string yScanDistance;

        // Z1轴扫描速度（mm/s）
        public static string z1ScanSpeed = "10";

        // Z1轴扫描距离（mm）
        public static string z1ScanDistance = "0.001";

        // Z2轴扫描速度（mm/s）
        public static string z2ScanSpeed = "10";

        // Z2轴扫描距离（mm）
        public static string z2ScanDistance = "0.001";

        // θ轴扫描速度（°/s）
        public static string thetaScanSpeed = "20";

        // θx轴扫描距离（°）
        public static string thetaScanDistance;

        // 低速操作时间
        public static string moveLowTime;

        // 高速操作时间
        public static string moveHighTime;

        // θ轴屏幕移动量
        public static string thetaScreenIndex;

        // X轴屏幕移动量
        public static string xScreenIndex;

        // Y轴屏幕移动量
        public static string yScreenIndex;

        // θ轴屏幕移动速度 ？从哪里设置
        public static string thetaScreenSpeed = "60";

        // X轴屏幕移动速度 ？从哪里设置
        public static string xScreenSpeed;

        // Y轴屏幕移动速度 ？从哪里设置
        public static string yScreenSpeed;

        // 上一次设置位置
        public static float upPosition = -100;

        // 上一次光栅尺
        public static float upRealPosition = -100;

        public static float allDeepValue = 0;

        // 位置小数点位数
        public static int decimalPlaces = 6;

        // ===============轴速度设置End=================

        // ====================设备相关传感器状态======================
        // 真空
        public static bool vacuumStatus = false;

        // 切削水
        public static bool spindleCuttingWaterStatus = false;

        // 主轴冷却水
        public static bool spindleCoolingWaterStatus = false;

        // 丝杆润源油
        public static bool screwOilStatus = false;

        // 主轴气源
        public static bool spindleAirStatus = false;

        // 主轴转速
        public static string spindleSpeed = "0";

        // ===================== 各模式初始位置 ================
        // 测高位置
        // x轴初始位置
        public static string bladeSetupInitX;

        // y轴初始位置
        public static string bladeSetupInitY;

        // z1轴初始位置
        public static string bladeSetupInitZ1;

        // z2轴初始位置
        public static string bladeSetupInitZ2;

        // 电火花修刀位置

        // 校准位置
        // x轴初始位置
        public static string alignInitX;

        // y轴初始位置
        public static string alignInitY;

        // z1轴初始位置
        public static string alignInitZ1;

        // z2轴初始位置
        public static string alignInitZ2;

        // 切割位置
        // x轴初始位置
        public static string cutInitX;

        // y轴初始位置
        public static string cutInitY;

        // z1轴初始位置
        public static string cutInitZ1;

        // z2轴初始位置
        public static string cutInitZ2;

        // 刀片更换位置
        // x轴初始位置
        public static string cutReplaceInitX;

        // y轴初始位置
        public static string cutReplaceInitY;

        // z1轴初始位置
        public static string cutReplaceInitZ1;

        // z2轴初始位置
        public static string cutReplaceInitZ2;

        // 触发事件
        private static void OnValueChanged()
        {
            ValueChanged?.Invoke(null, EventArgs.Empty);
        }

        // 日志类型枚举
    }
}