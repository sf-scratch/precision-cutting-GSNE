using MaterialDesignThemes.Wpf;
using ScottPlot.TickGenerators.TimeUnits;
using System.Diagnostics;
using 精密切割系统.Driver;
using 精密切割系统.Entities;
using 精密切割系统.Helpers;
using 精密切割系统.Model.cut.Workpieces;
using 精密切割系统.Model.logs;
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
            private set { _isRuning = value; }
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

        private int _currentChannelNum;

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

        public async Task<RunResult> RunAsync(List<ChCutStep> cutStepList, IWorkpieces workpiece, float margin, float bladeContactWorkingDiscZ1, float bladeLiftingHeight, bool isExchangeX, CancellationToken pauseToken)
        {
            CancellationToken usingPauseToken = pauseToken;
            ScratchInspectionParametersEntity scratchInspection = await SqlHelper.GetOrCreateEntityAsync(() => new ScratchInspectionParametersEntity());
            var (firstCheck, nextCheck) = GetScratchInspectionFlagAndInterval(cutStepList.First().ChName, scratchInspection);
            AutomaticCompensationCutHeightEntity automaticCompensationCutHeight = await SqlHelper.GetOrCreateEntityAsync(() => new AutomaticCompensationCutHeightEntity());
            int cutHeightCompensationFrequency = automaticCompensationCutHeight.CutHeightCompensationFrequency.ToInt();
            float cutHeightReductionDistance = automaticCompensationCutHeight.CutHeightReductionDistance.ToFloat();
            float currentAutomaticCompensationCutHeight = automaticCompensationCutHeight.CurrentAutomaticCompensationCutHeight.ToFloat();
            Stopwatch stopwatch = new();
            try
            {
                _isReady = false;
                _isRuning = true;
                //打开切割水
                await PlcControl.tagControl.wholeDevice.OpenCuttingWaterAsync();
                //进入全自动切割模式
                await PlcControl.tagControl.cutting.EnterCuttingModeAsync(usingPauseToken);
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
                    for (int currentChCutTimes = 0; currentChCutTimes < cutSteps.Count && cutTimes < cutLine; currentChCutTimes++, cutTimes++)
                    {
                        LineSegment? line = null;
                        CutStep cutStep = cutSteps[currentChCutTimes];
                        try
                        {
                            //停机检查划痕
                            if (firstCheck != 0 && nextCheck != 0 && (currentChCutTimes == firstCheck || (currentChCutTimes > firstCheck && (currentChCutTimes - firstCheck) % nextCheck == 0)))
                            {
                                //触发切割暂停事件
                                (RunResult runResult, usingPauseToken) = await WaitContinueAsync(preLine ?? line, workpiece, currentKnifeRemainTime, chCutStep.CutSteps, "停机检查，请检查工件情况！");
                                if (!runResult.IsSuccess)
                                {
                                    return runResult;
                                }
                            }
                            //自动补偿刀片高度
                            if (cutTimes % cutHeightCompensationFrequency == 0)
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
                            // 更新切割步骤的NextStepDistance为0，防止累加
                            cutSteps[currentChCutTimes] = cutStep with { NextStepDistance = 0 };
                            //检查是否暂停
                            if (usingPauseToken.IsCancellationRequested)
                            {
                                (RunResult runResult, usingPauseToken) = await WaitContinueAsync(preLine ?? line, workpiece, currentKnifeRemainTime, chCutStep.CutSteps);
                                if (!runResult.IsSuccess)
                                {
                                    return runResult;
                                }
                            }
                            line = workpiece.CalculateCuttingLine();
                            float actualCutHeight = cutStep.CutHeight + _depthCompensationValue - currentAutomaticCompensationCutHeight;
                            float targetEndZ = bladeContactWorkingDiscZ1 - actualCutHeight;
                            float startZ = bladeContactWorkingDiscZ1 - workpiece.WorkThickness - workpiece.TapeThickness - bladeLiftingHeight;
                            float depthEntry = bladeContactWorkingDiscZ1 - workpiece.WorkThickness - workpiece.TapeThickness - 0.5f;
                            float cutLength = MathF.Abs(line.EndPoint.X - line.StartPoint.X);
                            float cutSpeed;
                            if (_feedSpeedCompCompensationValue != 0)
                            {
                                //速度变更
                                cutSpeed = _feedSpeedCompCompensationValue;
                            }
                            else if (_isOpenPrecut && _preCutQueue.Count > 0)
                            {
                                //预切割
                                cutSpeed = _preCutQueue.Dequeue();
                            }
                            else
                            {
                                cutSpeed = cutStep.Speed;
                            }
                            //加上边距
                            float startX = line.StartPoint.X + cutStep.OffsetX - margin;
                            float endX = line.EndPoint.X + cutStep.OffsetX + margin;
                            //计算当前刀剩余时间
                            currentKnifeRemainTime = MathF.Abs(startX - endX) / (preCutSpeed is null ? cutSpeed : preCutSpeed.Value);
                            //x方向交替切割
                            if (cutStep.IsAlternatingCuttingStroke)
                            {
                                if (isExchangeX)
                                {
                                    (startX, endX) = (endX, startX);
                                }
                                isExchangeX = !isExchangeX;
                            }
                            List<float> endZList = [];
                            if (cutStep.SingleCutDeep is not null && cutStep.SingleCutDeep > 0)
                            {
                                for (float z = startZ + cutStep.SingleCutDeep.Value; z < targetEndZ; z += cutStep.SingleCutDeep.Value)
                                {
                                    endZList.Add(z);
                                }
                            }
                            endZList.Add(targetEndZ);
                            float compensateY = 0;
                            foreach (float endZ in endZList)
                            {
                                stopwatch.Restart();
                                await PlcControl.tagControl.ThetaAxis.SetAbsoluteSpeedAsync(GlobalParams.ThetaDefaultSpeed);
                                compensateY = await PlcControl.GetCompensateAsync(PlcControl.tagControl.Yaxis, line.StartPoint.Y);
                                if (preFirstCutServiceProcess == null)
                                {
                                    preFirstCutServiceProcess = new CutServiceProcess(actualCutHeight, cutSpeed, compensateY, cutSteps.Count, currentChCutTimes + 1, chCutStep.ChName);
                                }
                                //触发切割进度更新事件
                                CutServiceProcessChanged?.Invoke(preFirstCutServiceProcess.Value);
                                preFirstCutServiceProcess = new CutServiceProcess(actualCutHeight, cutSpeed, compensateY, cutSteps.Count, currentChCutTimes + 1, chCutStep.ChName);
                                //设置切割参数
                                await PlcControl.tagControl.cutting.SetCutParamsAsync(cutSpeed, endZ, startZ, startX, endX, compensateY, "0", cutStep.ThetaDeg, _spindleRev, depthEntry);
                                //当前切割次数
                                int? curCutNum = await PlcControl.tagControl.cutting.GetCutNumAsync();
                                if (curCutNum == null)
                                {
                                    return RunResult.Fail("获取当前切割次数失败！");
                                }
                                DateTime startTime = DateTime.Now;
                                var monitor = new ManualPropertyMonitor<int>();
                                monitor.StartMonitoring(() => PLCValue.SlightVibration);
                                //开始切割信号
                                await PlcControl.tagControl.cutting.StartCutAsync();
                                //等待切割次数变化
                                await PlcControl.tagControl.cutting.WaitCutNumUdatedAsync(curCutNum.Value + 1, usingPauseToken);
                                var monitorResult = monitor.StopMonitoring();
                                int actualMonitorCount = (int)(MathF.Abs(startX - endX) / cutSpeed * 1000 / 50);//切割过程中的震动点数
                                actualMonitorCount = (int)(actualMonitorCount * 0.9);//再去掉后面部分点，防止误差
                                if (monitorResult.Count > actualMonitorCount)
                                {
                                    monitorResult = monitorResult.GetRange(0, actualMonitorCount);
                                }
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
                                    new RunLogsViewModel("Z轴开始位置", endZ.ToString()),
                                    new RunLogsViewModel("Z轴结束位置", startZ.ToString()),
                                    new RunLogsViewModel("X轴开始位置", startX.ToString()),
                                    new RunLogsViewModel("X轴结束位置", endX.ToString()),
                                    new RunLogsViewModel("Y轴切割位置", line.StartPoint.Y.ToString()),
                                    new RunLogsViewModel("Y轴实际切割位置", compensateY.ToString()),
                                    new RunLogsViewModel("theta角度", (cutStep.ThetaDeg).ToString()),
                                    new RunLogsViewModel("主轴转速", _spindleRev.ToString()),
                                    new RunLogsViewModel("震动幅度", string.Join(" ", monitorResult))
                                    );
                                stopwatch.Stop();
                                if (preLine is not null)
                                {
                                    pathCalculator.ReportPass(cutLength, (float)stopwatch.Elapsed.TotalSeconds);
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
                        }
                        catch (OperationCanceledException)
                        {
                            (RunResult runResult, usingPauseToken) = await WaitContinueAsync(preLine ?? line, workpiece, currentKnifeRemainTime, chCutStep.CutSteps);
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
                        //更新工件到下一切割位置
                        workpiece.UpdateToNextCutPosition(_cutDirection, cutStep.NextStepDistance);
                    }
                }
            }
            finally
            {
                _isReady = true;
                _isRuning = false;
                _isContinueBeyondWorkpiece = false;
                _currentChannelNum = 0;
                //退出全自动切割模式
                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(default);
                var operationParameter = await CurrentUtils.GetOperationParametersModelAsync();
                if (operationParameter.IsAutoShutOffWaterWhenCuttingCompleted)
                {
                    await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
                }
                HasNotTakenOutWorkpiecesAfterCuttingCompleted = true;
                stopwatch.Stop();
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

        private async Task<(RunResult, CancellationToken)> WaitContinueAsync(LineSegment? line, IWorkpieces workpieces, float currentKnifeRemainTime, List<CutStep> remainCutSteps, string? message = null)
        {
            _isRuning = false;
            CutServicePaused?.Invoke(new CutServicePauseData(line, message, currentKnifeRemainTime, remainCutSteps));
            _continueTcs = new TaskCompletionSource<ServicePauseResult>();
            ServicePauseResult result = await _continueTcs.Task;
            switch (result.Type)
            {
                case ServicePauseResultType.ContinueAndResetCutY:
                    workpieces.Reset(await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync() ?? 0);
                    goto case ServicePauseResultType.Continue;
                case ServicePauseResultType.Continue:
                    // 更新使用的暂停令牌
                    CancellationToken usingPauseToken = result.Token ?? default;
                    await PlcControl.tagControl.wholeDevice.OpenCuttingWaterAsync();
                    return (RunResult.Success(), usingPauseToken);

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
    }

    public record CutStep(float CutHeight, float Speed, float NextStepDistance, float ThetaDeg, bool IsAbsolute, float ChannelStartY, float OffsetX = 0, bool IsAlternatingCuttingStroke = false, int ChannelNum = 1, float? SingleCutDeep = null);

    public record ChCutStep(string ChName, List<CutStep> CutSteps);

    public record CutServicePauseData(LineSegment? Line, string? Message, float CurrentKnifeRemainTime, List<CutStep> RemainCutSteps);
}