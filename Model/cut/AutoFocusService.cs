using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.database.db.modle;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.plc;
using 精密切割系统.PubSubEvent;
using 精密切割系统.Utils;
using 精密切割系统.View.Pages.common;
using 精密切割系统.ViewModel;

namespace 精密切割系统.Model.cut
{
    public class AutoFocusService
    {
        // 初始速度（高速）
        private const float InitialSpeed = 0.15f;

        // 精细对焦速度
        private const float FineTuneSpeed = 0.05f;

        // 模糊度变化阈值
        private const double BlurThreshold = 0.5;

        // Z轴位置限制
        private static float MaxLimitZ = 19.5f;

        // Z轴位置限制
        private static float MinLimitZ = 0f;

        // 对焦起始抬起位置
        private const float FocusStartingLiftPosition = 0.5f;

        public static async Task<CommonResult<float>> GlobalFocusAsync(IEventAggregator? eventAggregator, CancellationToken token)
        {
            try
            {
                eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("开始相机对焦..."));
                CommonResult<float> focusClearPositionResult = await AutoCutUtils.CalculateFocusClearPosition();
                if (!focusClearPositionResult.IsSuccess)
                {
                    return CommonResult<float>.Failure(focusClearPositionResult.Message);
                }
                float focusClearPosition = focusClearPositionResult.Data;
                await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(focusClearPosition - FocusStartingLiftPosition, default, token);
                MinLimitZ = focusClearPosition - FocusStartingLiftPosition;
                MaxLimitZ = focusClearPosition + FocusStartingLiftPosition;
                int direction = 1;
                // 阶段1：快速粗调（正向扫描）
                var coarseResult = await FindOptimalFocus(
                    direction: 0,
                    speed: InitialSpeed,
                    isCoarseScan: true,
                    eventAggregator,
                    token);

                if (!coarseResult.IsSuccess)
                {
                    direction = 0;
                    coarseResult = await FindOptimalFocus(
                    direction: 1,
                    speed: InitialSpeed,
                    isCoarseScan: true,
                    eventAggregator,
                    token);
                    if (!coarseResult.IsSuccess)
                        return coarseResult;
                }

                // 阶段2：反向精调
                CommonResult<float> result = await FindOptimalFocus(
                    direction: direction,
                    speed: FineTuneSpeed,
                    isCoarseScan: false,
                    eventAggregator,
                    token);
                if (result.IsSuccess)
                {
                    var currentZ2 = await PlcControl.tagControl.Z2axis.GetCurrentLocationAsync();
                    if (currentZ2 is not null)
                    {
                        Appsettings.FocusClearZ = currentZ2.Value;
                    }
                    await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(result.Data, default, token);
                }
                return result;
            }
            finally
            {
                await SafeStopAxis();
            }
        }

