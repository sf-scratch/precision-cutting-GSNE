using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Utils;

namespace 精密切割系统.ViewModel
{
    public class DirectOperateViewModel : BindableBase
    {
        private CancellationTokenSource _cancelGetAxisInfoCts;

        private DelegateCommand _startXCommand;
        public DelegateCommand StartXCommand =>
            _startXCommand ?? (_startXCommand = new DelegateCommand(ExecuteStartXCommand));

        async void ExecuteStartXCommand()
        {
            if (IsAbsMoveX)
            {
                await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(TargetPositionX, SpeedX);
            }
            else
            {
                await PlcControl.tagControl.Xaxis.StartRelativeAsync(TargetPositionX, SpeedX);
            }
        }

        private DelegateCommand _startHomingXCommand;
        public DelegateCommand StartHomingXCommand =>
            _startHomingXCommand ?? (_startHomingXCommand = new DelegateCommand(ExecuteStartHomingXCommand, CanExecuteStartHomingXCommand));

        async void ExecuteStartHomingXCommand()
        {
            await PlcControl.tagControl.Xaxis.StartHomingAsync();
        }

        bool CanExecuteStartHomingXCommand()
        {
            return true;
        }

        private DelegateCommand _relaxXCommand;
        public DelegateCommand RelaxXCommand =>
            _relaxXCommand ?? (_relaxXCommand = new DelegateCommand(ExecuteRelaxXCommand));

        async void ExecuteRelaxXCommand()
        {
            await PlcControl.tagControl.Xaxis.RelaxAxisAsync();
        }

        private float _currentPositionX;
        public float CurrentPositionX
        {
            get { return _currentPositionX; }
            set { SetProperty(ref _currentPositionX, value); }
        }

        private float _currentJogSpeedX;
        public float CurrentJogSpeedX
        {
            get { return _currentJogSpeedX; }
            set { SetProperty(ref _currentJogSpeedX, value); }
        }

