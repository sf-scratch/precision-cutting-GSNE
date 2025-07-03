using Org.BouncyCastle.Crypto.Generators;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.plc;
using 精密切割系统.PubSubEvent;
using 精密切割系统.Utils;
using 精密切割系统.View.common;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.View.Pages.common;

namespace 精密切割系统.ViewModel
{
    public class AutoCutPausingViewModel : CustomBindableBase
    {
        public RelayCommand ContinueCommand { get; set; }
        public RelayCommand StopCommand { get; set; }
        private readonly IRegionManager _regionManager;
        private CameraCommon? _cameraCommon;
        private AutoCutRuningViewModel _autoCutRuningViewModel;
        private DataPoint<float>? _originPoint;

        // 控制右侧按钮
        public ObservableCollection<RightButtonParams> RightPageButtonCollection;
        // 控制底部侧按钮
        public ObservableCollection<RightButtonParams> OperatePageButtonCollection;

        private static int _afterReplaceBladeCutTimes;
        /// <summary>
        /// 自更换刀片起刀片切了几道
        /// </summary>
        public int AfterReplaceBladeCutTimes
        {
            get { return _afterReplaceBladeCutTimes; }
            set { _afterReplaceBladeCutTimes = value; RaisePropertyChanged(); }
        }

        private float _afterHeightMeasurementZ;
        /// <summary>
        /// 测高位置
        /// </summary>
        public float AfterHeightMeasurementZ
        {
            get { return _afterHeightMeasurementZ; }
            set { _afterHeightMeasurementZ = value; RaisePropertyChanged(); }
        }

        private float _sharpenBladeHeight;
        /// <summary>
        /// 磨刀片高度
        /// </summary>
        public float SharpenBladeHeight
        {
            get { return _sharpenBladeHeight; }
            set { _sharpenBladeHeight = value; RaisePropertyChanged(); }
        }

        private float _sharpenSpeed;
        /// <summary>
        /// 磨刀速度
        /// </summary>
        public float SharpenSpeed
        {
            get { return _sharpenSpeed; }
            set { _sharpenSpeed = value; RaisePropertyChanged(); }
        }

        private string _sharpenProgress;
        /// <summary>
        /// 磨刀进度
        /// </summary>
        public string SharpenProgress
        {
            get { return _sharpenProgress; }
            set { _sharpenProgress = value; RaisePropertyChanged(); }
        }

        private string _deviceDataNo;
        /// <summary>
        /// 型号参数No
        /// </summary>
        public string DeviceDataNo
        {
            get { return _deviceDataNo; }
            set { _deviceDataNo = value; RaisePropertyChanged(); }
        }

        private float _cutBladeHeight;
        /// <summary>
        /// 切割刀片高度
        /// </summary>
        public float CutBladeHeight
        {
            get { return _cutBladeHeight; }
            set { _cutBladeHeight = value; RaisePropertyChanged(); }
        }

        private float _cutSpeed;
        /// <summary>
        /// 磨刀速度
        /// </summary>
        public float CutSpeed
        {
            get { return _cutSpeed; }
            set { _cutSpeed = value; RaisePropertyChanged(); }
        }

        private string _cutProgress;
        /// <summary>
        /// 切割进度
        /// </summary>
        public string CutProgress
        {
            get { return _cutProgress; }
            set { _cutProgress = value; RaisePropertyChanged(); }
        }

        private float _baselineWidth;
        /// <summary>
        /// 基准线宽度
        /// </summary>
        public float BaselineWidth
        {
            get { return _baselineWidth; }
            set { _baselineWidth = value; RaisePropertyChanged(); }
        }

        private float _brokenEdgeWidth;
        /// <summary>
        /// 崩边宽度
        /// </summary>
        public float BrokenEdgeWidth
        {
            get { return _brokenEdgeWidth; }
            set { _brokenEdgeWidth = value; RaisePropertyChanged(); }
        }

        public AutoCutPausingViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
            RightPageButtonCollection = WindowLayout.RightPageButtons;
            OperatePageButtonCollection = WindowLayout.OperatePageButtons;
            ContinueCommand = new RelayCommand(ContinueCommandExecute);
            StopCommand = new RelayCommand(StopCommandExecute);
            //Task monitorTask = StartMonitoringAlarmAsync(default);
        }

        public AutoCutPausingViewModel()
        {
        }

        private void InitRightButton()
        {
            RightPageButtonCollection.Clear();
            RightPageButtonCollection.Add(RightButtonParams.GreenRightButton("继续", "/Assets/icon/right/enter.png", ContinueCommandExecute));
            RightPageButtonCollection.Add(RightButtonParams.RedRightButton("停止", "/Assets/icon/right/stop.png", StopCommandExecute));
        }

        private void InitRightButtonOnlyStop()
        {
            RightPageButtonCollection.Clear();
            RightPageButtonCollection.Add(RightButtonParams.RedRightButton("停止", "/Assets/icon/right/stop.png", StopCommandExecute));
        }

