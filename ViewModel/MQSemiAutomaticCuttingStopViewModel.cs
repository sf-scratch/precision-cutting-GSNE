using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private float _countingClearX = 0;
        private float _countingClearY = 0;

        private DelegateCommand _loadedCommand;

        public DelegateCommand LoadedCommand =>
            _loadedCommand ?? (_loadedCommand = new DelegateCommand(ExecuteLoadedCommand));

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
            RightButtonCollection.Add(RightButtonParams.GreenRightButton("继续", "/Assets/icon/right/enter.png", ContinueAsync));
            RightButtonCollection.Add(RightButtonParams.RedRightButton("停止", "/Assets/icon/right/stop.png", StopAsync));
        }

        private void InitBottomButton()
        {
            BottomButtonCollection.Clear();
            //BottomButtonCollection.Add(RightButtonParams.BlueButton("刀片状态信息", "UnfoldMoreHorizontal", () => NavigateUtils.NavigateToPage("Pages/F4_BladeMaintenance/BladeInfo", false)));
            //BottomButtonCollection.Add(RightButtonParams.BlueButton("型号参数", "UnfoldMoreHorizontal", () => NavigateUtils.NavigateToPage("Pages/F3_ModelCatalog/MCDeviceDataListConf", false)));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("高度补偿", "FormatLineHeight", SetDepthCompensation));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("速度更改", "SpeedometerMedium", SetFeedSpeed));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("刀痕识别", "TextRecognition", AutomaticRecognition));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("工件吹气", "WeatherWindy", () => _semaph.ExecuteAsync(WorkpieceBlowing, "工件吹气")));
            //BottomButtonCollection.Add(RightButtonParams.BlueButton("精细对焦", "FocusAuto", () => _semaph.ExecuteAsync(FocusAuto, "精细对焦")));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("对焦", "FocusAuto", () => _semaph.ExecuteAsync(GlobalFocus, "对焦")));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("位置清零", "Numeric0BoxOutline", CountingClearAsync));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("基准线校准", "CrosshairsGps", () => _semaph.ExecuteAsync(BaselineCalibration, "基准线校准")));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("基准线调窄", "UnfoldLessHorizontal", null, BaselineNarrowing, StopUpdateCameraCommonLine));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("基准线调宽", "UnfoldMoreHorizontal", null, BaselineWidening, StopUpdateCameraCommonLine));
        }

        private async Task CountingClearAsync()
        {
            _countingClearX = await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync() ?? 0;
            _countingClearY = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync() ?? 0;
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
                        var curX = await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync();
                        var curY = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync();
                        if (curX == null || curY == null) continue;
                        AfterCountingClearX = (curX.Value - _countingClearX).ToString();
                        AfterCountingClearY = (curY.Value - _countingClearY).ToString();
                    }
                    catch (Exception ex)
                    {
                        Tools.LogError($"StartGetAxisInfo()报警监控异常: {ex.Message}");
                    }
                    await Task.Delay(200);
                }
            });
        }

        private void SetDepthCompensation()
        {
            // 高度补偿
            _semiAutoCutService.DepthCompensationValue = CutParam.DepthCompensation.ToFloat();
            _semiAutomaticCuttingRunViewModel.CutParam.DepthCompensation = CutParam.DepthCompensation;
            MaterialSnackUtils.MaterialSnack($"刀片高度补偿设置为{_semiAutoCutService.DepthCompensationValue}！", MaterialSnackUtils.SnackType.SUCCESS);
        }

        private void SetFeedSpeed()
        {
            // 速度更改
            _semiAutoCutService.FeedSpeedCompCompensationValue = CutParam.ChangeFeedSpeed.ToFloat();
            _semiAutomaticCuttingRunViewModel.CutParam.ChangeFeedSpeed = CutParam.ChangeFeedSpeed;
            MaterialSnackUtils.MaterialSnack($"变更进刀速度设置为{_semiAutoCutService.FeedSpeedCompCompensationValue}！", MaterialSnackUtils.SnackType.SUCCESS);
        }

        private async Task GlobalFocus()
        {
            try
            {
                CommonResult<float> focusRusult = await AutoFocusService.GlobalFocusAsync(_eventAggregator, _operatCts.Token);
                if (!focusRusult.IsSuccess)
                {
                    MaterialSnackUtils.MaterialSnack(focusRusult.Message, MaterialSnackUtils.SnackType.WARNING);
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
                MaterialSnackUtils.MaterialSnack($"基准线校准失败，请重试！", MaterialSnackUtils.SnackType.WARNING, 0);
                return;
            }
            MaterialSnackUtils.MaterialSnack($"基准线校准中", MaterialSnackUtils.SnackType.INFO, 0);
            DataPoint<float> relativePostion = Appsettings.CameraRelativeBladePosition;
            DataPoint<float> curPoint = new DataPoint<float>
            {
                X = await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync() ?? 0,
                Y = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync() ?? 0
            };
            float offsetX = _originPoint.X - curPoint.X;
            float offsetY = _originPoint.Y - curPoint.Y;
            Appsettings.CameraRelativeBladePosition = new DataPoint<float>(relativePostion.X, relativePostion.Y - offsetY);
            _originPoint = curPoint;
            MaterialSnackUtils.MaterialSnack($"基准线校准完成", MaterialSnackUtils.SnackType.SUCCESS, 0);
        }

        private async Task WorkpieceBlowing()
        {
            try
            {
                await AutoCutUtils.WorkpieceBlowingThenBackAsync(_eventAggregator, _operatCts.Token);
                await AutoCutUtils.FineTuneAxisYAsync();
                await AutoCutUtils.UpdateCameraCommonLineAsync();
            }
            catch (OperationCanceledException) { }
        }

        private async void AutomaticRecognition()
        {
            await AutoCutUtils.FineTuneAxisYAsync();
            await AutoCutUtils.UpdateCameraCommonLineAsync();
        }

        private async Task StopAsync()
        {
            await _operatCts.CancelAsync();
            await _semiAutomaticCuttingRunViewModel.StopAsync(ServicePauseResult.Stop);
        }

        private async Task ContinueAsync()
        {
            if (AlarmConfig.Instance.HasActiveErrorAlarm())
            {
                MaterialSnackUtils.MaterialSnack("请先处理错误报警！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            await _operatCts.CancelAsync();
            await _semiAutomaticCuttingRunViewModel.ContinueAsync();
            NavigationParameters parameters = new NavigationParameters { { "isContinue", true } };
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(MQSemiAutomaticCuttingRun), parameters);
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            _intervalTimer = new DynamicIntervalTimer(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(100));
            _operatCts = new CancellationTokenSource();
            InitBottomButton();
            InitRightButton();
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
            StartGetAxisInfo();
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
    }
}