        private float _currentSpeedX;
        public float CurrentSpeedX
        {
            get { return _currentSpeedX; }
            set { SetProperty(ref _currentSpeedX, value); }
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

        private bool _isAbsMoveX;
        public bool IsAbsMoveX
        {
            get { return _isAbsMoveX; }
            set { SetProperty(ref _isAbsMoveX, value); }
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
            if (IsAbsMoveY)
            {
                await PlcControl.tagControl.Yaxis.StartAbsoluteAsync(TargetPositionY, SpeedY);
            }
            else
            {
                await PlcControl.tagControl.Yaxis.StartRelativeAsync(TargetPositionY, SpeedY);
            }
        }

        private DelegateCommand _startHomingYCommand;
        public DelegateCommand StartHomingYCommand =>
            _startHomingYCommand ?? (_startHomingYCommand = new DelegateCommand(ExecuteStartHomingYCommand, CanExecuteStartHomingYCommand));

        async void ExecuteStartHomingYCommand()
        {
            await PlcControl.tagControl.Yaxis.StartHomingAsync();
        }

        bool CanExecuteStartHomingYCommand()
        {
            return true;
        }

        private DelegateCommand _relaxYCommand;
        public DelegateCommand RelaxYCommand =>
            _relaxYCommand ?? (_relaxYCommand = new DelegateCommand(ExecuteRelaxYCommand));

        async void ExecuteRelaxYCommand()
        {
            await PlcControl.tagControl.Yaxis.RelaxAxisAsync();
        }

        private float _currentPositionY;
        public float CurrentPositionY
        {
            get { return _currentPositionY; }
            set { SetProperty(ref _currentPositionY, value); }
        }

        private float _currentJogSpeedY;
        public float CurrentJogSpeedY
        {
            get { return _currentJogSpeedY; }
            set { SetProperty(ref _currentJogSpeedY, value); }
        }

        private float _currentSpeedY;
        public float CurrentSpeedY
        {
            get { return _currentSpeedY; }
            set { SetProperty(ref _currentSpeedY, value); }
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

        private bool _isAbsMoveY;
        public bool IsAbsMoveY
        {
            get { return _isAbsMoveY; }
            set { SetProperty(ref _isAbsMoveY, value); }
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
            if (IsAbsMoveZ1)
            {
                await PlcControl.tagControl.Z1axis.StartAbsoluteAsync(TargetPositionZ1, SpeedZ1);
            }
            else
            {
                await PlcControl.tagControl.Z1axis.StartRelativeAsync(TargetPositionZ1, SpeedZ1);
            }
        }

        private DelegateCommand _startHomingZ1Command;
        public DelegateCommand StartHomingZ1Command =>
            _startHomingZ1Command ?? (_startHomingZ1Command = new DelegateCommand(ExecuteStartHomingZ1Command, CanExecuteStartHomingZ1Command));

        async void ExecuteStartHomingZ1Command()
        {
            await PlcControl.tagControl.Z1axis.StartHomingAsync();
        }

        bool CanExecuteStartHomingZ1Command()
        {
            return true;
        }

        private DelegateCommand _relaxZ1Command;
        public DelegateCommand RelaxZ1Command =>
            _relaxZ1Command ?? (_relaxZ1Command = new DelegateCommand(ExecuteRelaxZ1Command));

        async void ExecuteRelaxZ1Command()
        {
            await PlcControl.tagControl.Z1axis.RelaxAxisAsync();
        }

        private float _currentPositionZ1;
        public float CurrentPositionZ1
        {
            get { return _currentPositionZ1; }
            set { SetProperty(ref _currentPositionZ1, value); }
        }

        private float _currentJogSpeedZ1;
        public float CurrentJogSpeedZ1
        {
            get { return _currentJogSpeedZ1; }
            set { SetProperty(ref _currentJogSpeedZ1, value); }
        }

        private float _currentSpeedZ1;
        public float CurrentSpeedZ1
        {
            get { return _currentSpeedZ1; }
            set { SetProperty(ref _currentSpeedZ1, value); }
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

        private bool _isAbsMoveZ1;
        public bool IsAbsMoveZ1
        {
            get { return _isAbsMoveZ1; }
            set { SetProperty(ref _isAbsMoveZ1, value); }
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
            if (IsAbsMoveZ2)
            {

               await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(TargetPositionZ2, SpeedZ2);
            }
            else
            {
                await PlcControl.tagControl.Z2axis.StartRelativeAsync(TargetPositionZ2, SpeedZ2);
            }
        }

        private DelegateCommand _startHomingZ2Command;
        public DelegateCommand StartHomingZ2Command =>
            _startHomingZ2Command ?? (_startHomingZ2Command = new DelegateCommand(ExecuteStartHomingZ2Command, CanExecuteStartHomingZ2Command));

        async void ExecuteStartHomingZ2Command()
        {
            await PlcControl.tagControl.Z2axis.StartHomingAsync();
        }

        bool CanExecuteStartHomingZ2Command()
        {
            return true;
        }

        private DelegateCommand _relaxZ2Command;
        public DelegateCommand RelaxZ2Command =>
            _relaxZ2Command ?? (_relaxZ2Command = new DelegateCommand(ExecuteRelaxZ2Command));

        async void ExecuteRelaxZ2Command()
        {
            await PlcControl.tagControl.Z2axis.RelaxAxisAsync();
        }

        private float _currentPositionZ2;
        public float CurrentPositionZ2
        {
            get { return _currentPositionZ2; }
            set { SetProperty(ref _currentPositionZ2, value); }
        }

        private float _currentJogSpeedZ2;
        public float CurrentJogSpeedZ2
        {
            get { return _currentJogSpeedZ2; }
            set { SetProperty(ref _currentJogSpeedZ2, value); }
        }

        private float _currentSpeedZ2;
        public float CurrentSpeedZ2
        {
            get { return _currentSpeedZ2; }
            set { SetProperty(ref _currentSpeedZ2, value); }
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

        private bool _isAbsMoveZ2;
        public bool IsAbsMoveZ2
        {
            get { return _isAbsMoveZ2; }
            set { SetProperty(ref _isAbsMoveZ2, value); }
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
            await PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(TargetPositionTheta, SpeedTheta);
        }

        private DelegateCommand _startHomingThetaCommand;
        public DelegateCommand StartHomingThetaCommand =>
            _startHomingThetaCommand ?? (_startHomingThetaCommand = new DelegateCommand(ExecuteStartHomingThetaCommand, CanExecuteStartHomingThetaCommand));

        async void ExecuteStartHomingThetaCommand()
        {
            if (IsAbsMoveTheta)
            {
                await PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(TargetPositionTheta, SpeedTheta);
            }
            else
            {
                await PlcControl.tagControl.ThetaAxis.StartRelativeAsync(TargetPositionTheta, SpeedTheta);
            }
        }

        bool CanExecuteStartHomingThetaCommand()
        {
            return true;
        }

        private DelegateCommand _relaxThetaCommand;
        public DelegateCommand RelaxThetaCommand =>
            _relaxThetaCommand ?? (_relaxThetaCommand = new DelegateCommand(ExecuteRelaxThetaCommand));

        async void ExecuteRelaxThetaCommand()
        {
            await PlcControl.tagControl.ThetaAxis.RelaxAxisAsync();
        }

        private float _currentPositionTheta;
        public float CurrentPositionTheta
        {
            get { return _currentPositionTheta; }
            set { SetProperty(ref _currentPositionTheta, value); }
        }

        private float _currentJogSpeedTheta;
        public float CurrentJogSpeedTheta
        {
            get { return _currentJogSpeedTheta; }
            set { SetProperty(ref _currentJogSpeedTheta, value); }
        }

        private float _currentSpeedTheta;
        public float CurrentSpeedTheta
        {
            get { return _currentSpeedTheta; }
            set { SetProperty(ref _currentSpeedTheta, value); }
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

        private bool _isAbsMoveTheta;
        public bool IsAbsMoveTheta
        {
            get { return _isAbsMoveTheta; }
            set { SetProperty(ref _isAbsMoveTheta, value); }
        }

        private bool _isReadyTheta;
        public bool IsReadyTheta
        {
            get { return _isReadyTheta; }
            set { SetProperty(ref _isReadyTheta, value); }
        }

        private bool _isShowKeyboard;
        public bool IsShowKeyboard
        {
            get { return _isShowKeyboard; }
            set { SetProperty(ref _isShowKeyboard, value); }
        }

        private DelegateCommand _startXCorotationCommand;
        public DelegateCommand StartXCorotationCommand =>
            _startXCorotationCommand ?? (_startXCorotationCommand = new DelegateCommand(ExecuteStartXCorotationCommand));

        async void ExecuteStartXCorotationCommand()
        {
            await PlcControl.tagControl.Xaxis.StartJogAsync(0);
        }

        private DelegateCommand _startXReversalCommand;
        public DelegateCommand StartXReversalCommand =>
            _startXReversalCommand ?? (_startXReversalCommand = new DelegateCommand(ExecuteStartXReversalCommand));

        async void ExecuteStartXReversalCommand()
        {
            await PlcControl.tagControl.Xaxis.StartJogAsync(1);
        }

        private DelegateCommand _stopJogXCommand;
        public DelegateCommand StopJogXCommand =>
            _stopJogXCommand ?? (_stopJogXCommand = new DelegateCommand(ExecuteStopJogXCommand));

        async void ExecuteStopJogXCommand()
        {
            await PlcControl.tagControl.Xaxis.StopJogAsync();
        }

        private DelegateCommand _startYCorotationCommand;
        public DelegateCommand StartYCorotationCommand =>
            _startYCorotationCommand ?? (_startYCorotationCommand = new DelegateCommand(ExecuteStartYCorotationCommand));

        async void ExecuteStartYCorotationCommand()
        {
            await PlcControl.tagControl.Yaxis.StartJogAsync(0);
        }

        private DelegateCommand _startYReversalCommand;
        public DelegateCommand StartYReversalCommand =>
            _startYReversalCommand ?? (_startYReversalCommand = new DelegateCommand(ExecuteStartYReversalCommand));

        async void ExecuteStartYReversalCommand()
        {
            await PlcControl.tagControl.Yaxis.StartJogAsync(1);
        }

        private DelegateCommand _stopJogYCommand;
        public DelegateCommand StopJogYCommand =>
            _stopJogYCommand ?? (_stopJogYCommand = new DelegateCommand(ExecuteStopJogYCommand));

        async void ExecuteStopJogYCommand()
        {
            await PlcControl.tagControl.Yaxis.StopJogAsync();
        }

        private DelegateCommand _startThetaCorotationCommand;
        public DelegateCommand StartThetaCorotationCommand =>
            _startThetaCorotationCommand ?? (_startThetaCorotationCommand = new DelegateCommand(ExecuteStartThetaCorotationCommand));

        async void ExecuteStartThetaCorotationCommand()
        {
            await PlcControl.tagControl.ThetaAxis.StartJogAsync(0);
        }

        private DelegateCommand _startThetaReversalCommand;
        public DelegateCommand StartThetaReversalCommand =>
            _startThetaReversalCommand ?? (_startThetaReversalCommand = new DelegateCommand(ExecuteStartThetaReversalCommand));

        async void ExecuteStartThetaReversalCommand()
        {
            await PlcControl.tagControl.ThetaAxis.StartJogAsync(1);
        }

        private DelegateCommand _stopJogThetaCommand;
        public DelegateCommand StopJogThetaCommand =>
            _stopJogThetaCommand ?? (_stopJogThetaCommand = new DelegateCommand(ExecuteStopJogThetaCommand));

        async void ExecuteStopJogThetaCommand()
        {
            await PlcControl.tagControl.ThetaAxis.StopJogAsync();
        }

        private DelegateCommand _startRaiseZ1Command;
        public DelegateCommand StartRaiseZ1Command =>
            _startRaiseZ1Command ?? (_startRaiseZ1Command = new DelegateCommand(ExecuteStartRaiseZ1Command));

        async void ExecuteStartRaiseZ1Command()
        {
            await PlcControl.tagControl.Z1axis.StartJogAsync(1);
        }

        private DelegateCommand _startDropZ1Command;
        public DelegateCommand StartDropZ1Command =>
            _startDropZ1Command ?? (_startDropZ1Command = new DelegateCommand(ExecuteStartDropZ1Command));

        async void ExecuteStartDropZ1Command()
        {
            await PlcControl.tagControl.Z1axis.StartJogAsync(0);
        }

        private DelegateCommand _stopJogZ1Command;
        public DelegateCommand StopJogZ1Command =>
            _stopJogZ1Command ?? (_stopJogZ1Command = new DelegateCommand(ExecuteStopJogZ1Command));

        async void ExecuteStopJogZ1Command()
        {
            await PlcControl.tagControl.Z1axis.StopJogAsync();
        }

        private DelegateCommand _startRaiseZ2Command;
        public DelegateCommand StartRaiseZ2Command =>
            _startRaiseZ2Command ?? (_startRaiseZ2Command = new DelegateCommand(ExecuteStartRaiseZ2Command));

        async void ExecuteStartRaiseZ2Command()
        {
            await PlcControl.tagControl.Z2axis.StartJogAsync(0);
        }

        private DelegateCommand _startDropZ2Command;
        public DelegateCommand StartDropZ2Command =>
            _startDropZ2Command ?? (_startDropZ2Command = new DelegateCommand(ExecuteStartDropZ2Command));

        async void ExecuteStartDropZ2Command()
        {
            await PlcControl.tagControl.Z2axis.StartJogAsync(1);
        }

        private DelegateCommand _stopJogZ2Command;
        public DelegateCommand StopJogZ2Command =>
            _stopJogZ2Command ?? (_stopJogZ2Command = new DelegateCommand(ExecuteStopJogZ2Command));

        async void ExecuteStopJogZ2Command()
        {
            await PlcControl.tagControl.Z2axis.StopJogAsync();
        }

        private bool _isHighSpeed;
        public bool IsHighSpeed
        {
            get { return _isHighSpeed; }
            set 
            { 
                SetProperty(ref _isHighSpeed, value);
                if (_isHighSpeed)
                {
                    Task.Run(async () =>
                    {
                        // 设置为高速
                        await PlcControl.tagControl.Xaxis.SetHighSpeedAsync(1);
                        await PlcControl.tagControl.Xaxis.SetJogRelativeSpeedAsync(GlobalParams.XDefaultSpeed);
                        await PlcControl.tagControl.Yaxis.SetHighSpeedAsync(1);
                        await PlcControl.tagControl.Yaxis.SetJogRelativeSpeedAsync(GlobalParams.YDefaultSpeed);
                        await PlcControl.tagControl.Z1axis.SetHighSpeedAsync(1);
                        await PlcControl.tagControl.Z1axis.SetJogRelativeSpeedAsync(GlobalParams.Z1DefaultSpeed);
                        await PlcControl.tagControl.Z2axis.SetHighSpeedAsync(1);
                        await PlcControl.tagControl.Z2axis.SetJogRelativeSpeedAsync(GlobalParams.Z2DefaultSpeed);
                        await PlcControl.tagControl.ThetaAxis.SetHighSpeedAsync(1);
                        await PlcControl.tagControl.ThetaAxis.SetJogRelativeSpeedAsync(GlobalParams.ThetaDefaultSpeed);
                    });
                }
                else
                {
                    Task.Run(async () =>
                    {
                        // 设置为低速
                        await PlcControl.tagControl.Xaxis.SetHighSpeedAsync(0);
                        await PlcControl.tagControl.Yaxis.SetHighSpeedAsync(0);
                        await PlcControl.tagControl.Z1axis.SetHighSpeedAsync(0);
                        await PlcControl.tagControl.Z2axis.SetHighSpeedAsync(0);
                        await PlcControl.tagControl.ThetaAxis.SetHighSpeedAsync(0);
                    });
                }
            }
        }

        public DirectOperateViewModel()
        {
            _speedX = 10;
            _speedY = 10;
            _speedZ1 = 3;
            _speedZ2 = 0.2f;
            _speedTheta = 2;
            IsShowKeyboard = true;
        }

        public void StartGetAxisInfo()
        {
            _cancelGetAxisInfoCts = new CancellationTokenSource();
            CancellationToken token = _cancelGetAxisInfoCts.Token;
            Task.Run(async () =>
            {
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1000));
                while (await timer.WaitForNextTickAsync(token))
                {
                    try
                    {
                        CurrentPositionX = await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync() ?? float.NaN;
                        CurrentPositionY = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync() ?? float.NaN;
                        CurrentPositionZ1 = await PlcControl.tagControl.Z1axis.GetCurrentLocationAsync() ?? float.NaN;
                        CurrentPositionZ2 = await PlcControl.tagControl.Z2axis.GetCurrentLocationAsync() ?? float.NaN;
                        CurrentPositionTheta = await PlcControl.tagControl.ThetaAxis.GetCurrentLocationAsync() ?? float.NaN;
                        //CurrentSpeedX = await PlcControl.tagControl.Xaxis.GetAbsoluteSpeedAsync() ?? float.NaN;
                        //CurrentSpeedY = await PlcControl.tagControl.Yaxis.GetAbsoluteSpeedAsync() ?? float.NaN;
                        //CurrentSpeedZ1 = await PlcControl.tagControl.Z1axis.GetAbsoluteSpeedAsync() ?? float.NaN;
                        //CurrentSpeedZ2 = await PlcControl.tagControl.Z2axis.GetAbsoluteSpeedAsync() ?? float.NaN;
                        //CurrentSpeedTheta = await PlcControl.tagControl.ThetaAxis.GetAbsoluteSpeedAsync() ?? float.NaN;
                        //CurrentJogSpeedX = await PlcControl.tagControl.Xaxis.GetJogRelativeSpeedAsync() ?? float.NaN;
                        //CurrentJogSpeedY = await PlcControl.tagControl.Yaxis.GetJogRelativeSpeedAsync() ?? float.NaN;
                        //CurrentJogSpeedZ1 = await PlcControl.tagControl.Z1axis.GetJogRelativeSpeedAsync() ?? float.NaN;
                        //CurrentJogSpeedZ2 = await PlcControl.tagControl.Z2axis.GetJogRelativeSpeedAsync() ?? float.NaN;
                        //CurrentJogSpeedTheta = await PlcControl.tagControl.ThetaAxis.GetJogRelativeSpeedAsync() ?? float.NaN;
                        //IsReadyX = await PlcControl.tagControl.Xaxis.IsReadyAsync();
                        //IsReadyY = await PlcControl.tagControl.Yaxis.IsReadyAsync();
                        //IsReadyZ1 = await PlcControl.tagControl.Z1axis.IsReadyAsync();
                        //IsReadyZ2 = await PlcControl.tagControl.Z2axis.IsReadyAsync();
                        //IsReadyTheta = await PlcControl.tagControl.ThetaAxis.IsReadyAsync();
                    }
                    catch (Exception ex)
                    {
                        Tools.LogError($"StartGetAxisInfo()报警监控异常: {ex.Message}");
                    }
                }
            });
        }

        public async Task StopGetAxisInfoAsync()
        {
            await _cancelGetAxisInfoCts.CancelAsync();
        }
    }
}
