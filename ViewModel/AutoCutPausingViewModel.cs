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
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.View.common;
using 精密切割系统.View.Pages.Auto;

namespace 精密切割系统.ViewModel
{
    public class AutoCutPausingViewModel : CustomBindableBase
    {
        public RelayCommand ContinueCommand { get; set; }
        public RelayCommand StopCommand { get; set; }
        private IRegionManager _regionManager;
        private AutoCutRuningViewModel _autoCutRuningViewModel;

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
        }

        public AutoCutPausingViewModel()
        {
        }

        private void InitRightButton()
        {
            RightPageButtonCollection.Add(RightButtonParams.GreenRightButton("继续", "/Assets/icon/right/enter.png", ContinueCommandExecute));
            RightPageButtonCollection.Add(RightButtonParams.RedRightButton("停止", "/Assets/icon/right/stop.png", StopCommandExecute));
        }

        private void InitBottonButton()
        {
            OperatePageButtonCollection.Add(RightButtonParams.BlueRightButton("基准线调窄", "/Assets/icon/tab_1/03/tab_02.png", BaselineNarrowing));
            OperatePageButtonCollection.Add(RightButtonParams.BlueRightButton("基准线校准", "/Assets/icon/tab_1/03/tab_08.png", BaselineCalibration));
            OperatePageButtonCollection.Add(RightButtonParams.BlueRightButton("基准线调宽", "/Assets/icon/tab_1/03/tab_05.png", BaselineWidthAdjustment));
        }

        private void BaselineWidthAdjustment()
        {
        }

        private void BaselineCalibration()
        {
        }

        private void BaselineNarrowing()
        {
        }

        private void ContinueCommandExecute()
        {
            NavigationParameters parameters = new NavigationParameters
            {
                { "SharpenParams", _autoCutRuningViewModel.SharpenParams },
                { "CutParams", _autoCutRuningViewModel.CutParams },
                { "LunguId", _autoCutRuningViewModel.LunguId }
            };
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(AutoCutRuning), parameters);
        }

        private void StopCommandExecute()
        {
            _autoCutRuningViewModel.StopAsync();
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            InitRightButton();
            InitBottonButton();
            _autoCutRuningViewModel = navigationContext.Parameters.GetValue<AutoCutRuningViewModel>("AutoCutRuningViewModel");
            _afterHeightMeasurementZ = _autoCutRuningViewModel.AfterHeightMeasurementZ;
            _sharpenBladeHeight = _autoCutRuningViewModel.SharpenBladeHeight;
            _sharpenSpeed = _autoCutRuningViewModel.SharpenSpeed;
            _sharpenProgress = _autoCutRuningViewModel.SharpenProgress;
            _deviceDataNo = _autoCutRuningViewModel.DeviceDataNo;
            _cutBladeHeight = _autoCutRuningViewModel.CutBladeHeight;
            _cutSpeed = _autoCutRuningViewModel.CutSpeed;
            _cutProgress = _autoCutRuningViewModel.CutProgress;
            _afterReplaceBladeCutTimes = _autoCutRuningViewModel.AfterReplaceBladeCutTimes;
        }
    }
}
