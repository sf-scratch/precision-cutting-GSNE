using OpenCvSharp.XFeatures2D;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.plc;
using 精密切割系统.View.Controls;
using 精密切割系统.View.Pages.F4_BladeMaintenance;
using static NPOI.HSSF.Util.HSSFColor;
using static 精密切割系统.Helpers.MaterialSnackUtils;

namespace 精密切割系统.ViewModel
{
    public class EmptyRunViewModel : CustomBindableBase
    {
        private readonly IRegionManager _regionManager;

        private bool _runX;

        public bool RunX
        {
            get { return _runX; }
            set { SetProperty(ref _runX, value); }
        }

        private string _speedX;

        public string SpeedX
        {
            get { return _speedX; }
            set { SetProperty(ref _speedX, value); }
        }

        private string _repeatCountX;

        public string RepeatCountX
        {
            get { return _repeatCountX; }
            set { SetProperty(ref _repeatCountX, value); }
        }

        private bool _runY;

        public bool RunY
        {
            get { return _runY; }
            set { SetProperty(ref _runY, value); }
        }

        private string _speedY;

        public string SpeedY
        {
            get { return _speedY; }
            set { SetProperty(ref _speedY, value); }
        }

        private string _repeatCountY;

        public string RepeatCountY
        {
            get { return _repeatCountY; }
            set { SetProperty(ref _repeatCountY, value); }
        }

        private bool _runZ1;

        public bool RunZ1
        {
            get { return _runZ1; }
            set { SetProperty(ref _runZ1, value); }
        }

        private string _speedZ1;

        public string SpeedZ1
        {
            get { return _speedZ1; }
            set { SetProperty(ref _speedZ1, value); }
        }

        private string _repeatCountZ1;

        public string RepeatCountZ1
        {
            get { return _repeatCountZ1; }
            set { SetProperty(ref _repeatCountZ1, value); }
        }

        private bool _runZ2;

        public bool RunZ2
        {
            get { return _runZ2; }
            set { SetProperty(ref _runZ2, value); }
        }

        private string _speedZ2;

        public string SpeedZ2
        {
            get { return _speedZ2; }
            set { SetProperty(ref _speedZ2, value); }
        }

        private string _repeatCountZ2;

        public string RepeatCountZ2
        {
            get { return _repeatCountZ2; }
            set { SetProperty(ref _repeatCountZ2, value); }
        }

        private bool _runTheta;

        public bool RunTheta
        {
            get { return _runTheta; }
            set { SetProperty(ref _runTheta, value); }
        }

        private string _speedTheta;

        public string SpeedTheta
        {
            get { return _speedTheta; }
            set { SetProperty(ref _speedTheta, value); }
        }

        private string _repeatCountTheta;

        public string RepeatCountTheta
        {
            get { return _repeatCountTheta; }
            set { SetProperty(ref _repeatCountTheta, value); }
        }

        private bool _isEnabledGrid;

        public bool IsEnabledGrid
        {
            get { return _isEnabledGrid; }
            set { SetProperty(ref _isEnabledGrid, value); }
        }

        private bool _isOpenWater;

        public bool IsOpenWater
        {
            get { return _isOpenWater; }
            set { SetProperty(ref _isOpenWater, value); }
        }

        private bool _isFlowing;

        public bool IsFlowing
        {
            get { return _isFlowing; }
            set { SetProperty(ref _isFlowing, value); }
        }

        private bool _hasAnyError;

        public bool HasAnyError
        {
            get { return _hasAnyError; }
            set { SetProperty(ref _hasAnyError, value); }
        }

        private readonly SemaphoreSlim _emptyRunSemaphore = new(1, 1);
        private CancellationTokenSource? _emptyRunCts;

        public EmptyRunViewModel(IRegionManager regionManager)
        {
            _isEnabledGrid = true;
            _speedX = GlobalParams.XDefaultSpeed.ToString();
            _speedY = GlobalParams.YDefaultSpeed.ToString();
            _speedZ1 = GlobalParams.Z1DefaultSpeed.ToString();
            _speedZ2 = GlobalParams.Z2DefaultSpeed.ToString();
            _speedTheta = GlobalParams.ThetaDefaultSpeed.ToString();
            _repeatCountX = "10";
            _repeatCountY = "10";
            _repeatCountZ1 = "10";
            _repeatCountZ2 = "10";
            _repeatCountTheta = "10";
            _regionManager = regionManager;
        }

