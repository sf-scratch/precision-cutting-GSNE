using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.common;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;
using static 精密切割系统.Helpers.MaterialSnackUtils;

namespace 精密切割系统.View.Pages.F2_ManualOperation
{
    /// <summary>
    /// MQManualAlignmentConf.xaml 的交互逻辑
    /// </summary>
    public partial class MQManualAlignmentConf : Page
    {
        private readonly ThetaAlignService _alignService;
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        // 操作类型 0 菜单进入 1 半自动进入 2 磨刀进入
        private int _operateType = 0;
        // 相机操作对象
        CameraCommon _cameraCommon;
        // 手动调整基准线标识
        bool adjustDatumLineFlag = false;
        // 创建一个定时器
        System.Timers.Timer? _timer = null;
        //获取参数
        string IdStr;
        string Flag;
        string BladeLotID;
        // 当前页面状态 0 校准 1 对焦 2 测量 根据状态不同，退出的操作不同
        int pageStatus = 0;
        // 读取轴实时位置标识
        bool axisRealTimeFlag = true;
        // 清零后X位置
        string cleanXPosition = "";
        // 清零后Y位置
        string cleanYPosition = "";

        public MQManualAlignmentConf()
        {
            InitializeComponent();
            mainWindow = Application.Current.MainWindow as MainWindow;
            _alignService = ThetaAlignService.Instance;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // 加载右边和底部按钮
            rightPage = mainWindow.rightFrame.Content as RightPage;
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BackClickHandle);
            rightPage.btnSure.Visibility = Visibility.Visible;
            rightPage.btnSure.SetRightClickedHandler(SureHandler);
            mainWindow.UpdateOperatePage(OperateData.GetManualAlignmentOperate(), ClickHandler, TouchLeaveHandler, TouchDownHandler);
            string type = QueryUtils.GetValueFromQueryParams(this, "type");
            if (!string.IsNullOrEmpty(type))
            {
                _operateType = int.Parse(type);
            }
            if (_operateType == 2)
            {
                //获取参数
                IdStr = QueryUtils.GetValueFromQueryParams(this, "Id");
                Flag = QueryUtils.GetValueFromQueryParams(this, "Flag");
                BladeLotID = QueryUtils.GetValueFromQueryParams(this, "BladeLotID");
            }
            // 设置相关参数
            channelNo.Text = CurrentUtils.GetCurrentConfiguration().ChannelNum;
            CameraCommon? cameraCommon = AutoCutUtils.GetCameraCommon();
            if (cameraCommon is null)
            {
                MaterialSnack("相机获取失败！", SnackType.WARNING);
                return;
            }
            _cameraCommon = cameraCommon;
            if (_operateType == 0)
            {
                MaterialSnack("进入校准模式成功！", SnackType.WARNING);
            }
            cutWidth.Text = Tools.FormatDecimalString((cameraCommon.CutMarkWidth / 1000).ToString(), 4);
            edgesWidth.Text = Tools.FormatDecimalString((cameraCommon.EdgeChipWidth / 1000).ToString(), 4);
            LoadPosition();
        }

        bool confirmFlag = false;
        private void SureHandler(object? sender, bool e)
        {
            // 判断是否Theta轴拉直 等于0 说明没有做Theta轴校准
            if (_alignService.CurrentThetaAlignStatus != ThetaAlignStatus.Completed && !confirmFlag)
            {
                MaterialSnack("请先进行校准，再次按下开始将强制切割！", SnackType.WARNING);
                confirmFlag = true;
                return;
            }
            if (_alignService.CurrentThetaAlignStatus == ThetaAlignStatus.Horizontal || _alignService.CurrentThetaAlignStatus == ThetaAlignStatus.Vertical)
            {
                MaterialSnack("请再次点击Theta轴校准，完成校准！", SnackType.WARNING);
                return;
            }
            // 根据当前的切割面，设置开始切割位置
            _operateType = 1;
            ToNextPage();
        }

        private void BackClickHandle(object? sender, bool e)
        {
            ToNextPage();
        }

