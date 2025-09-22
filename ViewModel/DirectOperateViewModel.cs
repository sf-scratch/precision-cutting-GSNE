using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using 精密切割系统.Behaviors;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Utils;

namespace 精密切割系统.ViewModel
{
    public class DirectOperateViewModel : BindableBase
    {
        private static readonly float RelativeDistance = 0.001f; // 相对移动距离
        private static readonly float RelativeDeg = 0.1f; // 相对角度
        private static readonly float RelativeSpeed = 0.2f; // 相对移动速度
        private CancellationTokenSource _cancelGetAxisInfoCts;

        private DelegateCommand _startXCommand;

        public DelegateCommand StartXCommand =>
            _startXCommand ?? (_startXCommand = new DelegateCommand(ExecuteStartXCommand));

        private async void ExecuteStartXCommand()
        {
            if (IsAbsMoveX)
            {
                await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(TargetPositionX, SpeedX, default);
            }
            else
            {
                await PlcControl.tagControl.Xaxis.StartRelativeAsync(TargetPositionX, SpeedX, default);
            }
        }

        private DelegateCommand _startHomingXCommand;

        public DelegateCommand StartHomingXCommand =>
            _startHomingXCommand ?? (_startHomingXCommand = new DelegateCommand(ExecuteStartHomingXCommand, CanExecuteStartHomingXCommand));

        private async void ExecuteStartHomingXCommand()
        {
            await PlcControl.tagControl.Xaxis.StartHomingAsync();
        }

        private bool CanExecuteStartHomingXCommand()
        {
            return true;
        }

        private DelegateCommand _relaxXCommand;

        public DelegateCommand RelaxXCommand =>
            _relaxXCommand ?? (_relaxXCommand = new DelegateCommand(ExecuteRelaxXCommand));

        private async void ExecuteRelaxXCommand()
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

        private async void ExecuteStartYCommand()
        {
            if (IsAbsMoveY)
            {
                await PlcControl.tagControl.Yaxis.StartAbsoluteAsync(TargetPositionY, SpeedY, default);
            }
            else
            {
                await PlcControl.tagControl.Yaxis.StartRelativeAsync(TargetPositionY, SpeedY, default);
            }
        }

        private DelegateCommand _startHomingYCommand;

        public DelegateCommand StartHomingYCommand =>
            _startHomingYCommand ?? (_startHomingYCommand = new DelegateCommand(ExecuteStartHomingYCommand, CanExecuteStartHomingYCommand));

        private async void ExecuteStartHomingYCommand()
        {
            await PlcControl.tagControl.Yaxis.StartHomingAsync();
        }

        private bool CanExecuteStartHomingYCommand()
        {
            return true;
        }

        private DelegateCommand _relaxYCommand;

        public DelegateCommand RelaxYCommand =>
            _relaxYCommand ?? (_relaxYCommand = new DelegateCommand(ExecuteRelaxYCommand));

        private async void ExecuteRelaxYCommand()
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

        private async void ExecuteStartZ1Command()
        {
            if (IsAbsMoveZ1)
            {
                await PlcControl.tagControl.Z1axis.StartAbsoluteAsync(TargetPositionZ1, SpeedZ1, default);
            }
            else
            {
                await PlcControl.tagControl.Z1axis.StartRelativeAsync(TargetPositionZ1, SpeedZ1, default);
            }
        }

        private DelegateCommand _startHomingZ1Command;

        public DelegateCommand StartHomingZ1Command =>
            _startHomingZ1Command ?? (_startHomingZ1Command = new DelegateCommand(ExecuteStartHomingZ1Command, CanExecuteStartHomingZ1Command));

        private async void ExecuteStartHomingZ1Command()
        {
            await PlcControl.tagControl.Z1axis.StartHomingAsync();
        }

        private bool CanExecuteStartHomingZ1Command()
        {
            return true;
        }

        private DelegateCommand _relaxZ1Command;

        public DelegateCommand RelaxZ1Command =>
            _relaxZ1Command ?? (_relaxZ1Command = new DelegateCommand(ExecuteRelaxZ1Command));

