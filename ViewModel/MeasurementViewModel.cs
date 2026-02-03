using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Utils;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.View.Pages.common;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.ViewModel
{
    internal class MeasurementViewModel : CustomBindableBase
    {
        private CancellationTokenSource _cts;
        private float _xOriginPositionValue;
        private float _yOriginPositionValue;
        private static CameraCommon? _cameraCommon;
        private DynamicIntervalTimer _intervalTimer;
        private string? _navigationPageName;

        private string _xCleanPosition;

        public string XCleanPosition
        {
            get { return _xCleanPosition; }
            set { SetProperty(ref _xCleanPosition, value); }
        }

        private string _yCleanPosition;

        public string YCleanPosition
        {
            get { return _yCleanPosition; }
            set { SetProperty(ref _yCleanPosition, value); }
        }

        private DelegateCommand _loadedCommand;

        public DelegateCommand LoadedCommand => _loadedCommand ??= new DelegateCommand(ExecuteLoadedCommand);

        private void ExecuteLoadedCommand()
        {
            _cameraCommon = AutoCutUtils.GetCameraCommon();
        }

        protected override void InitRightButton()
        {
            base.InitRightButton();
            AddRightButton(ButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        protected override void InitBottomButton()
        {
            base.InitBottomButton();
            AddBottomButton(ButtonParams.BlueButton("基准线调窄", "UnfoldLessHorizontal", null, BaselineNarrowing, StopUpdateCameraCommonLine));
            AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
            AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
            AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
            AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
            AddBottomButton(ButtonParams.BlueButton("基准线调宽", "UnfoldMoreHorizontal", null, BaselineWidening, StopUpdateCameraCommonLine));
            AddBottomButton(ButtonParams.BlueButton("位置清零", "/Assets/icon/tab_1/03/z_axis_down.png", PositionResetAsync));
        }

        private void Back()
        {
            if (_navigationPageName is not null)
            {
                ContainerLocator.Container.Resolve<IRegionManager>().RequestNavigate(RegionName.MainRegion, _navigationPageName);
            }
            else
            {
                NavigateUtils.NavigateToPage("Pages/F2_ManualOperation/MQManualAlignmentConf");
            }
        }

        private async Task PositionResetAsync()
        {
            _xOriginPositionValue = await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync() ?? 0;
            _yOriginPositionValue = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync() ?? 0;
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

        private async Task StartLoadPosition()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var xPostion = await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync();
                    var yPostion = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync();
                    if (xPostion is not null && yPostion is not null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            XCleanPosition = (xPostion.Value - _xOriginPositionValue).ToString("F5");
                            YCleanPosition = (yPostion.Value - _yOriginPositionValue).ToString("F5");
                        });
                    }
                }
                catch (Exception ex)
                {
                    Tools.LogError($"StartLoadPosition()报警监控异常: {ex.Message}");
                }
                await Task.Delay(200);
            }
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            _cts = new CancellationTokenSource();
            _intervalTimer = new DynamicIntervalTimer(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(30));
            _navigationPageName = navigationContext.Parameters.GetValue<string>("NavigationPageName");
            _ = Task.Run(StartLoadPosition);
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
            _cts.Cancel();
            _intervalTimer.Dispose();
        }
    }
}