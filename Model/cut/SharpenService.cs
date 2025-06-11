using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Ribbon;
using 精密切割系统.DTOs;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.HttpClients;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.ViewModel;

namespace 精密切割系统.Model.cut
{
    public class SharpenService
    {
        private static Lazy<SharpenService> _lazy = new(() => new SharpenService());
        public static SharpenService Instance
        {
            get { return _lazy.Value; }
        }

        public event Action<SharpenServiceProcess>? SharpenServiceProcessChanged;
        public event Action<LineSegment?>? SharpenServicePaused;
        public event Action? RemindReplaceSharpenBoard; 
        private TaskCompletionSource<CancellationToken?>? _continueTcs;

        /// <summary>
        /// theta轴中心点位置
        /// </summary>
        public readonly static DataPoint<float> _thetaCenterPoint = GlobalParams.ThetaCenterPoint;

        /// <summary>
        /// 磨刀板尺寸
        /// </summary>
        public readonly static DataRectangleF _sharpenRect = GlobalParams.SharpenRect;

        /// <summary>
        /// 正常步进距离
        /// </summary>
        private readonly float _normalStepDistance = GlobalParams.NormalStepDistance;

        /// <summary>
        /// 跳跃步进距离
        /// </summary>
        private readonly float _jumpStepDistance = GlobalParams.JumpStepDistance;

        /// <summary>
        /// 在磨刀几次后检测
        /// </summary>
        private readonly int _checkMarksSharpenTimes = GlobalParams.CheckMarksCutTimes;

        /// <summary>
        /// 相机相对刀片中心点位置
        /// </summary>
        public static DataPoint<float> _cameraRelativeBladePosition = Appsettings.CameraRelativeBladePosition;

        /// <summary>
        /// 单刀磨损量
        /// </summary>
        private readonly float _singleBladeWear = GlobalParams.SingleBladeWear;

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
        private bool _isNewestSharpen;

        /// <summary>
        /// 已完成的磨刀次数
        /// </summary>
        private int _finishedSharpenTimes;

        /// <summary>
        /// 当前Y轴磨刀距离
        /// </summary>
        private float _curSharpenDistance;

        /// <summary>
        /// 磨刀记录Y轴位置
        /// </summary>
        private float _recordSharpenY;