        public static async Task<CommonResult<float>> GlobalZeroPointFocusAsync(IEventAggregator? eventAggregator, CancellationToken token)
        {
            try
            {
                eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("开始相机对焦..."));
                CommonResult<float> focusClearPositionResult = await AutoCutUtils.CalculateFocusClearPosition();
                if (!focusClearPositionResult.IsSuccess)
                {
                    return CommonResult<float>.Failure(focusClearPositionResult.Message);
                }
                float focusClearPosition = focusClearPositionResult.Data;
                await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(0, default, token);
                MinLimitZ = 0;
                MaxLimitZ = Appsettings.PositiveLimitPositionY ?? 3;
                int direction = 1;
                // 阶段1：快速粗调（正向扫描）
                var coarseResult = await FindOptimalFocus(
                    direction: 0,
                    speed: 0.5f,
                    isCoarseScan: true,
                    eventAggregator,
                    token);

                if (!coarseResult.IsSuccess)
                {
                    direction = 0;
                    coarseResult = await FindOptimalFocus(
                    direction: 1,
                    speed: 0.5f,
                    isCoarseScan: true,
                    eventAggregator,
                    token);
                    if (!coarseResult.IsSuccess)
                        return coarseResult;
                }

                // 阶段2：反向精调
                CommonResult<float> result = await FindOptimalFocus(
                    direction: direction,
                    speed: FineTuneSpeed,
                    isCoarseScan: false,
                    eventAggregator,
                    token);
                if (result.IsSuccess)
                {
                    var currentZ2 = await PlcControl.tagControl.Z2axis.GetCurrentLocationAsync();
                    if (currentZ2 is not null)
                    {
                        Appsettings.FocusClearZ = currentZ2.Value;
                    }
                    await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(result.Data, default, token);
                }
                return result;
            }
            finally
            {
                await SafeStopAxis();
            }
        }

        private static async Task<CommonResult<float>> FindOptimalFocus(int direction, float speed, bool isCoarseScan, IEventAggregator? eventAggregator, CancellationToken token)
        {
            CameraCommon? camera = AutoCutUtils.GetCameraCommon();
            if (camera is null)
            {
                return CommonResult<float>.Failure("相机获取失败！");
            }
            try
            {
                await ConfigureAxis(speed, direction, token);
                double lastBlurScore = 0;
                float lastPosition = 0;
                while (!token.IsCancellationRequested)
                {
                    var bitmap = camera.LocalBitmap;
                    // 1. 获取当前帧和位置
                    if (bitmap == null)
                    {
                        LogMessage(eventAggregator, "获取当前帧失败！");
                        continue;
                    }

                    float? currentPos = await PlcControl.tagControl.Z2axis.GetCurrentLocationAsync();
                    if (currentPos is null) continue;

                    // 2. 计算模糊度
                    double blurScore = VisionAnalyzer.CalculateTenengrad2(bitmap.ToMat());
                    LogMessage(eventAggregator, $"位置: {currentPos:F4}mm, 模糊度: {blurScore:F2}");

                    // 3. 判断是否找到峰值
                    if (lastBlurScore > 0 && lastBlurScore - blurScore > BlurThreshold)
                    {
                        await PlcControl.tagControl.Z2axis.StopJogAsync();
                        LogMessage(eventAggregator, $"找到最清晰位置: {lastPosition:F4}mm ({(isCoarseScan ? "粗调" : "精调")})");
                        return CommonResult<float>.Success(lastPosition);
                    }
                    else if (direction == 0 && currentPos > MaxLimitZ)
                    {
                        LogMessage(eventAggregator, $"到达对焦限定位置: {lastPosition:F4}mm ({(isCoarseScan ? "粗调" : "精调")})");
                        return CommonResult<float>.Success(MaxLimitZ);
                    }
                    else if (direction == 1 && currentPos < MinLimitZ)
                    {
                        LogMessage(eventAggregator, $"到达对焦限定位置: {lastPosition:F4}mm ({(isCoarseScan ? "粗调" : "精调")})");
                        return CommonResult<float>.Success(MinLimitZ);
                    }

                    lastBlurScore = blurScore;
                    lastPosition = currentPos.Value;
                    await Task.Delay(100);
                }
            }
            finally
            {
                await SafeStopAxis();
            }

            return CommonResult<float>.Failure("对焦未完成");
        }

        private static async Task ConfigureAxis(float speed, int direction, CancellationToken token)
        {
            await PlcControl.tagControl.Z2axis.WaitAxisReadyAsync(token);
            await PlcControl.tagControl.Z2axis.SetJogRelativeSpeedAsync(speed);
            await PlcControl.tagControl.Z2axis.SetHighSpeedAsync(1);
            await PlcControl.tagControl.Z2axis.StartJogAsync(direction);
        }

        private static async Task SafeStopAxis()
        {
            await PlcControl.tagControl.Z2axis.StopJogAsync();
            SpeedManager.IsHighSpeed = false;
        }

        private static void LogMessage(IEventAggregator? eventAggregator, string message)
        {
            eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create(message));
        }
    }
}