using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.Driver;
using 精密切割系统.Extensions;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.plc;
using 精密切割系统.PubSubEvent;
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
        private readonly IEventAggregator? _eventAggregator = PrismUtils.GetEventAggregator();
        private readonly ThetaAlignService _alignService = ThetaAlignService.Instance;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // 确保线程安全
        private readonly DynamicIntervalTimer _intervalTimer = new DynamicIntervalTimer(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(100));
        private MainWindow _mainWindow;
        private RightPage _rightPage;
        // 操作类型 0 菜单进入 1 半自动进入 2 磨刀进入
        private int _operateType = 0;
        // 相机操作对象
        CameraCommon _cameraCommon;
        // 当前页面状态 0 校准 1 对焦 2 测量 根据状态不同，退出的操作不同
        int pageStatus = 0;
        // 读取轴实时位置标识
        bool axisRealTimeFlag = true;
        // 清零后X位置
        string cleanXPosition = "";
        // 清零后Y位置
        string cleanYPosition = "";
        private CancellationTokenSource _cts;

        public MQManualAlignmentConf()
        {
            InitializeComponent();
            _mainWindow = Application.Current.MainWindow as MainWindow ?? new MainWindow(); 
            _cts = new CancellationTokenSource();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_cts.IsCancellationRequested)
            {
                _cts = new CancellationTokenSource();
            }
            _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Subscribe(ReceivedMessage, ThreadOption.UIThread);
            // 加载右边和底部按钮
            _rightPage = _mainWindow.rightFrame.Content as RightPage ?? new RightPage();
            _rightPage.PanelAction.Visibility = Visibility.Visible;
            _rightPage.btnBack.Visibility = Visibility.Visible;
            _rightPage.btnBack.BackFlag = false;
            _rightPage.btnBack.SetRightClickedHandler(BackClickHandle);
            _rightPage.btnSure.Visibility = Visibility.Visible;
            _rightPage.btnSure.SetRightClickedHandler(SureHandler);
            _mainWindow.UpdateOperatePage(OperateData.GetManualAlignmentOperate(), ClickHandler, TouchLeaveHandler, TouchDownHandler);
            string type = QueryUtils.GetValueFromQueryParams(this, "type");
            if (!string.IsNullOrEmpty(type))
            {
                _operateType = int.Parse(type);
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

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Unsubscribe(ReceivedMessage);
            axisRealTimeFlag = false;
            _intervalTimer.Dispose();
        }

        private void ReceivedMessage(MessageModel model)
        {
            RealTimeInfo.Messages.Add(model);
        }

        bool confirmFlag = false;
        private async void SureHandler(object? sender, bool e)
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
            await SetLowSpeedAsync();
            ToNextPage();
        }

        private async void BackClickHandle(object? sender, bool e)
        {
            _cts.Cancel();
            await SetLowSpeedAsync();
            ToNextPage();
        }

        private async Task SetLowSpeedAsync()
        {
            // 设置为低速
            await PlcControl.tagControl.Xaxis.SetHighSpeedAsync(0);
            await PlcControl.tagControl.Yaxis.SetHighSpeedAsync(0);
            await PlcControl.tagControl.Z1axis.SetHighSpeedAsync(0);
            await PlcControl.tagControl.Z2axis.SetHighSpeedAsync(0);
            await PlcControl.tagControl.ThetaAxis.SetHighSpeedAsync(0);
        }

        private void ToNextPage()
        {
            if (pageStatus == 0)
            {
                // 等于0 则跳回菜单 等于1 则跳回切割
                if (_operateType == 0)
                {
                    _mainWindow?.NavigateToPage("MainMenu");
                }
                else if (_operateType == 1)
                {
                    _mainWindow?.NavigateToPage("Pages/F2_ManualOperation/MQSemiAutomaticCuttingConf", "type=1"); // type = 1 校准跳转
                }
                else if (_operateType == 2)
                {
                    string id = QueryUtils.GetValueFromQueryParams(this, "Id");
                    string flag = QueryUtils.GetValueFromQueryParams(this, "Flag");
                    string bladeLotID = QueryUtils.GetValueFromQueryParams(this, "BladeLotID");
                    _mainWindow?.NavigateToPage("Pages/F4_BladeMaintenance/BmSharpenParameterForm", "Id=" + id + "&Flag=" + flag + "&BladeLotID=" + bladeLotID);
                }
                else if (_operateType == 3)
                {
                    _mainWindow?.NavigateToPage("Pages/F7_ElectricSpark/AutoAlignPosition");
                }
            }
            else if (pageStatus == 2)
            {
                // absolutePositionPanel.Visibility = Visibility.Collapsed;
                cleanPositonPanel.Visibility = Visibility.Collapsed;
                channelPanel.Visibility = Visibility.Visible;
                channelTipsPanel.Visibility = Visibility.Visible;
                _mainWindow?.UpdateOperatePage(OperateData.GetManualAlignmentOperate(), ClickHandler, TouchLeaveHandler, TouchDownHandler);
                pageStatus = 0;
                titleName.Content = "单一切割面校准 (1.1)";
                _rightPage.btnSure.Visibility = Visibility.Visible;
            }

        }

        public async void ClickHandler(object? sender, int code)
        {
            switch (code)
            {
                case 2442:
                    {
                        await _semaphore.ExecuteAsync(async () =>
                        {
                            try
                            {
                                await using var timeoutToken = TaskUtils.GetTimeoutCancellationToken(TimeSpan.FromSeconds(120), _cts.Token);
                                var result = await AutoCutUtils.AutoFocusAsync(_eventAggregator, timeoutToken.Token);
                                if (!result.IsSuccess)
                                {
                                    MaterialSnack(result.Message, SnackType.WARNING, default, _eventAggregator);
                                    return;
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                MaterialSnack("精细对焦超时！", SnackType.WARNING, default, _eventAggregator);
                            }
                        }, "精细对焦");
                    }
                    break;
                case 2441:
                    {
                        await _semaphore.ExecuteAsync(async () =>
                        {
                            try
                            {
                                await using var timeoutToken = TaskUtils.GetTimeoutCancellationToken(TimeSpan.FromSeconds(120), _cts.Token);
                                var result = await AutoFocusService.GlobalFocusAsync(_eventAggregator, timeoutToken.Token);
                                if (!result.IsSuccess)
                                {
                                    MaterialSnack(result.Message, SnackType.WARNING, default, _eventAggregator);
                                    return;
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                MaterialSnack("全局对焦超时！", SnackType.WARNING, default, _eventAggregator);
                            }
                        }, "全局对焦");
                    }
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
                    _mainWindow.UpdateOperatePage(OperateData.GetMeasurementOperate(), ClickHandler, TouchLeaveHandler, TouchDownHandler);
                    pageStatus = 2;
                    cleanXPosition = PlcControl.plc.GetPlcValueString(DeviceKey.curLocationKey);
                    cleanYPosition = PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey);
                    titleName.Content = "测量";
                    _rightPage.btnSure.Visibility = Visibility.Collapsed;
                    break;
                case 2570:
                    // 位置清零
                    cleanXPosition = PlcControl.plc.GetPlcValueString(DeviceKey.curLocationKey);
                    cleanYPosition = PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey);
                    break;
                case 2433:
                    _cameraCommon.localBitmap.ToMat().SaveImage($"C:\\MySpace\\Dev\\ProjectXiHua\\precision-cutting-321\\bin\\x64\\Debug\\testImage\\{DateTime.Now.Ticks}.jpg");
                    //MaterialSnack("识别中...", SnackType.WARNING, 0);
                    //await AutoCutUtils.FineTuneAxisYAsync();
                    //await AutoCutUtils.UpdateCameraCommonLineAsync();
                    //MaterialSnack("识别完成！", SnackType.SUCCESS);
                    break;
                default:
                    break;
            }
        }

        private void LoadPosition()
        {
            Task.Run(async () =>
            {
                while (axisRealTimeFlag)
                {
                    var axisPostion = await AutoCutUtils.GetAxisPositionAsync();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        xAbsolutePosition.Text = axisPostion.X?.ToString("F4");
                        yAbsolutePosition.Text = axisPostion.Y?.ToString("F4");
                        zAbsolutePosition.Text = axisPostion.Z1?.ToString("F4");
                        z2AbsolutePosition.Text = axisPostion.Z2?.ToString("F4");
                        thetaAbsolutePosition.Text = axisPostion.Theta?.ToString("F4");
                        // 显示清零后位置
                        xCleanPosition.Text = (Tools.GetDoubleStringValue(xAbsolutePosition.Text) - Tools.GetDoubleStringValue(cleanXPosition)).ToString("F4");
                        yCleanPosition.Text = (Tools.GetDoubleStringValue(yAbsolutePosition.Text) - Tools.GetDoubleStringValue(cleanYPosition)).ToString("F4");
                    });
                    await Task.Delay(100);
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
            if (_cameraCommon is not null)
            {
                cutWidth.Text = Tools.FormatDecimalString((_cameraCommon.CutMarkWidth / 1000).ToString(), 4);
                edgesWidth.Text = Tools.FormatDecimalString((_cameraCommon.EdgeChipWidth / 1000).ToString(), 4);
            }
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
                    _intervalTimer.Stop();
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
                    DisposeDatumLine(code);
                    _intervalTimer.RegisterAction(() => DisposeDatumLine(code));
                    _intervalTimer.Start();
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
    }
}