        private SharpenService()
        {
            Init();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private void Init()
        {
            _isRotateTheta = true;
            _thetaDegQueue = new Queue<float>();
            _recordSharpenY = 0;
        }

        public async Task<RunResult> Run(LunguSksjDTO lunguSksj, float bladeContactWorkingDiscZ1, float bladeLiftingHeight, int spindleRev, float margin, float sharpenCalibratTheta, int sharpenTimes, CancellationToken pauseToken)
        {
            InitFromAppsettings();
            if (_thetaDegQueue.Count == 0)
            {
                InitThetaDegQueue(sharpenCalibratTheta);
                //保存磨刀参数
                Appsettings.SharpenThetaDegQueue = _thetaDegQueue.ToList();
            }
            CancellationToken usingPauseToken = pauseToken;
            int currentSharpenTimes = 0;
            try
            {
                //进入全自动切割模式
                await PlcControl.tagControl.cutting.EnterCuttingModeAsync(usingPauseToken);
                float abAverageThickness = lunguSksj.ABAverageThickness / 1000;
                float cutDeep = AutoCutUtils.GetSharpenDeep(lunguSksj.ABAverageThickness);
                int curSharpenTimes = 0;
                //开始磨刀，磨指定刀数
                while (curSharpenTimes < sharpenTimes)
                {
                    LineSegment? line = null;
                    try
                    {
                        if (_isRotateTheta)
                        {
                            // 该theta角度第一次切割，切割矩形最下边切为起始位置
                            _recordSharpenY = GeometryUtils.FindBottomTangentY(_thetaCenterPoint, _sharpenRect, _thetaDegQueue.Peek());
                            _isRotateTheta = false;
                        }
                        float cutSize = GetCutSize();
                        if (!CheckSharpenDistance(_sharpenRect, cutSize))
                        {
                            _thetaDegQueue.Dequeue();
                            if (_thetaDegQueue.Count == 0)
                            {
                                RemindReplaceSharpenBoard?.Invoke();
                                CancellationToken? token = await WaitContinueAsync(line);
                                if (token == null)
                                {
                                    return RunResult.Fail(RunExceptionType.Stop, "停止切割");
                                }
                                usingPauseToken = token.Value;
                                InitThetaDegQueue(sharpenCalibratTheta);
                            }
                            //保存磨刀参数
                            Appsettings.SharpenThetaDegQueue = _thetaDegQueue.ToList();
                            Appsettings.SharpenDistance = 0;
                            _isRotateTheta = true;
                            _isNewestSharpen = true;
                            _curSharpenDistance = 0;
                            continue;
                        }
                        _recordSharpenY = AutoCutUtils.CalculateCutY(_recordSharpenY, cutSize, _cutDirection);
                        //保存磨刀参数
                        Appsettings.SharpenY = _recordSharpenY;
                        line = AutoCutUtils.CalculateRectangleCuttingLine(_thetaCenterPoint, _sharpenRect, _thetaDegQueue.Peek(), _recordSharpenY, margin);
                        if (line == null)
                        {
                            return RunResult.Fail(RunExceptionType.Other, "获取磨刀线失败！");
                        }
                        //当前磨刀次数
                        int? curCutNum = await PlcControl.tagControl.cutting.GetCutNumAsync();
                        if (curCutNum == null)
                        {
                            return RunResult.Fail(RunExceptionType.None, "读取磨刀次数失败！");
                        }
                        float endZ = bladeContactWorkingDiscZ1 - GlobalParams.SharpeningBoardThickness - GlobalParams.FilmThickness + cutDeep;
                        float startZ = endZ - bladeLiftingHeight;
                        float sharpenSpeed = GetCutSpeed(abAverageThickness, _isNewestSharpen);
                        //检查是否暂停
                        if (usingPauseToken.IsCancellationRequested)
                        {
                            CancellationToken? token = await WaitContinueAsync(line);
                            if (token == null)
                            {
                                return RunResult.Fail(RunExceptionType.Stop, "停止切割");
                            }
                            usingPauseToken = token.Value;
                        }
                        //触发磨刀进度更新事件
                        SharpenServiceProcessChanged?.Invoke(new SharpenServiceProcess(endZ, sharpenSpeed, sharpenTimes + _finishedSharpenTimes, currentSharpenTimes + _finishedSharpenTimes));
                        await PlcControl.tagControl.ThetaAxis.SetAbsoluteSpeedAsync(GlobalParams.ThetaDefaultSpeed);
                        //设置磨刀参数
                        await PlcControl.tagControl.cutting.SetCutParamsAsync(sharpenSpeed, endZ, startZ, line.StartPoint.X, line.EndPoint.X, line.StartPoint.Y, "0", _thetaDegQueue.Peek(), spindleRev, _cutDirection);
                        //开始磨刀
                        await PlcControl.tagControl.cutting.StartCutAsync();
                        try
                        {
                            //等待磨刀次数变化
                            await PlcControl.tagControl.cutting.WaitCutNumUdatedAsync(curCutNum.Value, usingPauseToken);
                        }
                        finally
                        {
                            currentSharpenTimes++;
                            curSharpenTimes++;
                            //触发磨刀进度更新事件
                            SharpenServiceProcessChanged?.Invoke(new SharpenServiceProcess(endZ, sharpenSpeed, sharpenTimes + _finishedSharpenTimes, currentSharpenTimes + _finishedSharpenTimes, true));
                        }
                        //判断是否开始检查刀痕
                        //if (_totalSharpenTimes % GlobalParams.CheckMarksSharpenTimes == 0)
                        //{
                        //    await HttpUtils.SendSharpenDataToMES();
                        //    if (!AutoCutUtils.CheckKnifeMarksStatus())
                        //    {
                        //        Tools.LogDebug("刀痕不合格！");
                        //        return RunResult.Fail(RunExceptionType.BladeScrap, "刀痕不合格！");
                        //    }
                        //}
                    }
                    catch (OperationCanceledException)
                    {
                        CancellationToken? token = await WaitContinueAsync(line);
                        if (token == null)
                        {
                            return RunResult.Fail(RunExceptionType.Stop, "停止切割");
                        }
                        usingPauseToken = token.Value;
                    }
                    catch (Exception ex)
                    {
                        return RunResult.Fail(RunExceptionType.Other, $"执行磨刀失败！{ex.Message}");
                    }
                    _isNewestSharpen = false;
                }
            }
            finally
            {
                //退出全自动切割模式
                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(default);
                //记录本次磨刀完成的刀数
                _finishedSharpenTimes += currentSharpenTimes;
            }
            return RunResult.Success();
        }

        public void Continue(CancellationToken token)
        {
            _continueTcs?.TrySetResult(token); // 继续执行
            _continueTcs = null;
        }

        private async Task<CancellationToken?> WaitContinueAsync(LineSegment? line)
        {
            SharpenServicePaused?.Invoke(line);
            _continueTcs = new TaskCompletionSource<CancellationToken?>();
            return await _continueTcs.Task;
        }

        public void Stop()
        {
            _continueTcs?.TrySetResult(null); // 停止切割
            _continueTcs = null;
            _finishedSharpenTimes = 0;
            _isNewestSharpen = true;
            Init();
        }

        public void Reset()
        {
            Init();
            //清空记录
            Appsettings.SharpenY = null;
            Appsettings.SharpenThetaDegQueue = null;
        }

        private void InitThetaDegQueue(float sharpenCalibratTheta)
        {
            _thetaDegQueue.Enqueue(sharpenCalibratTheta);
            _thetaDegQueue.Enqueue(sharpenCalibratTheta + 90);
        }

        private void InitFromAppsettings()
        {
            float? recordSharpenY = Appsettings.SharpenY;
            List<float>? thetaDegList = Appsettings.SharpenThetaDegQueue;
            if (recordSharpenY != null && thetaDegList != null && thetaDegList.Count != 0)
            {
                _recordSharpenY = recordSharpenY.Value;
                _thetaDegQueue = new Queue<float>(thetaDegList);
                _isRotateTheta = false;
            }
            _curSharpenDistance = Appsettings.SharpenDistance ?? 0;
        }

        private bool CheckSharpenDistance(DataRectangleF sharpenRect, float cutSize)
        {
            bool res = true;
            _curSharpenDistance += cutSize;
            if (Appsettings.SharpenDistance is null) Appsettings.SharpenDistance = 0;
            Appsettings.SharpenDistance += cutSize;
            if (_thetaDegQueue.Count == 2)
            {
                //磨刀距离达到最终位置
                if (_curSharpenDistance >= sharpenRect.Height - 5)
                {
                    res = false;
                }
            }
            else
            {
                //磨刀距离达到最终位置
                if (_curSharpenDistance >= sharpenRect.Width - 5)
                {
                    res = false;
                }
            }
            return res;
        }

        private float GetCutSize()
        {
            return _isNewestSharpen ? _jumpStepDistance : _normalStepDistance;
        }

        public static float GetCutSpeed(float abAverageThickness, bool isNewestSharpen)
        {
            float cutSpeed;
            if (abAverageThickness <= 0.016f)
            {
                cutSpeed = 10f;
            }
            else if (abAverageThickness > 0.016 && abAverageThickness <= 0.021)
            {
                cutSpeed = isNewestSharpen ? 10f : 30f;
            }
            else
            {
                cutSpeed = isNewestSharpen ? 10f : 60f;
            }
            return cutSpeed;
        }
    }

    public struct SharpenServiceProcess(float sharpenBladeHeight, float sharpenSpeed, int totalSharpenTimes, int curSharpenTimes, bool isCompleted = false)
    {
        /// <summary>
        /// 磨刀刀片高度
        /// </summary>
        public float SharpenBladeHeight { get; set; } = sharpenBladeHeight;

        /// <summary>
        /// 磨刀速度
        /// </summary>
        public float SharpenSpeed { get; set; } = sharpenSpeed;

        /// <summary>
        /// 磨刀总次数
        /// </summary>
        public int TotalSharpenTimes { get; set; } = totalSharpenTimes;

        /// <summary>
        /// 当前磨刀数
        /// </summary>
        public int CurSharpenTimes { get; set; } = curSharpenTimes;

        /// <summary>
        /// 当前这刀磨刀是否完成
        /// </summary>
        public bool IsCompleted { get; set; } = isCompleted;
    }
}
