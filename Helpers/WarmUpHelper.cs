using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.ViewModel;
using static 精密切割系统.Helpers.MaterialSnackUtils;

namespace 精密切割系统.Helpers
{
    public class WarmUpHelper
    {
        private static readonly DispatcherTimer _timer;
        private static TaskCompletionSource? _warmUpTcs;
        private static CancellationTokenSource? _warmUpCts;
        private static int _remainTime = 0;
        private static int _warmUpTimeSeconds = 0;

        static WarmUpHelper()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // 每秒更新一次
            };
            _timer.Tick += Timer_Tick;
        }

        /// <summary>
        /// 暖机
        /// </summary>
        public static async Task TriggerWarmUpAsync()
        {
            if (_warmUpTcs == null)
            {
                UserDefineDataModel userDefine = CurrentUtils.GetCurrentUserDefineDataModel();
                string warmUpTimeStr = userDefine.WarmUpTime;
                if (int.TryParse(warmUpTimeStr, out int warmUpTimeSeconds))
                {
                    MaterialSnack("暖机中...", SnackType.WARNING, 0);
                    _remainTime = warmUpTimeSeconds;
                    _warmUpTimeSeconds = warmUpTimeSeconds;
                    _timer.Start();
                    _warmUpTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    _warmUpCts = new CancellationTokenSource();
                    _ = AutoCutUtils.MonitoringAlarmAsync(StopWarmUp, AlarmConfig.Instance.HasAutoRunUnexpectedAlarms, default, _warmUpCts.Token);
                    try
                    {
                        await PlcControl.tagControl.wholeDevice.OpenCuttingWaterAsync();
                        Task z1Task = PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0, default, _warmUpCts.Token);
                        Task z2Task = PlcControl.tagControl.Z2axis.StartAbsoluteAsync(0, default, _warmUpCts.Token);
                        await Task.WhenAll(z1Task, z2Task);
                        await PlcControl.tagControl.cutting.RunMotionAsync(userDefine.WarmUpStartX.ToFloat(), userDefine.WarmUpStartY.ToFloat(), _warmUpCts.Token);
                        await _warmUpTcs.Task;
                    }
                    catch (OperationCanceledException)
                    { }
                    finally
                    {
                        await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
                        _timer.Stop();
                        MaterialSnack("暖机结束！", SnackType.SUCCESS);
                    }
                }
            }
            else
            {
                StopWarmUp();
            }
        }

        public static bool IsRuning
        {
            get
            {
                return !_warmUpCts?.IsCancellationRequested ?? false;
            }
        }

        public static void StopWarmUp()
        {
            _warmUpCts?.Cancel();
            _warmUpCts = null;
            _warmUpTcs?.TrySetResult();
            _warmUpTcs = null;
        }

        private static void Timer_Tick(object? sender, EventArgs e)
        {
            if (_remainTime <= 0)
            {
                StopWarmUp();
            }
            else
            {
                TimeSpan time = TimeSpan.FromSeconds(_warmUpTimeSeconds - _remainTime);
                string formattedTime = time.ToString(@"m\:ss");
                MaterialSnack($"已暖机时间: {formattedTime}", SnackType.WARNING, 1);
            }
            _remainTime--;
        }
    }
}