        private void ToNextPage()
        {
            if (pageStatus == 0)
            {
                // 等于0 则跳回菜单 等于1 则跳回切割
                if (_operateType == 0)
                {
                    mainWindow?.NavigateToPage("MainMenu");
                }
                else if (_operateType == 1)
                {
                    mainWindow?.NavigateToPage("Pages/F2_ManualOperation/MQSemiAutomaticCuttingConf", "type=1"); // type = 1 校准跳转
                }
                else if (_operateType == 2)
                {
                    mainWindow?.NavigateToPage("Pages/F4_BladeMaintenance/BmSharpenParameterForm", "Id=" + IdStr + "&Flag=" + Flag + "&BladeLotID=" + BladeLotID);
                }
                else if (_operateType == 3)
                {
                    mainWindow?.NavigateToPage("Pages/F7_ElectricSpark/AutoAlignPosition");
                }
            }
            else if (pageStatus == 2)
            {
                // absolutePositionPanel.Visibility = Visibility.Collapsed;
                cleanPositonPanel.Visibility = Visibility.Collapsed;
                channelPanel.Visibility = Visibility.Visible;
                channelTipsPanel.Visibility = Visibility.Visible;
                mainWindow?.UpdateOperatePage(OperateData.GetManualAlignmentOperate(), ClickHandler, TouchLeaveHandler, TouchDownHandler);
                pageStatus = 0;
                titleName.Content = "单一切割面校准 (1.1)";
                rightPage.btnSure.Visibility = Visibility.Visible;
            }

        }

