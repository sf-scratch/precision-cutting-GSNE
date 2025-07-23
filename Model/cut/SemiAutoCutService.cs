using 精密切割系统.FrmWindow.common;
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

        public event Action<CutServiceProcess>? CutServiceProcessChanged;
        public event Action<LineSegment?, string?>? CutServicePaused;
        public event Action? RemindReplaceWafer;
        private TaskCompletionSource<ServicePauseResult>? _continueTcs;
        private CancellationToken _usingPauseToken;

        /// <summary>
        /// 工件半径
        /// </summary>
        private readonly float _workpieceRadius = GlobalParams.WorkpieceRadius;

        /// <summary>
        /// 工件中心点到theta轴中心点距离
        /// <summary>
        private readonly float _centerDistance = GlobalParams.CenterDistance;

        /// <summary>
        /// theta轴中心点位置
        /// </summary>
        private readonly DataPoint<float> _thetaCenterPoint = GlobalParams.ThetaCenterPoint;

        /// <summary>
        /// 相机相对刀片中心点位置
        /// </summary>
        private readonly DataPoint<float> _cameraRelativeBladePosition = Appsettings.CameraRelativeBladePosition;

        private CutDirection _cutDirection;

        /// <summary>
        /// 切割方向
        /// </summary>
        public CutDirection CutDirection
        {
            get { return _cutDirection; }
            set { _cutDirection = value; }
        }


        private float _cutThetaAlignDeg;

        private SemiAutoCutService()
        {
            _cutDirection = CutDirection.Backward;
        }

        public async Task<RunResult> RunAsync(List<CutStep> cutStepList, float margin, int spindleRev, float bladeContactWorkingDiscZ1, float bladeLiftingHeight, CancellationToken pauseToken = default)
        {
            _usingPauseToken = pauseToken;
            _cutThetaAlignDeg = await GetCutThetaAlignDegAsync(_usingPauseToken);
            try
            {
                //打开切割水
                await PlcControl.tagControl.wholeDevice.OpenCuttingWaterAsync();
                //进入全自动切割模式
                await PlcControl.tagControl.cutting.EnterCuttingModeAsync(_usingPauseToken);
                CircularWorkpiece workpiece = new(new System.Drawing.PointF(_thetaCenterPoint.X, _thetaCenterPoint.Y), _workpieceRadius, await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync() ?? 0);
                int cutTime = 0;
                int needCutTimes = cutStepList.Count;
                while (cutTime < needCutTimes)
                {
                    LineSegment? line = null;
                    CutStep cutStep = cutStepList[cutTime];
                    try
                    {
                        //检查是否暂停
                        if (_usingPauseToken.IsCancellationRequested)
                        {
                            RunResult runResult = await WaitContinueAsync(line);
                            if (!runResult.IsSuccess)
                            {
                                return runResult;
                            }
                        }
                        //检测工件是否切完
                        if (!workpiece.CheckCutDistance(_cutDirection, cutStep.OffsetY))
                        {
                            RemindReplaceWafer?.Invoke();
                            RunResult runResult = await WaitContinueAsync(line);
                            if (!runResult.IsSuccess)
                            {
                                return runResult;
                            }
                            workpiece.Reset(await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync() ?? 0);
                        }
                        float cutY = workpiece.CalculateCutY();
                        line = AutoCutUtils.CalculateSemicircleCuttingLine(_thetaCenterPoint, _cutThetaAlignDeg + cutStep.ThetaDeg, _workpieceRadius, _centerDistance, cutY);
                        if (line == null)
                        {
                            return RunResult.Fail("获取切割线失败！");
                        }
                        //当前切割次数
                        int? curCutNum = await PlcControl.tagControl.cutting.GetCutNumAsync();
                        if (curCutNum == null)
                        {
                            return RunResult.Fail("获取当前切割次数失败！");
                        }
                        float targetEndZ = bladeContactWorkingDiscZ1 - cutStep.CutHeight;
                        float startZ = targetEndZ - bladeLiftingHeight;
                        List<float> endZList = [];
                        if (cutStep.SingleCutDeep is not null)
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
                            CutServiceProcessChanged?.Invoke(new CutServiceProcess(endZ, cutStep.Speed, needCutTimes, cutTime));
                            //加上边距
                            float startX = line.StartPoint.X - margin;
                            float endX = line.EndPoint.X + margin;
                            await PlcControl.tagControl.ThetaAxis.SetAbsoluteSpeedAsync(GlobalParams.ThetaDefaultSpeed);
                            //设置切割参数
                            await PlcControl.tagControl.cutting.SetCutParamsAsync(cutStep.Speed, endZ, startZ, startX, endX, line.StartPoint.Y, "0", _cutThetaAlignDeg + cutStep.ThetaDeg, spindleRev);
                            //开始切割信号
                            await PlcControl.tagControl.cutting.StartCutAsync();
                            //等待切割次数变化
                            await PlcControl.tagControl.cutting.WaitCutNumUdatedAsync(curCutNum.Value + 1, _usingPauseToken);
                        }
                        cutTime++;
                        //触发切割进度更新事件
                        CutServiceProcessChanged?.Invoke(new CutServiceProcess(targetEndZ, cutStep.Speed, needCutTimes, cutTime, true));
                    }
                    catch (OperationCanceledException)
                    {
                        RunResult runResult = await WaitContinueAsync(line);
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
                //退出全自动切割模式
                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(default);
            }
            return RunResult.Success();
        }

        public void Continue(CancellationToken token)
        {
            _continueTcs?.TrySetResult(ServicePauseResult.Continue(token)); // 继续执行
            _continueTcs = null;
        }

        public void Stop(ServicePauseResult pauseResult)
        {
            _continueTcs?.TrySetResult(pauseResult); // 停止切割
            _continueTcs = null;
        }

        private async Task<RunResult> WaitContinueAsync(LineSegment? line, string? message = null)
        {
            CutServicePaused?.Invoke(line, message);
            _continueTcs = new TaskCompletionSource<ServicePauseResult>();
            ServicePauseResult result = await _continueTcs.Task;
            switch (result.Type)
            {
                case ServicePauseResultType.Continue:
                    // 更新使用的暂停令牌
                    _usingPauseToken = result.Token ?? default; 
                    _cutThetaAlignDeg = await GetCutThetaAlignDegAsync(_usingPauseToken);
                    await PlcControl.tagControl.wholeDevice.OpenCuttingWaterAsync();
                    break;
                case ServicePauseResultType.BladeScrap:
                    return RunResult.Fail("刀片已报废！");
                case ServicePauseResultType.Stop: 
                    return RunResult.Fail("停止切割！");
                default: 
                    return RunResult.Fail("切割异常！");
            }
            return RunResult.Success();
        }

        private async Task<float> GetCutThetaAlignDegAsync(CancellationToken token)
        {
            return ThetaAlignService.Instance.ThetaAlignCompletedDeg ?? await PlcControl.tagControl.ThetaAxis.GetCurrentLocationAsync(token) ?? 0;
        }
    }

    public record CutStep(float CutHeight, float Speed, float OffsetY, float ThetaDeg, float? SingleCutDeep = null);
}
