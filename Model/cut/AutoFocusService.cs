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
using 精密切割系统.View.Pages.common;
using 精密切割系统.ViewModel;

namespace 精密切割系统.Model.cut
{
    public class AutoFocusService
    {
        // 初始速度（高速）
        private const float InitialSpeed = 1.0f;

        // 精细对焦速度
        private const float FineTuneSpeed = 0.05f;

        // 模糊度变化阈值
        private const double BlurThreshold = 0.5;

        // Z轴位置限制
        private const float MaxLimitZ = 19.5f;

        // Z轴位置限制
        private const float MinLimitZ = 0f;

        // 对焦起始抬起位置
        private const float FocusStartingLiftPosition = 1f;

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
                    Appsettings.FocusClearZ = result.Data;
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
                await ConfigureAxis(speed, direction);
                double lastBlurScore = 0;
                float lastPosition = 0;
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
                while (await timer.WaitForNextTickAsync(token))
                {
                    var bitmap = camera.LocalBitmap;
                    // 1. 获取当前帧和位置
                    if (bitmap == null)
                    {
                        LogMessage(eventAggregator, "获取当前帧失败！");
                        continue;
                    }

                    float currentPos = await PlcControl.tagControl.Z2axis.GetCurrentLocationAsync() ?? 0;
                    if ((direction == 0 && currentPos > MaxLimitZ) || (direction == 1 && currentPos < MinLimitZ))
                        return CommonResult<float>.Failure("超过限位！");

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

                    lastBlurScore = blurScore;
                    lastPosition = currentPos;
                }
            }
            finally
            {
                await SafeStopAxis();
            }

            return CommonResult<float>.Failure("对焦未完成");
        }

        private static async Task ConfigureAxis(float speed, int direction)
        {
            await PlcControl.tagControl.Z2axis.SetJogRelativeSpeedAsync(speed);
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