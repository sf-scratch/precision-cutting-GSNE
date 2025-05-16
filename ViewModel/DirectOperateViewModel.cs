using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Utils;

namespace 精密切割系统.ViewModel
{
    public class DirectOperateViewModel : BindableBase
    {
        private DelegateCommand _startXCommand;
        public DelegateCommand StartXCommand =>
            _startXCommand ?? (_startXCommand = new DelegateCommand(ExecuteStartXCommand));

        async void ExecuteStartXCommand()
        {
            await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(TargetPositionX, default, SpeedX);
        }

        private float _currentPositionX;
        public float CurrentPositionX
        {
            get { return _currentPositionX; }
            set { SetProperty(ref _currentPositionX, value); }
        }

        private float _speedX;
        public float SpeedX
        {
            get { return _speedX; }
            set { SetProperty(ref _speedX, value); }
        }

        private float _targetPositionX;
        public float TargetPositionX
        {
            get { return _targetPositionX; }
            set { SetProperty(ref _targetPositionX, value); }
        }

        private bool _isReadyX;
        public bool IsReadyX
        {
            get { return _isReadyX; }
            set { SetProperty(ref _isReadyX, value); }
        }

        private DelegateCommand _startYCommand;
        public DelegateCommand StartYCommand =>
            _startYCommand ?? (_startYCommand = new DelegateCommand(ExecuteStartYCommand));

        async void ExecuteStartYCommand()
        {
            await PlcControl.tagControl.Yaxis.StartAbsoluteAsync(TargetPositionY, default, SpeedY);
        }

        private float _currentPositionY;
        public float CurrentPositionY
        {
            get { return _currentPositionY; }
            set { SetProperty(ref _currentPositionY, value); }
        }

        private float _speedY;
        public float SpeedY
        {
            get { return _speedY; }
            set { SetProperty(ref _speedY, value); }
        }

        private float _targetPositionY;
        public float TargetPositionY
        {
            get { return _targetPositionY; }
            set { SetProperty(ref _targetPositionY, value); }
        }

        private bool _isReadyY;
        public bool IsReadyY
        {
            get { return _isReadyY; }
            set { SetProperty(ref _isReadyY, value); }
        }

        private DelegateCommand _startZ1Command;
        public DelegateCommand StartZ1Command =>
            _startZ1Command ?? (_startZ1Command = new DelegateCommand(ExecuteStartZ1Command));

        async void ExecuteStartZ1Command()
        {
            await PlcControl.tagControl.Z1axis.StartAbsoluteAsync(TargetPositionZ1, default, SpeedZ1);
        }

        private float _currentPositionZ1;
        public float CurrentPositionZ1
        {
            get { return _currentPositionZ1; }
            set { SetProperty(ref _currentPositionZ1, value); }
        }

        private float _speedZ1;
        public float SpeedZ1
        {
            get { return _speedZ1; }
            set { SetProperty(ref _speedZ1, value); }
        }

        private float _targetPositionZ1;
        public float TargetPositionZ1
        {
            get { return _targetPositionZ1; }
            set { SetProperty(ref _targetPositionZ1, value); }
        }

        private bool _isReadyZ1;
        public bool IsReadyZ1
        {
            get { return _isReadyZ1; }
            set { SetProperty(ref _isReadyZ1, value); }
        }

        private DelegateCommand _startZ2Command;
        public DelegateCommand StartZ2Command =>
            _startZ2Command ?? (_startZ2Command = new DelegateCommand(ExecuteStartZ2Command));

        async void ExecuteStartZ2Command()
        {
            await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(TargetPositionZ2, default, SpeedZ2);
        }

        private float _currentPositionZ2;
        public float CurrentPositionZ2
        {
            get { return _currentPositionZ2; }
            set { SetProperty(ref _currentPositionZ2, value); }
        }

        private float _speedZ2;
        public float SpeedZ2
        {
            get { return _speedZ2; }
            set { SetProperty(ref _speedZ2, value); }
        }

        private float _targetPositionZ2;
        public float TargetPositionZ2
        {
            get { return _targetPositionZ2; }
            set { SetProperty(ref _targetPositionZ2, value); }
        }

        private bool _isReadyZ2;
        public bool IsReadyZ2
        {
            get { return _isReadyZ2; }
            set { SetProperty(ref _isReadyZ2, value); }
        }

        private DelegateCommand _startThetaCommand;
        public DelegateCommand StartThetaCommand =>
            _startThetaCommand ?? (_startThetaCommand = new DelegateCommand(ExecuteStartThetaCommand));

        async void ExecuteStartThetaCommand()
        {
            await PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(TargetPositionTheta, default, SpeedTheta);
        }

        private float _currentPositionTheta;
        public float CurrentPositionTheta
        {
            get { return _currentPositionTheta; }
            set { SetProperty(ref _currentPositionTheta, value); }
        }

        private float _speedTheta;
        public float SpeedTheta
        {
            get { return _speedTheta; }
            set { SetProperty(ref _speedTheta, value); }
        }

        private float _targetPositionTheta;
        public float TargetPositionTheta
        {
            get { return _targetPositionTheta; }
            set { SetProperty(ref _targetPositionTheta, value); }
        }

        private bool _isReadyTheta;
        public bool IsReadyTheta
        {
            get { return _isReadyTheta; }
            set { SetProperty(ref _isReadyTheta, value); }
        }


        public DirectOperateViewModel()
        {
            _speedX = 100;
            _speedY = 100;
            _speedZ1 = 30;
            _speedZ2 = 2;
            _speedTheta = 2;
            Task.Run(() => StartGetAxisInfoAsync(default));
        }

        private async Task StartGetAxisInfoAsync(CancellationToken token)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
            while (await timer.WaitForNextTickAsync(token))
            {
                try
                {
                    CurrentPositionX = await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync()??float.NaN;
                    CurrentPositionY = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync()??float.NaN;
                    CurrentPositionZ1 = await PlcControl.tagControl.Z1axis.GetCurrentLocationAsync()??float.NaN;
                    CurrentPositionZ2 = await PlcControl.tagControl.Z2axis.GetCurrentLocationAsync()??float.NaN;
                    CurrentPositionTheta = await PlcControl.tagControl.ThetaAxis.GetCurrentLocationAsync()??float.NaN;
                    IsReadyX = await PlcControl.tagControl.Xaxis.IsReadyAsync();
                    IsReadyY = await PlcControl.tagControl.Yaxis.IsReadyAsync();
                    IsReadyZ1 = await PlcControl.tagControl.Z1axis.IsReadyAsync();
                    IsReadyZ2 = await PlcControl.tagControl.Z2axis.IsReadyAsync();
                    IsReadyTheta = await PlcControl.tagControl.ThetaAxis.IsReadyAsync();
                }
                catch (Exception ex)
                {
                    Tools.LogError($"报警监控异常: {ex.Message}");
                }
            }
        }
    }
}
