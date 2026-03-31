using HslCommunication.Profinet.OpenProtocol;
using MaterialDesignThemes.Wpf;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using ScottPlot.TickGenerators.TimeUnits;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using 精密切割系统.Driver;
using 精密切割系统.Entities;
using 精密切割系统.Extensions;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut.Workpieces;
using 精密切割系统.Model.logs;
using 精密切割系统.PubSubEvent;
using 精密切割系统.Utils;
using 精密切割系统.ViewModel;
using static 精密切割系统.Model.cut.ServicePauseResult;

namespace 精密切割系统.Model.cut
{
    public class SemiAutoCutService
    {
        private static readonly Lazy<SemiAutoCutService> _lazy = new(() => new SemiAutoCutService());

        public static SemiAutoCutService Instance
        {
            get { return _lazy.Value; }
        }

        private readonly ThetaAlignService _alignService;

        public event Action<CutServiceProcess>? CutServiceProcessChanged;

        public event Action<CutServicePauseData>? CutServicePaused;

        public event Action? CutServiceCanPause;

        public event Action<CutServiceCompleteData>? CutServiceCompleted;

        public event Action? RemindReplaceWafer;

        private TaskCompletionSource<ServicePauseResult>? _continueTcs;

        private Queue<float> _preCutQueue = new();

        private CutDirection _cutDirection;

        /// <summary>
        /// 切割方向
        /// </summary>
        public CutDirection CutDirection
        {
            get { return _cutDirection; }
            set { _cutDirection = value; }
        }

        private bool _isReady;

        /// <summary>
        /// 是否准备就绪
        /// </summary>
        public bool IsReady
        {
            get { return _isReady; }
            private set { _isReady = value; }
        }

        private bool _isRuning;

        /// <summary>
        /// 是否运行中
        /// </summary>
        public bool IsRuning
        {
            get { return _isRuning; }
            set { _isRuning = value; }
        }

        private float _depthCompensationValue;

        /// <summary>
        /// 深度补偿值
        /// </summary>
        public float DepthCompensationValue
        {
            get { return _depthCompensationValue; }
            set { _depthCompensationValue = value; }
        }

        private float _feedSpeedCompCompensationValue;

        /// <summary>
        /// 进给速度补偿值
        /// </summary>
        public float FeedSpeedCompCompensationValue
        {
            get { return _feedSpeedCompCompensationValue; }
            set { _feedSpeedCompCompensationValue = value; }
        }

        private bool _isOpenPrecut;

        /// <summary>
        /// 是否打开预切割
        /// </summary>
        public bool IsOpenPrecut
        {
            get { return _isOpenPrecut; }
            set { _isOpenPrecut = value; }
        }

        private int _cutLine;

        /// <summary>
        /// 切割刀数（0=all）
        /// </summary>
        public int CutLine
        {
            get { return _cutLine; }
            set { _cutLine = value; }
        }

        private int _spindleRev;

        /// <summary>
        /// 主轴转速
        /// </summary>
        public int SpindleRev
        {
            get { return _spindleRev; }
            set { _spindleRev = value; }
        }

        private bool _hasNotTakenOutWorkpiecesAfterCuttingCompleted = false;

        /// <summary>
        /// 切割结束后是否有未取出的工件
        /// </summary>
        public bool HasNotTakenOutWorkpiecesAfterCuttingCompleted
        {
            get { return _hasNotTakenOutWorkpiecesAfterCuttingCompleted; }
            set { _hasNotTakenOutWorkpiecesAfterCuttingCompleted = value; }
        }

        private int _currentChannelNum = 1;

        /// <summary>
        /// 当前切割通道
        /// </summary>
        public int CurrentChannelNum
        {
            get { return _currentChannelNum; }
            set { _currentChannelNum = value; }
        }

        /// <summary>
        /// 超出工件后是否继续切割
        /// </summary>
        private bool _isContinueBeyondWorkpiece;

