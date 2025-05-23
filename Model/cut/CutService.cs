using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.DTOs;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.HttpClients;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.ViewModel;

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
        public event Action<LineSegment?>? CutServicePaused;
        private TaskCompletionSource<CancellationToken?>? _continueTcs;

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
        private readonly int _checkMarksCutTimes = GlobalParams.CheckMarksSharpenTimes;

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
        /// 总磨刀次数
        /// </summary>
        private int _totalCutTimes;

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
            _totalCutTimes = 0;
            _curCutDistance = 0;
            _recordCutY = 0;
        }

        public async Task<RunResult> Run(LunguSksjDTO lunguSksj, float bladeContactWorkingDiscZ1, float bladeLiftingHeight, int needCutTimes, int spindleRev, float margin, float cutCalibratTheta, IEventAggregator? eventAggregator, CancellationToken pauseToken = default)
        {
            if (_thetaDegQueue.Count == 0)
            {
                InitThetaDegQueue();
            }
            CancellationToken usingPauseToken = pauseToken;
            try
            {
                //进入全自动切割模式
                await PlcControl.tagControl.cutting.EnterCuttingModeAsync(usingPauseToken);
                float abAverageThickness = lunguSksj.ABAverageThickness;
                float cutDeep = AutoCutUtils.GetCuttingZ(lunguSksj.BladeType);
                int cutTime = 0;
                while (cutTime < needCutTimes)
                {
                    LineSegment? line = null;
                    try
                    {
                        if (_isRotateTheta)
                        {
                            //计算工件圆心坐标
                            DataPoint<float> workpieceCenterPoint = new DataPoint<float>(_thetaCenterPoint.X, _thetaCenterPoint.Y + _centerDistance);
                            // 该theta角度第一次切割，切割半圆最下边切为起始位置
                            _recordCutY = GeometryUtils.FindBottomTangentY(_thetaCenterPoint, workpieceCenterPoint, _workpieceRadius, _thetaDegQueue.Peek() + cutCalibratTheta) - 45.45f;
                            _isRotateTheta = false;
                        }
                        float cutSize = GetCutSize();
                        if (!CheckCutDistance(_workpieceRadius, cutSize))
                        {
                            _thetaDegQueue.Dequeue();
                            if (_thetaDegQueue.Count == 0)
                            {
                                ChangeWorkpiece();
                                InitThetaDegQueue();
                            }
                            _isRotateTheta = true;
                            _isNewestCut = true;
                            _curCutDistance = 0;
                            continue;
                        }
                        _recordCutY = AutoCutUtils.CalculateCutY(_recordCutY, cutSize, _cutDirection);
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
                        float cutSpeed = GetCutSpeed(abAverageThickness);
                        //检查是否暂停
                        if (usingPauseToken.IsCancellationRequested)
                        {
                            _continueTcs = new TaskCompletionSource<CancellationToken?>();
                            CancellationToken? token = await _continueTcs.Task;
                            //如果token为null，表示停止切割
                            if (token == null)
                            {
                                return RunResult.Fail(RunExceptionType.Stop, "停止切割");
                            }
                            usingPauseToken = token.Value;
                        }
                        //触发切割进度更新事件
                        CutServiceProcessChanged?.Invoke(new CutServiceProcess(endZ, cutSpeed, needCutTimes, _totalCutTimes));
                        //加上边距
                        var (startX, endX) = CalculateCuttingX(line, _thetaDegQueue.Peek(), margin);
                        //设置切割参数
                        await PlcControl.tagControl.cutting.SetCutParamsAsync(cutSpeed, endZ, startZ, startX, endX, line.StartPoint.Y, "0", _thetaDegQueue.Peek() + cutCalibratTheta, spindleRev, _cutDirection);
                        //开始切割信号
                        await PlcControl.tagControl.cutting.StartCutAsync();
                        //等待磨刀次数变化
                        await PlcControl.tagControl.cutting.WaitCutNumUdatedAsync(curCutNum.Value, usingPauseToken);
                        _totalCutTimes++;
                        cutTime++;
                        //触发切割进度更新事件
                        CutServiceProcessChanged?.Invoke(new CutServiceProcess(endZ, cutSpeed, needCutTimes, _totalCutTimes, true));
                        //判断是否开始检查刀痕
                        if (_totalCutTimes % GlobalParams.CheckMarksSharpenTimes == 0)
                        {
                            MaterialSnackUtils.MaterialSnack("检查刀痕中...", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
                            bool isOkKnifeMarksStatus;
                            try
                            {
                                //退出全自动切割模式
                                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(usingPauseToken);
                                //刀痕检查
                                isOkKnifeMarksStatus = await AutoCutUtils.CheckKnifeMarksStatus(line, eventAggregator, usingPauseToken);
                            }
                            finally
                            {
                                //进入全自动切割模式
                                await PlcControl.tagControl.cutting.EnterCuttingModeAsync(usingPauseToken);
                            }
                            if (!isOkKnifeMarksStatus)
                            {
                                return RunResult.Fail(RunExceptionType.BladeScrap, "刀痕不合格！");
                            }
                            MaterialSnackUtils.MaterialSnack("刀痕合格！", MaterialSnackUtils.SnackType.WARNING, 0, eventAggregator);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        //退出全自动切割模式
                        await PlcControl.tagControl.cutting.ExitCuttingModeAsync(default);
                        CutServicePaused?.Invoke(line);
                        _continueTcs = new TaskCompletionSource<CancellationToken?>();
                        CancellationToken? token = await _continueTcs.Task;
                        //进入全自动切割模式
                        await PlcControl.tagControl.cutting.EnterCuttingModeAsync(default);
                        if (token == null)
                        {
                            return RunResult.Fail(RunExceptionType.Stop, "停止切割");
                        }
                        usingPauseToken = token.Value;
                    }
                    catch (Exception ex)
                    {
                        return RunResult.Fail(RunExceptionType.Other, $"执行切割步骤失败！{ex.Message}");
                    }
                    _isNewestCut = false;
                }
                _totalCutTimes = 0;
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
            _continueTcs?.TrySetResult(token); // 继续执行
            _continueTcs = null;
        }

        public void Stop()
        {
            _continueTcs?.TrySetResult(null); // 停止切割
            _continueTcs = null;
            Init();
        }

        private void InitThetaDegQueue()
        {
            _thetaDegQueue.Clear();
            _thetaDegQueue.Enqueue(0);
            _thetaDegQueue.Enqueue(90);
        }

        private bool CheckCutDistance(float workpieceRadius, float cutSize)
        {
            bool res = true;
            if (_thetaDegQueue.Count == 0)
            {
                //切割距离达到最终位置
                if (_curCutDistance + cutSize * 2 >= workpieceRadius)
                {
                    res = false;
                }
            }
            else
            {
                //切割距离达到最终位置
                if (_curCutDistance + cutSize * 2 >= workpieceRadius * 2)
                {
                    res = false;
                }
            }
            _curCutDistance += cutSize;
            return res;
        }

        private float GetCutSize()
        {
            return _isNewestCut ? _jumpStepDistance : _normalStepDistance;
        }

        private float GetCutSpeed(float abAverageThickness)
        {
            return 100f;
            float cutSpeed;
            if (abAverageThickness <= 16)
            {
                cutSpeed = 40f;
            }
            else
            {
                cutSpeed = FindCutComparisonTable();
            }
            return cutSpeed;
        }

        private float FindCutComparisonTable()
        {
            return 20f;
        }

        private void ChangeWorkpiece()
        {
            //换磨刀板
            Tools.LogDebug("提示换工件");
        }

        private (float startX, float endX) CalculateCuttingX(LineSegment line, float theta, float margin)
        {
            float startX = line.StartPoint.X - margin;
            float endX = line.EndPoint.X + margin;
            //90度切割时，X轴结束位置不加上边距，防止切到磨刀板
            if (theta == 90)
            {
                endX = line.EndPoint.X;
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
