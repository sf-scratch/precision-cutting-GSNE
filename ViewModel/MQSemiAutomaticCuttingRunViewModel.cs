using HslCommunication.Profinet.OpenProtocol;
using Newtonsoft.Json;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using 精密切割系统.Data;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.Entities;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.cut.Workpieces;
using 精密切割系统.Model.MeasureHeight;
using 精密切割系统.Model.plc;
using 精密切割系统.PubSubEvent;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.View.Pages.F2_ManualOperation;

namespace 精密切割系统.ViewModel
{
    public class MQSemiAutomaticCuttingRunViewModel : CustomBindableBase
    {
        public DelegateCommand RunAutoCutCommand { get; set; }
        public ObservableCollection<MessageModel> MessageList { get; set; } = new ObservableCollection<MessageModel>();
        public SemiAutomaticCutParamModel CutParam { get; set; } = new SemiAutomaticCutParamModel();
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;
        private readonly SemiAutoCutService _semiAutoCutService;
        private CancellationTokenSource _pauseCts;
        private CancellationTokenSource _monitoringCts;
        private List<CutStep>? _cutSteps;
        private float _currentCutYPosition = 0;
        private string? _backPageName;

        private string _yAxisCutPosition;

        public string YAxisCutPosition
        {
            get { return _yAxisCutPosition; }
            set { SetProperty(ref _yAxisCutPosition, value); }
        }

        private string _xAxisCurrentPosition;

        public string XAxisCurrentPosition
        {
            get { return _xAxisCurrentPosition; }
            set { SetProperty(ref _xAxisCurrentPosition, value); }
        }

        private string _yAxisCurrentPosition;

        public string YAxisCurrentPosition
        {
            get { return _yAxisCurrentPosition; }
            set { SetProperty(ref _yAxisCurrentPosition, value); }
        }

        private string _zAxisCurrentPosition;

        public string ZAxisCurrentPosition
        {
            get { return _zAxisCurrentPosition; }
            set { SetProperty(ref _zAxisCurrentPosition, value); }
        }

        private string _z2AxisCurrentPosition;

        public string Z2AxisCurrentPosition
        {
            get { return _z2AxisCurrentPosition; }
            set { SetProperty(ref _z2AxisCurrentPosition, value); }
        }

        private string _thetaAxisCurrentPosition;

        public string ThetaAxisCurrentPosition
        {
            get { return _thetaAxisCurrentPosition; }
            set { SetProperty(ref _thetaAxisCurrentPosition, value); }
        }

        public MQSemiAutomaticCuttingRunViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            _semiAutoCutService = SemiAutoCutService.Instance;
            RunAutoCutCommand = new DelegateCommand(RunAutoCut);
        }

        public MQSemiAutomaticCuttingRunViewModel()
        {
        }

        private void InitRightButton()
        {
            RightButtonCollection.Add(ButtonParams.YelloRightButton("暂停", "/Assets/icon/right/stop.png", async () => { await PauseAsync(); }));
        }