        private SemiAutoCutService()
        {
            _isReady = true;
            _isContinueBeyondWorkpiece = false;
            _alignService = ThetaAlignService.Instance;
            _cutDirection = CutDirection.Backward;
            _depthCompensationValue = 0;
            _feedSpeedCompCompensationValue = 0;
            _isOpenPrecut = false;
        }

        public static async Task<CommonResult> CheckCutAsync()
        {
            if (!GlobalParams.OnlineFlag) return CommonResult.Success();
            if (!await PlcControl.tagControl.wholeDevice.IsCompletedSystemInitAsync())
            {
                return CommonResult.Failure("请完成系统初始化！");
            }
            if (!await PlcControl.tagControl.wholeDevice.IsOpenVacuumSwitchAsync())
            {
                return CommonResult.Failure("请打开工作盘真空！");
            }
            //if (await PlcControl.tagControl.wholeDevice.IsOpenCutSecurityDoorAsync())
            //{
            //    return CommonResult.Failure("请关闭切割安全门！");
            //}
            //if (await PlcControl.tagControl.wholeDevice.IsOpenCameraSecurityDoorAsync())
            //{
            //    return CommonResult.Failure("请关闭相机安全门！");
            //}
            return CommonResult.Success();
        }

        private (int firstCheck, int nextCheck) GetScratchInspectionFlagAndInterval(string channelNum, ScratchInspectionParametersEntity scratchInspection)
        {
            return channelNum switch
            {
                GlobalParams.CH1 => (scratchInspection.FirstCheckCh1.ToInt(), scratchInspection.NextCheckCh1.ToInt()),
                GlobalParams.CH2 => (scratchInspection.FirstCheckCh2.ToInt(), scratchInspection.NextCheckCh2.ToInt()),
                GlobalParams.CH3 => (scratchInspection.FirstCheckCh3.ToInt(), scratchInspection.NextCheckCh3.ToInt()),
                GlobalParams.CH4 => (scratchInspection.FirstCheckCh4.ToInt(), scratchInspection.NextCheckCh4.ToInt()),
                _ => (0, 0),
            };
        }

