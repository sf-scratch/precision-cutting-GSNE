using Emgu.CV.Dai;
using HslCommunication.Profinet.OpenProtocol;
using MathNet.Numerics.LinearAlgebra.Complex.Solvers;
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
using System.Windows.Documents;
using System.Windows.Interop;
using 精密切割系统.Assets.config.buttom;
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
using 精密切割系统.View.Pages.F4_BladeMaintenance;

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
        private List<ChCutStep>? _cutSteps;
        private volatile float _currentCutYPosition = 0;
        private string? _backPageName;
        private CutServiceCompleteData? _completeData;

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

        protected override void InitBottomButton()
        {
            base.InitBottomButton();
            AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
            AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
            AddBottomButton(ButtonParams.BlueButton("高度补偿", "FormatLineHeight", SetDepthCompensationAsync));
            AddBottomButton(ButtonParams.BlueButton("预切启动", "/Assets/icon/tab_1/02/tab_27.png", OpenPrecut));
            AddBottomButton(ButtonParams.BlueButton("刀片状态信息", "/Assets/icon/tab_1/02/tab_27.png", NavigateToBladeInfo));
            AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
            AddBottomButton(ButtonParams.BlueButton("", "", null, buttonVisibility: System.Windows.Visibility.Hidden));
            AddBottomButton(ButtonParams.BlueButton("速度更改", "SpeedometerMedium", SetFeedSpeed));
        }

        private async Task SetDepthCompensationAsync()
        {
            if (RegionUtils.FormError(_regionManager))
            {
                MaterialSnack(RegionUtils.FormErrorMessage, SnackType.WARNING);
                return;
            }
            _semiAutoCutService.DepthCompensationValue = CutParam.DepthCompensation.ToFloat();
            MaterialSnack($"刀片高度补偿设置为 {_semiAutoCutService.DepthCompensationValue}！", SnackType.SUCCESS);
        }

        private void SetFeedSpeed()
        {
            if (RegionUtils.FormError(_regionManager))
            {
                MaterialSnack(RegionUtils.FormErrorMessage, SnackType.WARNING);
                return;
            }
            _semiAutoCutService.FeedSpeedCompCompensationValue = CutParam.ChangeFeedSpeed.ToFloat();
            MaterialSnack($"变更进刀速度设置为 {_semiAutoCutService.FeedSpeedCompCompensationValue}！", SnackType.SUCCESS);
        }

        private void NavigateToBladeInfo()
        {
            BladeInfo.PageName = nameof(MQSemiAutomaticCuttingRun);
            NavigateUtils.NavigateToPage("Pages/F4_BladeMaintenance/BladeInfo");
        }

        private void OpenPrecut()
        {
            _semiAutoCutService.IsOpenPrecut = true;
            var preCutSpeedLis = AutoCutUtils.GetPreCutSpeedList(CutParam.FeedSpeed.ToFloat());
            _semiAutoCutService.UpdatePreCutQueue(preCutSpeedLis);
            MaterialSnack("开启预切割！", SnackType.SUCCESS);
        }

        protected override void InitRightButton()
        {
            base.InitRightButton();
            AddRightButton(ButtonParams.YelloRightButton("暂停", "/Assets/icon/right/stop.png", async () => { await PauseAsync(); }));
        }

        private async Task MonitoringAlarmAsync(CancellationToken token)
        {
            await Task.Delay(2000);
            while (!token.IsCancellationRequested)
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
                                        _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"Error报警监控：{alarmMessages}"));
                                        await PlcControl.tagControl.wholeDevice.OpenBuzzerAsync();
                                        await PauseAsync(PlcControl.tagControl.wholeDevice.OpenRedLightAsync);
                                        Tools.CuttingRecord(alarmMessages);
                                    }
                                    else
                                    {
                                        _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"Error报警监控：未获取到有效报警信息！{alarms}"));
                                    }
                                }
                                else
                                {
                                    _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"Error报警监控：读到空数据！"));
                                }
                            }
                        }
                        else
                        {
                            if (alarms != null && AlarmConfig.Instance.TryGetActiveAlarms(alarms, out List<AlarmInfo> alarmInfos))
                            {
                                string alarmMessages = string.Join(",", alarmInfos.Select(a => a.Message));
                                _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"Error报警监控：切割中途闪烁异常 {alarmMessages}"));
                            }
                        }
                    }

                    if (AlarmConfig.Instance.HasTargetActiveAlarm(out bool[]? targetAlarms, "MR60111", "MR60112"))
                    {
                        // 等待1秒钟再判断报警是否还存在
                        await Task.Delay(1000, default);
                        if (AlarmConfig.Instance.HasTargetActiveAlarm("MR60111", "MR60112"))
                        {
                            if (!_pauseCts.IsCancellationRequested)
                            {
                                if (targetAlarms != null)
                                {
                                    if (AlarmConfig.Instance.TryGetActiveAlarms(targetAlarms, out List<AlarmInfo> alarmInfos))
                                    {
                                        string alarmMessages = string.Join(",", alarmInfos.Select(a => a.Message));
                                        _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"Warn报警监控：{alarmMessages}"));
                                        await PlcControl.tagControl.wholeDevice.OpenBuzzerAsync();
                                        await PauseAsync(PlcControl.tagControl.wholeDevice.OpenRedLightAsync);
                                        Tools.CuttingRecord(alarmMessages);
                                    }
                                    else
                                    {
                                        _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"Warn报警监控：未获取到有效报警信息！{targetAlarms}"));
                                    }
                                }
                                else
                                {
                                    _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"Warn报警监控：读到空数据！"));
                                }
                            }
                        }
                        else
                        {
                            if (targetAlarms != null && AlarmConfig.Instance.TryGetActiveAlarms(targetAlarms, out List<AlarmInfo> alarmInfos))
                            {
                                string alarmMessages = string.Join(",", alarmInfos.Select(a => a.Message));
                                _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"Warn报警监控：切割中途闪烁异常 {alarmMessages}"));
                            }
                        }
                    }
                }
                catch (Exception) { }
                await Task.Delay(200);
            }
        }

        private async Task MonitoringCutProgressAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var axisPostion = await AutoCutUtils.GetAxisPositionAsync();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        YAxisCutPosition = (axisPostion.Y is null ? 0 : axisPostion.Y.Value - _currentCutYPosition).ToString(GlobalParams.DecimalStringFormat);
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
                await Task.Delay(100);
            }
        }

        private async void RunAutoCut()
        {
            if (!_semiAutoCutService.IsReady)
            {
                _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("切割中..."));
                return;
            }
            if (!GlobalParams.OnlineFlag)
            {
                SpeedManager.IsHighSpeed = false;
                MaterialSnack("切割中...", SnackType.WARNING, 0, _eventAggregator);
                return;
            }
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
            CommonResult checkAutomaticCompensationCutHeightResult = await VerifyUtils.CheckAutomaticCompensationCutHeightAsync(_cutSteps);
            if (!checkAutomaticCompensationCutHeightResult.IsSuccess)
            {
                ShowWarnMessageNavigateHome(checkAutomaticCompensationCutHeightResult.Message);
            }
            List<ChCutStep> cutSteps = _cutSteps;
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
            var currentY = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync();
            if (currentY is null)
            {
                ShowWarnMessageNavigateHome("获取Y轴当前位置失败！");
                return;
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
                CutStep firtStep = cutSteps.First().CutSteps.First();
                float cutY = firtStep.IsAbsolute ? firtStep.ChannelStartY : currentY.Value.ToActualY() - firtStep.ChannelStartY;
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
                PositionAlignmentModel positionAlignment = await SqlHelper.GetOrCreateEntityAsync(() => new PositionAlignmentModel());
                measureHeightZ -= positionAlignment.MeasurementHeightCompensation.ToFloat();
                Stopwatch completeStopwatch = Stopwatch.StartNew();
                try
                {
                    IWorkpieces workpiece = GenerateWorkpieces(fileTableItem, cutY);
                    _semiAutoCutService.CutServiceProcessChanged += CutService_CutServiceProcessChanged;
                    _semiAutoCutService.CutServicePaused += CutService_CutServicePaused;
                    _semiAutoCutService.CutServiceCompleted += CutService_CutServiceCompleted;
                    float preCutMaxSpeed = cutSteps.First().CutSteps.First().Speed;
                    var preCutSpeedLis = AutoCutUtils.GetPreCutSpeedList(preCutMaxSpeed);
                    _semiAutoCutService.UpdatePreCutQueue(preCutSpeedLis);
                    MaterialSnack($"切割中...", SnackType.WARNING, 0, _eventAggregator);
                    float margin = Appsettings.AdditionalMargin ?? 20;
                    _ = MonitoringAlarmAsync(_monitoringCts.Token);
                    _ = MonitoringCutProgressAsync(_monitoringCts.Token);
                    RunResult cutResult = await _semiAutoCutService.RunAsync(cutSteps, workpiece, margin, measureHeightZ, Appsettings.SafetyMarginZ1 ?? GlobalParams.BladeLiftingHeight, _eventAggregator, _pauseCts.Token);
                    if (!cutResult.IsSuccess)
                    {
                        MaterialSnack($"{cutResult.Message}", SnackType.WARNING, 0, _eventAggregator);
                        return;
                    }
                    completeStopwatch.Stop();
                    if (GlobalParams.DeviceModel != GlobalParams.Device_321)
                    {
                        TimeSpan timeSpan = TimeSpan.FromSeconds(completeStopwatch.Elapsed.TotalSeconds);
                        string formattedTime = $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
                        await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(Appsettings.ThetaCenterPoint.X, 80, _pauseCts.Token);
                        _ = PlcControl.tagControl.wholeDevice.OpenBuzzerAsync(5);
                        MaterialSnack($"切割完成！ 总用时：{formattedTime}", SnackType.SUCCESS, 0);
                    }
                }
                catch (Exception ex)
                {
                    Tools.LogDebug(ex.ToString());
                    MaterialSnack($"切割异常：{ex.Message}", SnackType.WARNING, 0, _eventAggregator);
                }
                finally
                {
                    _semiAutoCutService.CutServiceProcessChanged -= CutService_CutServiceProcessChanged;
                    _semiAutoCutService.CutServicePaused -= CutService_CutServicePaused;
                    _semiAutoCutService.CutServiceCompleted -= CutService_CutServiceCompleted; ;
                    _monitoringCts.Cancel();
                    _pauseCts.Cancel();
                    completeStopwatch.Stop();
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

        private async void CutService_CutServiceCompleted(CutServiceCompleteData completeData)
        {
            _completeData = completeData;
        }

        private async void CutService_CutServicePaused(CutServicePauseData pauseData)
        {
            await AfterPauseThenMoveToPositionAsync(pauseData);
        }

        private async Task AfterPauseThenMoveToPositionAsync(CutServicePauseData pauseData)
        {
            MaterialSnack("正在暂停切割...", SnackType.WARNING, 0, _eventAggregator);
            float runTime = 60 + pauseData.CurrentKnifeRemainTime;
            try
            {
                // 超时自动取消
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(runTime));
                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(cts.Token);
                LineSegment? line = pauseData.Line;
                // 轴不报警时移动到指定位置
                if (line != null && !AlarmConfig.Instance.HasAxisErrorAlarms())
                {
                    await PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0, default, cts.Token);
                    await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
                    if (!pauseData.IsCompleted)
                    {
                        await AutoCutUtils.WorkpieceBlowingAsync(line.StartPoint.Y.ToCameraY(), default, false, _eventAggregator, cts.Token);
                    }
                    await Task.WhenAll(
                        PlcControl.tagControl.Xaxis.StartAbsoluteAsync(((line.StartPoint.X + line.EndPoint.X) / 2).ToCameraX(), 50, cts.Token),
                        PlcControl.tagControl.Yaxis.StartAbsoluteAsync(line.StartPoint.Y.ToCameraY(), 30, cts.Token));
                }
                if (!AlarmConfig.Instance.HasActiveErrorAlarm())
                {
                    await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
                }
                if (pauseData.IsCompleted)
                {
                    _ = PlcControl.tagControl.wholeDevice.OpenBuzzerAsync(5);
                    MaterialSnack(pauseData.Message ?? "切割完成...", SnackType.SUCCESS, 0, _eventAggregator);
                }
                else
                {
                    if (AlarmConfig.Instance.HasAutoRunUnexpectedAlarms(out bool[]? alarms) && alarms is not null && AlarmConfig.Instance.TryGetActiveAlarms(alarms, out List<AlarmInfo> alarmInfos))
                    {
                        string alarmMessages = string.Join(",", alarmInfos.Select(a => a.Message));
                        MaterialSnack(pauseData.Message ?? alarmMessages, SnackType.WARNING, 0, _eventAggregator);
                    }
                    else
                    {
                        MaterialSnack(pauseData.Message ?? "暂停中...", SnackType.WARNING, 0, _eventAggregator);
                    }
                }
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
                NavigationParameters parameters = new NavigationParameters { { nameof(MQSemiAutomaticCuttingRunViewModel), this }, { nameof(CutServicePauseData), pauseData } };
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
                CutParam.ChannelNum = process.ChannelNum;
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
                        if (process.RemainingTime > 0)
                        {
                            CutParam.ExpectedProcessingEndTime = DateTime.Now.AddSeconds(process.RemainingTime).ToString("HH:mm:ss");
                        }
                        else
                        {
                            Tools.LogDebug($"计算剩余时间异常! RemainingTime:{process.RemainingTime}");
                        }
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
            BladeInfo.PageName = null;
            _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Subscribe(ReceivedMessage, ThreadOption.UIThread);
            if (!navigationContext.Parameters.TryGetValue("isContinue", out bool _))
            {
                _pauseCts = new CancellationTokenSource();
                _monitoringCts = new CancellationTokenSource();
            }
            if (navigationContext.Parameters.TryGetValue<List<ChCutStep>>("cutSteps", out var cutSteps))
            {
                _cutSteps = cutSteps;
                if (cutSteps.Count > 0)
                {
                    ChCutStep firstStep = cutSteps.First();
                    CutParam.ChannelNum = firstStep.ChName;
                }
            }
            if (navigationContext.Parameters.TryGetValue<string>("backPageName", out var backPageName))
            {
                _backPageName = backPageName;
            }
            // 加载参数
            FileTableItemModel fileTableItem = CurrentUtils.GetFileTableItemModel();
            CutParam.DeviceDataNo = fileTableItem.DeviceDataNo;
            CutParam.DeviceDataId = fileTableItem.DeviceDataId;
            CutParam.ChangeFeedSpeed = _semiAutoCutService.FeedSpeedCompCompensationValue.ToString();
            CutParam.DepthCompensation = _semiAutoCutService.DepthCompensationValue.ToString(GlobalParams.DecimalStringFormat);
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