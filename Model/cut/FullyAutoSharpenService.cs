using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Ribbon;
using 精密切割系统.DTOs;
using 精密切割系统.Helpers;
using 精密切割系统.HttpClients;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.ViewModel;

namespace 精密切割系统.Model.cut
{
    public class FullyAutoSharpenService
    {
        private static readonly Lazy<FullyAutoSharpenService> _lazy = new(() => new FullyAutoSharpenService());

        public static FullyAutoSharpenService Instance
        {
            get { return _lazy.Value; }
        }

        public event Action<SharpenServiceProcess>? SharpenServiceProcessChanged;

        public event Action<LineSegment?>? SharpenServicePaused;

        public event Action? RemindReplaceSharpenBoard;

        private TaskCompletionSource<CancellationToken?>? _continueTcs;

        /// <summary>
        /// 磨刀板尺寸
        /// </summary>
        public static readonly DataRectangleF _sharpenRect = GlobalParams.SharpenRect;

        /// <summary>
        /// 在磨刀几次后检测
        /// </summary>
        private readonly int _checkMarksSharpenTimes = GlobalParams.CheckMarksCutTimes;

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

        private FullyAutoSharpenService()
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

        public async Task<RunResult> Run(LunguSksjModel lunguSksj, SharpenParamsModel sharpenParams, float bladeContactWorkingDiscZ1, float bladeLiftingHeight, float sharpenCalibratTheta, int sharpenTimes, float singleBladeWear, CancellationToken pauseToken)
        {
            DataPoint<float> thetaCenterPoint = Appsettings.ThetaCenterPoint;
            InitFromAppsettings();
            if (_thetaDegQueue.Count == 0)
            {
                InitThetaDegQueue(sharpenCalibratTheta);
                //保存磨刀参数
                Appsettings.SharpenThetaDegList = _thetaDegQueue.ToList();
            }
            CancellationToken usingPauseToken = pauseToken;
            int currentSharpenTimes = 0;
            try
            {
                //打开切割水
                await OutputConfig.Instance.SetCutWaterOpenAsync(true);
                float abAverageThickness = lunguSksj.ABAverageThickness / 1000;
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
                            _recordSharpenY = GeometryUtils.FindBottomTangentY(thetaCenterPoint, _sharpenRect, _thetaDegQueue.Peek());
                            _isRotateTheta = false;
                        }
                        float cutSize = GetCutSize(sharpenParams.CutSize);
                        if (!CheckSharpenDistance(_sharpenRect, cutSize))
                        {
                            _thetaDegQueue.Dequeue();
                            if (_thetaDegQueue.Count == 0)
                            {
                                RemindReplaceSharpenBoard?.Invoke();
                                CancellationToken? token = await WaitContinueAsync(line);
                                if (token == null)
                                {
                                    return RunResult.Fail("停止切割");
                                }
                                await OutputConfig.Instance.SetCutWaterOpenAsync(true);
                                usingPauseToken = token.Value;
                                InitThetaDegQueue(sharpenCalibratTheta);
                            }
                            //保存磨刀参数
                            Appsettings.SharpenThetaDegList = _thetaDegQueue.ToList();
                            Appsettings.SharpenDistance = 0;
                            _isRotateTheta = true;
                            _isNewestSharpen = true;
                            _curSharpenDistance = 0;
                            continue;
                        }
                        _recordSharpenY = AutoCutUtils.CalculateCutY(_recordSharpenY, cutSize, _cutDirection);
                        //保存磨刀参数
                        Appsettings.SharpenY = _recordSharpenY;
                        line = AutoCutUtils.CalculateRectangleCuttingLine(thetaCenterPoint, _sharpenRect, _thetaDegQueue.Peek(), _recordSharpenY, sharpenParams.OffsetX);
                        if (line == null)
                        {
                            return RunResult.Fail("获取磨刀线失败！");
                        }
                        float bladeWaer = singleBladeWear * curSharpenTimes <= 0.1f ? singleBladeWear * curSharpenTimes : 0.1f;
                        float endZ = bladeContactWorkingDiscZ1 - sharpenParams.CutHeight + bladeWaer;
                        float depthEntry = bladeContactWorkingDiscZ1 - sharpenParams.CutHeight - 0.5f;
                        float startZ = endZ - bladeLiftingHeight;
                        float sharpenSpeed = GetCutSpeed(sharpenParams.HightestCutSpeed);
                        //检查是否暂停
                        if (usingPauseToken.IsCancellationRequested)
                        {
                            CancellationToken? token = await WaitContinueAsync(line);
                            if (token == null)
                            {
                                return RunResult.Fail("停止磨刀");
                            }
                            await OutputConfig.Instance.SetCutWaterOpenAsync(true);
                            usingPauseToken = token.Value;
                        }
                        //当前磨刀次数
                        int? curCutNum = CuttingAutomation.Instance.CutCount;
                        if (curCutNum == null)
                        {
                            return RunResult.Fail("读取磨刀次数失败！");
                        }
                        //触发磨刀进度更新事件
                        SharpenServiceProcessChanged?.Invoke(new SharpenServiceProcess(endZ, sharpenSpeed, sharpenTimes + _finishedSharpenTimes, currentSharpenTimes + _finishedSharpenTimes));
                        await PlcControl.tagControl.ThetaAxis.SetAbsoluteSpeedAsync(GlobalParams.ThetaDefaultSpeed);
                        //开始磨刀
                        await PlcControl.tagControl.cutting.StartCutAsync();
                        try
                        {
                            //等待磨刀次数变化
                            await PlcControl.tagControl.cutting.WaitCutNumUdatedAsync(curCutNum.Value + 1, usingPauseToken);
                        }
                        finally
                        {
                            currentSharpenTimes++;
                            curSharpenTimes++;
                            //触发磨刀进度更新事件
                            SharpenServiceProcessChanged?.Invoke(new SharpenServiceProcess(endZ, sharpenSpeed, sharpenTimes + _finishedSharpenTimes, currentSharpenTimes + _finishedSharpenTimes, true));
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        CancellationToken? token = await WaitContinueAsync(line);
                        if (token == null)
                        {
                            return RunResult.Fail("停止切割");
                        }
                        await OutputConfig.Instance.SetCutWaterOpenAsync(true);
                        usingPauseToken = token.Value;
                    }
                    catch (Exception ex)
                    {
                        return RunResult.Fail($"执行磨刀失败！{ex.Message}");
                    }
                    _isNewestSharpen = false;
                }
            }
            finally
            {
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
            Appsettings.SharpenThetaDegList = null;
        }

        private void InitThetaDegQueue(float sharpenCalibratTheta)
        {
            _thetaDegQueue.Enqueue(sharpenCalibratTheta);
            _thetaDegQueue.Enqueue(sharpenCalibratTheta + 90);
        }

        private void InitFromAppsettings()
        {
            float? recordSharpenY = Appsettings.SharpenY;
            List<float>? thetaDegList = Appsettings.SharpenThetaDegList;
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
                if (_curSharpenDistance >= sharpenRect.Height - 1)
                {
                    res = false;
                }
            }
            else
            {
                //磨刀距离达到最终位置
                if (_curSharpenDistance >= sharpenRect.Width - 1)
                {
                    res = false;
                }
            }
            return res;
        }

        private float GetCutSize(float stepDistance)
        {
            return _isNewestSharpen ? stepDistance * 2 : stepDistance;
        }

        public float GetCutSpeed(float speed)
        {
            return _isNewestSharpen ? 10 : speed;
        }

        public static float GetSharpenDeep(float abAverageThickness)
        {
            if (10 <= abAverageThickness && abAverageThickness <= 24)
            {
                return 0.2f; // 10-24mm 切割深度 0.2mm
            }
            else
            {
                return 0.3f; // 其他情况切割深度 0.3mm
            }
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