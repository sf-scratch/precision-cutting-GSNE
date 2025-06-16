using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using 精密切割系统.Driver;
using 精密切割系统.DTOs;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.HttpClients;
using 精密切割系统.Model.common;
using 精密切割系统.Model.plc;
using 精密切割系统.PubSubEvent;
using 精密切割系统.Utils;
using 精密切割系统.ViewModel;
using static 精密切割系统.Model.cut.ServicePauseResult;

namespace 精密切割系统.Model.cut
{
    public class CutService
    {
        private static readonly Lazy<CutService> _lazy = new(() => new CutService());
        public static CutService Instance
        {
            get { return _lazy.Value; }
        }

        public event Action<CutServiceProcess>? CutServiceProcessChanged;
        public event Action<LineSegment?,string?>? CutServicePaused;
        public event Action? RemindReplaceWafer;
        private TaskCompletionSource<ServicePauseResult>? _continueTcs;
        private CancellationToken _usingPauseToken;

        /// <summary>
        /// 工件半径
        /// </summary>
        public readonly float _workpieceRadius = GlobalParams.WorkpieceRadius;

        /// <summary>
        /// 工件中心点到theta轴中心点距离
        /// <summary>
        public readonly float _centerDistance = GlobalParams.CenterDistance;

        /// <summary>
        /// theta轴中心点位置
        /// </summary>
        public readonly DataPoint<float> _thetaCenterPoint = GlobalParams.ThetaCenterPoint;

        /// <summary>
        /// 正常步进距离
        /// </summary>
        private readonly float _normalStepDistance = GlobalParams.NormalStepDistance;

        /// <summary>
        /// 跳跃步进距离
        /// </summary>
        private readonly float _jumpStepDistance = GlobalParams.JumpStepDistance;

        /// <summary>
        /// 在切割几次后检测
        /// </summary>
        private readonly int _checkMarksCutTimes = GlobalParams.CheckMarksCutTimes;

        /// <summary>
        /// 相机相对刀片中心点位置
        /// </summary>
        public static DataPoint<float> _cameraRelativeBladePosition = Appsettings.CameraRelativeBladePosition;

        /// <summary>
        /// 切割方向
        /// </summary>
        private CutDirection _cutDirection = CutDirection.Backward;

        /// <summary>
        /// 是否旋转过theta轴
        /// </summary>
        private bool _isRotateTheta;

        /// <summary>
        /// theta角度队列
        /// </summary>
        private Queue<float> _thetaDegQueue;

        /// <summary>
        /// 是否是最新的一刀
        /// </summary>
        private bool _isNewestCut;

        /// <summary>
        /// 已完成的磨刀次数
        /// </summary>
        private int _finishedCutTimes;

        /// <summary>
        /// 当前Y轴磨刀距离
        /// </summary>
        private float _curCutDistance;

        /// <summary>
        /// 记录切割Y轴坐标
        /// </summary>
        private float _recordCutY;

        private CutService()
        {
            Init();
        }

        private void Init()
        {
            _isRotateTheta = true;
            _thetaDegQueue = new Queue<float>();
            _isNewestCut = true;
            _finishedCutTimes = 0;
            _recordCutY = 0;
        }

