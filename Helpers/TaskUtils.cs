using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Timers;
using NPOI.SS.Formula.Functions;

namespace 精密切割系统.Helpers
{
    public static class TaskUtils
    {
        public static async Task WaitExpectedResultAsync<T>(Func<Task<T>> func, T expectedResult, CancellationToken cancellationToken = default, TimeSpan? checkInterval = null)
        {
            ArgumentNullException.ThrowIfNull(func);
            var tcs = new TaskCompletionSource<bool>();
            var interval = checkInterval ?? TimeSpan.FromMilliseconds(500);
            var timer = new System.Timers.Timer()
            {
                Interval = interval.TotalMilliseconds
            };
            async void Timer_Tick(object? sender, EventArgs e)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // 确保在正确的Dispatcher上执行func
                    //var result = await dispatcher.InvokeAsync(func, DispatcherPriority.Normal);
                    var result = await func.Invoke();
                    if (EqualityComparer<T>.Default.Equals(result, expectedResult))
                    {
                        timer.Stop();
                        tcs.TrySetResult(true);
                    }
                }
                catch (OperationCanceledException)
                {
                    timer.Stop();
                    tcs.TrySetCanceled(cancellationToken);
                }
                catch (Exception ex)
                {
                    timer.Stop();
                    tcs.TrySetException(ex);
                }
            }
            timer.Elapsed += Timer_Tick;
            timer.Start();
            try
            {
                using (cancellationToken.Register(() =>
                {
                    timer.Stop();
                    tcs.TrySetCanceled(cancellationToken);
                }))
                {
                    await tcs.Task;
                }
            }
            finally
            {
                timer.Elapsed -= Timer_Tick;
            }
        }

        public static async Task WaitExpectedResultAsync(Func<Task<bool>> func, CancellationToken cancellationToken = default, TimeSpan? checkInterval = null)
        {
            ArgumentNullException.ThrowIfNull(func);
            var tcs = new TaskCompletionSource<bool>();
            var interval = checkInterval ?? TimeSpan.FromMilliseconds(500);
            var timer = new System.Timers.Timer()
            {
                Interval = interval.TotalMilliseconds
            };
            async void Timer_Tick(object? sender, EventArgs e)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (await func.Invoke())
                    {
                        timer.Stop();
                        tcs.TrySetResult(true);
                    }
                }
                catch (OperationCanceledException)
                {
                    timer.Stop();
                    tcs.TrySetCanceled(cancellationToken);
                }
                catch (Exception ex)
                {
                    timer.Stop();
                    tcs.TrySetException(ex);
                }
            }
            timer.Elapsed += Timer_Tick;
            timer.Start();
            try
            {
                using (cancellationToken.Register(() =>
                {
                    timer.Stop();
                    tcs.TrySetCanceled(cancellationToken);
                }))
                {
                    await tcs.Task;
                }
            }
            finally
            {
                timer.Elapsed -= Timer_Tick;
            }
        }
    }
}