        public async void ClickHandler(object? sender, int code)
        {
            switch (code)
            {
                case 2442:
                    await AutoCutUtils.AutoFocusAsync();
                    break;
                case 2441:
                    await AutoCutUtils.AutoCoarseFocusAsync();
                    break;
                case 2445:
                    Appsettings.FocusClearZ = await PlcControl.tagControl.Z2axis.GetCurrentLocationAsync();
                    break;
                case 2443:
                    await _alignService.ThetaVerticalAlignAsync();
                    break;
                case 2453:
                    await _alignService.ThetaHorizontalAlignAsync();
                    break;
                case 2479:
                    // 倍率变更
                    _cameraCommon.ChangeCamera();
                    Thread.Sleep(100);;
                    commonDimming.InitData();
                    break;
                case 2050:
                    // 测量
                    // absolutePositionPanel.Visibility = Visibility.Visible;
                    cleanPositonPanel.Visibility = Visibility.Visible;
                    channelPanel.Visibility = Visibility.Collapsed;
                    channelTipsPanel.Visibility = Visibility.Collapsed;
                    mainWindow.UpdateOperatePage(OperateData.GetMeasurementOperate(), ClickHandler, TouchLeaveHandler, TouchDownHandler);
                    pageStatus = 2;
                    cleanXPosition = PlcControl.plc.GetPlcValueString(DeviceKey.curLocationKey);
                    cleanYPosition = PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey);
                    titleName.Content = "测量";
                    rightPage.btnSure.Visibility = Visibility.Collapsed;
                    break;
                case 2570:
                    // 位置清零
                    cleanXPosition = PlcControl.plc.GetPlcValueString(DeviceKey.curLocationKey);
                    cleanYPosition = PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey);
                    break;
                case 2433:
                    MaterialSnack("识别中...", SnackType.WARNING, 0);
                    await AutoCutUtils.FineTuneAxisYAsync();
                    await AutoCutUtils.UpdateCameraCommonLineAsync();
                    MaterialSnack("识别完成！", SnackType.SUCCESS);
                    break;
                default:
                    break;
            }
        }

        private void LoadPosition()
        {
            Task.Run(() =>
            {
                while (axisRealTimeFlag)
                {
                    Application.Current.Dispatcher.Invoke(async () =>
                    {
                        // 显示实时位置
                        xAbsolutePosition.Text = (await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync())?.ToString("F4");
                        yAbsolutePosition.Text = (await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync())?.ToString("F4");
                        zAbsolutePosition.Text = (await PlcControl.tagControl.Z1axis.GetCurrentLocationAsync())?.ToString("F4");
                        z2AbsolutePosition.Text = (await PlcControl.tagControl.Z2axis.GetCurrentLocationAsync())?.ToString("F4");
                        thetaAbsolutePosition.Text = (await PlcControl.tagControl.ThetaAxis.GetCurrentLocationAsync())?.ToString("F4");
                        // 显示清零后位置
                        xCleanPosition.Text = (Tools.GetDoubleStringValue(xAbsolutePosition.Text)
                            - Tools.GetDoubleStringValue(cleanXPosition)).ToString("F4");
                        yCleanPosition.Text = (Tools.GetDoubleStringValue(yAbsolutePosition.Text)
                            - Tools.GetDoubleStringValue(cleanYPosition)).ToString("F4");
                    });
                    Thread.Sleep(100);
                }
            });
        }

        private void DisposeDatumLine(int code)
        {
            if (code == 2040)
            {
                _cameraCommon?.SetEdgeWidth(-1, 2);
            }
            else if (code == 2041)
            {
                _cameraCommon?.SetEdgeWidth(1, 2);
            }
            else if (code == 2407)
            {
                _cameraCommon?.SetCutMarkWidth(-1, 2);
            }
            else if (code == 2408)
            {
                _cameraCommon?.SetCutMarkWidth(1, 2);
            }
            cutWidth.Text = Tools.FormatDecimalString((_cameraCommon.CutMarkWidth / 1000).ToString(), 4);
            edgesWidth.Text = Tools.FormatDecimalString((_cameraCommon.EdgeChipWidth / 1000).ToString(), 4);
        }

        private void TouchLeaveHandler(object? sender, int code)
        {
            switch (code)
            {
                case 2407:
                // 基准线调窄
                case 2408:
                // 基准线调宽
                case 2040:
                // 崩边调窄
                case 2041:
                    adjustDatumLineFlag = false;
                    if (_timer != null)
                    {
                        _timer.Stop();
                        _timer.Dispose();
                        _timer = null;
                    }
                    break;
                case 2466:
                case 2477:
                    PlcControl.tagControl.Z2axis.StopMove();
                    break;
            }
        }
        private void TouchDownHandler(object? sender, int code)
        {
            switch (code)
            {
                case 2407:
                // 基准线调窄
                case 2408:
                // 基准线调宽
                case 2040:
                // 崩边调窄
                case 2041:
                    DisposeDatumLine(code); // 初始调用 DisposeDatumLine
                    adjustDatumLineFlag = true;
                    if (_timer != null)
                    {
                        _timer.Stop();
                    }
                    // 创建定时器
                    _timer = new System.Timers.Timer
                    {
                        Interval = 500, // 初始延迟 500 毫秒
                        AutoReset = false // 每次触发后需要手动重新启动
                    };
                    _timer.Elapsed += (sender, e) =>
                    {
                        if (_timer != null)
                        {
                            if (!adjustDatumLineFlag)
                            {
                                _timer.Stop();
                                _timer.Dispose(); // 释放资源
                                return;
                            }

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                DisposeDatumLine(code);
                            });

                            // 重新设置间隔为 100 毫秒并重新启动定时器
                            _timer.Interval = 100;
                            _timer.Start();
                        }
                    };

                    _timer.Start(); // 启动定时器
                    break;
                case 2466:
                case 2477:
                    // Z2轴上升
                    // Z2轴上升
                    // 设置Z轴为低速
                    PlcControl.tagControl.Z2axis.SetHighSpeed("0");
                    PlcControl.tagControl.Z2axis.SetRelativeSpeed("0.2");
                    PlcControl.tagControl.Z2axis.StartJog(code == 2466 ? 1 : 0);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 设置当前通道
        /// </summary>
        /// <param name="channelNoValue"></param>
        public void SetChannelNo(string channelNoValue)
        {
            channelNo.Text = channelNoValue;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            axisRealTimeFlag = false;
        }
    }
}
