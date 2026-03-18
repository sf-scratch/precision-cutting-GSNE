using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.Entities;
using 精密切割系统.Extensions;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.cut.Workpieces;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.common;
using 精密切割系统.View.Pages.F2_ManualOperation;
using 精密切割系统.View.Pages.F4_BladeMaintenance;

namespace 精密切割系统.ViewModel
{
    internal class BaselineCalibrationViewModel : CustomBindableBase
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1); // 确保线程安全
        private readonly ThetaAlignService _alignService = ThetaAlignService.Instance;
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private static CameraCommon? _cameraCommon;
        private DynamicIntervalTimer _intervalTimer;
        private CancellationTokenSource _cts;
        private CancellationTokenSource? _monitorCts;
        private float _measureHeigthY;
        private float? _cutY;

        private DelegateCommand _loadedCommand;

        public DelegateCommand LoadedCommand => _loadedCommand ??= new DelegateCommand(ExecuteLoadedCommand);

        private void ExecuteLoadedCommand()
        {
            _cameraCommon = AutoCutUtils.GetCameraCommon();
        }

        private BaselineCalibrationEntity _baselineCalibrationEntity;

        public BaselineCalibrationEntity Entity
        {
            get { return _baselineCalibrationEntity; }
            set { SetProperty(ref _baselineCalibrationEntity, value); }
        }

        private bool _isSelectRect = true;

        public bool IsSelectRect
        {
            get { return _isSelectRect; }
            set { SetProperty(ref _isSelectRect, value); }
        }

        private float _cameraRelativeBladePositionY;

        public float CameraRelativeBladePositionY
        {
            get { return _cameraRelativeBladePositionY; }
            set { SetProperty(ref _cameraRelativeBladePositionY, value); }
        }

        public BaselineCalibrationViewModel()
        {
        }

        public BaselineCalibrationViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
        }

        protected override void InitRightButton()
        {
            base.InitRightButton();
            AddRightButton(ButtonParams.Start(StartAsync));
            AddRightButton(ButtonParams.Sure(SureAsync));
            AddRightButton(ButtonParams.Back(Back));
        }

        private void InitRightButtonRuning()
        {
            base.InitRightButton();
            AddRightButton(ButtonParams.Stop(Stop));
        }

        private void Stop()
        {
            _cts?.Cancel();
            _monitorCts?.Cancel();
        }

        private async Task StartAsync()
        {
            InitRightButtonRuning();
            try
            {
                if (RegionUtils.FormError(_regionManager))
                {
                    MaterialSnack(RegionUtils.FormErrorMessage, SnackType.WARNING);
                    return;
                }
                if (_alignService.CurrentThetaAlignStatus == ThetaAlignStatus.Horizontal || _alignService.CurrentThetaAlignStatus == ThetaAlignStatus.Vertical)
                {
                    MaterialSnack("请完成Theta轴校准后，再点击确认！", SnackType.WARNING);
                    return;
                }
                if (AlarmConfig.Instance.HasActiveErrorAlarm())
                {
                    MaterialSnack(AlarmConfig.HasErrorAlarmMessage, SnackType.WARNING);
                    return;
                }
                if (Appsettings.BladeOuterDiameter is null)
                {
                    MaterialSnack("未设置刀片外径！", SnackType.WARNING);
                    return;
                }
                if (Appsettings.MeasureHeightLast is null)
                {
                    MaterialSnack("刀具未测高，请先测高！", SnackType.WARNING);
                    return;
                }
                var yPostion = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync();
                if (yPostion is null)
                {
                    MaterialSnack("获取Y轴当前位置失败！", SnackType.WARNING);
                    return;
                }
                await SaveEntityAsync();
                _measureHeigthY = Appsettings.MeasureHeightLast.Value;
                await RunCutSingleLineAsync(yPostion.Value.ToActualY(), _cts.Token);
            }
            catch (OperationCanceledException)
            {
                MaterialSnack("切割已取消！", SnackType.WARNING);
            }
            catch (Exception ex)
            {
                Tools.LogDebug(ex.Message);
                MaterialSnack($"切割失败：{ex.Message}", SnackType.ERROR, 0);
            }
            finally
            {
                InitRightButton();
            }
        }

        private async Task SureAsync()
        {
            if (RegionUtils.FormError(_regionManager))
            {
                MaterialSnack(RegionUtils.FormErrorMessage, SnackType.WARNING);
                return;
            }
            await SaveEntityAsync();
            NavigateUtils.ToOperateButton();
            MaterialSnack("保存成功！", SnackType.SUCCESS, 3);
        }

        private void Back()
        {
            NavigateUtils.NavigateToPage("MainMenu");
        }

        private async Task SaveEntityAsync()
        {
            await SqlHelper.UpdateAsync(Entity);
        }

        protected override void InitBottomButton()
        {
            base.InitBottomButton();
            switch (GlobalParams.DeviceModel)
            {
                case GlobalParams.Device_321:
                    AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
                    AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
                    AddBottomButton(ButtonParams.BlueButton("对焦", "FocusAuto", FocusAutoAsync));
                    AddBottomButton(ButtonParams.BlueButton("基准线调窄", "UnfoldLessHorizontal", null, BaselineNarrowing, StopUpdateCameraCommonLine));
                    AddBottomButton(ButtonParams.BlueButton("θ轴横向校正", "/Assets/icon/tab_1/03/tab_04.png", _alignService.ThetaHorizontalAlignAsync));
                    AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
                    AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
                    AddBottomButton(ButtonParams.BlueButton("测量", "/Assets/icon/tab_1/03/tab_03.png", NavigateMeasurement));
                    AddBottomButton(ButtonParams.BlueButton("基准线调宽", "UnfoldMoreHorizontal", null, BaselineWidening, StopUpdateCameraCommonLine));
                    AddBottomButton(ButtonParams.BlueButton("基准线校准", "CrosshairsGps", BaselineCalibrationAsync));
                    break;

                case GlobalParams.Device_562:
                    AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
                    AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
                    AddBottomButton(ButtonParams.BlueButton("对焦", "FocusAuto", FocusAutoAsync));
                    AddBottomButton(ButtonParams.BlueButton("基准线调窄", "UnfoldLessHorizontal", null, BaselineNarrowing, StopUpdateCameraCommonLine));
                    AddBottomButton(ButtonParams.BlueButton("θ轴竖向校正", "/Assets/icon/tab_1/03/theta-align-vertical.png", _alignService.ThetaVerticalAlignAsync));
                    AddBottomButton(ButtonParams.BlueButton("基准线校准", "CrosshairsGps", BaselineCalibrationAsync));
                    AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
                    AddBottomButton(ButtonParams.BlueButton("测量", "/Assets/icon/tab_1/03/tab_03.png", NavigateMeasurement));
                    AddBottomButton(ButtonParams.BlueButton("基准线调宽", "UnfoldMoreHorizontal", null, BaselineWidening, StopUpdateCameraCommonLine));
                    AddBottomButton(ButtonParams.BlueButton("θ轴横向校正", "/Assets/icon/tab_1/03/tab_04.png", _alignService.ThetaHorizontalAlignAsync));
                    break;

                case GlobalParams.Device_551:
                    AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
                    AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
                    AddBottomButton(ButtonParams.BlueButton("对焦", "FocusAuto", FocusAutoAsync));
                    AddBottomButton(ButtonParams.BlueButton("基准线调窄", "UnfoldLessHorizontal", null, BaselineNarrowing, StopUpdateCameraCommonLine));
                    AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
                    AddBottomButton(ButtonParams.BlueButton("基准线校准", "CrosshairsGps", BaselineCalibrationAsync));
                    AddBottomButton(ButtonParams.BlueButton("测量", "/Assets/icon/tab_1/03/tab_03.png", NavigateMeasurement));
                    AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
                    AddBottomButton(ButtonParams.BlueButton("基准线调宽", "UnfoldMoreHorizontal", null, BaselineWidening, StopUpdateCameraCommonLine));
                    AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
                    break;
            }
        }

        private async Task BaselineCalibrationAsync()
        {
            if (_cutY != null)
            {
                float cutY = _cutY.Value;
                MaterialSnack($"基准线校准中", SnackType.INFO, 0);
                DataPoint<float> relativePostion = Appsettings.CameraRelativeBladePosition;
                DataPoint<float> curPoint = new DataPoint<float>
                {
                    X = await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync() ?? 0,
                    Y = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync() ?? 0
                };
                float offsetY = cutY - curPoint.Y;
                var userdefineDdata = await SqlHelper.GetOrCreateEntityAsync(() => new UserDefineDataModel());
                float hairlineAdjustLimit = userdefineDdata.HairlineAdjustLimit.ToFloat();
                if (MathF.Abs(offsetY) > hairlineAdjustLimit)
                {
                    MaterialSnack($"超出基准线校准调整极限！", SnackType.WARNING);
                    return;
                }
                Appsettings.CameraRelativeBladePosition = new DataPoint<float>(relativePostion.X, relativePostion.Y - offsetY);
                CameraRelativeBladePositionY = Appsettings.CameraRelativeBladePosition.Y;
                _cutY = curPoint.Y;
                MaterialSnack($"基准线校准完成", SnackType.SUCCESS);
            }
            else
            {
                MaterialSnack($"基准线校准失败，请开始切割！", SnackType.WARNING);
            }
        }

        private void NavigateMeasurement()
        {
            NavigationParameters parameters = new NavigationParameters { { "NavigationPageName", nameof(BaselineCalibration) } };
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(Measurement), parameters);
        }

        private void BaselineWidening()
        {
            _cameraCommon?.SetCutMarkWidth(CameraOperateUtils.DatumLineChangeStep, 2);
            _intervalTimer.RegisterAction(() => _cameraCommon?.SetCutMarkWidth(CameraOperateUtils.DatumLineChangeStep, 2));
            _intervalTimer.Start();
        }

        private void StopUpdateCameraCommonLine()
        {
            _intervalTimer.Stop();
        }

        private void BaselineNarrowing()
        {
            _cameraCommon?.SetCutMarkWidth(-CameraOperateUtils.DatumLineChangeStep, 2);
            _intervalTimer.RegisterAction(() => _cameraCommon?.SetCutMarkWidth(-CameraOperateUtils.DatumLineChangeStep, 2));
            _intervalTimer.Start();
        }

        private async Task FocusAutoAsync()
        {
            CancellationToken token = _cts.Token;
            await _semaphore.ExecuteAsync(async () =>
            {
                try
                {
                    await using var timeoutToken = TaskUtils.GetTimeoutCancellationToken(TimeSpan.FromSeconds(120), token);
                    var result = await AutoFocusService.GlobalFocusAsync(_eventAggregator, timeoutToken.Token);
                    if (!result.IsSuccess)
                    {
                        MaterialSnack(result.Message, SnackType.WARNING, default, _eventAggregator);
                        return;
                    }
                    MaterialSnack(result.Message, SnackType.WARNING, default, _eventAggregator);
                }
                catch (OperationCanceledException)
                {
                    if (token.IsCancellationRequested)
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

        private async Task RunCutSingleLineAsync(float startY, CancellationToken token)
        {
            if (!GlobalParams.OnlineFlag) return;
            if (_monitorCts is null || _monitorCts.IsCancellationRequested)
            {
                _monitorCts = new CancellationTokenSource();
                _ = AutoCutUtils.MonitoringAlarmAsync(Stop, AlarmConfig.Instance.HasAutoRunUnexpectedAlarms, default, _monitorCts.Token);
            }
            await PlcControl.tagControl.wholeDevice.CloseCameraLensCapAsync();
            //打开切割水
            await PlcControl.tagControl.wholeDevice.OpenCuttingWaterAsync();
            //进入全自动切割模式
            await PlcControl.tagControl.cutting.EnterCuttingModeAsync(token);
            float endZ = _measureHeigthY - Entity.BladeHeight.ToFloat();
            float startZ = _measureHeigthY - Entity.WorkThickness.ToFloat() - Entity.TapeThickness.ToFloat() - GlobalParams.BladeLiftingHeight;
            float depthEntry = _measureHeigthY - Entity.WorkThickness.ToFloat() - Entity.TapeThickness.ToFloat() - 0.5f;
            float thetaDeg = ThetaAlignService.Instance.ThetaAlignCompletedDeg ?? await PlcControl.tagControl.ThetaAxis.GetCurrentLocationAsync() ?? 0;
            IWorkpieces workpiece = GenerateWorkpieces(startY);
            LineSegment line = workpiece.CalculateCuttingLine();
            float margin = Appsettings.AdditionalMargin ?? 20;
            float startX = line.StartPoint.X - margin;
            float endX = line.EndPoint.X + margin;
            try
            {
                //当前切割次数
                int? curCutNum = await PlcControl.tagControl.cutting.GetCutNumAsync();
                if (curCutNum == null)
                {
                    MaterialSnack("获取当前切割次数失败！", SnackType.WARNING, 0);
                    return;
                }
                await PlcControl.tagControl.ThetaAxis.SetAbsoluteSpeedAsync(GlobalParams.ThetaDefaultSpeed);
                //设置切割参数
                await PlcControl.tagControl.cutting.SetCutParamsAsync(Entity.CutSpeed.ToFloat(), endZ, startZ, startX, endX, startY, "0", thetaDeg, Entity.SpindleRev.ToInt(), depthEntry);
                //开始切割信号
                await PlcControl.tagControl.cutting.StartCutAsync();
                //等待切割次数变化
                await PlcControl.tagControl.cutting.WaitCutNumUdatedAsync(curCutNum.Value + 1, token);
            }
            finally
            {
                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(token);
                await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
                // 工作盘吹气
                await AutoCutUtils.WorkpieceBlowingAsync(default, default, true, default, token);
                await PlcControl.tagControl.cutting.RunMotionAsync(((startX + endX) / 2).ToCameraX(), startY.ToCameraY(), token);
                await PlcControl.tagControl.wholeDevice.OpenCameraLensCapAsync();
                _cutY = startY.ToCameraY();
            }
        }

        private IWorkpieces GenerateWorkpieces(float cutY)
        {
            DataPoint<float> thetaCenterPoint = Appsettings.ThetaCenterPoint;
            IWorkpieces workpiece;
            if (IsSelectRect)
            {
                float width = Entity.RectangularLength.ToFloat();
                float height = Entity.RectangularWidth.ToFloat();
                workpiece = new RectangleWorkpiece(thetaCenterPoint, width, height, cutY);
            }
            else
            {
                workpiece = new CircularWorkpiece(thetaCenterPoint, Entity.CircularRadius.ToFloat() / 2, cutY);
            }
            workpiece.WorkThickness = float.Parse(Entity.WorkThickness);
            workpiece.TapeThickness = float.Parse(Entity.TapeThickness);
            return workpiece;
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            _cts = new CancellationTokenSource();
            _intervalTimer = new DynamicIntervalTimer(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(30));
            Entity = await SqlHelper.GetOrCreateEntityAsync(() => new BaselineCalibrationEntity());
            CameraRelativeBladePositionY = Appsettings.CameraRelativeBladePosition.Y;
            await PlcControl.tagControl.wholeDevice.OpenCameraLensCapAsync();
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
            _cts.Cancel();
            _intervalTimer.Dispose();
        }
    }
}