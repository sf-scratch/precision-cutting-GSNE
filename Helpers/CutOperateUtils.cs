using Emgu.CV.Dnn;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using 精密切割系统.database.db.modle;
using 精密切割系统.Helpers;
using 精密切割系统.Model.logs;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.ViewModel;

namespace 精密切割系统.Driver
{
    internal class CutOperateUtils
    {
        // true 运行中 false 空闲
        public static bool _disposed = false;

        // 检查状态 false 未检查完成 true 检查完成
        public static bool checkStatus = false;

        // 是否停机检查
        public static bool stopCheckFlag = true;

        // 如果切割方式是Z_KEEP 则是来回重复切 0 A(从左往右切)  1 Z_KEEP 左右来回切
        public static int cutMethod = 0;

        // 是否暂停
        public static bool pauseFlag = false;

        // 交换x轴的开始和结束位置
        public static bool exchangeXPosition = false;

        // 默认检查刀数
        public static int defaultCheckCutNum = 0;

        // 当前刀数
        public static int currentCutLine = 0;

        // 当前面刀数
        public static int chCurrentCutLine = 0;

        // 当前刀数
        public static string tempCurrentCutLine = "0";

        // 切割模式 0 全自动 1 半自动
        public static int cutType = 0;

        // 切割方向 0 前切 1 后切
        public static int cutDirection = -1;

        // x轴停止位置
        public static float xStopLocation = 0;

        // y轴停止位置
        public static float yStopLocation = 0;

        // 预切割信息
        private static List<float> preSpeeds = new List<float>();

        // 是否启用预切割
        public static bool precutFlag = false;

        // 当前切割深度
        public static float _cutDepth;

        // 自定义切割刀数
        public static int _cutLineNum = 0;

        // 当前面总切割刀数
        public static int allRunCutLine = 0;

        // 刀片高度补偿
        // public static float bladeHeightComp = 0;
        // 进刀速度补偿
        public static float feedSpeedComp = 0;

        // 当前进刀速度
        public static float currentFeedSpeed = 0;

        // 是否一直重复循环
        public static bool repeatedFlag = false;

        // 循环次数
        public static int repeatedCount = 0;

        private static float lastYCurrentPosition = -100;

        // theta轴是否校准
        public static bool thetaAlignFlag = false;

        // 是否蜂鸣提示
        public static bool buzzerTipFlag = true;

        // z轴开始位置
        private static float zStartLocation = 0;

        // 暂停超时时间
        public static int stopDelayTime = 90;

        public static float globalXCutStartPosition = 0;
        public static float globalXCutEndPosition = 0;
        public static float globalYCutPosition = 0;
        public static float globalZCutPosition = 0;

        private static bool absoluteCutFlag = false;
        private static RightButton _startBtn;
        private static MainWindow _mainWindow;
        private static PositionCompensationModel axisModel = null;
        private static CancellationTokenSource cts = new CancellationTokenSource();

        // 进行过程中是否校验异常 如果有，则不提示切割完成
        private static bool errorFlag = false;

        // 重新初始化参数
        public static void InitParams(int _cutType, MainWindow mainWindow)
        {
            exchangeXPosition = false;
            precutFlag = false;
            _cutLineNum = 0;
            currentFeedSpeed = 0;
            repeatedCount = 0;
            cutMethod = 0;
            _disposed = false;
            checkStatus = false;
            pauseFlag = false;
            repeatedFlag = false;
            stopCheckFlag = true;
            buzzerTipFlag = true;
            defaultCheckCutNum = 0;
            currentCutLine = 0;
            allRunCutLine = 0;
            zStartLocation = 0;
            cutDirection = errorFlag ? cutDirection : -1;
            cutType = _cutType;
            errorFlag = false;
            GlobalParams.upPosition = -100;
            GlobalParams.upRealPosition = -100;
            lastYCurrentPosition = -100;
            absoluteCutFlag = false;
            stopDelayTime = 90;
            _mainWindow = mainWindow;
        }

        public const string A = "A";
        public const string B_ZKEEP = "B_ZKEEP";

        public static int GetCutMethod(string cutMethod)
        {
            return cutMethod switch
            {
                A => 0,
                B_ZKEEP => 1,
                _ => 0 // 默认值
            };
        }
    }

    public class PrecutItem
    {
        private int cutNum;
        private float cutSpeed;

        public PrecutItem(int _cutNum, float _cutSpeed)
        {
            cutNum = _cutNum;
            cutSpeed = _cutSpeed;
        }
    }
}