        public async Task<RunResult> Run(LunguSksjModel lunguSksj, List<float> cutSpeedList, float bladeContactWorkingDiscZ1, float bladeLiftingHeight, int spindleRev, float margin, float cutCalibratTheta, IEventAggregator? eventAggregator, CancellationToken pauseToken = default)
        {
            InitFromAppsettings();
            if (_thetaDegQueue.Count == 0)
            {
                InitThetaDegQueue(cutCalibratTheta);
                //保存切割参数
                Appsettings.CutThetaDegQueue = _thetaDegQueue.ToList();
            }
            _usingPauseToken = pauseToken;
            int needCutTimes = cutSpeedList.Count;
            int cutTime = 0;
            try
            {
                //进入全自动切割模式
                await PlcControl.tagControl.cutting.EnterCuttingModeAsync(_usingPauseToken);
                float abAverageThickness = lunguSksj.ABAverageThickness;
                float cutDeep = AutoCutUtils.GetCuttingDeep(lunguSksj.ABAverageThickness);
                int chekcTimes = 0;
                while (cutTime < needCutTimes)
                {
                    LineSegment? line = null;
                    float cutSpeed = cutSpeedList[cutTime];
                    try
                    {
                        if (_isRotateTheta)
                        {
                            //计算工件圆心坐标
                            DataPoint<float> workpieceCenterPoint = new DataPoint<float>(_thetaCenterPoint.X, _thetaCenterPoint.Y + _centerDistance);
                            // 该theta角度第一次切割，切割半圆最下边切为起始位置
                            //_recordCutY = GeometryUtils.FindBottomTangentY(_thetaCenterPoint, workpieceCenterPoint, _workpieceRadius, _thetaDegQueue.Peek() + cutCalibratTheta);
                            _recordCutY = 150;
                            Appsettings.CutDistance = GlobalParams.ThetaCenterPoint.Y + GlobalParams.WorkpieceRadius + GlobalParams.CenterDistance - _recordCutY + 2;
                            _isRotateTheta = false;
                        }
                        float cutSize = GetCutSize();
                        if (!CheckCutDistance(_workpieceRadius, cutSize))
                        {
                            _thetaDegQueue.Dequeue();
                            if (_thetaDegQueue.Count == 0)
                            {
                                RemindReplaceWafer?.Invoke();
                                ServicePauseResult result = await WaitContinueAsync(line);
                                if (result.Type == ServicePauseResult.ServicePauseResultType.Stop)
                                {
                                    return RunResult.Fail(RunExceptionType.Stop, "停止切割");
                                }
                                InitThetaDegQueue(cutCalibratTheta);
                            }
                            //保存切割参数
                            Appsettings.CutThetaDegQueue = _thetaDegQueue.ToList();
                            Appsettings.CutDistance = 0;
                            _isRotateTheta = true;
                            _isNewestCut = true;
                            _curCutDistance = 0;
                            continue;
                        }
                        _recordCutY = AutoCutUtils.CalculateCutY(_recordCutY, cutSize, _cutDirection);
                        //保存切割参数
                        Appsettings.CutY = _recordCutY;
                        line = AutoCutUtils.CalculateSemicircleCuttingLine(_thetaCenterPoint, _thetaDegQueue.Peek() + cutCalibratTheta, _workpieceRadius, _centerDistance, _recordCutY);
                        if (line == null)
                        {
                            return RunResult.Fail(RunExceptionType.Other, "获取切割线失败！");
                        }
                        //当前切割次数
                        int? curCutNum = await PlcControl.tagControl.cutting.GetCutNumAsync();
                        if (curCutNum == null)
                        {
                            return RunResult.Fail(RunExceptionType.Other, "获取当前切割次数失败！");
                        }
                        float endZ = bladeContactWorkingDiscZ1 - GlobalParams.WaferThickness - GlobalParams.FilmThickness + cutDeep;
                        float startZ = endZ - bladeLiftingHeight;
                        //检查是否暂停
                        if (_usingPauseToken.IsCancellationRequested)
                        {
                            ServicePauseResult result = await WaitContinueAsync(line);
                            if (result.Type == ServicePauseResult.ServicePauseResultType.Stop)
                            {
                                return RunResult.Fail(RunExceptionType.Stop, "停止切割");
                            }
                        }
                        //触发切割进度更新事件
                        CutServiceProcessChanged?.Invoke(new CutServiceProcess(endZ, cutSpeed, needCutTimes + _finishedCutTimes, cutTime + _finishedCutTimes));
                        //加上边距
                        var (startX, endX) = CalculateCuttingX(line, _thetaDegQueue.Peek(), margin);
                        await PlcControl.tagControl.ThetaAxis.SetAbsoluteSpeedAsync(GlobalParams.ThetaDefaultSpeed);
                        //设置切割参数
                        await PlcControl.tagControl.cutting.SetCutParamsAsync(cutSpeed, endZ, startZ, startX, endX, line.StartPoint.Y, "0", _thetaDegQueue.Peek() + cutCalibratTheta, spindleRev, _cutDirection);
                        //开始切割信号
                        await PlcControl.tagControl.cutting.StartCutAsync();
                        //等待磨刀次数变化
                        await PlcControl.tagControl.cutting.WaitCutNumUdatedAsync(curCutNum.Value, _usingPauseToken);
                        cutTime++;
                        //触发切割进度更新事件
                        CutServiceProcessChanged?.Invoke(new CutServiceProcess(endZ, cutSpeed, needCutTimes + _finishedCutTimes, cutTime + _finishedCutTimes, true));
                        //停止切割前
                        int beforeStopCutTimes = cutTime + _finishedCutTimes;
                        //判断是否开始检查刀痕
                        if (beforeStopCutTimes % _checkMarksCutTimes == 1 || beforeStopCutTimes == needCutTimes)
                        {
                            chekcTimes++;
                            // 如果是第一次检查刀痕，且需要检查基准线位置，则提示检查基准线位置
                            if (chekcTimes == 1 && (Appsettings.IsNeedCheckBaseLine ?? true))
                            {
                                ServicePauseResult result = await WaitContinueAsync(line, "请检查基准线位置！");
                                if (result.Type == ServicePauseResult.ServicePauseResultType.Stop)
                                {
                                    return RunResult.Fail(RunExceptionType.Stop, "停止切割");
                                }
                            }
                            MaterialSnackUtils.MaterialSnack("检查刀痕中...", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
                            bool isOkKnifeMarksStatus = false;
                            try
                            {
                                //退出全自动切割模式
                                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(_usingPauseToken);
                                //刀痕检查
                                ImagesAnalysisResult result = await AutoCutUtils.CheckKnifeMarksStatus(line, eventAggregator, _usingPauseToken);
                                //刀痕检查结果失败，表示未检测到刀痕
                                if (!result.IsSuccess)
                                {
                                    ServicePauseResult pauseResult = await WaitContinueAsync(line, "图像识别刀痕异常，请人工检查刀痕状态！");
                                    switch (pauseResult.Type)
                                    {
                                        case ServicePauseResultType.BladeScrap:
                                            await PdaUtils.ScrapAsync(result.AnalysisFailMats.First());
                                            return RunResult.Fail(RunExceptionType.Stop, "刀片已报废！");
                                        case ServicePauseResultType.Stop:
                                            return RunResult.Fail(RunExceptionType.Stop, "停止切割");
                                        default:
                                            continue;
                                    }
                                }
                                // 处理图像数据
                                if (result.ImageDatas.Count != 0)
                                {
                                    ImageData bladeWidthMaxImage = result.BladeWidthMaxImage;
                                    switch (chekcTimes)
                                    {
                                        case 1:
                                            PdaUtils.AddToolMarkWidth(bladeWidthMaxImage.BladeWidth);
                                            PdaUtils.AddToolMarkActualWidth(bladeWidthMaxImage.BladeWidth);
                                            PdaUtils.AddFirstToolMarkWidth(bladeWidthMaxImage.BladeWidth);
                                            PdaUtils.AddFirstToolMarkImage(bladeWidthMaxImage.Mat);
                                            //上传崩边
                                            double singleCollapseAngle = (result.CollapseWidthMaxImage.CollapseWidth - result.CollapseWidthMaxImage.BladeWidth) / 2;
                                            PdaUtils.AddSingleCollapseAngle(singleCollapseAngle);
                                            PdaUtils.AddMaximumCollapseAngle(result.CollapseWidthMaxImage.CollapseWidth);
                                            PdaUtils.AddMaximumCollapseAngleImage(result.CollapseWidthMaxImage.Mat);
                                            string bladeEdgeBreakageGrade = await GetDpbbdjAsync(lunguSksj.LunguId, (float)singleCollapseAngle);
                                            PdaUtils.AddBladeEdgeBreakageGrade(bladeEdgeBreakageGrade);
                                            break;
                                        case 2:
                                            PdaUtils.AddSecondToolMarkImage(bladeWidthMaxImage.Mat);
                                            break;
                                        default:
                                            break;
                                    }
                                    eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create(
                                        $"最大刀痕宽度：{result.BladeWidthMaxImage.BladeWidth} " +
                                        $"最大崩边宽度：{result.BladeWidthMaxImage.CollapseWidth} " +
                                        $"是否蛇形：{result.IsSnakelike}"));
                                }
                                isOkKnifeMarksStatus = true;
                            }
                            finally
                            {
                                //进入全自动切割模式
                                await PlcControl.tagControl.cutting.EnterCuttingModeAsync(_usingPauseToken);
                            }
                            if (!isOkKnifeMarksStatus)
                            {
                                return RunResult.Fail(RunExceptionType.BladeScrap, "刀痕不合格！");
                            }
                            MaterialSnackUtils.MaterialSnack("刀痕合格！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
                            MaterialSnackUtils.MaterialSnack("切割进行中...", MaterialSnackUtils.SnackType.SUCCESS, 0, eventAggregator);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        ServicePauseResult pauseResult = await WaitContinueAsync(line);
                        if (pauseResult.Type == ServicePauseResult.ServicePauseResultType.Stop)
                        {
                            return RunResult.Fail(RunExceptionType.Stop, "停止切割");
                        }
                    }
                    catch (Exception ex)
                    {
                        return RunResult.Fail(RunExceptionType.Other, $"执行切割步骤失败！{ex.Message}");
                    }
                    _isNewestCut = false;
                }
            }
            finally
            {
                //退出全自动切割模式
                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(default);
                _finishedCutTimes = cutTime;
            }
            return RunResult.Success();
        }

        //public async Task<RunResult> Run(LunguSksjDTO lunguSksj, float cutSpeed, float bladeContactWorkingDiscZ1, float bladeLiftingHeight, int needCutTimes, int spindleRev, float margin, float cutCalibratTheta, IEventAggregator? eventAggregator, CancellationToken pauseToken = default)
        //{
        //    InitFromAppsettings();
        //    if (_thetaDegQueue.Count == 0)
        //    {
        //        InitThetaDegQueue(cutCalibratTheta);
        //        //保存切割参数
        //        Appsettings.CutThetaDegQueue = _thetaDegQueue.ToList();
        //    }
        //    CancellationToken usingPauseToken = pauseToken;
        //    int currentCutTimes = 0;
        //    try
        //    {
        //        //进入全自动切割模式
        //        await PlcControl.tagControl.cutting.EnterCuttingModeAsync(usingPauseToken);
        //        float abAverageThickness = lunguSksj.ABAverageThickness;
        //        float cutDeep = AutoCutUtils.GetCuttingDeep(lunguSksj.BladeType);
        //        int cutTime = 0;
        //        while (cutTime < needCutTimes)
        //        {
        //            LineSegment? line = null;
        //            try
        //            {
        //                if (_isRotateTheta)
        //                {
        //                    //计算工件圆心坐标
        //                    DataPoint<float> workpieceCenterPoint = new DataPoint<float>(_thetaCenterPoint.X, _thetaCenterPoint.Y + _centerDistance);
        //                    // 该theta角度第一次切割，切割半圆最下边切为起始位置
        //                    //_recordCutY = GeometryUtils.FindBottomTangentY(_thetaCenterPoint, workpieceCenterPoint, _workpieceRadius, _thetaDegQueue.Peek() + cutCalibratTheta);
        //                    _recordCutY = 140;
        //                    _isRotateTheta = false;
        //                }
        //                float cutSize = GetCutSize();
        //                if (!CheckCutDistance(_workpieceRadius, cutSize))
        //                {
        //                    _thetaDegQueue.Dequeue();
        //                    if (_thetaDegQueue.Count == 0)
        //                    {
        //                        RemindReplaceWafer?.Invoke();
        //                        CancellationToken? token = await WaitContinueAsync(line);
        //                        if (token == null)
        //                        {
        //                            return RunResult.Fail(RunExceptionType.Stop, "停止切割");
        //                        }
        //                        usingPauseToken = token.Value;
        //                        InitThetaDegQueue(cutCalibratTheta);
        //                    }
        //                    //保存切割参数
        //                    Appsettings.CutThetaDegQueue = _thetaDegQueue.ToList();
        //                    Appsettings.CutDistance = 0;
        //                    _isRotateTheta = true;
        //                    _isNewestCut = true;
        //                    _curCutDistance = 0;
        //                    continue;
        //                }
        //                _recordCutY = AutoCutUtils.CalculateCutY(_recordCutY, cutSize, _cutDirection);
        //                //保存切割参数
        //                Appsettings.CutY = _recordCutY;
        //                line = AutoCutUtils.CalculateSemicircleCuttingLine(_thetaCenterPoint, _thetaDegQueue.Peek() + cutCalibratTheta, _workpieceRadius, _centerDistance, _recordCutY);
        //                if (line == null)
        //                {
        //                    return RunResult.Fail(RunExceptionType.Other, "获取切割线失败！");
        //                }
        //                //当前切割次数
        //                int? curCutNum = await PlcControl.tagControl.cutting.GetCutNumAsync();
        //                if (curCutNum == null)
        //                {
        //                    return RunResult.Fail(RunExceptionType.Other, "获取当前切割次数失败！");
        //                }
        //                float endZ = bladeContactWorkingDiscZ1 - GlobalParams.WaferThickness - GlobalParams.FilmThickness + cutDeep;
        //                float startZ = endZ - bladeLiftingHeight;
        //                //检查是否暂停
        //                if (usingPauseToken.IsCancellationRequested)
        //                {
        //                    CancellationToken? token = await WaitContinueAsync(line);
        //                    if (token == null)
        //                    {
        //                        return RunResult.Fail(RunExceptionType.Stop, "停止切割");
        //                    }
        //                    usingPauseToken = token.Value;
        //                }
        //                //触发切割进度更新事件
        //                CutServiceProcessChanged?.Invoke(new CutServiceProcess(endZ, cutSpeed, needCutTimes + _finishedCutTimes, currentCutTimes + _finishedCutTimes));
        //                //加上边距
        //                var (startX, endX) = CalculateCuttingX(line, _thetaDegQueue.Peek(), margin);
        //                await PlcControl.tagControl.ThetaAxis.SetAbsoluteSpeedAsync(GlobalParams.ThetaDefaultSpeed);
        //                //设置切割参数
        //                await PlcControl.tagControl.cutting.SetCutParamsAsync(cutSpeed, endZ, startZ, startX, endX, line.StartPoint.Y, "0", _thetaDegQueue.Peek() + cutCalibratTheta, spindleRev, _cutDirection);
        //                //开始切割信号
        //                await PlcControl.tagControl.cutting.StartCutAsync();
        //                //等待磨刀次数变化
        //                await PlcControl.tagControl.cutting.WaitCutNumUdatedAsync(curCutNum.Value, usingPauseToken);
        //                currentCutTimes++;
        //                cutTime++;
        //                //触发切割进度更新事件
        //                CutServiceProcessChanged?.Invoke(new CutServiceProcess(endZ, cutSpeed, needCutTimes + _finishedCutTimes, currentCutTimes + _finishedCutTimes, true));
        //                //停止切割前
        //                int beforeStopCutTimes = currentCutTimes + _finishedCutTimes;
        //                //判断是否开始检查刀痕
        //                if (beforeStopCutTimes % _checkMarksCutTimes == 0 || beforeStopCutTimes == needCutTimes)
        //                {
        //                    int chekcTimes = beforeStopCutTimes / _checkMarksCutTimes;
        //                    MaterialSnackUtils.MaterialSnack("检查刀痕中...", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
        //                    bool isOkKnifeMarksStatus = false;
        //                    try
        //                    {
        //                        //退出全自动切割模式
        //                        await PlcControl.tagControl.cutting.ExitCuttingModeAsync(usingPauseToken);
        //                        //刀痕检查
        //                        ImagesAnalysisResult? result = await AutoCutUtils.CheckKnifeMarksStatus(line, eventAggregator, usingPauseToken);
        //                        //刀痕检查结果为空，表示未检测到刀痕
        //                        if (result == null)
        //                        {
        //                            MaterialSnackUtils.MaterialSnack("图像识别刀痕异常，请人工检查刀痕状态！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
        //                            CancellationToken? token = await WaitContinueAsync(line);
        //                            if (token == null)
        //                            {
        //                                return RunResult.Fail(RunExceptionType.Stop, "停止切割");
        //                            }
        //                            usingPauseToken = token.Value;
        //                            continue;
        //                        }
        //                        // 处理图像数据
        //                        if (result.ImageDatas.Count != 0)
        //                        {
        //                            if (beforeStopCutTimes == needCutTimes)
        //                            {
        //                                PdaUtils.AddSingleCollapseAngle((result.CollapseWidthMaxImage.CollapseWidth - result.CollapseWidthMaxImage.BladeWidth) / 2);
        //                                PdaUtils.AddMaximumCollapseAngle(result.CollapseWidthMaxImage.CollapseWidth);
        //                                PdaUtils.AddMaximumCollapseAngleImage(result.CollapseWidthMaxImage.Mat);
        //                            }
        //                            else
        //                            {
        //                                ImageData bladeWidthMaxImage = result.BladeWidthMaxImage;
        //                                switch (chekcTimes)
        //                                {
        //                                    case 1:
        //                                        PdaUtils.AddFirstToolMarkWidth(bladeWidthMaxImage.BladeWidth);
        //                                        PdaUtils.AddFirstToolMarkImage(bladeWidthMaxImage.Mat);
        //                                        break;
        //                                    case 2:
        //                                        PdaUtils.AddToolMarkWidth(bladeWidthMaxImage.BladeWidth);
        //                                        PdaUtils.AddToolMarkActualWidth(bladeWidthMaxImage.BladeWidth);
        //                                        PdaUtils.AddSecondToolMarkImage(bladeWidthMaxImage.Mat);
        //                                        break;
        //                                    default:
        //                                        break;
        //                                }
        //                            }
        //                            eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create(
        //                                $"最大刀痕宽度：{result.BladeWidthMaxImage.BladeWidth} " +
        //                                $"最大崩边宽度：{result.BladeWidthMaxImage.CollapseWidth} " +
        //                                $"是否蛇形：{result.IsSnakelike}"));
        //                        }
        //                        isOkKnifeMarksStatus = true;
        //                    }
        //                    finally
        //                    {
        //                        //进入全自动切割模式
        //                        await PlcControl.tagControl.cutting.EnterCuttingModeAsync(usingPauseToken);
        //                    }
        //                    if (!isOkKnifeMarksStatus)
        //                    {
        //                        return RunResult.Fail(RunExceptionType.BladeScrap, "刀痕不合格！");
        //                    }
        //                    MaterialSnackUtils.MaterialSnack("刀痕合格！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
        //                    MaterialSnackUtils.MaterialSnack("切割进行中...", MaterialSnackUtils.SnackType.SUCCESS, 0, eventAggregator);
        //                }
        //            }
        //            catch (OperationCanceledException)
        //            {
        //                CancellationToken? token = await WaitContinueAsync(line);
        //                if (token == null)
        //                {
        //                    return RunResult.Fail(RunExceptionType.Stop, "停止切割");
        //                }
        //                usingPauseToken = token.Value;
        //            }
        //            catch (Exception ex)
        //            {
        //                return RunResult.Fail(RunExceptionType.Other, $"执行切割步骤失败！{ex.Message}");
        //            }
        //            _isNewestCut = false;
        //        }
        //    }
        //    finally
        //    {
        //        //退出全自动切割模式
        //        await PlcControl.tagControl.cutting.ExitCuttingModeAsync(default);
        //        _finishedCutTimes = currentCutTimes;
        //    }
        //    return RunResult.Success();
        //}

        public void Continue(CancellationToken token)
        {
            _continueTcs?.TrySetResult(ServicePauseResult.Continue(token)); // 继续执行
            _continueTcs = null;
        }

        private async Task<ServicePauseResult> WaitContinueAsync(LineSegment? line, string? message = null)
        {
            CutServicePaused?.Invoke(line, message);
            _continueTcs = new TaskCompletionSource<ServicePauseResult>();
            ServicePauseResult result = await _continueTcs.Task;
            if (result.Type == ServicePauseResultType.Continue)
            {
                _usingPauseToken = result.Token ?? default; // 更新使用的暂停令牌
            }
            return result;
        }

        public void Stop(ServicePauseResult pauseResult)
        {
            _continueTcs?.TrySetResult(pauseResult); // 停止切割
            _continueTcs = null;
            _finishedCutTimes = 0;
            _isNewestCut = true;
            Init();
        }

        private void InitThetaDegQueue(float cutCalibratTheta)
        {
            _thetaDegQueue.Enqueue(cutCalibratTheta);
            _thetaDegQueue.Enqueue(cutCalibratTheta + 90);
        }

        private void InitFromAppsettings()
        {
            float? recordCutY = Appsettings.CutY;
            List<float>? thetaDegList = Appsettings.CutThetaDegQueue;
            if (recordCutY != null && thetaDegList != null && thetaDegList.Count != 0)
            {
                _recordCutY = recordCutY.Value;
                _thetaDegQueue = new Queue<float>(thetaDegList);
                _isRotateTheta = false;
            }
            _curCutDistance = Appsettings.CutDistance ?? 0;
        }

        private bool CheckCutDistance(float workpieceRadius, float cutSize)
        {
            bool res = true;
            if (_thetaDegQueue.Count == 2)
            {
                //切割距离达到最终位置
                if (_curCutDistance + cutSize * 2 >= workpieceRadius - 5)
                {
                    res = false;
                }
            }
            else
            {
                //切割距离达到最终位置
                if (_curCutDistance + cutSize * 2 >= workpieceRadius * 2 - 5)
                {
                    res = false;
                }
            }
            _curCutDistance += cutSize;
            if (Appsettings.CutDistance is null)
            {
                Appsettings.CutDistance = 0;
            }
            Appsettings.CutDistance += cutSize;
            return res;
        }

        private float GetCutSize()
        {
            return _isNewestCut ? _jumpStepDistance : _normalStepDistance;
        }

        /// <summary>
        /// 获取切割速率
        /// </summary>
        /// <param name="hubNumber"></param>
        /// <param name="sydrcd">剩余刀刃长度</param>
        /// <returns>切割速率</returns>
        public static async Task<float> GetCutSpeed(string hubNumber, float sydrcd)
        {
            QgParamsDTO? qgParams = await HttpUtils.GetQgParamsByHub(hubNumber, sydrcd);
            if (qgParams == null)
            {
                return 40f; // 如果获取失败，返回默认切割速度
            }
            return qgParams.QgSpeed;
        }

        /// <summary>
        /// 获取刀片崩边等级
        /// </summary>
        /// <param name="hubNumber"></param>
        /// <param name="dpbbdj">单边崩角大小</param>
        /// <returns>刀片崩边等级</returns>
        public static async Task<string> GetDpbbdjAsync(string hubNumber, float dpbbdj)
        {
            QgParamsDTO? qgParams = await HttpUtils.GetQgParamsByHub(hubNumber, default, dpbbdj);
            if (qgParams == null)
            {
                return string.Empty;
            }
            return qgParams.Dpbbdj;
        }

        /// <summary>
        /// 获取刀刃寿命等级
        /// </summary>
        /// <param name="hubNumber"></param>
        /// <param name="zyhddmsl">真圆后单刀磨损量</param>
        /// <returns>刀刃寿命等级</returns>
        public static async Task<string> GetDrsmdjAsync(string hubNumber, float zyhddmsl)
        {
            QgParamsDTO? qgParams = await HttpUtils.GetQgParamsByHub(hubNumber, default, default, zyhddmsl);
            if (qgParams == null)
            {
                return string.Empty;
            }
            return qgParams.Drsmdj;
        }

        private (float startX, float endX) CalculateCuttingX(LineSegment line, float theta, float margin)
        {
            float startX = line.StartPoint.X - margin;
            float endX = line.EndPoint.X + margin;
            //90度切割时，X轴结束位置不加上边距，防止切到磨刀板
            if (theta >= 90)
            {
                //endX = line.EndPoint.X;
                endX = 140;
            }
            return (startX, endX);
        }
    }

    public struct CutServiceProcess(float cutBladeHeight, float cutSpeed, int totalCutTimes, int curCutTimes, bool isCompleted = false)
    {
        /// <summary>
        /// 切割刀片高度
        /// </summary>
        public float CutBladeHeight { get; set; } = cutBladeHeight;

        /// <summary>
        /// 切割速度
        /// </summary>
        public float CutSpeed { get; set; } = cutSpeed;

        /// <summary>
        /// 磨刀总次数
        /// </summary>
        public int TotalCutTimes { get; set; } = totalCutTimes;

        /// <summary>
        /// 当前磨刀数
        /// </summary>
        public int CurCutTimes { get; set; } = curCutTimes;

        /// <summary>
        /// 当前这刀切割是否完成
        /// </summary>
        public bool IsCompleted { get; set; } = isCompleted;
    }
}
