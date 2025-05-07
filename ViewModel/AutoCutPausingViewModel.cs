using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;

namespace 精密切割系统.ViewModel
{
    public class AutoCutPausingViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public RelayCommand ContinueCommand { get; set; }
        public RelayCommand StopCommand { get; set; }
        private AutoCutRuningViewModel _autoCutRuningViewModel;

        private static int _afterReplaceBladeCutTimes;
        /// <summary>
        /// 自更换刀片起刀片切了几道
        /// </summary>
        public int AfterReplaceBladeCutTimes
        {
            get { return _afterReplaceBladeCutTimes; }
            set { _afterReplaceBladeCutTimes = value; OnPropertyChanged(); }
        }

        private float _afterHeightMeasurementZ;
        /// <summary>
        /// 测高位置
        /// </summary>
        public float AfterHeightMeasurementZ
        {
            get { return _afterHeightMeasurementZ; }
            set { _afterHeightMeasurementZ = value; OnPropertyChanged(); }
        }

        private float _sharpenBladeHeight;
        /// <summary>
        /// 磨刀片高度
        /// </summary>
        public float SharpenBladeHeight
        {
            get { return _sharpenBladeHeight; }
            set { _sharpenBladeHeight = value; OnPropertyChanged(); }
        }

        private float _sharpenSpeed;
        /// <summary>
        /// 磨刀速度
        /// </summary>
        public float SharpenSpeed
        {
            get { return _sharpenSpeed; }
            set { _sharpenSpeed = value; OnPropertyChanged(); }
        }

        private string _sharpenProgress;
        /// <summary>
        /// 磨刀进度
        /// </summary>
        public string SharpenProgress
        {
            get { return _sharpenProgress; }
            set { _sharpenProgress = value; OnPropertyChanged(); }
        }

        private string _deviceDataNo;
        /// <summary>
        /// 型号参数No
        /// </summary>
        public string DeviceDataNo
        {
            get { return _deviceDataNo; }
            set { _deviceDataNo = value; OnPropertyChanged(); }
        }

        private float _cutBladeHeight;
        /// <summary>
        /// 切割刀片高度
        /// </summary>
        public float CutBladeHeight
        {
            get { return _cutBladeHeight; }
            set { _cutBladeHeight = value; OnPropertyChanged(); }
        }

        private float _cutSpeed;
        /// <summary>
        /// 磨刀速度
        /// </summary>
        public float CutSpeed
        {
            get { return _cutSpeed; }
            set { _cutSpeed = value; OnPropertyChanged(); }
        }

        private string _cutProgress;
        /// <summary>
        /// 切割进度
        /// </summary>
        public string CutProgress
        {
            get { return _cutProgress; }
            set { _cutProgress = value; OnPropertyChanged(); }
        }

        private float _baselineWidth;
        /// <summary>
        /// 基准线宽度
        /// </summary>
        public float BaselineWidth
        {
            get { return _baselineWidth; }
            set { _baselineWidth = value; OnPropertyChanged(); }
        }

        private float _brokenEdgeWidth;
        /// <summary>
        /// 崩边宽度
        /// </summary>
        public float BrokenEdgeWidth
        {
            get { return _brokenEdgeWidth; }
            set { _brokenEdgeWidth = value; OnPropertyChanged(); }
        }

        public AutoCutPausingViewModel(AutoCutRuningViewModel autoCutRuningViewModel)
        {
            _autoCutRuningViewModel = autoCutRuningViewModel;
            InitRightButton();
            _afterHeightMeasurementZ = autoCutRuningViewModel.AfterHeightMeasurementZ;
            _sharpenBladeHeight = autoCutRuningViewModel.SharpenBladeHeight;
            _sharpenSpeed = autoCutRuningViewModel.SharpenSpeed;
            _sharpenProgress = autoCutRuningViewModel.SharpenProgress;
            _deviceDataNo = autoCutRuningViewModel.DeviceDataNo;
            _cutBladeHeight = autoCutRuningViewModel.CutBladeHeight;
            _cutSpeed = autoCutRuningViewModel.CutSpeed;
            _cutProgress = autoCutRuningViewModel.CutProgress;
            _afterReplaceBladeCutTimes = autoCutRuningViewModel.AfterReplaceBladeCutTimes;
            ContinueCommand = new RelayCommand(ContinueCommandExecute);
            StopCommand = new RelayCommand(StopCommandExecute);
        }

        public AutoCutPausingViewModel()
        {
        }

        private void InitRightButton()
        {
            _autoCutRuningViewModel.RightButtonParamsCollection.Clear();
            _autoCutRuningViewModel.RightButtonParamsCollection.Add(RightButtonParams.GreenRightButton("继续", "/Assets/icon/right/enter.png", ContinueCommandExecute));
            _autoCutRuningViewModel.RightButtonParamsCollection.Add(RightButtonParams.RedRightButton("停止", "/Assets/icon/right/stop.png", StopCommandExecute));
        }

        private void ContinueCommandExecute()
        {
            NavigateUtils.NavigateToPage("Pages/Auto/AutoCutRuning", _autoCutRuningViewModel);
        }

        private void StopCommandExecute()
        {
            _autoCutRuningViewModel.StopAsync();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
