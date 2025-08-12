using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Timers;
using NPOI.SS.Formula.Functions;
using System.Collections.Concurrent;

namespace 精密切割系统.Helpers
{
    public static class TaskUtils
    {
        public static ConcurrentDictionary<string, string> CurrentWaitingFuncDict = new ConcurrentDictionary<string, string>();
        private const int DefaultCheckInterval = 200; // 默认检查间隔为200毫秒
        private const int CancelAfterTime = 40; // 默认取消时间为40秒
        private static CancellationTokenSource? _newtestCancelAfterCts;

        public static CancellationToken NewtestCancelAfterToken
        {
            get
            {
                if (_newtestCancelAfterCts is null || _newtestCancelAfterCts.IsCancellationRequested)
                {
                    _newtestCancelAfterCts?.Cancel();
                    _newtestCancelAfterCts = new CancellationTokenSource();
                    _newtestCancelAfterCts.CancelAfter(TimeSpan.FromSeconds(CancelAfterTime));
                }
                return _newtestCancelAfterCts.Token;
            }
        }

        public static async Task WaitExpectedResultAsync<T>(Func<Task<T>> func, T expectedResult, TimeSpan? checkInterval = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(func);
            var tcs = new TaskCompletionSource<bool>();
            var interval = checkInterval ?? TimeSpan.FromMilliseconds(DefaultCheckInterval);
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
            string uuid = Guid.NewGuid().ToString("N");
            try
            {
                using (cancellationToken.Register(() =>
                {
                    timer.Stop();
                    tcs.TrySetCanceled(cancellationToken);
                }))
                {
                    CurrentWaitingFuncDict.TryAdd(uuid, func.Method.Name);
                    await tcs.Task;
                }
            }
            finally
            {
                timer.Elapsed -= Timer_Tick;
                CurrentWaitingFuncDict.TryRemove(uuid, out _);
            }
        }

        public static async Task WaitExpectedResultAsync(Func<Task<bool>> func, TimeSpan? checkInterval = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(func);
            var tcs = new TaskCompletionSource<bool>();
            var interval = checkInterval ?? TimeSpan.FromMilliseconds(DefaultCheckInterval);
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
            string uuid = Guid.NewGuid().ToString("N");
            try
            {
                using (cancellationToken.Register(() =>
                {
                    timer.Stop();
                    tcs.TrySetCanceled(cancellationToken);
                }))
                {
                    CurrentWaitingFuncDict.TryAdd(uuid, func.Method.Name);
                    await tcs.Task;
                }
            }
            finally
            {
                timer.Elapsed -= Timer_Tick;
                CurrentWaitingFuncDict.TryRemove(uuid, out _);
            }
        }

        //public static (CancellationToken Token, IDisposable Cts) GetTimeoutCancellationToken(TimeSpan timeout = default, CancellationToken linkedToken = default)
        //{
        //    timeout = timeout == default ? TimeSpan.FromSeconds(1) : timeout;

        //    // 如果不需要链接Token
        //    if (linkedToken == default || !linkedToken.CanBeCanceled)
        //    {
        //        var cts = new CancellationTokenSource(timeout);
        //        return (cts.Token, cts);
        //    }

        //    // 需要链接外部Token的情况
        //    var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
        //        linkedToken,
        //        new CancellationTokenSource(timeout).Token
        //    );
        //    return (linkedCts.Token, linkedCts);
        //}
        public static TimeoutToken GetTimeoutCancellationToken(
            TimeSpan timeout = default,
            CancellationToken linkedToken = default)
        {
            timeout = timeout == default ? TimeSpan.FromSeconds(1) : timeout;
            return new TimeoutToken(timeout, linkedToken);
        }
    }
}
