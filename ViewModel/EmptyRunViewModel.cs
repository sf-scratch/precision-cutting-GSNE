using OpenCvSharp.XFeatures2D;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.plc;
using 精密切割系统.View.Pages.F4_BladeMaintenance;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.ViewModel
{
    public class EmptyRunViewModel : CustomBindableBase
    {
        private bool _runX;

        public bool RunX
        {
            get { return _runX; }
            set { SetProperty(ref _runX, value); }
        }

        private bool _runY;

        public bool RunY
        {
            get { return _runY; }
            set { SetProperty(ref _runY, value); }
        }

        private bool _runZ1;

        public bool RunZ1
        {
            get { return _runZ1; }
            set { SetProperty(ref _runZ1, value); }
        }

        private bool _runZ2;

        public bool RunZ2
        {
            get { return _runZ2; }
            set { SetProperty(ref _runZ2, value); }
        }

        private bool _runTheta;

        public bool RunTheta
        {
            get { return _runTheta; }
            set { SetProperty(ref _runTheta, value); }
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

        private SemaphoreSlim _emptyRunSemaphore;
        private CancellationTokenSource _emptyRunCts;

        public EmptyRunViewModel()
        {
            _isEnabledGrid = true;
            _emptyRunSemaphore = new SemaphoreSlim(1, 1);
        }

        private void InitRightButton()
        {
            RightButtonCollection.Clear();
            RightButtonCollection.Add(RightButtonParams.GreenRightButton("空运行", "LocationEnter", ExecuteEmptyRun));
            RightButtonCollection.Add(RightButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private void InitRuningRightButton()
        {
            RightButtonCollection.Clear();
            RightButtonCollection.Add(RightButtonParams.RedRightButton("停止", "/Assets/icon/right/stop.png", Stop));
        }

        private async void ExecuteEmptyRun()
        {
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
                _ = AutoCutUtils.MonitoringAlarmAsync(Stop, AlarmConfig.Instance.HasActiveErrorAlarm, default, _emptyRunCts.Token);
                _ = Task.Run(async () =>
                {
                    try
                    {
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
                        while (await timer.WaitForNextTickAsync())
                        {
                            await PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0, default, _emptyRunCts.Token);
                            await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(0, default, _emptyRunCts.Token);
                            if (RunX)
                            {
                                tasks.Add(PlcControl.tagControl.Xaxis.StartAbsoluteAsync(PlcControl.tagControl.Xaxis.softUpperLimit.defaultValue.ToFloat(), default, _emptyRunCts.Token));
                            }
                            if (RunY)
                            {
                                tasks.Add(PlcControl.tagControl.Yaxis.StartAbsoluteAsync(PlcControl.tagControl.Yaxis.softUpperLimit.defaultValue.ToFloat(), default, _emptyRunCts.Token));
                            }
                            if (RunTheta)
                            {
                                tasks.Add(PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(PlcControl.tagControl.ThetaAxis.softUpperLimit.defaultValue.ToFloat(), default, _emptyRunCts.Token));
                            }
                            await Task.WhenAll(tasks);
                            if (RunX)
                            {
                                tasks.Add(PlcControl.tagControl.Xaxis.StartAbsoluteAsync(0, default, _emptyRunCts.Token));
                            }
                            if (RunY)
                            {
                                tasks.Add(PlcControl.tagControl.Yaxis.StartAbsoluteAsync(0, default, _emptyRunCts.Token));
                            }
                            if (RunTheta)
                            {
                                tasks.Add(PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(0, default, _emptyRunCts.Token));
                            }
                            await Task.WhenAll(tasks);
                            if (RunZ1)
                            {
                                //tasks.Add(PlcControl.tagControl.Z1axis.StartAbsoluteAsync(PlcControl.tagControl.Z1axis.softUpperLimit.value.ToFloat(), default, _emptyRunCts.Token));
                                tasks.Add(PlcControl.tagControl.Z1axis.StartAbsoluteAsync(8, default, _emptyRunCts.Token));
                            }
                            if (RunZ2)
                            {
                                //tasks.Add(PlcControl.tagControl.Z2axis.StartAbsoluteAsync(PlcControl.tagControl.Z2axis.softUpperLimit.value.ToFloat(), default, _emptyRunCts.Token));
                                tasks.Add(PlcControl.tagControl.Z2axis.StartAbsoluteAsync(8, default, _emptyRunCts.Token));
                            }
                            await Task.WhenAll(tasks);
                            if (RunZ1)
                            {
                                tasks.Add(PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0, default, _emptyRunCts.Token));
                            }
                            if (RunZ2)
                            {
                                tasks.Add(PlcControl.tagControl.Z2axis.StartAbsoluteAsync(0, default, _emptyRunCts.Token));
                            }
                            await Task.WhenAll(tasks);
                            tasks.Clear();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        MaterialSnackUtils.MaterialSnack("空运行已取消！", MaterialSnackUtils.SnackType.INFO);
                    }
                    catch (Exception ex)
                    {
                        MaterialSnackUtils.MaterialSnack($"空运行发生错误: {ex.Message}", MaterialSnackUtils.SnackType.ERROR);
                    }
                    finally
                    {
                        await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
                        await PlcControl.tagControl.wholeDevice.CloseWorkpieceBlowingAsync();
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
                InitRightButton();
            }
            finally
            {
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
            base.OnNavigatedTo(navigationContext);
            InitRightButton();
        }
    }
}