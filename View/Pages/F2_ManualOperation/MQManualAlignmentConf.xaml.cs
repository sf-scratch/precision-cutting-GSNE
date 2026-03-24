using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using 精密切割系统.Assets.config.buttom;
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
        private DynamicIntervalTimer _intervalTimer;
        private MainWindow _mainWindow;
        private RightPage _rightPage;

        // 操作类型 0 菜单进入 1 半自动进入 2 磨刀进入
        private int _operateType = 0;

        private CameraCommon _cameraCommon;
        private CancellationTokenSource _cts;
        private bool _isConfirmFocusPosition = false;

        public MQManualAlignmentConf()
        {
            InitializeComponent();
            _mainWindow = Application.Current.MainWindow as MainWindow ?? new MainWindow();
            _cts = new CancellationTokenSource();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _intervalTimer = new DynamicIntervalTimer(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(30));
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
            channelNo.Text = CurrentUtils.GetCurrentCh();
            CameraCommon? cameraCommon = AutoCutUtils.GetCameraCommon();
            if (cameraCommon is null)
            {
                MaterialSnack("相机获取失败！", SnackType.WARNING);
                return;
            }
            _cameraCommon = cameraCommon;
            if (_operateType == 0)
            {
                MaterialSnack("进入校准模式成功！", SnackType.SUCCESS);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Unsubscribe(ReceivedMessage);
            _intervalTimer.Dispose();
        }

        private void ReceivedMessage(MessageModel model)
        {
            RealTimeInfo.Messages.Add(model);
        }

        private bool confirmFlag = false;

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
            _operateType = 1;
            SpeedManager.IsHighSpeed = false;
            ToNextPage();
        }

        private async void BackClickHandle(object? sender, bool e)
        {
            _cts.Cancel();
            SpeedManager.IsHighSpeed = false;
            ToNextPage();
        }

        private void ToNextPage()
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

        public async void ClickHandler(object? sender, int code)
        {
            switch (code)
            {
                // 自动对焦
                case 2441:
                    {
                        await _semaphore.ExecuteAsync(async () =>
                        {
                            try
                            {
                                await using var timeoutToken = TaskUtils.GetTimeoutCancellationToken(TimeSpan.FromSeconds(120), _cts.Token);
                                var result = await AutoFocusService.GlobalFocusAsync(_isConfirmFocusPosition ? Appsettings.FocusClearZ : null, _eventAggregator, timeoutToken.Token);
                                if (!result.IsSuccess)
                                {
                                    MaterialSnack(result.Message, SnackType.WARNING, default, _eventAggregator);
                                    return;
                                }
                                MaterialSnack("对焦完成", SnackType.SUCCESS, 2, _eventAggregator);
                            }
                            catch (OperationCanceledException)
                            {
                                if (_cts.IsCancellationRequested)
                                {
                                    MaterialSnack("对焦已取消！", SnackType.WARNING, default, _eventAggregator);
                                }
                                else
                                {
                                    MaterialSnack("对焦超时！", SnackType.WARNING, default, _eventAggregator);
                                }
                            }
                        }, "对焦");
                    }
                    break;

                // 确认Z1对焦位置
                case 2445:
                    _isConfirmFocusPosition = true;
                    Appsettings.FocusClearZ = await PlcControl.tagControl.Z2axis.GetCurrentLocationAsync();
                    MaterialSnack($"对焦位置已确认：{Appsettings.FocusClearZ}mm！", SnackType.WARNING, default, _eventAggregator);
                    break;

                // Theta垂直校准
                case 2443:
                    await _alignService.ThetaVerticalAlignAsync();
                    break;

                // Theta水平校准
                case 2453:
                    await _alignService.ThetaHorizontalAlignAsync();
                    break;

                // 倍率变更
                case 2479:
                    commonDimming.InitData();
                    break;

                // 测量
                case 2050:
                    ContainerLocator.Container.Resolve<IRegionManager>().RequestNavigate(RegionName.MainRegion, nameof(Measurement));
                    break;

                // 辅助线
                case 2051:
                    ContainerLocator.Container.Resolve<IRegionManager>().RequestNavigate(RegionName.MainRegion, nameof(AuxiliaryLine));
                    break;

                // 切割道中心线
                case 2052:
                    ContainerLocator.Container.Resolve<IRegionManager>().RequestNavigate(RegionName.MainRegion, nameof(FindCenterLine));
                    break;

                // 位置清零
                case 2570:
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

        private void DisposeDatumLine(int code)
        {
            if (code == 2040)
            {
                _cameraCommon?.SetEdgeWidth(-CameraOperateUtils.DatumLineChangeStep, 2);
            }
            else if (code == 2041)
            {
                _cameraCommon?.SetEdgeWidth(CameraOperateUtils.DatumLineChangeStep, 2);
            }
            else if (code == 2407)
            {
                _cameraCommon?.SetCutMarkWidth(-CameraOperateUtils.DatumLineChangeStep, 2);
            }
            else if (code == 2408)
            {
                _cameraCommon?.SetCutMarkWidth(CameraOperateUtils.DatumLineChangeStep, 2);
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
            }
        }

        private async void TouchDownHandler(object? sender, int code)
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
                    await PlcControl.tagControl.Z2axis.SetHighSpeedAsync(0);
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