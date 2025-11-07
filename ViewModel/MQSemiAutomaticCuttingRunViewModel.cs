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
using static 精密切割系统.Helpers.MaterialSnackUtils;

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
            RightButtonCollection.Add(RightButtonParams.YelloRightButton("暂停", "/Assets/icon/right/stop.png", async () => { await PauseAsync(PlcControl.tagControl.wholeDevice.OpenYellowLightAsync); }));
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
                    CutParam.ChannelNum = CurrentUtils.GetCurrentConfiguration().ChannelNum;
                    var axisPostion = await AutoCutUtils.GetAxisPositionAsync();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        YAxisCutPosition = CutOperateUtils.globalYCutPosition.ToString();
                        XAxisCurrentPosition = MathF.Round(axisPostion.X ?? 0, 5).ToString();
                        YAxisCurrentPosition = MathF.Round(axisPostion.Y ?? 0, 5).ToString();
                        ZAxisCurrentPosition = MathF.Round(axisPostion.Z1 ?? 0, 5).ToString();
                        Z2AxisCurrentPosition = MathF.Round(axisPostion.Z2 ?? 0, 5).ToString();
                        ThetaAxisCurrentPosition = MathF.Round(axisPostion.Theta ?? 0, 5).ToString();
                    });
                }
                catch (Exception)
                {
                }
            }
        }

        private async void RunAutoCut()
        {
            if (!GlobalParams.OnlineFlag)
            {
                MaterialSnack($"切割中...", SnackType.WARNING, 0, _eventAggregator);
                return;
            }
            if (!_semiAutoCutService.IsReady)
            {
                //正在切割中
                return;
            }
            CommonResult checkResult = await SemiAutoCutService.CheckCutAsync();
            if (!checkResult.IsSuccess)
            {
                ShowWarnMessageNavigateHome(checkResult.Message);
                return;
            }
            CommonResult<List<CutStep>> cutStepResult = await GenerateCutStepListAsync(_semiAutoCutService.IsOpenPrecut);
            if (!cutStepResult.IsSuccess || cutStepResult.Data is null)
            {
                ShowWarnMessageNavigateHome(cutStepResult.Message);
                return;
            }
            List<CutStep> cutSteps = cutStepResult.Data;
            CommonResult<FileTableItemModel> fileTableItemResult = await GetFileTableItemModelAsync();
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
            if (!GlobalParams.HasTheta)
            {
                await PlcControl.tagControl.wholeDevice.OpenCutSecurityDoorAsync();
            }
            AtomicConfig.IsCutProcessing = true;
            FileTableItemModel fileTableItem = fileTableItemResult.Data;
            _ = MonitoringAlarmAsync(_monitoringCts.Token);
            _ = MonitoringCutProgressAsync(_monitoringCts.Token);
            CutStep firtStep = cutSteps.First();
            float cutY = firtStep.IsAbsolute ? firtStep.ChannelStartY : (await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync() ?? 0).ToActualY();
            float measureHeightZ = 0;
            if (!BmSetupData.Instance.IsAutomHeightMeasureBeforeCutting && Appsettings.MeasureHeightLast is not null)
            {
                measureHeightZ = Appsettings.MeasureHeightLast.Value;
            }
            else
            {
                CommonResult<float> curHeightZ = await AutoCutUtils.ProcessCombineMeasureHeightAsync(_eventAggregator, _pauseCts.Token);
                if (!curHeightZ.IsSuccess)
                {
                    ShowWarnMessageNavigateHome(curHeightZ.Message);
                    AtomicConfig.IsCutProcessing = false;
                    return;
                }
                measureHeightZ = curHeightZ.Data;
            }
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                IWorkpieces workpiece = GenerateWorkpieces(fileTableItem, cutY);
                _semiAutoCutService.CutServiceProcessChanged += CutService_CutServiceProcessChanged;
                _semiAutoCutService.CutServicePaused += CutService_CutServicePaused;
                MaterialSnack($"切割中...", SnackType.WARNING, 0, _eventAggregator);
                float margin = Appsettings.AdditionalMargin ?? 20;
                RunResult cutResult = await _semiAutoCutService.RunAsync(cutSteps, workpiece, margin, fileTableItem.SpindleRev, measureHeightZ, GlobalParams.BladeLiftingHeight, false, _pauseCts.Token);
                if (!cutResult.IsSuccess)
                {
                    MaterialSnack($"{cutResult.Message}", SnackType.WARNING, 0, _eventAggregator);
                    return;
                }
                stopwatch.Stop();
                TimeSpan timeSpan = TimeSpan.FromSeconds(stopwatch.Elapsed.TotalSeconds);
                string formattedTime = $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
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
                AtomicConfig.IsCutProcessing = false;
            }
        }

        private void ShowWarnMessageNavigateHome(string message)
        {
            MaterialSnack($"{message}", SnackType.WARNING, 0, _eventAggregator);
            NavigateUtils.NavigateToPage("Pages/F2_ManualOperation/MQSemiAutomaticCuttingConf");
            ShowMessage();
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

        private async Task PauseAsync(Func<Task> actionAsync)
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
            await actionAsync.Invoke();
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
                NavigateUtils.NavigateToPage("Pages/F2_ManualOperation/MQSemiAutomaticCuttingConf");
                ShowMessage();
                return;
            }
            SemiAutoCutService.Instance.Stop(pauseResult);
            await PlcControl.tagControl.wholeDevice.OpenYellowLightAsync();
            //结束切割
            await PlcControl.tagControl.cutting.ExitCuttingModeAsync(default);
            NavigateUtils.NavigateToPage("Pages/F2_ManualOperation/MQSemiAutomaticCuttingConf");
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
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(runTime));// 超时自动取消
                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(cts.Token);
                // 轴不报警时移动到指定位置
                if (line != null && !AlarmConfig.Instance.HasAxisErrorAlarms())
                {
                    // 执行默认动作
                    Task z1Task = PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0, default, cts.Token);
                    Task z2Task = PlcControl.tagControl.Z2axis.StartAbsoluteAsync(Appsettings.FocusClearZ ?? 0, default, cts.Token);
                    await Task.WhenAll(z1Task, z2Task);
                    await AutoCutUtils.WorkpieceBlowingAsync(_eventAggregator, cts.Token);
                    await PlcControl.tagControl.cutting.RunMotionAsync(((line.StartPoint.X + line.EndPoint.X) / 2).ToCameraX(), line.StartPoint.Y.ToCameraY(), cts.Token);
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
                CutParam.ChannelNum = $"CH{process.ChannelNum}";
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
            });
        }

        private async Task<CommonResult<List<CutStep>>> GenerateCutStepListAsync(bool isOpenPrecut)
        {
            //获取功能选择数据
            var selectionModels = await SqlHelper.TableAsync<FunctionSelectionModel>().Where(t => t.Id == 1).ToListAsync();
            if (selectionModels.Count <= 0)
            {
                return CommonResult<List<CutStep>>.Failure("功能选择配置异常！");
            }
            FunctionSelectionModel functionModel = selectionModels[0];
            bool isDeep = functionModel.DepthStepsFunction;
            bool isLoop = functionModel.LoopFunction;
            CommonResult<FileTableItemModel> fileTableItemResult = await GetFileTableItemModelAsync();
            if (!fileTableItemResult.IsSuccess || fileTableItemResult.Data is null)
            {
                return CommonResult<List<CutStep>>.Failure(fileTableItemResult.Message);
            }
            FileTableItemModel fileTableItem = fileTableItemResult.Data;
            string cuttingChSeq = fileTableItem.CuttingChSeq;
            // 参数校验
            if (fileTableItem.SpindleRev == 0 || fileTableItem.SpindleRev > 30000)
            {
                return CommonResult<List<CutStep>>.Failure("切割转速配置错误！");
            }
            List<CutStep> cutSteps = [];
            // 查询通道信息
            List<FileTableItemChModel> chModels = await SqlHelper.TableAsync<FileTableItemChModel>().Where(t => t.ItemId == fileTableItem.Id).ToListAsync();
            int[] chSeqs = Tools.StringToIntegerArray(cuttingChSeq);
            foreach (int chSeq in chSeqs)
            {
                FileTableItemChModel ch = chModels[chSeq - 1];
                float[] setBladeHeight = Tools.StringToFloatArray(ch.BladeHeight);// 设置的刀片高度
                float[] feedSpeeds = Tools.StringToFloatArray(ch.FeedSpeed); // 获取进给速度
                float[] yIndexs = Tools.StringToFloatArray(ch.YIndex);       // 获取Y轴偏移
                float[] repeatTimes = Tools.StringToFloatArray(ch.RepeatTimes); // 获取重复次数
                float[] cutDepths = Tools.StringToFloatArray(ch.DepthSteps); // 获取切割深度
                string[] loops = Tools.StringToStringArray(ch.Loop);         // 获取循环控制信息
                // 检查索引是否连续
                int maxIndex = AreIndexesContinuous(setBladeHeight, feedSpeeds, yIndexs, repeatTimes);
                if (maxIndex == -1)
                {
                    return CommonResult<List<CutStep>>.Failure("切割参数错误！");
                }
                if (cutDepths.Length <= maxIndex)
                {
                    return CommonResult<List<CutStep>>.Failure("切割深度参数错误！");
                }
                // 生成子序列
                List<string> repetitions = [.. loops];
                List<int> sequences = [.. Enumerable.Range(0, maxIndex + 1)];
                List<int> newSeq = isLoop ? CutUtils.CombineSequences(sequences, repetitions) : sequences;
                List<CutStep> tempCutSteps = [];
                foreach (int index in newSeq)
                {
                    for (int i = 0; i < repeatTimes[index]; i++)
                    {
                        float cutHeight = setBladeHeight[index];
                        float speed = feedSpeeds[index];
                        float offsetY = yIndexs[index];
                        float thetaDeg = float.Parse(ch.ThetaDeg);
                        bool isAbsolute = ch.ComBoxCutMethod.Equals("绝对");
                        float channelStartY = isAbsolute ? ch.AbsoluteCutPosition.ToFloat() : 0;
                        float offsetX = ch.OffsetX.ToFloat();
                        bool isAlternatingCuttingStroke = ch.CutMode == CutOperateUtils.B_ZKEEP;
                        int channelNum = chSeq;
                        float? singleCutDeep = isDeep ? cutDepths[index] : null;
                        tempCutSteps.Add(new CutStep(cutHeight, speed, offsetY, thetaDeg, isAbsolute, channelStartY, offsetX, isAlternatingCuttingStroke, channelNum, singleCutDeep));
                    }
                }
                int chCutLines = Tools.GetIntStringValue(ch.CutLine);
                if (chCutLines == 0)
                {
                    cutSteps.AddRange(tempCutSteps);
                }
                else if (chCutLines > tempCutSteps.Count)
                {
                    cutSteps.AddRange(Enumerable.Range(0, chCutLines).Select(i => tempCutSteps[i % tempCutSteps.Count]));
                }
                else
                {
                    cutSteps.AddRange(tempCutSteps.GetRange(0, chCutLines));
                }
            }
            if (isOpenPrecut)
            {
                CommonResult<List<float>> preCutSpeedResult = AutoCutUtils.GetPreCutSpeedList();
                if (!preCutSpeedResult.IsSuccess || preCutSpeedResult.Data is null)
                {
                    return CommonResult<List<CutStep>>.Failure(preCutSpeedResult.Message);
                }
                List<float> speeds = preCutSpeedResult.Data;
                if (speeds.Count > cutSteps.Count)
                {
                    speeds = speeds.GetRange(0, cutSteps.Count);
                }
                for (int i = 0; i < speeds.Count; i++)
                {
                    if (speeds[i] < cutSteps[i].Speed)
                    {
                        cutSteps[i] = cutSteps[i] with { Speed = speeds[i] };
                    }
                }
            }
            return CommonResult<List<CutStep>>.Success(cutSteps);
        }

        private async Task<CommonResult<FileTableItemModel>> GetFileTableItemModelAsync()
        {
            long id = CurrentUtils.GetCurrentConfiguration().DeviceDataId;
            // 判断是否确认配置信息
            if (id == 0)
            {
                return CommonResult<FileTableItemModel>.Failure("未确认配置信息！");
            }
            // 查询配置信息
            List<FileTableItemModel> listConf = await SqlHelper.TableAsync<FileTableItemModel>().Where(t => t.Id == id).ToListAsync();
            if (listConf.Count == 0)
            {
                return CommonResult<FileTableItemModel>.Failure("未确认配置信息！");
            }
            FileTableItemModel fileTableItem = listConf[0];
            return CommonResult<FileTableItemModel>.Success(fileTableItem);
        }

        public static int AreIndexesContinuous(float[] setBladeHeight, float[] feedSpeeds, float[] yIndexs, float[] repeatTimes)
        {
            // 获取满足条件的索引
            var validIndexes = setBladeHeight
                .Select((value, index) => new { Value = value, Index = index })
                .Where(x => x.Value > 0 && feedSpeeds[x.Index] > 0 && yIndexs[x.Index] != 0 && repeatTimes[x.Index] > 0)
                .Select(x => x.Index)
                .OrderBy(x => x)
                .ToList();
            // 检查是否有有效的索引
            if (!validIndexes.Any())
            {
                return 0; // 没有符合条件的索引
            }
            // 检查索引是否连续
            bool areIndexesContinuous = validIndexes.Zip(validIndexes.Skip(1), (current, next) => next - current == 1).All(x => x);

            // 如果有效索引是连续的，返回最大索引，否则返回0
            return areIndexesContinuous ? validIndexes.Max() : -1;
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
            if (!navigationContext.Parameters.TryGetValue("isContinue", out bool isContinue))
            {
                _pauseCts = new CancellationTokenSource();
                _monitoringCts = new CancellationTokenSource();
            }
            // 加载参数
            FileTableItemModel fileTableItem = CurrentUtils.GetFileTableItemModel();
            CutParam.DeviceDataNo = fileTableItem.DeviceDataNo;
            CutParam.DeviceDataId = fileTableItem.DeviceDataId;
            CutParam.ChannelNum = CurrentUtils.GetCurrentConfiguration().ChannelNum;
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