        private async void ExecuteRelaxZ1Command()
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

        private async void ExecuteStartZ2Command()
        {
            if (IsAbsMoveZ2)
            {
                await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(TargetPositionZ2, SpeedZ2, default);
            }
            else
            {
                await PlcControl.tagControl.Z2axis.StartRelativeAsync(TargetPositionZ2, SpeedZ2, default);
            }
        }

        private DelegateCommand _startHomingZ2Command;

        public DelegateCommand StartHomingZ2Command =>
            _startHomingZ2Command ?? (_startHomingZ2Command = new DelegateCommand(ExecuteStartHomingZ2Command, CanExecuteStartHomingZ2Command));

        private async void ExecuteStartHomingZ2Command()
        {
            await PlcControl.tagControl.Z2axis.StartHomingAsync();
        }

        private bool CanExecuteStartHomingZ2Command()
        {
            return true;
        }

        private DelegateCommand _relaxZ2Command;

        public DelegateCommand RelaxZ2Command =>
            _relaxZ2Command ?? (_relaxZ2Command = new DelegateCommand(ExecuteRelaxZ2Command));

        private async void ExecuteRelaxZ2Command()
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

        private async void ExecuteStartThetaCommand()
        {
            await PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(TargetPositionTheta, SpeedTheta, default);
        }

        private DelegateCommand _startHomingThetaCommand;

        public DelegateCommand StartHomingThetaCommand =>
            _startHomingThetaCommand ?? (_startHomingThetaCommand = new DelegateCommand(ExecuteStartHomingThetaCommand, CanExecuteStartHomingThetaCommand));

        private async void ExecuteStartHomingThetaCommand()
        {
            if (IsAbsMoveTheta)
            {
                await PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(TargetPositionTheta, SpeedTheta, default);
            }
            else
            {
                await PlcControl.tagControl.ThetaAxis.StartRelativeAsync(TargetPositionTheta, SpeedTheta, default);
            }
        }

        private bool CanExecuteStartHomingThetaCommand()
        {
            return true;
        }

        private DelegateCommand _relaxThetaCommand;

        public DelegateCommand RelaxThetaCommand =>
            _relaxThetaCommand ?? (_relaxThetaCommand = new DelegateCommand(ExecuteRelaxThetaCommand));

        private async void ExecuteRelaxThetaCommand()
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

        private AsyncDelegateCommand _xRelativeNegativeCommand;

        public AsyncDelegateCommand XRelativeNegativeCommand =>
            _xRelativeNegativeCommand ?? (_xRelativeNegativeCommand = new AsyncDelegateCommand(ExecuteXRelativeNegativeCommand));

