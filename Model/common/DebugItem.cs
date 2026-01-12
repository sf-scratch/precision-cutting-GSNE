using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Helpers;
using 精密切割系统.ViewModel;


namespace 精密切割系统.Model.common
{
    internal class DebugItem : BindableBase
    {
        private AsyncDelegateCommand _setCurrentPositionCommand;

        public AsyncDelegateCommand SetCurrentPositionCommand =>
            _setCurrentPositionCommand ?? (_setCurrentPositionCommand = new AsyncDelegateCommand(ExecuteSetCurrentPositionCommand));

        private async Task ExecuteSetCurrentPositionCommand()
        {
            if (IsCheckedX)
            {
                XPosition = (await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync())?.ToString(GlobalParams.DecimalStringFormat) ?? "未读取到当前位置";
            }
            if (IsCheckedY)
            {
                YPosition = (await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync())?.ToString(GlobalParams.DecimalStringFormat) ?? "未读取到当前位置";
            }
            if (IsCheckedZ1)
            {
                Z1Position = (await PlcControl.tagControl.Z1axis.GetCurrentLocationAsync())?.ToString(GlobalParams.DecimalStringFormat) ?? "未读取到当前位置";
            }
            if (IsCheckedZ2)
            {
                Z2Position = (await PlcControl.tagControl.Z2axis.GetCurrentLocationAsync())?.ToString(GlobalParams.DecimalStringFormat) ?? "未读取到当前位置";
            }
            if (IsCheckedTheta)
            {
                ThetaPosition = (await PlcControl.tagControl.ThetaAxis.GetCurrentLocationAsync())?.ToString(GlobalParams.DecimalStringFormat) ?? "未读取到当前位置";
            }
        }

        private string _itemName;

        public string ItemName
        {
            get { return _itemName; }
            set { SetProperty(ref _itemName, value); }
        }

        private bool _IsChecked;

        public bool IsChecked
        {
            get { return _IsChecked; }
            set { SetProperty(ref _IsChecked, value); }
        }

        private string _xPosition;

        public string XPosition
        {
            get { return _xPosition; }
            set { SetProperty(ref _xPosition, value); }
        }

        private string _yPosition;

        public string YPosition
        {
            get { return _yPosition; }
            set { SetProperty(ref _yPosition, value); }
        }

        private string _z1Position;

        public string Z1Position
        {
            get { return _z1Position; }
            set { SetProperty(ref _z1Position, value); }
        }

        private string _z2Position;

        public string Z2Position
        {
            get { return _z2Position; }
            set { SetProperty(ref _z2Position, value); }
        }

        private string _thetaPosition;

        public string ThetaPosition
        {
            get { return _thetaPosition; }
            set { SetProperty(ref _thetaPosition, value); }
        }

        private bool _isCheckedX;

        public bool IsCheckedX
        {
            get { return _isCheckedX; }
            set { SetProperty(ref _isCheckedX, value); }
        }

        private bool _isCheckedY;

        public bool IsCheckedY
        {
            get { return _isCheckedY; }
            set { SetProperty(ref _isCheckedY, value); }
        }

        private bool _isCheckedZ1;

        public bool IsCheckedZ1
        {
            get { return _isCheckedZ1; }
            set { SetProperty(ref _isCheckedZ1, value); }
        }

        private bool _isCheckedZ2;

        public bool IsCheckedZ2
        {
            get { return _isCheckedZ2; }
            set { SetProperty(ref _isCheckedZ2, value); }
        }

        private bool _isCheckedTheta;

        public bool IsCheckedTheta
        {
            get { return _isCheckedTheta; }
            set { SetProperty(ref _isCheckedTheta, value); }
        }
    }
}