        private void InitBottomButton()
        {
            OperatePageButtonCollection.Clear();
            OperatePageButtonCollection.Add(RightButtonParams.BlueRightButton("工件吹气", "/Assets/icon/tab_1/03/tab_08.png", WorkpieceBlowing, null, 8));
            OperatePageButtonCollection.Add(RightButtonParams.BlueRightButton("基准线校准", "/Assets/icon/tab_1/03/tab_08.png", BaselineCalibration, null, 8));
            OperatePageButtonCollection.Add(RightButtonParams.BlueRightButton("基准线调窄", "/Assets/icon/tab_1/03/tab_02.png", BaselineNarrowing, null, 8));
            OperatePageButtonCollection.Add(RightButtonParams.BlueRightButton("基准线调宽", "/Assets/icon/tab_1/03/tab_05.png", BaselineWidening, null, 8));
            OperatePageButtonCollection.Add(RightButtonParams.BlueRightButton("崩边调窄", "/Assets/icon/tab_1/03/tab_02.png", BrokenEdgeNarrowing, null, 8));
            OperatePageButtonCollection.Add(RightButtonParams.BlueRightButton("崩边调宽", "/Assets/icon/tab_1/03/tab_05.png", BrokenEdgeWidening, null, 8));
            OperatePageButtonCollection.Add(RightButtonParams.BlueRightButton("报废", "/Assets/icon/tab_1/03/tab_08.png", BladeScrap, null, 8));
        }

        private bool _isSureBladeScrap = false;

        private async void BladeScrap()
        {
            if (!_isSureBladeScrap)
            {
                _isSureBladeScrap = true;
                MaterialSnackUtils.MaterialSnack("再次点击报废，刀片将提交报废并退出自动执行！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            InitRightButtonOnlyStop();
            _isSureBladeScrap = false;
            await _autoCutRuningViewModel.StopAsync(ServicePauseResult.BladeScrap);
        }

        private void BaselineWidening()
        {
            _cameraCommon?.SetCutMarkWidth(1, 2);
            UpdateBaselineWidth();
        }

        private void BaselineNarrowing()
        {
            _cameraCommon?.SetCutMarkWidth(-1, 2);
            UpdateBaselineWidth();
        }

        private void BrokenEdgeWidening()
        {
            _cameraCommon?.SetEdgeWidth(1, 2);
            UpdateBrokenEdgeWidth();
        }

        private void BrokenEdgeNarrowing()
        {
            _cameraCommon?.SetEdgeWidth(-1, 2);
            UpdateBrokenEdgeWidth();
        }

        private async void BaselineCalibration()
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

        private async void WorkpieceBlowing()
        {
            await AutoCutUtils.WorkpieceBlowingAsync();
        }

        private void UpdateBaselineWidth()
        {
            BaselineWidth = MathF.Round(_cameraCommon?._cutMarkWidth / 1000 ?? 0, 4);
        }

        private void UpdateBrokenEdgeWidth()
        {
            BrokenEdgeWidth = MathF.Round(_cameraCommon?._edgeChipWidth / 1000 ?? 0, 4);
        }

        public async Task StartMonitoringAlarmAsync(CancellationToken token)
        {
            try
            {
                await MonitoringAlarmAsync(token);
            }
            catch (OperationCanceledException)
            {
                // 正常取消，无需处理
            }
            catch (Exception ex)
            {
                Tools.LogError(ex.Message);
            }
        }

        private async Task MonitoringAlarmAsync(CancellationToken token)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
            while (await timer.WaitForNextTickAsync(token))
            {
                try
                {
                    if (AlarmConfig.Instance.HasActiveErrorAlarm() && RightPageButtonCollection.Any(button => button.ContentText == "继续"))
                    {
                        InitRightButtonOnlyStop();
                    }
                }
                catch (Exception ex)
                {
                    Tools.LogError(ex.Message);
                }
            }
        }

        private void ContinueCommandExecute()
        {
            if (AlarmConfig.Instance.HasActiveErrorAlarm())
            {
                MaterialSnackUtils.MaterialSnack("请先处理错误报警！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            NavigationParameters parameters = new NavigationParameters
            {
                { "SharpenParams", _autoCutRuningViewModel.SharpenParams },
                { "CutParams", _autoCutRuningViewModel.CutParams },
                { "LunguSksj", _autoCutRuningViewModel.LunguSksj }
            };
            _regionManager.RequestNavigate(RegionName.AutoCutStateRegion, nameof(AutoCutRuning), parameters);
        }

        private async void StopCommandExecute()
        {
            await _autoCutRuningViewModel.StopAsync(ServicePauseResult.Stop);
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            InitBottomButton();
            InitRightButton();
            _cameraCommon = AutoCutUtils.GetCameraCommon();
            if (_cameraCommon is not null)
            {
                _cameraCommon.LineChanged += CameraCommon_LineChanged;
            }
            _autoCutRuningViewModel = navigationContext.Parameters.GetValue<AutoCutRuningViewModel>("AutoCutRuningViewModel");
            AfterHeightMeasurementZ = _autoCutRuningViewModel.AfterHeightMeasurementZ;
            SharpenBladeHeight = _autoCutRuningViewModel.SharpenBladeHeight;
            SharpenSpeed = _autoCutRuningViewModel.SharpenSpeed;
            SharpenProgress = _autoCutRuningViewModel.SharpenProgress;
            DeviceDataNo = _autoCutRuningViewModel.DeviceDataNo;
            CutBladeHeight = _autoCutRuningViewModel.CutBladeHeight;
            CutSpeed = _autoCutRuningViewModel.CutSpeed;
            CutProgress = _autoCutRuningViewModel.CutProgress;
            AfterReplaceBladeCutTimes = _autoCutRuningViewModel.AfterReplaceBladeCutTimes;
            UpdateBaselineWidth();
            UpdateBrokenEdgeWidth();
            float? xLocation = await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync();
            float? yLocation = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync();
            // 初始化起始点位置
            if (xLocation != null && yLocation != null)
            {
                _originPoint = new DataPoint<float>(xLocation.Value, yLocation.Value);
            }
        }

        private void CameraCommon_LineChanged()
        {
            UpdateBaselineWidth();
            UpdateBrokenEdgeWidth();
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
            if (_cameraCommon is not null)
            {
                _cameraCommon.LineChanged -= CameraCommon_LineChanged;
            }
        }
    }
}