        private async Task ExecuteXRelativeNegativeCommand(CancellationToken token)
        {
            try
            {
                await PlcControl.tagControl.Xaxis.StartRelativeUseToJogAsync(-RelativeDistance, RelativeSpeed, token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private AsyncDelegateCommand _xRelativePositiveCommand;

        public AsyncDelegateCommand XRelativePositiveCommand =>
            _xRelativePositiveCommand ?? (_xRelativePositiveCommand = new AsyncDelegateCommand(ExecuteXRelativePositiveCommand));

        private async Task ExecuteXRelativePositiveCommand(CancellationToken token)
        {
            try
            {
                await PlcControl.tagControl.Xaxis.StartRelativeUseToJogAsync(RelativeDistance, RelativeSpeed, token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private DelegateCommand _startXCorotationCommand;

        public DelegateCommand StartXCorotationCommand =>
            _startXCorotationCommand ?? (_startXCorotationCommand = new DelegateCommand(ExecuteStartXCorotationCommand));

        private async void ExecuteStartXCorotationCommand()
        {
            await PlcControl.tagControl.Xaxis.StartJogAsync(0);
        }

        private DelegateCommand _startXReversalCommand;

        public DelegateCommand StartXReversalCommand =>
            _startXReversalCommand ?? (_startXReversalCommand = new DelegateCommand(ExecuteStartXReversalCommand));

        private async void ExecuteStartXReversalCommand()
        {
            await PlcControl.tagControl.Xaxis.StartJogAsync(1);
        }

        private DelegateCommand _stopJogXCommand;

        public DelegateCommand StopJogXCommand =>
            _stopJogXCommand ?? (_stopJogXCommand = new DelegateCommand(ExecuteStopJogXCommand));

        private async void ExecuteStopJogXCommand()
        {
            try
            {
                await using var timeoutToken = TaskUtils.GetTimeoutCancellationToken();
                await PlcControl.tagControl.Xaxis.WaitStopJogAsync(timeoutToken.Token);
            }
            catch (OperationCanceledException) { }
        }

        private AsyncDelegateCommand _yRelativeNegativeCommand;

        public AsyncDelegateCommand YRelativeNegativeCommand =>
            _yRelativeNegativeCommand ?? (_yRelativeNegativeCommand = new AsyncDelegateCommand(ExecuteYRelativeNegativeCommand));

        private async Task ExecuteYRelativeNegativeCommand(CancellationToken token)
        {
            try
            {
                await PlcControl.tagControl.Yaxis.StartRelativeUseToJogAsync(-RelativeDistance, RelativeSpeed, token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private AsyncDelegateCommand _yRelativePositiveCommand;

        public AsyncDelegateCommand YRelativePositiveCommand =>
            _yRelativePositiveCommand ?? (_yRelativePositiveCommand = new AsyncDelegateCommand(ExecuteYRelativePositiveCommand));

        private async Task ExecuteYRelativePositiveCommand(CancellationToken token)
        {
            try
            {
                await PlcControl.tagControl.Yaxis.StartRelativeUseToJogAsync(RelativeDistance, RelativeSpeed, token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private DelegateCommand _startYCorotationCommand;

        public DelegateCommand StartYCorotationCommand =>
            _startYCorotationCommand ?? (_startYCorotationCommand = new DelegateCommand(ExecuteStartYCorotationCommand));

        private async void ExecuteStartYCorotationCommand()
        {
            await PlcControl.tagControl.Yaxis.StartJogAsync(0);
        }

        private DelegateCommand _startYReversalCommand;

        public DelegateCommand StartYReversalCommand =>
            _startYReversalCommand ?? (_startYReversalCommand = new DelegateCommand(ExecuteStartYReversalCommand));

        private async void ExecuteStartYReversalCommand()
        {
            await PlcControl.tagControl.Yaxis.StartJogAsync(1);
        }

        private DelegateCommand _stopJogYCommand;

        public DelegateCommand StopJogYCommand =>
            _stopJogYCommand ?? (_stopJogYCommand = new DelegateCommand(ExecuteStopJogYCommand));

        private async void ExecuteStopJogYCommand()
        {
            try
            {
                await using var timeoutToken = TaskUtils.GetTimeoutCancellationToken();
                await PlcControl.tagControl.Yaxis.WaitStopJogAsync(timeoutToken.Token);
            }
            catch (OperationCanceledException) { }
        }

        private DelegateCommand _startThetaCorotationCommand;

        public DelegateCommand StartThetaCorotationCommand =>
            _startThetaCorotationCommand ?? (_startThetaCorotationCommand = new DelegateCommand(ExecuteStartThetaCorotationCommand));

        private async void ExecuteStartThetaCorotationCommand()
        {
            await PlcControl.tagControl.ThetaAxis.StartJogAsync(0);
        }

        private DelegateCommand _startThetaReversalCommand;

        public DelegateCommand StartThetaReversalCommand =>
            _startThetaReversalCommand ?? (_startThetaReversalCommand = new DelegateCommand(ExecuteStartThetaReversalCommand));

        private async void ExecuteStartThetaReversalCommand()
        {
            await PlcControl.tagControl.ThetaAxis.StartJogAsync(1);
        }

        private DelegateCommand _stopJogThetaCommand;

        public DelegateCommand StopJogThetaCommand =>
            _stopJogThetaCommand ?? (_stopJogThetaCommand = new DelegateCommand(ExecuteStopJogThetaCommand));

        private async void ExecuteStopJogThetaCommand()
        {
            try
            {
                await using var timeoutToken = TaskUtils.GetTimeoutCancellationToken();
                await PlcControl.tagControl.ThetaAxis.WaitStopJogAsync(timeoutToken.Token);
            }
            catch (OperationCanceledException) { }
        }

        private DelegateCommand _startRaiseZ1Command;

        public DelegateCommand StartRaiseZ1Command =>
            _startRaiseZ1Command ?? (_startRaiseZ1Command = new DelegateCommand(ExecuteStartRaiseZ1Command));

        private async void ExecuteStartRaiseZ1Command()
        {
            await PlcControl.tagControl.Z1axis.StartJogAsync(1);
        }

        private DelegateCommand _startDropZ1Command;

        public DelegateCommand StartDropZ1Command =>
            _startDropZ1Command ?? (_startDropZ1Command = new DelegateCommand(ExecuteStartDropZ1Command));

        private async void ExecuteStartDropZ1Command()
        {
            await PlcControl.tagControl.Z1axis.StartJogAsync(0);
        }

        private DelegateCommand _stopJogZ1Command;

        public DelegateCommand StopJogZ1Command =>
            _stopJogZ1Command ?? (_stopJogZ1Command = new DelegateCommand(ExecuteStopJogZ1Command));

        private async void ExecuteStopJogZ1Command()
        {
            try
            {
                await using var timeoutToken = TaskUtils.GetTimeoutCancellationToken();
                await PlcControl.tagControl.Z1axis.WaitStopJogAsync(timeoutToken.Token);
            }
            catch (OperationCanceledException) { }
        }

        private DelegateCommand _startRaiseZ2Command;

        public DelegateCommand StartRaiseZ2Command =>
            _startRaiseZ2Command ?? (_startRaiseZ2Command = new DelegateCommand(ExecuteStartRaiseZ2Command));

        private async void ExecuteStartRaiseZ2Command()
        {
            await PlcControl.tagControl.Z2axis.StartJogAsync(1);
        }

        private DelegateCommand _startDropZ2Command;

        public DelegateCommand StartDropZ2Command =>
            _startDropZ2Command ?? (_startDropZ2Command = new DelegateCommand(ExecuteStartDropZ2Command));

        private async void ExecuteStartDropZ2Command()
        {
            await PlcControl.tagControl.Z2axis.StartJogAsync(0);
        }

        private DelegateCommand _stopJogZ2Command;

        public DelegateCommand StopJogZ2Command =>
            _stopJogZ2Command ?? (_stopJogZ2Command = new DelegateCommand(ExecuteStopJogZ2Command));

        private async void ExecuteStopJogZ2Command()
        {
            try
            {
                await using var timeoutToken = TaskUtils.GetTimeoutCancellationToken();
                await PlcControl.tagControl.Z2axis.WaitStopJogAsync(timeoutToken.Token);
            }
            catch (OperationCanceledException) { }
        }

        private AsyncDelegateCommand _z1RelativePositiveCommand;

        public AsyncDelegateCommand Z1RelativePositiveCommand =>
            _z1RelativePositiveCommand ?? (_z1RelativePositiveCommand = new AsyncDelegateCommand(ExecuteZ1RelativePositiveCommand));

        private async Task ExecuteZ1RelativePositiveCommand(CancellationToken token)
        {
            try
            {
                await PlcControl.tagControl.Z1axis.StartRelativeUseToJogAsync(RelativeDistance, RelativeSpeed, token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private AsyncDelegateCommand _z1RelativeNegativeCommand;

        public AsyncDelegateCommand Z1RelativeNegativeCommand =>
            _z1RelativeNegativeCommand ?? (_z1RelativeNegativeCommand = new AsyncDelegateCommand(ExecuteZ1RelativeNegativeCommand));

        private async Task ExecuteZ1RelativeNegativeCommand(CancellationToken token)
        {
            try
            {
                await PlcControl.tagControl.Z1axis.StartRelativeUseToJogAsync(-RelativeDistance, RelativeSpeed, token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private AsyncDelegateCommand _z2RelativePositiveCommand;

        public AsyncDelegateCommand Z2RelativePositiveCommand =>
            _z2RelativePositiveCommand ?? (_z2RelativePositiveCommand = new AsyncDelegateCommand(ExecuteZ2RelativePositiveCommand));

        private async Task ExecuteZ2RelativePositiveCommand(CancellationToken token)
        {
            try
            {
                await PlcControl.tagControl.Z2axis.StartRelativeUseToJogAsync(RelativeDistance, RelativeSpeed, token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private AsyncDelegateCommand _z2RelativeNegativeCommand;

        public AsyncDelegateCommand Z2RelativeNegativeCommand =>
            _z2RelativeNegativeCommand ?? (_z2RelativeNegativeCommand = new AsyncDelegateCommand(ExecuteZ2RelativeNegativeCommand));

        private async Task ExecuteZ2RelativeNegativeCommand(CancellationToken token)
        {
            try
            {
                await PlcControl.tagControl.Z2axis.StartRelativeUseToJogAsync(-RelativeDistance, RelativeSpeed, token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private AsyncDelegateCommand _thetaRelativePositiveCommand;

        public AsyncDelegateCommand ThetaRelativePositiveCommand =>
            _thetaRelativePositiveCommand ?? (_thetaRelativePositiveCommand = new AsyncDelegateCommand(ExecuteThetaRelativePositiveCommand));

        private async Task ExecuteThetaRelativePositiveCommand(CancellationToken token)
        {
            try
            {
                await PlcControl.tagControl.ThetaAxis.StartRelativeUseToJogAsync(RelativeDeg, RelativeSpeed, token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private AsyncDelegateCommand _thetaRelativeNegativeCommand;

        public AsyncDelegateCommand ThetaRelativeNegativeCommand =>
            _thetaRelativeNegativeCommand ??= new AsyncDelegateCommand(ExecuteThetaRelativeNegativeCommand);

        private async Task ExecuteThetaRelativeNegativeCommand(CancellationToken token)
        {
            try
            {
                await PlcControl.tagControl.ThetaAxis.StartRelativeUseToJogAsync(-RelativeDeg, RelativeSpeed, token);
            }
            catch (OperationCanceledException)
            {
            }
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
                        await PlcControl.tagControl.Xaxis.SetJogRelativeSpeedAsync(8);
                        await PlcControl.tagControl.Yaxis.SetHighSpeedAsync(1);
                        await PlcControl.tagControl.Yaxis.SetJogRelativeSpeedAsync(8);
                        await PlcControl.tagControl.Z1axis.SetHighSpeedAsync(1);
                        await PlcControl.tagControl.Z1axis.SetJogRelativeSpeedAsync(2);
                        await PlcControl.tagControl.Z2axis.SetHighSpeedAsync(1);
                        await PlcControl.tagControl.Z2axis.SetJogRelativeSpeedAsync(2);
                        await PlcControl.tagControl.ThetaAxis.SetHighSpeedAsync(1);
                        await PlcControl.tagControl.ThetaAxis.SetJogRelativeSpeedAsync(10);
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
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
                while (await timer.WaitForNextTickAsync(token))
                {
                    try
                    {
                        var axisPostion = await AutoCutUtils.GetAxisPositionAsync();
                        CurrentPositionX = MathF.Round(axisPostion.X ?? float.NaN, 3);
                        CurrentPositionY = MathF.Round(axisPostion.Y ?? float.NaN, 3);
                        CurrentPositionZ1 = MathF.Round(axisPostion.Z1 ?? float.NaN, 3);
                        CurrentPositionZ2 = MathF.Round(axisPostion.Z2 ?? float.NaN, 3);
                        CurrentPositionTheta = MathF.Round(axisPostion.Theta ?? float.NaN, 3);
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
                        //var axisState = await AutoCutUtils.GetAxisStateAsync();
                        //IsReadyX = axisState.X == true;
                        //IsReadyY = axisState.Y == true;
                        //IsReadyZ1 = axisState.Z1 == true;
                        //IsReadyZ2 = axisState.Z2 == true;
                        //IsReadyTheta = axisState.Theta == true;
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