        private void InitRightButton()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                RightButtonCollection.Clear();
                RightButtonCollection.Add(ButtonParams.GreenRightButton("空运行", "LocationEnter", ExecuteEmptyRun));
                RightButtonCollection.Add(ButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
            });
        }

        private void InitRuningRightButton()
        {
            RightButtonCollection.Clear();
            RightButtonCollection.Add(ButtonParams.RedRightButton("停止", "/Assets/icon/right/stop.png", Stop));
        }

        private async void ExecuteEmptyRun()
        {
            if (RegionUtils.FormError(_regionManager))
            {
                MaterialSnackUtils.MaterialSnack(RegionUtils.FormErrorMessage, SnackType.WARNING);
                return;
            }
            if (AlarmConfig.Instance.HasActiveErrorAlarm())
            {
                MaterialSnackUtils.MaterialSnack(AlarmConfig.HasErrorAlarmMessage, SnackType.WARNING);
                return;
            }
            if (!await _emptyRunSemaphore.WaitAsync(TimeSpan.Zero))
            {
                MaterialSnackUtils.MaterialSnack("准备空运行中！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            try
            {
                if (!RunX && !RunY && !RunZ1 && !RunZ2 && !RunTheta && !IsOpenWater && !IsFlowing)
                {
                    MaterialSnackUtils.MaterialSnack("请选择至少一个项进行空运行！", MaterialSnackUtils.SnackType.WARNING);
                    return;
                }
                if (_emptyRunCts != null && !_emptyRunCts.IsCancellationRequested)
                {
                    MaterialSnackUtils.MaterialSnack("空运行已在进行中！", MaterialSnackUtils.SnackType.WARNING);
                    return;
                }
                _emptyRunCts?.Dispose();
                _emptyRunCts = new CancellationTokenSource();
                IsEnabledGrid = false;
                InitRuningRightButton();
                _ = AutoCutUtils.MonitoringAlarmAsync(Stop, () => AlarmConfig.Instance.HasActiveErrorAlarm(false), default, _emptyRunCts.Token);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        float speedX = _speedX.ToFloat();
                        float speedY = _speedY.ToFloat();
                        float speedZ1 = _speedZ1.ToFloat();
                        float speedZ2 = _speedZ2.ToFloat();
                        float speedTheta = _speedTheta.ToFloat();
                        int repeatCountX = RunX ? _repeatCountX.ToInt() : 0;
                        int repeatCountY = RunY ? _repeatCountY.ToInt() : 0;
                        int repeatCountZ1 = RunZ1 ? _repeatCountZ1.ToInt() : 0;
                        int repeatCountZ2 = RunZ2 ? _repeatCountZ2.ToInt() : 0;
                        int repeatCountTheta = RunTheta ? _repeatCountTheta.ToInt() : 0;
                        int currentRepeat = 0;
                        int maxRepeatCount = new int[] { repeatCountX, repeatCountY, repeatCountZ1, repeatCountZ2, repeatCountTheta }.Max();
                        if (IsOpenWater)
                        {
                            await PlcControl.tagControl.wholeDevice.OpenCuttingWaterAsync();
                        }
                        if (IsFlowing)
                        {
                            await PlcControl.tagControl.wholeDevice.OpenWorkpieceBlowingAsync();
                        }
                        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
                        List<Task> tasks = [];
                        while (await timer.WaitForNextTickAsync() && currentRepeat < maxRepeatCount)
                        {
                            await PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0, speedZ1, _emptyRunCts.Token);
                            await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(0, speedZ2, _emptyRunCts.Token);
                            if (RunX && currentRepeat < repeatCountX)
                            {
                                tasks.Add(PlcControl.tagControl.Xaxis.StartAbsoluteAsync(Appsettings.PositiveLimitPositionX ?? 0, speedX, _emptyRunCts.Token));
                            }
                            if (RunY && currentRepeat < repeatCountY)
                            {
                                tasks.Add(PlcControl.tagControl.Yaxis.StartAbsoluteAsync(Appsettings.PositiveLimitPositionY ?? 0, speedY, _emptyRunCts.Token));
                            }
                            if (RunTheta && currentRepeat < repeatCountTheta)
                            {
                                tasks.Add(PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(Appsettings.PositiveLimitPositionTheta ?? 0, speedTheta, _emptyRunCts.Token));
                            }
                            await Task.WhenAll(tasks);
                            if (RunX && currentRepeat < repeatCountX)
                            {
                                tasks.Add(PlcControl.tagControl.Xaxis.StartAbsoluteAsync(0, speedX, _emptyRunCts.Token));
                            }
                            if (RunY && currentRepeat < repeatCountY)
                            {
                                tasks.Add(PlcControl.tagControl.Yaxis.StartAbsoluteAsync(0, speedY, _emptyRunCts.Token));
                            }
                            if (RunTheta && currentRepeat < repeatCountTheta)
                            {
                                tasks.Add(PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(0, speedTheta, _emptyRunCts.Token));
                            }
                            await Task.WhenAll(tasks);
                            if (RunZ1 && currentRepeat < repeatCountZ1)
                            {
                                tasks.Add(PlcControl.tagControl.Z1axis.StartAbsoluteAsync(5, speedZ1, _emptyRunCts.Token));
                            }
                            if (RunZ2 && currentRepeat < repeatCountZ2)
                            {
                                tasks.Add(PlcControl.tagControl.Z2axis.StartAbsoluteAsync(5, speedZ2, _emptyRunCts.Token));
                            }
                            await Task.WhenAll(tasks);
                            if (RunZ1 && currentRepeat < repeatCountZ1)
                            {
                                tasks.Add(PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0, speedZ1, _emptyRunCts.Token));
                            }
                            if (RunZ2 && currentRepeat < repeatCountZ2)
                            {
                                tasks.Add(PlcControl.tagControl.Z2axis.StartAbsoluteAsync(0, speedZ2, _emptyRunCts.Token));
                            }
                            await Task.WhenAll(tasks);
                            tasks.Clear();
                            currentRepeat++;
                        }
                        MaterialSnackUtils.MaterialSnack("空运行已完成！", MaterialSnackUtils.SnackType.SUCCESS);
                    }
                    catch (OperationCanceledException)
                    {
                        MaterialSnackUtils.MaterialSnack("空运行已取消！", MaterialSnackUtils.SnackType.WARNING);
                    }
                    catch (Exception ex)
                    {
                        MaterialSnackUtils.MaterialSnack($"空运行发生错误: {ex.Message}", MaterialSnackUtils.SnackType.ERROR);
                    }
                    finally
                    {
                        await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
                        await PlcControl.tagControl.wholeDevice.CloseWorkpieceBlowingAsync();
                        Stop();
                    }
                }, _emptyRunCts.Token);
            }
            finally
            {
                _emptyRunSemaphore.Release();
            }
        }

        private async void Stop()
        {
            if (!await _emptyRunSemaphore.WaitAsync(TimeSpan.Zero))
            {
                MaterialSnackUtils.MaterialSnack("终止空运行中！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            try
            {
                _emptyRunCts?.Cancel();
                IsEnabledGrid = true;
                NavigateUtils.SetWindowIsEnable(false);
                var timeoutToken = TaskUtils.GetTimeoutCancellationToken(TimeSpan.FromSeconds(60));
                await Task.WhenAll(
                    PlcControl.tagControl.Xaxis.WaitAxisStopAsync(timeoutToken.Token),
                    PlcControl.tagControl.Yaxis.WaitAxisStopAsync(timeoutToken.Token),
                    PlcControl.tagControl.Z1axis.WaitAxisStopAsync(timeoutToken.Token),
                    PlcControl.tagControl.Z2axis.WaitAxisStopAsync(timeoutToken.Token),
                    PlcControl.tagControl.ThetaAxis.WaitAxisStopAsync(timeoutToken.Token)
                );
                InitRightButton();
            }
            catch (OperationCanceledException)
            {
                MaterialSnackUtils.MaterialSnack($"停止空运行超时！", MaterialSnackUtils.SnackType.WARNING);
            }
            catch (Exception ex)
            {
                MaterialSnackUtils.MaterialSnack($"停止空运行发生错误: {ex.Message}", MaterialSnackUtils.SnackType.ERROR);
            }
            finally
            {
                NavigateUtils.SetWindowIsEnable(true);
                _emptyRunSemaphore.Release();
            }
        }

        private void Back()
        {
            NavigateUtils.NavigateToPage("MainMenu");
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            NavigateUtils.ClearMainFrame();
            InitRightButton();
        }
    }
}