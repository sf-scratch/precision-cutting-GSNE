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
        private readonly int _checkMarksSharpenTimes = GlobalParams.CheckMarksSharpenTimes;

        /// <summary>
        /// 相机相对刀片中心点位置
        /// </summary>
        public static DataPoint<float> _cameraRelativeBladePosition = GlobalParams.CameraRelativeBladePosition;

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
        /// 总磨刀次数
        /// </summary>
        private int _totalSharpenTimes;

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
            _isNewestSharpen = true;
            _totalSharpenTimes = 0;
            _curSharpenDistance = 0;
            _recordSharpenY = 0;
            _curSharpenDistance = 0;
        }

        public async Task<RunResult> Run(LunguSksjDTO lunguSksj, float bladeContactWorkingDiscZ1, float bladeLiftingHeight, int spindleRev, float margin, float sharpenCalibratTheta, int sharpenTimes, CancellationToken pauseToken)
        {
            InitFromAppsettings();
            if (_thetaDegQueue.Count == 0)
            {
                InitThetaDegQueue(sharpenCalibratTheta);
                //保存磨刀参数
                Appsettings.UpdateAppSettings(Appsettings.ThetaDegQueue, _thetaDegQueue.ToList());
            }
            try
            {
                //打开切割水
                await PlcControl.tagControl.wholeDevice.OpenCuttingWaterAsync();
                //进入全自动切割模式
                await PlcControl.tagControl.cutting.EnterCuttingModeAsync(pauseToken);
                CancellationToken usingPauseToken = pauseToken;
                float abAverageThickness = lunguSksj.ABAverageThickness / 1000;
                float cutDeep = AutoCutUtils.GetSharpenDeep(lunguSksj.BladeType);
                int curSharpenTimes = 0;
                //开始磨刀，磨指定刀数
                while (curSharpenTimes < sharpenTimes)
                {
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
                                //清空记录
                                Appsettings.UpdateAppSettingsToNull(Appsettings.RecordSharpenY);
                                Appsettings.UpdateAppSettingsToNull(Appsettings.ThetaDegQueue);
                                InitThetaDegQueue(sharpenCalibratTheta);
                            }
                            //保存磨刀参数
                            Appsettings.UpdateAppSettings(Appsettings.ThetaDegQueue, _thetaDegQueue.ToList());
                            _isRotateTheta = true;
                            _isNewestSharpen = true;
                            _curSharpenDistance = 0;
                            continue;
                        }
                        //保存磨刀参数
                        Appsettings.UpdateAppSettings(Appsettings.RecordSharpenY, _recordSharpenY);
                        _recordSharpenY = AutoCutUtils.CalculateCutY(_recordSharpenY, cutSize, _cutDirection);
                        LineSegment? line = AutoCutUtils.CalculateRectangleCuttingLine(_thetaCenterPoint, _sharpenRect, _thetaDegQueue.Peek(), _recordSharpenY, margin);
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
                            _continueTcs = new TaskCompletionSource<CancellationToken?>();
                            CancellationToken? token = await _continueTcs.Task;
                            if (token == null)
                            {
                                return RunResult.Fail(RunExceptionType.Stop, "停止磨刀");
                            }
                            usingPauseToken = token.Value;
                        }
                        //var invocationCount = SharpenServiceProcessChanged?.GetInvocationList().Length ?? 0;
                        //触发磨刀进度更新事件
                        SharpenServiceProcessChanged?.Invoke(new SharpenServiceProcess(endZ, sharpenSpeed, sharpenTimes, _totalSharpenTimes));
                        //设置磨刀参数
                        await PlcControl.tagControl.cutting.SetCutParamsAsync(sharpenSpeed, endZ, startZ, line.StartPoint.X, line.EndPoint.X, line.StartPoint.Y, "0", _thetaDegQueue.Peek(), spindleRev, _cutDirection);
                        //设置停止位置
                        await PlcControl.tagControl.cutting.SetStopLocationAsync((line.StartPoint.X + line.EndPoint.X) / 2 + _cameraRelativeBladePosition.X, line.StartPoint.Y + _cameraRelativeBladePosition.Y, startZ);
                        //开始磨刀
                        await PlcControl.tagControl.cutting.StartCutAsync();
                        //等待磨刀次数变化
                        await PlcControl.tagControl.cutting.WaitCutNumUdatedAsync(curCutNum.Value, usingPauseToken);
                        _totalSharpenTimes++;
                        curSharpenTimes++;
                        //触发磨刀进度更新事件
                        SharpenServiceProcessChanged?.Invoke(new SharpenServiceProcess(endZ, sharpenSpeed, sharpenTimes, _totalSharpenTimes, true));
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
                        _continueTcs = new TaskCompletionSource<CancellationToken?>();
                        CancellationToken? token = await _continueTcs.Task;
                        // 如果token为null，表示停止磨刀
                        if (token == null)
                        {
                            return RunResult.Fail(RunExceptionType.Stop, "停止磨刀");
                        }
                        usingPauseToken = token.Value;
                    }
                    catch (Exception ex)
                    {
                        return RunResult.Fail(RunExceptionType.Other, $"执行磨刀失败！{ex.Message}");
                    }
                    _isNewestSharpen = false;
                }
                //总磨刀数置零
                _totalSharpenTimes = 0;
            }
            finally
            {
                //退出全自动切割模式
                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(pauseToken);
                //关闭切割水
                await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
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
        }

        public void Reset()
        {
            Init();
            //清空记录
            Appsettings.UpdateAppSettingsToNull(Appsettings.RecordSharpenY);
            Appsettings.UpdateAppSettingsToNull(Appsettings.ThetaDegQueue);
        }

        private void InitThetaDegQueue(float sharpenCalibratTheta)
        {
            _thetaDegQueue.Enqueue(sharpenCalibratTheta);
            _thetaDegQueue.Enqueue(sharpenCalibratTheta + 90);
        }

        private void InitFromAppsettings()
        {
            float? recordSharpenY = Appsettings.GetValue<float>(Appsettings.RecordSharpenY);
            List<float> thetaDegList = Appsettings.GetList<float>(Appsettings.ThetaDegQueue);
            if (recordSharpenY != null && thetaDegList.Count != 0)
            {
                _recordSharpenY = recordSharpenY.Value;
                _thetaDegQueue = new Queue<float>(thetaDegList);
                _isRotateTheta = false;
            }
        }

        private bool CheckSharpenDistance(DataRectangleF sharpenRect, float cutSize)
        {
            bool res = true;
            _curSharpenDistance += cutSize;
            if (_thetaDegQueue.Count == 2)
            {
                //磨刀距离达到最终位置
                if (_curSharpenDistance >= sharpenRect.Height)
                {
                    res = false;
                }
            }
            else
            {
                //磨刀距离达到最终位置
                if (_curSharpenDistance >= sharpenRect.Width)
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

        private float GetCutSpeed(float abAverageThickness, bool isNewestSharpen)
        {
            float cutSpeed;
            if (abAverageThickness <= 0.016)
            {
                cutSpeed = 10f;
            }
            else if (abAverageThickness > 0.016 && abAverageThickness <= 0.021)
            {
                cutSpeed = isNewestSharpen ? 10f : 30f;
            }
            else
            {
                cutSpeed = isNewestSharpen ? 10f : 30f;
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