        private async Task StartCuttingRecord(CancellationToken token)
        {
            string parentPath = System.Environment.CurrentDirectory + "\\Records\\";
            if (!Directory.Exists(parentPath))
            {
                Directory.CreateDirectory(parentPath);
            }

            DateTime now = DateTime.Now;
            string format1 = now.ToString("yyyy_MM_dd_HHmmss");
            string fileName = "cuttingRecord" + format1 + ".txt";
            string filePath = Path.Combine(parentPath, fileName); // 使用Path.Combine更安全

            int colWidth = 30;

            // 使用using语句确保资源释放
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (StreamWriter streamWriter = new StreamWriter(fs, Encoding.UTF8))
            {
                // 写入标题
                string cuttingRecord = string.Format("{0}{1}{2}{3}{4}{5}{6}{7}",
                    "切割次数".PadRightDisplay(colWidth),
                    "指令位置Y".PadRightDisplay(colWidth),
                    "平均位置Y".PadRightDisplay(colWidth),
                    "切割进入时位置Y".PadRightDisplay(colWidth),
                    "切割出来时位置Y".PadRightDisplay(colWidth),
                    "指令位置Z".PadRightDisplay(colWidth),
                    "平均位置Z".PadRightDisplay(colWidth),
                    "传感器温度");

                await streamWriter.WriteLineAsync(cuttingRecord);
                await streamWriter.FlushAsync(); // 确保标题写入

                int cutTimes = 1;

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await PlcControl.tagControl.cutting.WaitReadyCuttingDataAsyncAsync(token);
                        var (instructionPositionY, averagePositionY, justEnterPositionY, justOutPositionY, instructionPositionZ1, averagePositionZ1) =
                            await PlcControl.tagControl.cutting.GetCuttingDataAsync();

                        await PlcControl.tagControl.cutting.SetIsReadyCuttingDataAsync(false);

                        var temperatures = await PlcControl.tagControl.wholeDevice.GetTemperatureSensorsAsync();
                        string temperatureInfo = temperatures != null
                            ? string.Join("  ", temperatures.Select(t => $"{t:F1}°C"))
                            : "N/A";

                        cuttingRecord = string.Format("{0}{1}{2}{3}{4}{5}{6}{7}",
                            cutTimes.ToString().PadRightDisplay(colWidth),
                            instructionPositionY.ToString("F6").PadRightDisplay(colWidth),
                            averagePositionY.ToString("F6").PadRightDisplay(colWidth),
                            justEnterPositionY.ToString("F6").PadRightDisplay(colWidth),
                            justOutPositionY.ToString("F6").PadRightDisplay(colWidth),
                            instructionPositionZ1.ToString("F6").PadRightDisplay(colWidth),
                            averagePositionZ1.ToString("F6").PadRightDisplay(colWidth),
                            temperatureInfo);

                        await streamWriter.WriteLineAsync(cuttingRecord);
                        await streamWriter.FlushAsync();

                        cutTimes++;
                    }
                    catch (OperationCanceledException)
                    {
                        // 正常取消，退出循环
                        break;
                    }
                    catch (Exception ex)
                    {
                        await streamWriter.WriteLineAsync($"写入异常: {ex.ToString()}");
                        await streamWriter.FlushAsync();
                    }

                    await Task.Delay(200, token); // 传入token，支持取消
                }
            } // 离开using作用域时自动调用Dispose，确保数据写入
        }

        public async Task<RunResult> RunAsync(List<ChCutStep> cutStepList, IWorkpieces workpiece, float margin, float bladeContactWorkingDiscZ1, float bladeLiftingHeight, IEventAggregator? eventAggregator, CancellationToken pauseToken)
        {
            CancellationToken usingPauseToken = pauseToken;
            ScratchInspectionParametersEntity scratchInspection = await SqlHelper.GetOrCreateEntityAsync(() => new ScratchInspectionParametersEntity());
            var (firstCheck, nextCheck) = GetScratchInspectionFlagAndInterval(cutStepList.First().ChName, scratchInspection);
            AutomaticCompensationCutHeightEntity automaticCompensationCutHeight = await SqlHelper.GetOrCreateEntityAsync(() => new AutomaticCompensationCutHeightEntity());
            int cutHeightCompensationFrequency = automaticCompensationCutHeight.CutHeightCompensationFrequency.ToInt();
            float cutHeightReductionDistance = automaticCompensationCutHeight.CutHeightReductionDistance.ToFloat();
            float currentAutomaticCompensationCutHeight = automaticCompensationCutHeight.CurrentAutomaticCompensationCutHeight.ToFloat();
            Stopwatch stopwatch = new();
            Stopwatch completeStopwatch = Stopwatch.StartNew();
            CancellationTokenSource cuttingRecordCts = new CancellationTokenSource();
            _ = StartCuttingRecord(cuttingRecordCts.Token);
            try
            {
                _isReady = false;
                _isRuning = true;
                var timeoutToken = TaskUtils.GetTimeoutCancellationToken(TimeSpan.FromMinutes(30));
                await PlcControl.tagControl.wholeDevice.OpenCuttingWaterAndConfirmStatusAsync(timeoutToken.Token);
                //打开切割水
                await PlcControl.tagControl.wholeDevice.OpenCuttingWaterAsync();
                //进入全自动切割模式
                await PlcControl.tagControl.cutting.EnterCuttingModeAsync(timeoutToken.Token);
                List<CutStep> steps = cutStepList.SelectMany(ch => ch.CutSteps).ToList();
                int cutLine = steps.Count;
                //切割刀数（0 = all）
                if (_cutLine != 0 && _cutLine < steps.Count)
                {
                    cutLine = _cutLine;
                }
                PathCalculator pathCalculator = new(steps.Select(p => p.Speed).ToList());
                float currentKnifeRemainTime = 60; //初始值
                LineSegment? preLine = null;
                float? preCutSpeed = null;
                CutServiceProcess? preFirstCutServiceProcess = null;
                CutServiceProcess? preNextCutServiceProcess = null;
                bool isFirstCutAfterPause = true;
                bool isXFromSmallToLarge = true;
                int cutTimes = 0;
                foreach (ChCutStep chCutStep in cutStepList)
                {
                    if (cutTimes >= cutLine)
                    {
                        break;
                    }
                    List<CutStep> cutSteps = chCutStep.CutSteps;
                    _currentChannelNum = cutSteps.First().ChannelNum;
                    if (cutSteps.First().IsAbsolute)
                    {
                        workpiece.Reset(cutSteps.First().ChannelStartY);
                    }
                    (firstCheck, nextCheck) = GetScratchInspectionFlagAndInterval(chCutStep.ChName, scratchInspection);
                    int currentChCutTimes = 0;
                    while (currentChCutTimes < cutSteps.Count && cutTimes < cutLine)
                    {
                        LineSegment? line = null;
                        CutStep cutStep = cutSteps[currentChCutTimes];
                        try
                        {
                            //停机检查划痕
                            if (firstCheck != 0 && nextCheck != 0 && (currentChCutTimes == firstCheck || (currentChCutTimes > firstCheck && (currentChCutTimes - firstCheck) % nextCheck == 0)))
                            {
                                //触发切割暂停事件
                                (RunResult runResult, usingPauseToken) = await WaitContinueAsync(preLine ?? line, workpiece, currentKnifeRemainTime, chCutStep.CutSteps, false, "停机检查，请检查工件情况！");
                                preLine = null;
                                preCutSpeed = null;
                                isFirstCutAfterPause = true;
                                if (!runResult.IsSuccess)
                                {
                                    return runResult;
                                }
                            }
                            //自动补偿刀片高度
                            if (ShouldApplyHeightCompensation(cutHeightCompensationFrequency, cutTimes))
                            {
                                currentAutomaticCompensationCutHeight += cutHeightReductionDistance;
                                automaticCompensationCutHeight.CurrentAutomaticCompensationCutHeight = currentAutomaticCompensationCutHeight.ToString(GlobalParams.DecimalStringFormat);
                                await SqlHelper.UpdateAsync(automaticCompensationCutHeight);
                            }
                            //检测工件是否切完
                            //if (!workpiece.CheckCutDistance(_cutDirection, cutStep.NextStepDistance) && !_isContinueBeyondWorkpiece)
                            //{
                            //    RemindReplaceWafer?.Invoke();
                            //    (RunResult runResult, usingPauseToken) = await WaitContinueAsync(preLine ?? line, workpiece, currentKnifeRemainTime, cutSteps.GetRange(cutTimes, cutSteps.Count - cutTimes), "下一刀将超出工件！");
                            //    if (!runResult.IsSuccess)
                            //    {
                            //        return runResult;
                            //    }
                            //    _isContinueBeyondWorkpiece = true;
                            //}
                            line = workpiece.CalculateCuttingLine();
                            float totaHeightlCompensation = _depthCompensationValue - currentAutomaticCompensationCutHeight;
                            float actualCutHeight = cutStep.CutHeight + totaHeightlCompensation;
                            float targetEndZ = bladeContactWorkingDiscZ1 - actualCutHeight;
                            float startZ = bladeContactWorkingDiscZ1 - workpiece.WorkThickness - workpiece.TapeThickness - bladeLiftingHeight;
                            float depthEntry = bladeContactWorkingDiscZ1 - workpiece.WorkThickness - workpiece.TapeThickness - 0.5f;
                            float cutSpeed;
                            Tools.LogDebug($"_feedSpeedCompCompensationValue：{_feedSpeedCompCompensationValue}");
                            Tools.LogDebug($"_isOpenPrecut：{_isOpenPrecut}");
                            Tools.LogDebug($"_preCutQueue.Count：{_preCutQueue.Count}");
                            Tools.LogDebug($"_preCutQueue：{_preCutQueue.ToString()}");
                            if (_feedSpeedCompCompensationValue != 0)
                            {
                                //速度变更
                                cutSpeed = _feedSpeedCompCompensationValue;
                                Tools.LogDebug($"切割速度变更为：{cutSpeed}");
                            }
                            else if (_isOpenPrecut && _preCutQueue.Count > 0)
                            {
                                //预切割
                                cutSpeed = _preCutQueue.Dequeue();
                                Tools.LogDebug($"预切割速度：{cutSpeed}");
                            }
                            else
                            {
                                cutSpeed = cutStep.Speed;
                                Tools.LogDebug($"正常切割速度：{cutSpeed}");
                            }
                            //加上边距
                            float startX = line.StartPoint.X + cutStep.OffsetX - margin;
                            float endX = line.EndPoint.X + cutStep.OffsetX + margin;
                            float cutLength = MathF.Abs(endX - startX);
                            //计算当前刀剩余时间
                            currentKnifeRemainTime = MathF.Abs(startX - endX) / (preCutSpeed is null ? cutSpeed : preCutSpeed.Value);
                            float justContactWork = bladeContactWorkingDiscZ1 - workpiece.WorkThickness - workpiece.TapeThickness - totaHeightlCompensation;
                            List<float> endZList = [];
                            if (cutStep.SingleCutDeep is not null && cutStep.SingleCutDeep > 0)
                            {
                                if (chCutStep.CutMode == CutMode.B_A)
                                {
                                    float z = justContactWork + cutStep.SingleCutDeep.Value;
                                    if (z < targetEndZ)
                                    {
                                        endZList.Add(z);
                                    }
                                }
                                else
                                {
                                    for (float z = justContactWork + cutStep.SingleCutDeep.Value; z < targetEndZ; z += cutStep.SingleCutDeep.Value)
                                    {
                                        endZList.Add(z);
                                    }
                                }
                            }
                            endZList.Add(targetEndZ);
                            float compensateY = 0;
                            await PlcControl.tagControl.ThetaAxis.SetAbsoluteSpeedAsync(GlobalParams.ThetaDefaultSpeed);
                            foreach (float endZ in endZList)
                            {
                                //x方向交替切割
                                if (chCutStep.CutMode == CutMode.B_ZKEEP)
                                {
                                    if (isXFromSmallToLarge)
                                    {
                                        if (startX > endX)
                                        {
                                            (startX, endX) = (endX, startX);
                                        }
                                    }
                                    else
                                    {
                                        if (startX < endX)
                                        {
                                            (startX, endX) = (endX, startX);
                                        }
                                    }
                                    isXFromSmallToLarge = !isXFromSmallToLarge;
                                }
                                stopwatch.Restart();
                                compensateY = await PlcControl.GetCompensateAsync(PlcControl.tagControl.Yaxis, line.StartPoint.Y);
                                if (preFirstCutServiceProcess == null)
                                {
                                    preFirstCutServiceProcess = new CutServiceProcess(actualCutHeight, cutSpeed, compensateY, cutSteps.Count, currentChCutTimes + 1, chCutStep.ChName);
                                }
                                //触发切割进度更新事件
                                CutServiceProcessChanged?.Invoke(preFirstCutServiceProcess.Value);
                                preFirstCutServiceProcess = new CutServiceProcess(actualCutHeight, cutSpeed, compensateY, cutSteps.Count, currentChCutTimes + 1, chCutStep.ChName);
                                //设置切割参数
                                bool sendCutParams = await PlcControl.tagControl.cutting.SetCutParamsAsync(cutSpeed, endZ, startZ, startX, endX, compensateY, "0", cutStep.ThetaDeg, _spindleRev, depthEntry);
                                if (!sendCutParams)
                                {
                                    while (!sendCutParams)
                                    {
                                        eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("切割参数发送失败，重试中..."));
                                        sendCutParams = await PlcControl.tagControl.cutting.SetCutParamsAsync(cutSpeed, endZ, startZ, startX, endX, compensateY, "0", cutStep.ThetaDeg, _spindleRev, depthEntry);
                                        await Task.Delay(1000);
                                    }
                                    eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("切割参数重新发送成功！"));
                                }
                                //当前切割次数
                                int? curCutNum = await PlcControl.tagControl.cutting.GetCutNumAsync();
                                if (curCutNum == null)
                                {
                                    return RunResult.Fail("获取当前切割次数失败！");
                                }
                                DateTime startTime = DateTime.Now;
                                var monitor = new ManualPropertyMonitor<int>();
                                monitor.StartMonitoring(() => PLCValue.SlightVibration, PLCValue.UpdateFrequency);
                                //开始切割前检查是否暂停（暂停后第一刀不检查）
                                if (isFirstCutAfterPause)
                                {
                                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                                    await PlcControl.tagControl.cutting.StartCutAsync();
                                    await PlcControl.tagControl.cutting.WaitCutNumUdatedAsync(curCutNum.Value + 1, cts.Token);
                                }
                                else
                                {
                                    usingPauseToken.ThrowIfCancellationRequested();
                                    await PlcControl.tagControl.cutting.StartCutAsync();
                                    await PlcControl.tagControl.cutting.WaitCutNumUdatedAsync(curCutNum.Value + 1, usingPauseToken);
                                }
                                isFirstCutAfterPause = false;
                                var monitorResult = monitor.StopMonitoring();
                                int actualMonitorCount = (int)(MathF.Abs(startX - endX) / cutSpeed * 1000 / 50);//切割过程中的震动点数
                                actualMonitorCount = (int)(actualMonitorCount * 0.9);//再去掉后面部分点，防止误差
                                if (monitorResult.Count > actualMonitorCount)
                                {
                                    monitorResult = monitorResult.GetRange(0, actualMonitorCount);
                                }
                                var temperatures = await PlcControl.tagControl.wholeDevice.GetTemperatureSensorsAsync();
                                string temperatureInfo = temperatures != null ? string.Join("  ", temperatures.Select(t => $"{t:F1}°C")) : "N/A";
                                // 记录日志
                                RunLogsCommon.LogEvent(
                                    LogType.Cut,
                                    new RunLogsViewModel(LogType.Cut, "切割"),
                                    new RunLogsViewModel("切割面", chCutStep.ChName),
                                    new RunLogsViewModel("刀数", (currentChCutTimes + 1).ToString()),
                                    new RunLogsViewModel("开始时间", startTime.ToString("yyyy年MM月dd日 HH:mm:ss")),
                                    new RunLogsViewModel("结束时间", DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss")),
                                    new RunLogsViewModel("耗时", (DateTime.Now - startTime).TotalSeconds.ToString("F2") + "sec"),
                                    new RunLogsViewModel("切割速度", cutSpeed.ToString()),
                                    new RunLogsViewModel("X轴开始位置", startX.ToString()),
                                    new RunLogsViewModel("X轴结束位置", endX.ToString()),
                                    new RunLogsViewModel("Z轴开始位置", endZ.ToString()),
                                    new RunLogsViewModel("Z轴结束位置", startZ.ToString()),
                                    new RunLogsViewModel("Y轴切割位置", line.StartPoint.Y.ToString()),
                                    new RunLogsViewModel("Y轴实际切割位置", compensateY.ToString()),
                                    new RunLogsViewModel("步进距离", cutStep.NextStepDistance.ToString()),
                                    new RunLogsViewModel("theta角度", cutStep.ThetaDeg.ToString()),
                                    new RunLogsViewModel("主轴转速", _spindleRev.ToString()),
                                    new RunLogsViewModel("传感器温度", temperatureInfo),
                                    new RunLogsViewModel("震动幅度", string.Join(" ", monitorResult))
                                    );
                                stopwatch.Stop();
                                if (preLine is not null)
                                {
                                    pathCalculator.ReportPass(cutTimes - 1, cutLength, (float)stopwatch.Elapsed.TotalSeconds);
                                }
                            }
                            if (preNextCutServiceProcess == null)
                            {
                                preNextCutServiceProcess = new CutServiceProcess(actualCutHeight, cutSpeed, compensateY, cutSteps.Count, currentChCutTimes + 1, chCutStep.ChName, cutLength, pathCalculator.EstimateRemainingTime(), true);
                            }
                            //触发切割进度更新事件
                            CutServiceProcessChanged?.Invoke(preNextCutServiceProcess.Value);
                            preNextCutServiceProcess = new CutServiceProcess(actualCutHeight, cutSpeed, compensateY, cutSteps.Count, currentChCutTimes + 1, chCutStep.ChName, cutLength, pathCalculator.EstimateRemainingTime(), true);
                            preLine = line;
                            preCutSpeed = cutSpeed;
                            currentChCutTimes++;
                            cutTimes++;
                            //更新工件到下一切割位置
                            workpiece.UpdateToNextCutPosition(_cutDirection, cutStep.NextStepDistance);
                        }
                        catch (OperationCanceledException)
                        {
                            (RunResult runResult, usingPauseToken) = await WaitContinueAsync(preLine ?? line, workpiece, currentKnifeRemainTime, chCutStep.CutSteps, false);
                            preLine = null;
                            preCutSpeed = null;
                            isFirstCutAfterPause = true;
                            if (!runResult.IsSuccess)
                            {
                                return runResult;
                            }
                        }
                        catch (Exception ex)
                        {
                            Tools.LogDebug(ex.ToString());
                            return RunResult.Fail($"执行切割步骤失败！{ex.Message}");
                        }
                    }
                }
                if (preNextCutServiceProcess != null)
                {
                    CutServiceProcessChanged?.Invoke(preNextCutServiceProcess.Value);
                }
                completeStopwatch.Stop();
                // 切割完成
                HasNotTakenOutWorkpiecesAfterCuttingCompleted = true;
                if (GlobalParams.DeviceModel == GlobalParams.Device_321)
                {
                    TimeSpan timeSpan = TimeSpan.FromSeconds(completeStopwatch.Elapsed.TotalSeconds);
                    string formattedTime = $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
                    await WaitContinueAsync(preLine, workpiece, currentKnifeRemainTime, [], true, $"切割完成！ 总用时：{formattedTime}");
                }
                //触发切割完成事件
                //CutServiceCompleted?.Invoke(new CutServiceCompleteData(preLine));
            }
            finally
            {
                _isReady = true;
                _isRuning = false;
                _isContinueBeyondWorkpiece = false;
                _currentChannelNum = 1;
                completeStopwatch.Stop();
                await PlcControl.tagControl.wholeDevice.CloseWorkpieceBlowingAsync();
                //退出全自动切割模式
                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(default);
                var operationParameter = await CurrentUtils.GetOperationParametersModelAsync();
                if (operationParameter.IsAutoShutOffWaterWhenCuttingCompleted)
                {
                    await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
                }
                HasNotTakenOutWorkpiecesAfterCuttingCompleted = true;
                stopwatch.Stop();
                await cuttingRecordCts.CancelAsync();
            }
            return RunResult.Success();
        }

        public void Continue(CancellationToken token)
        {
            _continueTcs?.TrySetResult(ServicePauseResult.Continue(token)); // 继续执行
            _continueTcs = null;
        }

        public void ContinueAndResetCutY(CancellationToken token)
        {
            _continueTcs?.TrySetResult(ServicePauseResult.ContinueAndResetCutY(token)); // 继续执行
            _continueTcs = null;
        }

        public void Stop(ServicePauseResult pauseResult)
        {
            _continueTcs?.TrySetResult(pauseResult); // 停止切割
            _continueTcs = null;
        }

        private async Task<(RunResult, CancellationToken)> WaitContinueAsync(LineSegment? line, IWorkpieces workpieces, float currentKnifeRemainTime, List<CutStep> remainCutSteps, bool isCompleted, string? message = null)
        {
            CutServicePaused?.Invoke(new CutServicePauseData(line, message, currentKnifeRemainTime, remainCutSteps, isCompleted));
            _continueTcs = new TaskCompletionSource<ServicePauseResult>();
            ServicePauseResult result = await _continueTcs.Task;
            await PlcControl.tagControl.wholeDevice.CloseWorkpieceBlowingAsync();
            switch (result.Type)
            {
                case ServicePauseResultType.ContinueAndResetCutY:
                    workpieces.Reset(await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync() ?? 0);
                    goto case ServicePauseResultType.Continue;
                case ServicePauseResultType.Continue:
                    // 更新使用的暂停令牌
                    CancellationToken usingPauseToken = result.Token ?? default;
                    try
                    {
                        await PlcControl.tagControl.wholeDevice.CloseWorkpieceBlowingAsync();
                        var timeoutToken = TaskUtils.GetTimeoutCancellationToken(TimeSpan.FromMinutes(10), usingPauseToken);
                        await PlcControl.tagControl.wholeDevice.OpenCuttingWaterAndConfirmStatusAsync(timeoutToken.Token);
                        _isRuning = true;
                        return (RunResult.Success(), usingPauseToken);
                    }
                    catch (Exception)
                    {
                        return (RunResult.Fail("打开切割水出现异常！"), default);
                    }
                case ServicePauseResultType.BladeScrap:
                    return (RunResult.Fail("刀片已报废！"), default);

                case ServicePauseResultType.Stop:
                    return (RunResult.Fail("停止切割！"), default);

                default:
                    return (RunResult.Fail("切割异常！"), default);
            }
        }

        private async Task<float> GetCutThetaAlignDegAsync()
        {
            return _alignService.ThetaAlignCompletedDeg ?? await PlcControl.tagControl.ThetaAxis.GetCurrentLocationAsync() ?? 0;
        }

        public void TriggerPrecut(bool isShowMaterial = false)
        {
            _isOpenPrecut = !_isOpenPrecut;
            if (isShowMaterial)
            {
                if (_isOpenPrecut)
                {
                    MaterialSnack("开启预切割！", SnackType.SUCCESS);
                }
                else
                {
                    MaterialSnack("关闭预切割！", SnackType.SUCCESS);
                }
            }
        }

        public void UpdatePreCutQueue(List<float> cutSpeeds)
        {
            _preCutQueue.Clear();
            foreach (var cutSpeed in cutSpeeds)
            {
                _preCutQueue.Enqueue(cutSpeed);
            }
        }

        private bool ShouldApplyHeightCompensation(int cutHeightCompensationFrequency, int cutTimes)
        {
            // 参数验证
            if (cutHeightCompensationFrequency == 0) return false;
            if (cutTimes == 0) return false;

            // 判断是否到达补偿周期
            return cutTimes % cutHeightCompensationFrequency == 0;
        }
    }

    public record CutStep(float CutHeight, float Speed, float NextStepDistance, float ThetaDeg, bool IsAbsolute, float ChannelStartY, float OffsetX = 0, int ChannelNum = 1, float? SingleCutDeep = null);

    public record ChCutStep(string ChName, CutMode CutMode, List<CutStep> CutSteps);

    public record CutServicePauseData(LineSegment? Line, string? Message, float CurrentKnifeRemainTime, List<CutStep> RemainCutSteps, bool IsCompleted);

    public record CutServiceCompleteData(LineSegment? Line);
}