        private async Task MonitoringAlarmAsync(CancellationToken token)
        {
            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
                while (await timer.WaitForNextTickAsync(token))
                {
                    try
                    {
                        if (AlarmConfig.Instance.HasAutoRunUnexpectedAlarms(out bool[]? alarms))
                        {
                            // 等待1秒钟再判断报警是否还存在
                            await Task.Delay(1000, default);
                            if (AlarmConfig.Instance.HasAutoRunUnexpectedAlarms())
                            {
                                if (!_pauseCts.IsCancellationRequested)
                                {
                                    if (alarms != null)
                                    {
                                        if (AlarmConfig.Instance.TryGetActiveAlarms(alarms, out List<AlarmInfo> alarmInfos))
                                        {
                                            string alarmMessages = string.Join(",", alarmInfos.Select(a => a.Message));
                                            _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"切割报警监控：{alarmMessages}"));
                                            await PauseAsync(PlcControl.tagControl.wholeDevice.OpenRedLightAsync);
                                        }
                                        else
                                        {
                                            _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"切割报警监控：未获取到有效报警信息！{alarms}"));
                                        }
                                    }
                                    else
                                    {
                                        _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"切割报警监控：读到空数据！"));
                                    }
                                }
                            }
                            else
                            {
                                if (alarms != null && AlarmConfig.Instance.TryGetActiveAlarms(alarms, out List<AlarmInfo> alarmInfos))
                                {
                                    string alarmMessages = string.Join(",", alarmInfos.Select(a => a.Message));
                                    _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"切割报警监控：切割中途闪烁异常 {alarmMessages}"));
                                }
                            }
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task MonitoringCutProgressAsync(CancellationToken token)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
            while (await timer.WaitForNextTickAsync(token))
            {
                try
                {
                    CutParam.ChannelNum = CurrentUtils.GetCurrentCh();
                    var axisPostion = await AutoCutUtils.GetAxisPositionAsync();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        YAxisCutPosition = (axisPostion.Y ?? 0 - _currentCutYPosition).ToString(GlobalParams.DecimalStringFormat);
                        XAxisCurrentPosition = (axisPostion.X ?? 0).ToString(GlobalParams.DecimalStringFormat);
                        YAxisCurrentPosition = (axisPostion.Y ?? 0).ToString(GlobalParams.DecimalStringFormat);
                        ZAxisCurrentPosition = (axisPostion.Z1 ?? 0).ToString(GlobalParams.DecimalStringFormat);
                        Z2AxisCurrentPosition = (axisPostion.Z2 ?? 0).ToString(GlobalParams.DecimalStringFormat);
                        ThetaAxisCurrentPosition = (axisPostion.Theta ?? 0).ToString(GlobalParams.DecimalStringFormat);
                    });
                }
                catch (Exception)
                {
                }
            }
        }

        private async void RunAutoCut()
        {
            if (!_semiAutoCutService.IsReady)
            {
                //正在切割中
                return;
            }
            //if (!GlobalParams.OnlineFlag)
            //{
            //    SpeedManager.IsHighSpeed = false;
            //    MaterialSnack($"切割中...", SnackType.WARNING, 0, _eventAggregator);
            //    return;
            //}
            CommonResult checkResult = await SemiAutoCutService.CheckCutAsync();
            if (!checkResult.IsSuccess)
            {
                ShowWarnMessageNavigateHome(checkResult.Message);
                return;
            }
            if (_cutSteps is null)
            {
                ShowWarnMessageNavigateHome("切割步骤数据异常，请重新设置切割参数！");
                return;
            }
            List<CutStep> cutSteps = _cutSteps;
            CommonResult<FileTableItemModel> fileTableItemResult = await AutoCutUtils.GetFileTableItemModelAsync();
            if (!fileTableItemResult.IsSuccess || fileTableItemResult.Data is null)
            {
                ShowWarnMessageNavigateHome(fileTableItemResult.Message);
                return;
            }
            if (Appsettings.BladeOuterDiameter is null)
            {
                ShowWarnMessageNavigateHome("未设置刀片外径！");
                return;
            }
            var bmParameter = await SqlHelper.GetEntityAsync<BMParameterMaintenanceEntity>();
            if (bmParameter == null)
            {
                ShowWarnMessageNavigateHome("获取测高参数失败！");
                return;
            }
            if (Appsettings.MeasureHeightFirst is null || Appsettings.MeasureHeightLast is null)
            {
                ShowWarnMessageNavigateHome("未进行测高，请先测高！");
                return;
            }
            float maximumWearAmount = bmParameter.MaximumWearAmount.ToFloat();
            if (maximumWearAmount < Appsettings.MeasureHeightLast - Appsettings.MeasureHeightFirst)
            {
                ShowWarnMessageNavigateHome("超出刀片最大磨损量，请更换刀片！");
                return;
            }
            if (Appsettings.AfterReplaceBladeCutLength is not null && Appsettings.AfterReplaceBladeCutTimes is not null)
            {
                float cuttingLifeLength = bmParameter.CuttingLifeLength.ToFloat();
                int cuttingLifeCutNumber = bmParameter.CuttingLifeCutNumber.ToInt();
                float afterReplaceBladeCutLength = Appsettings.AfterReplaceBladeCutLength.Value / 1000;
                if (afterReplaceBladeCutLength >= cuttingLifeLength || Appsettings.AfterReplaceBladeCutTimes >= cuttingLifeCutNumber)
                {
                    ShowWarnMessageNavigateHome("已超出刀片使用寿命，请更换刀片！");
                    return;
                }
            }
            if (!GlobalParams.HasFullyAutomatic)
            {
                await PlcControl.tagControl.wholeDevice.CloseCameraLensCapAsync();
            }
            AtomicConfig.IsCutProcessing = true;
            await PlcControl.tagControl.wholeDevice.OpenGreenLightAsync();
            try
            {
                FileTableItemModel fileTableItem = fileTableItemResult.Data;
                _ = MonitoringAlarmAsync(_monitoringCts.Token);
                _ = MonitoringCutProgressAsync(_monitoringCts.Token);
                CutStep firtStep = cutSteps.First();
                float cutY = firtStep.IsAbsolute ? firtStep.ChannelStartY : (await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync() ?? 0).ToActualY() + firtStep.ChannelStartY;
                float measureHeightZ = 0;
                if (bmParameter.IsAutomHeightMeasureBeforeCutting)
                {
                    CommonResult<float> curHeightZ = await AutoCutUtils.ProcessCombineMeasureHeightAsync(_eventAggregator, _pauseCts.Token);
                    if (!curHeightZ.IsSuccess)
                    {
                        ShowWarnMessageNavigateHome(curHeightZ.Message);
                        return;
                    }
                    measureHeightZ = curHeightZ.Data;
                }
                else
                {
                    measureHeightZ = Appsettings.MeasureHeightLast.Value;
                }
                Stopwatch stopwatch = Stopwatch.StartNew();
                try
                {
                    IWorkpieces workpiece = GenerateWorkpieces(fileTableItem, cutY);
                    _semiAutoCutService.CutServiceProcessChanged += CutService_CutServiceProcessChanged;
                    _semiAutoCutService.CutServicePaused += CutService_CutServicePaused;
                    MaterialSnack($"切割中...", SnackType.WARNING, 0, _eventAggregator);
                    float margin = Appsettings.AdditionalMargin ?? 20;
                    RunResult cutResult = await _semiAutoCutService.RunAsync(cutSteps, workpiece, margin, measureHeightZ, GlobalParams.BladeLiftingHeight, false, _pauseCts.Token);
                    if (!cutResult.IsSuccess)
                    {
                        MaterialSnack($"{cutResult.Message}", SnackType.WARNING, 0, _eventAggregator);
                        return;
                    }
                    stopwatch.Stop();
                    TimeSpan timeSpan = TimeSpan.FromSeconds(stopwatch.Elapsed.TotalSeconds);
                    string formattedTime = $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
                    await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(Appsettings.ThetaCenterPoint.X, default, _pauseCts.Token);
                    _ = PlcControl.tagControl.wholeDevice.OpenBuzzerAsync(5);
                    MaterialSnack($"切割完成！ 总用时：{formattedTime}", SnackType.SUCCESS, 0);
                }
                catch (Exception ex)
                {
                    MaterialSnack($"切割异常：{ex.Message}", SnackType.WARNING, 0, _eventAggregator);
                }
                finally
                {
                    _semiAutoCutService.CutServiceProcessChanged -= CutService_CutServiceProcessChanged;
                    _semiAutoCutService.CutServicePaused -= CutService_CutServicePaused;
                    stopwatch.Stop();
                    _monitoringCts.Cancel();
                    _pauseCts.Cancel();
                    await StopAsync(ServicePauseResult.Stop);
                }
            }
            finally
            {
                AtomicConfig.IsCutProcessing = false;
            }
        }

        private void ShowWarnMessageNavigateHome(string message)
        {
            MaterialSnack($"{message}", SnackType.WARNING, 0, _eventAggregator);
            NavigateToHome();
            ShowMessage();
        }

        public void NavigateToHome()
        {
            if (_backPageName is null)
            {
                NavigateUtils.NavigateToPage("Pages/F2_ManualOperation/MQSemiAutomaticCuttingConf");
            }
            else
            {
                _regionManager.RequestNavigate(RegionName.MainRegion, _backPageName);
            }
        }

        private IWorkpieces GenerateWorkpieces(FileTableItemModel fileTableItem, float cutY)
        {
            DataPoint<float> thetaCenterPoint = Appsettings.ThetaCenterPoint;
            IWorkpieces workpiece;
            if (fileTableItem.WorkShape == 1)
            {
                workpiece = new CircularWorkpiece(thetaCenterPoint, fileTableItem.Round.ToFloat() / 2, cutY);
            }
            else if (fileTableItem.WorkShape == 2)
            {
                float width = fileTableItem.SquareCh1.ToFloat();
                float height = fileTableItem.SquareCh2.ToFloat();
                workpiece = new RectangleWorkpiece(thetaCenterPoint, width, height, cutY);
            }
            else
            {
                workpiece = new CircularWorkpiece(thetaCenterPoint, fileTableItem.Round.ToFloat() / 2, cutY);
            }
            workpiece.WorkThickness = float.Parse(fileTableItem.WorkThickness);
            workpiece.TapeThickness = float.Parse(fileTableItem.TapeThickness);
            return workpiece;
        }

        private async Task PauseAsync(Func<Task>? actionAsync = default)
        {
            if (!GlobalParams.OnlineFlag)
            {
                // 暂停token
                _pauseCts.Cancel();
                NavigationParameters parameters = new NavigationParameters { { nameof(MQSemiAutomaticCuttingRunViewModel), this } };
                _regionManager.RequestNavigate(RegionName.MainRegion, nameof(MQSemiAutomaticCuttingStop), parameters);
                return;
            }
            if (_pauseCts.IsCancellationRequested)
            {
                _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("正在执行暂停，无需重复操作！"));
                return;
            }
            // 暂停token
            _pauseCts.Cancel();
            if (actionAsync != null)
            {
                await actionAsync.Invoke();
            }
        }

        public async Task ContinueAsync()
        {
            if (!GlobalParams.OnlineFlag)
            {
                SemiAutoCutService.Instance.Continue(_pauseCts.Token);
                return;
            }
            MaterialSnack("正在继续切割...", SnackType.WARNING, 0, _eventAggregator);
            _pauseCts = new CancellationTokenSource();
            await PlcControl.tagControl.wholeDevice.OpenGreenLightAsync();
            await PlcControl.tagControl.cutting.EnterCuttingModeAsync(_pauseCts.Token);
            SemiAutoCutService.Instance.Continue(_pauseCts.Token);
            MaterialSnack("切割中...", SnackType.WARNING, 0, _eventAggregator);
        }

        public async Task ContinueAndResetCutYAsync()
        {
            if (!GlobalParams.OnlineFlag)
            {
                SemiAutoCutService.Instance.ContinueAndResetCutY(_pauseCts.Token);
                return;
            }
            MaterialSnack("正在继续切割...", SnackType.WARNING, 0, _eventAggregator);
            _pauseCts = new CancellationTokenSource();
            await PlcControl.tagControl.cutting.EnterCuttingModeAsync(_pauseCts.Token);
            SemiAutoCutService.Instance.ContinueAndResetCutY(_pauseCts.Token);
            MaterialSnack("切割中...", SnackType.WARNING, 0, _eventAggregator);
        }

        public async Task StopAsync(ServicePauseResult pauseResult)
        {
            if (!GlobalParams.OnlineFlag)
            {
                NavigateToHome();
                MaterialSnack($"切割完成！ 总用时：1:10:26", SnackType.SUCCESS, 0);
                return;
            }
            SemiAutoCutService.Instance.Stop(pauseResult);
            await PlcControl.tagControl.wholeDevice.OpenYellowLightAsync();
            //结束切割
            await PlcControl.tagControl.cutting.ExitCuttingModeAsync(default);
            NavigateToHome();
            ShowMessage();
        }

        private async void CutService_CutServicePaused(LineSegment? line, string? message, float currentKnifeRemainTime)
        {
            await AfterPauseThenMoveToPosition(line, message, currentKnifeRemainTime);
        }

        private async Task AfterPauseThenMoveToPosition(LineSegment? line, string? message, float currentKnifeRemainTime)
        {
            MaterialSnack("正在暂停切割...", SnackType.WARNING, 0, _eventAggregator);
            float runTime = 60 + currentKnifeRemainTime;
            try
            {
                // 超时自动取消
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(runTime));
                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(cts.Token);
                await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
                // 轴不报警时移动到指定位置
                if (line != null && !AlarmConfig.Instance.HasAxisErrorAlarms())
                {
                    // Z1安全余量
                    float posZ = 0;
                    CommonResult<FileTableItemModel> fileTableItemResult = await AutoCutUtils.GetFileTableItemModelAsync();
                    if (fileTableItemResult.IsSuccess && fileTableItemResult.Data is not null)
                    {
                        FileTableItemModel fileTableItem = fileTableItemResult.Data;
                        float workThickness = fileTableItem.WorkThickness.ToFloat();
                        float tapeThickness = fileTableItem.TapeThickness.ToFloat();
                        if (Appsettings.MeasureHeightLast is not null && Appsettings.SafetyMarginZ1 is not null)
                        {
                            posZ = Appsettings.MeasureHeightLast.Value - workThickness - tapeThickness - Appsettings.SafetyMarginZ1.Value;
                        }
                    }
                    await PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0, default, cts.Token);
                    await AutoCutUtils.WorkpieceBlowingAsync(line.StartPoint.Y.ToCameraY(), default, _eventAggregator, cts.Token);
                    await Task.WhenAll(
                        PlcControl.tagControl.Xaxis.StartAbsoluteAsync(((line.StartPoint.X + line.EndPoint.X) / 2).ToCameraX(), 30, cts.Token),
                        PlcControl.tagControl.Yaxis.StartAbsoluteAsync(line.StartPoint.Y.ToCameraY(), 30, cts.Token));
                    // 执行默认动作
                    //await PlcControl.tagControl.Z1axis.StartAbsoluteAsync(posZ, default, cts.Token);
                }
                MaterialSnack(message ?? "暂停中...", SnackType.WARNING, 0, _eventAggregator);
            }
            catch (OperationCanceledException)
            {
                MaterialSnack("暂停切割超时", SnackType.WARNING, 0, _eventAggregator);
            }
            catch (Exception ex)
            {
                MaterialSnack($"暂停切割时遇到其他错误: {ex.Message}", SnackType.WARNING, 0, _eventAggregator);
            }
            finally
            {
                NavigationParameters parameters = new NavigationParameters { { nameof(MQSemiAutomaticCuttingRunViewModel), this } };
                _regionManager.RequestNavigate(RegionName.MainRegion, nameof(MQSemiAutomaticCuttingStop), parameters);
            }
        }

        private void CutService_CutServiceProcessChanged(CutServiceProcess process)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 设置切割进度
                CutParam.RunCutLine = process.CutTimes;
                CutParam.AllRunCutLine = process.TotalCutTimes;
                CutParam.FeedSpeed = process.CutSpeed.ToString("F5");
                CutParam.BladeHeight = process.CutBladeHeight.ToString("F5");
                CutParam.ChannelNum = string.Format(GlobalParams.StringFormatCH, process.ChannelNum);
                if (process.IsCompleted)
                {
                    Appsettings.AfterReplaceBladeCutTimes++;
                    Appsettings.AfterReplaceBladeCutLength += process.CutLength;
                    Appsettings.AfterMeasureHeightCutTimes++;
                    Appsettings.AfterMeasureHeightCutLength += process.CutLength;
                    Appsettings.AfterClearDataCutTimes++;
                    Appsettings.AfterClearDataCutLength += process.CutLength;
                    CutParam.AllCutLine = Appsettings.AfterReplaceBladeCutTimes ?? 0;
                    CutParam.AllCutLineLength = (Appsettings.AfterReplaceBladeCutLength / 1000 ?? 0).ToString("F2");
                    if (process.CutTimes > 1)
                    {
                        CutParam.ExpectedProcessingEndTime = DateTime.Now.AddSeconds(process.RemainingTime).ToString("HH:mm:ss");
                    }
                }
                _currentCutYPosition = process.CutYPosition;
            });
        }

        private void ReceivedMessage(MessageModel message)
        {
            Tools.LogDebug(message.Message);
            MessageList.Add(message);
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Subscribe(ReceivedMessage, ThreadOption.UIThread);
            if (!navigationContext.Parameters.TryGetValue("isContinue", out bool _))
            {
                _pauseCts = new CancellationTokenSource();
                _monitoringCts = new CancellationTokenSource();
            }
            if (navigationContext.Parameters.TryGetValue<List<CutStep>>("cutSteps", out var cutSteps))
            {
                _cutSteps = cutSteps;
            }
            if (navigationContext.Parameters.TryGetValue<string>("backPageName", out var backPageName))
            {
                _backPageName = backPageName;
            }
            // 加载参数
            FileTableItemModel fileTableItem = CurrentUtils.GetFileTableItemModel();
            CutParam.DeviceDataNo = fileTableItem.DeviceDataNo;
            CutParam.DeviceDataId = fileTableItem.DeviceDataId;
            CutParam.ChannelNum = CurrentUtils.GetCurrentCh();
            CutParam.ChangeFeedSpeed = _semiAutoCutService.FeedSpeedCompCompensationValue.ToString();
            CutParam.DepthCompensation = _semiAutoCutService.DepthCompensationValue.ToString();
            InitRightButton();
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
            _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Unsubscribe(ReceivedMessage);
        }

        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return !_pauseCts.IsCancellationRequested || !_monitoringCts.IsCancellationRequested;
        }
    }
}