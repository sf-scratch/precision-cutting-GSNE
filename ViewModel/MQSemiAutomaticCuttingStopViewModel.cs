using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.Extensions;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.View.Pages.common;
using 精密切割系统.View.Pages.F2_ManualOperation;
using 精密切割系统.View.Pages.operate;
using static 精密切割系统.View.Pages.operate.OperatePage;

namespace 精密切割系统.ViewModel
{
    internal class MQSemiAutomaticCuttingStopViewModel : CustomBindableBase
    {
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly SemiAutoCutService _semiAutoCutService = SemiAutoCutService.Instance;
        private DynamicIntervalTimer _intervalTimer;
        private static CameraCommon? _cameraCommon;
        private MQSemiAutomaticCuttingRunViewModel _semiAutomaticCuttingRunViewModel;
        private DataPoint<float>? _originPoint;
        private SemaphoreSlim _semaph = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _operatCts;
        private float _xOriginPositionValue = 0;
        private float _yOriginPositionValue = 0;
        private bool _isReuseView = false;
        public CutServicePauseData PauseData { get; set; }

        private DelegateCommand _loadedCommand;

        public DelegateCommand LoadedCommand => _loadedCommand ??= new DelegateCommand(ExecuteLoadedCommand);

        private void ExecuteLoadedCommand()
        {
            _cameraCommon = AutoCutUtils.GetCameraCommon();
        }

        private SemiAutomaticCutParamModel _cutParam;

        public SemiAutomaticCutParamModel CutParam
        {
            get { return _cutParam; }
            set { SetProperty(ref _cutParam, value); }
        }

        private string _afterCountingClearX;

        public string AfterCountingClearX
        {
            get { return _afterCountingClearX; }
            set { SetProperty(ref _afterCountingClearX, value); }
        }

        private string _afterCountingClearY;

        public string AfterCountingClearY
        {
            get { return _afterCountingClearY; }
            set { SetProperty(ref _afterCountingClearY, value); }
        }

        public MQSemiAutomaticCuttingStopViewModel()
        {
        }

        public MQSemiAutomaticCuttingStopViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
        }

        private void InitRightButton()
        {
            RightButtonCollection.Clear();
            RightButtonCollection.Add(ButtonParams.GreenRightButton("继续", "/Assets/icon/right/enter.png", ContinueAsync));
            RightButtonCollection.Add(ButtonParams.RedRightButton("停止", "/Assets/icon/right/stop.png", StopAsync));
        }

        private void InitRightOnlyStopButton()
        {
            RightButtonCollection.Clear();
            RightButtonCollection.Add(ButtonParams.RedRightButton("停止", "/Assets/icon/right/stop.png", StopAsync));
        }

        private void InitBottomButton()
        {
            BottomButtonCollection.Clear();
            //BottomButtonCollection.Add(RightButtonParams.BlueButton("刀片状态信息", "UnfoldMoreHorizontal", () => NavigateUtils.NavigateToPage("Pages/F4_BladeMaintenance/BladeInfo", false)));
            //BottomButtonCollection.Add(RightButtonParams.BlueButton("型号参数", "UnfoldMoreHorizontal", () => NavigateUtils.NavigateToPage("Pages/F3_ModelCatalog/MCDeviceDataListConf", false)));
            //BottomButtonCollection.Add(RightButtonParams.BlueButton("精细对焦", "FocusAuto", () => _semaph.ExecuteAsync(FocusAuto, "精细对焦")));
            BottomButtonCollection.Add(ButtonParams.BlueButton("对焦", "FocusAuto", () => _semaph.ExecuteAsync(GlobalFocus, "对焦")));
            BottomButtonCollection.Add(ButtonParams.BlueButton("测量", "/Assets/icon/tab_1/03/tab_03.png", NavigateMeasurement));
            BottomButtonCollection.Add(ButtonParams.BlueButton("高度补偿", "FormatLineHeight", SetDepthCompensationAsync));
            BottomButtonCollection.Add(ButtonParams.BlueButton("基准线调窄", "UnfoldLessHorizontal", null, BaselineNarrowing, StopUpdateCameraCommonLine));
            BottomButtonCollection.Add(ButtonParams.BlueButton("基准线校准", "CrosshairsGps", () => _semaph.ExecuteAsync(BaselineCalibration, "基准线校准")));
            BottomButtonCollection.Add(ButtonParams.BlueButton("刀痕识别", "TextRecognition", AutomaticRecognition));
            BottomButtonCollection.Add(ButtonParams.BlueButton("位置清零", "Numeric0BoxOutline", CountingClearAsync));
            BottomButtonCollection.Add(ButtonParams.BlueButton("速度更改", "SpeedometerMedium", SetFeedSpeed));
            BottomButtonCollection.Add(ButtonParams.BlueButton("基准线调宽", "UnfoldMoreHorizontal", null, BaselineWidening, StopUpdateCameraCommonLine));
            BottomButtonCollection.Add(ButtonParams.BlueButton("工件吹气", "WeatherWindy", () => _semaph.ExecuteAsync(WorkpieceBlowing, "工件吹气")));
        }

