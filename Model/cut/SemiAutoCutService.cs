using MaterialDesignThemes.Wpf;
using ScottPlot.TickGenerators.TimeUnits;
using System.Diagnostics;
using 精密切割系统.Driver;
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

        public event Action<LineSegment?, string?, float>? CutServicePaused;

        public event Action? RemindReplaceWafer;

        private TaskCompletionSource<ServicePauseResult>? _continueTcs;

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

        /// <summary>
        /// theta轴校准角度
        /// </summary>
        private float _cutThetaAlignDeg;

        private SemiAutoCutService()
        {
            _isReady = true;
            _isContinueBeyondWorkpiece = false;
            _alignService = ThetaAlignService.Instance;
            _cutDirection = CutDirection.Backward;
            _depthCompensationValue = 0;
            _feedSpeedCompCompensationValue = 0;
            _isOpenPrecut = false;
            _currentChannelNum = 1;
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

        public async Task<RunResult> RunAsync(List<CutStep> cutStepList, IWorkpieces workpiece, float margin, float bladeContactWorkingDiscZ1, float bladeLiftingHeight, bool isExchangeX, CancellationToken pauseToken)
        {
            List<CutStep> cutSteps;
            //切割刀数（0 = all）
            if (_cutLine != 0 && _cutLine < cutStepList.Count)
            {
                cutSteps = cutStepList.GetRange(0, _cutLine);
            }
            else
            {
                cutSteps = cutStepList.ToList();
            }
            CancellationToken usingPauseToken = pauseToken;
            _cutThetaAlignDeg = await GetCutThetaAlignDegAsync();
            Stopwatch stopwatch = new();
            try
            {
                _isReady = false;
                _isRuning = true;
                PathCalculator pathCalculator = new(cutSteps.Select(p => p.Speed).ToList());
                //打开切割水
                await PlcControl.tagControl.wholeDevice.OpenCuttingWaterAsync();
                //进入全自动切割模式
                await PlcControl.tagControl.cutting.EnterCuttingModeAsync(usingPauseToken);
                float currentKnifeRemainTime = 60; //初始值
                LineSegment? preLine = null;
                _currentChannelNum = 1;
                int cutTime = 0;
                while (cutTime < cutSteps.Count)
                {
                    LineSegment? line = null;
                    CutStep cutStep = cutSteps[cutTime];
                    try
                    {
                        //检查切换通道
                        if (cutStep.ChannelNum != _currentChannelNum)
                        {
                            workpiece.Reset(cutStep.ChannelStartY);
                        }
                        _currentChannelNum = cutStep.ChannelNum;
                        //检测工件是否切完
                        if (!workpiece.CheckCutDistance(_cutDirection, cutStep.NextStepDistance) && !_isContinueBeyondWorkpiece)
                        {
                            RemindReplaceWafer?.Invoke();
                            (RunResult runResult, usingPauseToken) = await WaitContinueAsync(preLine ?? line, workpiece, currentKnifeRemainTime, "下一刀将超出工件！");
                            if (!runResult.IsSuccess)
                            {
                                return runResult;
                            }
                            _isContinueBeyondWorkpiece = true;
                        }
                        // 更新切割步骤的NextStepDistance为0，防止累加
                        cutSteps[cutTime] = cutStep with { NextStepDistance = 0 };
                        //检查是否暂停
                        if (usingPauseToken.IsCancellationRequested)
                        {
                            (RunResult runResult, usingPauseToken) = await WaitContinueAsync(preLine ?? line, workpiece, currentKnifeRemainTime);
                            if (!runResult.IsSuccess)
                            {
                                return runResult;
                            }
                        }
                        line = workpiece.CalculateCuttingLine();
                        float actualCutHeight = cutStep.CutHeight + _depthCompensationValue;
                        float targetEndZ = bladeContactWorkingDiscZ1 - actualCutHeight;
                        float startZ = bladeContactWorkingDiscZ1 - workpiece.WorkThickness - workpiece.TapeThickness - bladeLiftingHeight;
                        float cutLength = MathF.Abs(line.EndPoint.X - line.StartPoint.X);
                        float cutSpeed = cutStep.Speed + _feedSpeedCompCompensationValue;
                        //加上边距
                        float startX = line.StartPoint.X + cutStep.OffsetX - margin;
                        float endX = line.EndPoint.X + cutStep.OffsetX + margin;
                        //计算当前刀剩余时间
                        if (cutTime <= 0)
                        {
                            currentKnifeRemainTime = MathF.Abs(startX - endX) / cutSpeed;
                        }
                        else
                        {
                            currentKnifeRemainTime = MathF.Abs(startX - endX) / cutSteps[cutTime - 1].Speed;
                        }
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
                        foreach (float endZ in endZList)
                        {
                            stopwatch.Restart();
                            await PlcControl.tagControl.ThetaAxis.SetAbsoluteSpeedAsync(GlobalParams.ThetaDefaultSpeed);
                            var compensateY = await PlcControl.GetCompensateAsync(PlcControl.tagControl.Yaxis, line.StartPoint.Y);
                            //触发切割进度更新事件
                            CutServiceProcessChanged?.Invoke(new CutServiceProcess(actualCutHeight, cutTime <= 0 ? cutSpeed : cutSteps[cutTime - 1].Speed, compensateY, cutSteps.Count, cutTime));
                            //设置切割参数
                            await PlcControl.tagControl.cutting.SetCutParamsAsync(cutSpeed, endZ, startZ, startX, endX, compensateY, "0", _cutThetaAlignDeg + cutStep.ThetaDeg, _spindleRev, true);
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
                                new RunLogsViewModel("刀数", (cutTime + 1).ToString()),
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
                                new RunLogsViewModel("theta角度", (_cutThetaAlignDeg + cutStep.ThetaDeg).ToString()),
                                new RunLogsViewModel("主轴转速", _spindleRev.ToString()),
                                new RunLogsViewModel("震动幅度", string.Join(" ", monitorResult))
                                );

                            stopwatch.Stop();
                            if (preLine is not null)
                            {
                                pathCalculator.ReportPass(cutTime, cutLength, (float)stopwatch.Elapsed.TotalSeconds);
                            }
                        }
                        cutTime++;
                        //触发切割进度更新事件
                        CutServiceProcessChanged?.Invoke(new CutServiceProcess(actualCutHeight, cutTime <= 0 ? cutSpeed : cutSteps[cutTime - 1].Speed, default, cutSteps.Count, cutTime, cutLength, cutStep.ChannelNum, pathCalculator.EstimateRemainingTime(), true));
                        preLine = line;
                    }
                    catch (OperationCanceledException)
                    {
                        (RunResult runResult, usingPauseToken) = await WaitContinueAsync(preLine ?? line, workpiece, currentKnifeRemainTime);
                        if (!runResult.IsSuccess)
                        {
                            return runResult;
                        }
                    }
                    catch (Exception ex)
                    {
                        return RunResult.Fail($"执行切割步骤失败！{ex.Message}");
                    }
                }
            }
            finally
            {
                _isReady = true;
                _isRuning = false;
                _isContinueBeyondWorkpiece = false;
                //退出全自动切割模式
                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(default);
                var operationParameter = CurrentUtils.GetOperationParametersModel();
                if (operationParameter is not null && operationParameter.IsAutoShutOffWaterWhenCuttingCompleted)
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

        private async Task<(RunResult, CancellationToken)> WaitContinueAsync(LineSegment? line, IWorkpieces workpieces, float currentKnifeRemainTime, string? message = null)
        {
            _isRuning = false;
            CutServicePaused?.Invoke(line, message, currentKnifeRemainTime);
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
                    _cutThetaAlignDeg = await GetCutThetaAlignDegAsync();
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
    }

    public record CutStep(float CutHeight, float Speed, float NextStepDistance, float ThetaDeg, bool IsAbsolute, float ChannelStartY, float OffsetX = 0, bool IsAlternatingCuttingStroke = false, int ChannelNum = 1, float? SingleCutDeep = null);
}