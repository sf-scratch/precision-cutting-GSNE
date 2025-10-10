using System.Diagnostics;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;
using 精密切割系统.Model.cut.Workpieces;
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

        public event Action<LineSegment?, string?>? CutServicePaused;

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
            set { _isReady = value; }
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

        private float _cutThetaAlignDeg;

        private SemiAutoCutService()
        {
            _alignService = ThetaAlignService.Instance;
            _cutDirection = CutDirection.Backward;
            _depthCompensationValue = 0;
            _feedSpeedCompCompensationValue = 0;
            _isReady = true;
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

        public async Task<RunResult> RunAsync(List<CutStep> cutStepList, IWorkpieces workpiece, float margin, int spindleRev, float bladeContactWorkingDiscZ1, float bladeLiftingHeight, CancellationToken pauseToken)
        {
            CancellationToken usingPauseToken = pauseToken;
            _cutThetaAlignDeg = await GetCutThetaAlignDegAsync();
            Stopwatch stopwatch = new();
            try
            {
                PathCalculator pathCalculator = new(cutStepList.Select(p => p.Speed).ToList());
                IsReady = false;
                //打开切割水
                await PlcControl.tagControl.wholeDevice.OpenCuttingWaterAsync();
                //进入全自动切割模式
                await PlcControl.tagControl.cutting.EnterCuttingModeAsync(usingPauseToken);
                bool isExchangeX = false;
                LineSegment? preLine = null;
                int cutTime = 0;
                while (cutTime < cutStepList.Count)
                {
                    LineSegment? line = null;
                    CutStep cutStep = cutStepList[cutTime];
                    try
                    {
                        //检查是否暂停
                        if (usingPauseToken.IsCancellationRequested)
                        {
                            (RunResult runResult, usingPauseToken) = await WaitContinueAsync(preLine ?? line, workpiece);
                            if (!runResult.IsSuccess)
                            {
                                return runResult;
                            }
                        }
                        float cutY = workpiece.CalculateCutY();
                        line = workpiece.CalculateCuttingLine();
                        float targetEndZ = bladeContactWorkingDiscZ1 - cutStep.CutHeight - _depthCompensationValue;
                        float startZ = bladeContactWorkingDiscZ1 - workpiece.WorkThickness - workpiece.TapeThickness - bladeLiftingHeight;
                        float cutLength = MathF.Abs(line.EndPoint.X - line.StartPoint.X);
                        float cutSpeed = cutStep.Speed + _feedSpeedCompCompensationValue;
                        //加上边距
                        float startX = line.StartPoint.X - cutStep.OffsetX - margin;
                        float endX = line.EndPoint.X + cutStep.OffsetX + margin;
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
                            //触发切割进度更新事件
                            CutServiceProcessChanged?.Invoke(new CutServiceProcess(endZ, cutSpeed, cutStepList.Count, cutTime));
                            stopwatch.Restart();
                            await PlcControl.tagControl.ThetaAxis.SetAbsoluteSpeedAsync(GlobalParams.ThetaDefaultSpeed);
                            //设置切割参数
                            await PlcControl.tagControl.cutting.SetCutParamsAsync(cutSpeed, endZ, startZ, startX, endX, line.StartPoint.Y, "0", _cutThetaAlignDeg + cutStep.ThetaDeg, spindleRev, true);
                            //当前切割次数
                            int? curCutNum = await PlcControl.tagControl.cutting.GetCutNumAsync();
                            if (curCutNum == null)
                            {
                                return RunResult.Fail("获取当前切割次数失败！");
                            }
                            //开始切割信号
                            await PlcControl.tagControl.cutting.StartCutAsync();
                            //等待切割次数变化
                            await PlcControl.tagControl.cutting.WaitCutNumUdatedAsync(curCutNum.Value + 1, usingPauseToken);
                            stopwatch.Stop();
                            if (preLine is not null)
                            {
                                pathCalculator.ReportPass(cutTime, cutLength, (float)stopwatch.Elapsed.TotalSeconds);
                            }
                        }
                        cutTime++;
                        //触发切割进度更新事件
                        CutServiceProcessChanged?.Invoke(new CutServiceProcess(targetEndZ, cutSpeed, cutStepList.Count, cutTime, cutLength, cutStep.ChannelNum, pathCalculator.EstimateRemainingTime(), true));
                        //检测工件是否切完
                        if (!workpiece.CheckCutDistance(_cutDirection, cutStep.OffsetY))
                        {
                            RemindReplaceWafer?.Invoke();
                            (RunResult runResult, usingPauseToken) = await WaitContinueAsync(preLine ?? line, workpiece, "检测到工件当前面已切完");
                            if (!runResult.IsSuccess)
                            {
                                return runResult;
                            }
                            workpiece.Reset(await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync() ?? 0);
                        }
                        preLine = line;
                    }
                    catch (OperationCanceledException)
                    {
                        (RunResult runResult, usingPauseToken) = await WaitContinueAsync(preLine ?? line, workpiece);
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
                IsReady = true;
                //退出全自动切割模式
                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(default);
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

        private async Task<(RunResult, CancellationToken)> WaitContinueAsync(LineSegment? line, IWorkpieces workpieces, string? message = null)
        {
            CutServicePaused?.Invoke(line, message);
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

    public record CutStep(float CutHeight, float Speed, float OffsetY, float ThetaDeg, float OffsetX = 0, bool IsAlternatingCuttingStroke = false, int ChannelNum = 1, float? SingleCutDeep = null);
}