        private void NavigateMeasurement()
        {
            _isReuseView = true;
            NavigationParameters parameters = new NavigationParameters { { "NavigationPageName", nameof(MQSemiAutomaticCuttingStop) } };
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(Measurement), parameters);
        }

        private async Task CountingClearAsync()
        {
            _xOriginPositionValue = await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync() ?? 0;
            _yOriginPositionValue = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync() ?? 0;
        }

        public void StartGetAxisInfo()
        {
            CancellationToken token = _operatCts.Token;
            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var xPostion = await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync();
                        var yPostion = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync();
                        if (xPostion is not null && yPostion is not null)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                AfterCountingClearX = (xPostion.Value - _xOriginPositionValue).ToString("F5");
                                AfterCountingClearY = (yPostion.Value - _yOriginPositionValue).ToString("F5");
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Tools.LogError($"StartGetAxisInfo()报警监控异常: {ex.Message}");
                    }
                    await Task.Delay(200);
                }
            });
        }

        private async Task SetDepthCompensationAsync()
        {
            if (RegionUtils.FormError(_regionManager))
            {
                MaterialSnack(RegionUtils.FormErrorMessage, SnackType.WARNING);
                return;
            }
            CommonResult checkAutomaticCompensationCutHeightResult = await VerifyUtils.CheckAutomaticCompensationCutHeightAsync(PauseData.RemainCutSteps);
            if (!checkAutomaticCompensationCutHeightResult.IsSuccess)
            {
                MaterialSnack(checkAutomaticCompensationCutHeightResult.Message, SnackType.WARNING);
                return;
            }
            // 高度补偿
            _semiAutoCutService.DepthCompensationValue = CutParam.DepthCompensation.ToFloat();
            _semiAutomaticCuttingRunViewModel.CutParam.DepthCompensation = CutParam.DepthCompensation;
            MaterialSnack($"刀片高度补偿设置为 {_semiAutoCutService.DepthCompensationValue}！", SnackType.SUCCESS);
        }

        private void SetFeedSpeed()
        {
            if (RegionUtils.FormError(_regionManager))
            {
                MaterialSnack(RegionUtils.FormErrorMessage, SnackType.WARNING);
                return;
            }
            // 速度更改
            _semiAutoCutService.FeedSpeedCompCompensationValue = CutParam.ChangeFeedSpeed.ToFloat();
            _semiAutomaticCuttingRunViewModel.CutParam.ChangeFeedSpeed = CutParam.ChangeFeedSpeed;
            MaterialSnack($"变更进刀速度设置为 {_semiAutoCutService.FeedSpeedCompCompensationValue}！", SnackType.SUCCESS);
        }

        private async Task GlobalFocus()
        {
            try
            {
                CommonResult<float> focusRusult = await AutoFocusService.GlobalFocusAsync(default, _eventAggregator, _operatCts.Token);
                if (!focusRusult.IsSuccess)
                {
                    MaterialSnack(focusRusult.Message, SnackType.WARNING);
                    return;
                }
                await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(focusRusult.Data, default, _operatCts.Token);
            }
            catch (OperationCanceledException) { }
        }

        private void StopUpdateCameraCommonLine()
        {
            _intervalTimer.Stop();
        }

        private void BaselineWidening()
        {
            _cameraCommon?.SetCutMarkWidth(CameraOperateUtils.DatumLineChangeStep, 2);
            _intervalTimer.RegisterAction(() => _cameraCommon?.SetCutMarkWidth(CameraOperateUtils.DatumLineChangeStep, 2));
            _intervalTimer.Start();
        }

        private void BaselineNarrowing()
        {
            _cameraCommon?.SetCutMarkWidth(-CameraOperateUtils.DatumLineChangeStep, 2);
            _intervalTimer.RegisterAction(() => _cameraCommon?.SetCutMarkWidth(-CameraOperateUtils.DatumLineChangeStep, 2));
            _intervalTimer.Start();
        }

        private void BrokenEdgeWidening()
        {
            _cameraCommon?.SetEdgeWidth(CameraOperateUtils.DatumLineChangeStep, 2);
        }

        private void BrokenEdgeNarrowing()
        {
            _cameraCommon?.SetEdgeWidth(-CameraOperateUtils.DatumLineChangeStep, 2);
        }

        private async Task BaselineCalibration()
        {
            if (_originPoint == null)
            {
                MaterialSnack($"基准线校准失败，请重试！", SnackType.WARNING, 0);
                return;
            }
            MaterialSnack($"基准线校准中", SnackType.INFO, 0);
            DataPoint<float> relativePostion = Appsettings.CameraRelativeBladePosition;
            DataPoint<float> curPoint = new DataPoint<float>
            {
                X = await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync() ?? 0,
                Y = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync() ?? 0
            };
            float offsetY = _originPoint.Y - curPoint.Y;
            var userdefineDdata = await SqlHelper.GetOrCreateEntityAsync(() => new UserDefineDataModel());
            float hairlineAdjustLimit = userdefineDdata.HairlineAdjustLimit.ToFloat();
            if (MathF.Abs(offsetY) > hairlineAdjustLimit)
            {
                MaterialSnack($"超出基准线校准调整极限！", SnackType.WARNING);
                return;
            }
            Appsettings.CameraRelativeBladePosition = new DataPoint<float>(relativePostion.X, relativePostion.Y - offsetY);
            _originPoint = curPoint;
            MaterialSnack($"基准线校准完成", SnackType.SUCCESS, 0);
        }

        private async Task WorkpieceBlowing()
        {
            try
            {
                await AutoCutUtils.WorkpieceBlowingNoMoveAsync(default, default, _eventAggregator, _operatCts.Token);
            }
            catch (OperationCanceledException) { }
        }

        private async void AutomaticRecognition()
        {
            await AutoCutUtils.FineTuneAxisYAsync();
            await AutoCutUtils.UpdateCameraCommonLineAsync();
        }

        public async Task StopAsync()
        {
            await _operatCts.CancelAsync();
            await _semiAutomaticCuttingRunViewModel.StopAsync(ServicePauseResult.Stop);
        }

        private async Task ContinueAsync()
        {
            if (AlarmConfig.Instance.HasActiveErrorAlarm())
            {
                MaterialSnack("请先处理错误报警！", SnackType.WARNING);
                return;
            }
            await PlcControl.tagControl.wholeDevice.CloseWorkpieceBlowingAsync();
            await _operatCts.CancelAsync();
            await _semiAutomaticCuttingRunViewModel.ContinueAsync();
            NavigationParameters parameters = new NavigationParameters { { "isContinue", true } };
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(MQSemiAutomaticCuttingRun), parameters);
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            _intervalTimer = new DynamicIntervalTimer(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(30));
            _operatCts = new CancellationTokenSource();
            InitBottomButton();
            if (navigationContext.Parameters.TryGetValue<CutServicePauseData>(nameof(CutServicePauseData), out var pauseData))
            {
                PauseData = pauseData;
                if (pauseData != null && pauseData.IsCompleted)
                {
                    InitRightOnlyStopButton();
                    if (Application.Current.MainWindow is MainWindow mainWindow && mainWindow.operateFrame.Content is OperatePage operatePage)
                    {
                        operatePage.SetOperateShowType(OperateType.OperationMenu);
                        mainWindow.ShortcutBtnClick();
                        operatePage.UpdateOperate(OperateData.GetTab01Operate());
                    }
                }
                else
                {
                    InitRightButton();
                }
            }
            else
            {
                InitRightButton();
            }
            StartGetAxisInfo();
            if (_isReuseView)
            {
                _isReuseView = false;
                return;
            }
            _semiAutomaticCuttingRunViewModel = navigationContext.Parameters.GetValue<MQSemiAutomaticCuttingRunViewModel>(nameof(MQSemiAutomaticCuttingRunViewModel));
            CutParam = _semiAutomaticCuttingRunViewModel.CutParam;
            float? xLocation = await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync();
            float? yLocation = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync();
            // 初始化起始点位置
            if (xLocation != null && yLocation != null)
            {
                _originPoint = new DataPoint<float>(xLocation.Value, yLocation.Value);
            }
            // 设置三色灯
            if (AlarmConfig.Instance.HasAutoRunUnexpectedAlarms())
            {
                await PlcControl.tagControl.wholeDevice.OpenRedLightAsync();
            }
            else
            {
                await PlcControl.tagControl.wholeDevice.OpenYellowLightAsync();
            }
            if (!GlobalParams.HasFullyAutomatic)
            {
                await PlcControl.tagControl.wholeDevice.OpenCameraLensCapAsync();
            }
        }

        public override async void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
            _operatCts.Cancel();
            _intervalTimer.Dispose();
            if (!GlobalParams.HasFullyAutomatic)
            {
                await PlcControl.tagControl.wholeDevice.CloseCameraLensCapAsync();
            }
        }

        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return _isReuseView;
        }
    }
}