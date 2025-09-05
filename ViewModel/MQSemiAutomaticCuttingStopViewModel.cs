using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    class MQSemiAutomaticCuttingStopViewModel : CustomBindableBase
    {
        private readonly IRegionManager _regionManager;
        private readonly SemiAutoCutService _semiAutoCutService = SemiAutoCutService.Instance;
        private static CameraCommon? _cameraCommon;
        private MQSemiAutomaticCuttingRunViewModel _semiAutomaticCuttingRunViewModel;
        private DataPoint<float>? _originPoint;
        private SemaphoreSlim _semaph = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _operatCts;

        private string _directoryId;
        private string _deviceDataNo;
        private string _deviceDataId;
        private string _channelNum;
        private string _bladeHeight;
        private string _feedSpeed;
        private string _depthCompensation;
        private string _changeFeedSpeed;
        private int _allCutLine;
        private string _allCutLineLength;
        private double _cutWidth;
        private double _edgesWidth;

        public string DirectoryId
        {
            get => _directoryId;
            set => SetProperty(ref _directoryId, value);
        }

        // DeviceDataNo
        public string DeviceDataNo
        {
            get => _deviceDataNo;
            set => SetProperty(ref _deviceDataNo, value);
        }

        // DeviceDataId
        public string DeviceDataId
        {
            get => _deviceDataId;
            set => SetProperty(ref _deviceDataId, value);
        }

        // DepthCompensation
        public string DepthCompensation
        {
            get => _depthCompensation;
            set => SetProperty(ref _depthCompensation, value);
        }

        // ChannelNum
        public string ChannelNum
        {
            get => _channelNum;
            set => SetProperty(ref _channelNum, value);
        }

        // BladeHeight
        public string BladeHeight
        {
            get => _bladeHeight;
            set => SetProperty(ref _bladeHeight, value);
        }

        // FeedSpeed
        public string FeedSpeed
        {
            get => _feedSpeed;
            set => SetProperty(ref _feedSpeed, value);
        }

        // CutWidth
        public double CutWidth
        {
            get => _cutWidth;
            set => SetProperty(ref _cutWidth, value);
        }

        // DdgesWidth
        public double DdgesWidth
        {
            get => _edgesWidth;
            set => SetProperty(ref _edgesWidth, value);
        }

        // ChangeFeedSpeed
        public string ChangeFeedSpeed
        {
            get => _changeFeedSpeed;
            set => SetProperty(ref _changeFeedSpeed, value);
        }

        private int _runCutLine;
        public int RunCutLine
        {
            get => _runCutLine;
            set => SetProperty(ref _runCutLine, value);
        }

        private int _allRunCutLine;
        public int AllRunCutLine
        {
            get => _allRunCutLine;
            set => SetProperty(ref _allRunCutLine, value);
        }

        private string _expectedProcessingEndTime;

        public string ExpectedProcessingEndTime
        {
            get => _expectedProcessingEndTime;
            set => SetProperty(ref _expectedProcessingEndTime, value);
        }

        public int AllCutLine
        {
            get => _allCutLine;
            set => SetProperty(ref _allCutLine, value);
        }

        public string AllCutLineLength
        {
            get => _allCutLineLength;
            set => SetProperty(ref _allCutLineLength, value);
        }

        public MQSemiAutomaticCuttingStopViewModel()
        {

        }

        public MQSemiAutomaticCuttingStopViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
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
            BottomButtonCollection.Add(RightButtonParams.BlueButton("高度补偿", "UnfoldMoreHorizontal", SetDepthCompensation));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("速度更改", "UnfoldMoreHorizontal", SetFeedSpeed));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("刀痕识别", "TextRecognition", AutomaticRecognition));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("工件吹气", "WeatherWindy", () => _semaph.ExecuteAsync(WorkpieceBlowing, "工件吹气")));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("精细对焦", "FocusAuto", () => _semaph.ExecuteAsync(FocusAuto, "精细对焦")));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("全局对焦", "FocusAuto", () => _semaph.ExecuteAsync(GlobalFocus, "全局对焦")));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("基准线校准", "CrosshairsGps", () => _semaph.ExecuteAsync(BaselineCalibration, "基准线校准")));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("基准线调窄", "UnfoldLessHorizontal", BaselineNarrowing));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("基准线调宽", "UnfoldMoreHorizontal", BaselineWidening));
        }

        private void SetDepthCompensation()
        {
            // 高度补偿
            _semiAutoCutService.DepthCompensationValue = DepthCompensation.ToFloat();
            MaterialSnackUtils.MaterialSnack("刀片高度补偿设置成功！", MaterialSnackUtils.SnackType.SUCCESS);
        }

        private void SetFeedSpeed()
        {
            // 速度更改
            _semiAutoCutService.FeedSpeedCompCompensationValue = ChangeFeedSpeed.ToFloat();
            MaterialSnackUtils.MaterialSnack("变更进刀速度成功！", MaterialSnackUtils.SnackType.SUCCESS);
        }

        private async Task GlobalFocus()
        {
            try
            {
                CommonResult<float> focusRusult = await AutoFocusService.GlobalFocusAsync(default, _operatCts.Token);
                if (!focusRusult.IsSuccess)
                {
                    MaterialSnackUtils.MaterialSnack(focusRusult.Message, MaterialSnackUtils.SnackType.WARNING);
                    return;
                }
                await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(focusRusult.Data, default, default);
            }
            catch (OperationCanceledException) { }
        }

        private async Task FocusAuto()
        {
            try
            {
                CommonResult<float> focusRusult = await AutoCutUtils.AutoFocusAsync(token: _operatCts.Token);
                if (!focusRusult.IsSuccess)
                {
                    MaterialSnackUtils.MaterialSnack(focusRusult.Message, MaterialSnackUtils.SnackType.WARNING);
                    return;
                }
                await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(focusRusult.Data, default, default);
            }
            catch (OperationCanceledException) { }
        }

        private void BaselineWidening()
        {
            _cameraCommon?.SetCutMarkWidth(1, 2);
        }

        private void BaselineNarrowing()
        {
            _cameraCommon?.SetCutMarkWidth(-1, 2);
        }

        private void BrokenEdgeWidening()
        {
            _cameraCommon?.SetEdgeWidth(1, 2);
        }

        private void BrokenEdgeNarrowing()
        {
            _cameraCommon?.SetEdgeWidth(-1, 2);
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
                float curX = await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync() ?? 0;
                await AutoCutUtils.WorkpieceBlowingAsync(token: _operatCts.Token);
                await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(curX, default, default);
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
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(MQSemiAutomaticCuttingRun));
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            _operatCts = new CancellationTokenSource();
            InitBottomButton();
            InitRightButton();
            _semiAutomaticCuttingRunViewModel = navigationContext.Parameters.GetValue<MQSemiAutomaticCuttingRunViewModel>(nameof(MQSemiAutomaticCuttingRunViewModel));
            float? xLocation = await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync();
            float? yLocation = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync();
            // 初始化起始点位置
            if (xLocation != null && yLocation != null)
            {
                _originPoint = new DataPoint<float>(xLocation.Value, yLocation.Value);
            }
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
            _operatCts.Cancel();
        }
    }
}
