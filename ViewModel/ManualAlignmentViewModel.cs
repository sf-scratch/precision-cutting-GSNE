using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.Driver;
using 精密切割系统.Extensions;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.View.Pages.common;
using 精密切割系统.View.Pages.F2_ManualOperation;

namespace 精密切割系统.ViewModel
{
    internal class ManualAlignmentViewModel : CustomBindableBase
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1); // 确保线程安全
        private readonly ThetaAlignService _alignService = ThetaAlignService.Instance;
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private static CameraCommon? _cameraCommon;
        private DynamicIntervalTimer _intervalTimer;
        private CancellationTokenSource _cts;
        private Dictionary<string, ChData>? _chDictionary;
        private Queue<string>? _chQueue;

        private DelegateCommand _loadedCommand;

        public DelegateCommand LoadedCommand => _loadedCommand ??= new DelegateCommand(ExecuteLoadedCommand);

        private void ExecuteLoadedCommand()
        {
            _cameraCommon = AutoCutUtils.GetCameraCommon();
        }

        private string _currentCh;

        public string CurrentCh
        {
            get { return _currentCh; }
            set { SetProperty(ref _currentCh, value); }
        }

        public ManualAlignmentViewModel()
        {
        }

        public ManualAlignmentViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
        }

        protected override void InitRightButton()
        {
            base.InitRightButton();
            AddRightButton(ButtonParams.Sure(SureAsync));
            AddRightButton(ButtonParams.Back(Back));
        }

        private async Task SureAsync()
        {
            if (_chDictionary is null || _chQueue is null)
            {
                MaterialSnack("未获取到CH序列，请检查型号参数配置是否正确！", SnackType.WARNING);
                return;
            }
            if (_alignService.CurrentThetaAlignStatus == ThetaAlignStatus.Horizontal || _alignService.CurrentThetaAlignStatus == ThetaAlignStatus.Vertical)
            {
                MaterialSnack("请完成Theta轴校准后，再点击确认！", SnackType.WARNING);
                return;
            }
            ThetaAlignService.ChDictionary = null;
            if (_chDictionary.TryGetValue(CurrentCh, out var chData))
            {
                var thetaDeg = await PlcControl.tagControl.ThetaAxis.GetCurrentLocationAsync();
                var yPosition = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync();
                if (thetaDeg is null || yPosition is null)
                {
                    MaterialSnack("未获取到θ轴或Y轴当前位置，请检查设备连接是否正常！", SnackType.WARNING);
                    return;
                }
                chData.AfterCalibrationThetaDeg = thetaDeg.Value;
                chData.AfterCalibrationYPosition = yPosition.Value;
                if (_chQueue.Count > 0)
                {
                    CurrentCh = _chQueue.Dequeue();
                }
                else
                {
                    MaterialSnack("已完成所有CH校准！", SnackType.SUCCESS);
                    ThetaAlignService.ChDictionary = _chDictionary;
                    _regionManager.RequestNavigate(RegionName.MainRegion, nameof(AutomaticCuttingConf));
                    return;
                }
            }
            else
            {
                MaterialSnack($"未获取到当前CH：{CurrentCh}的信息，请检查型号参数配置是否正确！", SnackType.WARNING);
                return;
            }
        }

        private void Back()
        {
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(AutomaticCuttingConf));
        }

        protected override void InitBottomButton()
        {
            base.InitBottomButton();
            AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
            AddBottomButton(ButtonParams.BlueButton("对焦", "/Assets/icon/tab_1/03/tab_01.png", FocusAutoAsync));
            AddBottomButton(ButtonParams.BlueButton("对焦确认", "/Assets/icon/tab_1/03/tab_01.png", FocusAutoSureAsync));
            AddBottomButton(ButtonParams.BlueButton("基准线调窄", "/Assets/icon/tab_1/03/tab_02.png", null, BaselineNarrowing, StopUpdateCameraCommonLine));
            AddBottomButton(ButtonParams.BlueButton("θ轴竖向校正", "/Assets/icon/tab_1/03/theta-align-vertical.png", _alignService.ThetaVerticalAlignAsync));
            AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
            AddBottomButton(ButtonParams.BlueButton("刀痕识别", "/Assets/icon/tab_1/03/tab_03.png", TextRecognitionAsync));
            AddBottomButton(ButtonParams.BlueButton("测量", "/Assets/icon/tab_1/03/tab_03.png", MeasureAsync));
            AddBottomButton(ButtonParams.BlueButton("基准线调宽", "/Assets/icon/tab_1/03/tab_02.png", null, BaselineWidening, StopUpdateCameraCommonLine));
            AddBottomButton(ButtonParams.BlueButton("θ轴横向校正", "/Assets/icon/tab_1/03/tab_04.png", _alignService.ThetaHorizontalAlignAsync));
        }

        private void BaselineWidening()
        {
            _cameraCommon?.SetCutMarkWidth(CameraOperateUtils.DatumLineChangeStep, 2);
            _intervalTimer.RegisterAction(() => _cameraCommon?.SetCutMarkWidth(CameraOperateUtils.DatumLineChangeStep, 2));
            _intervalTimer.Start();
        }

        private void MeasureAsync()
        {
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(Measurement));
        }

        private void TextRecognitionAsync()
        {
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

        private async Task FocusAutoSureAsync()
        {
            Appsettings.FocusClearZ = await PlcControl.tagControl.Z2axis.GetCurrentLocationAsync();
            MaterialSnack($"对焦位置已确认：{Appsettings.FocusClearZ}mm！", SnackType.WARNING, default, _eventAggregator);
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

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            _cts = new CancellationTokenSource();
            _intervalTimer = new DynamicIntervalTimer(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(100));
            CommonResult<ChData[]> chDataResult = await AutoCutUtils.GetChSequenseAsync();
            if (chDataResult.IsSuccess && chDataResult.Data != null)
            {
                _chDictionary = [];
                ChData[] chDatas = chDataResult.Data;
                foreach (var ch in chDatas)
                {
                    _chDictionary.Add(ch.ChName, ch);
                }
                _chQueue = new Queue<string>(chDatas.Select(chData => chData.ChName));
                CurrentCh = _chQueue.Dequeue();
            }
            else
            {
                MaterialSnack(chDataResult.Message, SnackType.WARNING);
            }
            //RegexMatchUtils.ExtractChNumber(CurrentUtils.GetCurrentCh())?.ToString();
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
            _cts.Cancel();
            _intervalTimer.Dispose();
        }
    }
}