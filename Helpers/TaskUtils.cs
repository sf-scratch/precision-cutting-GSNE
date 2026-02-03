using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Threading;
using 精密切割系统.Utils;

namespace 精密切割系统.Helpers
{
    public static class TaskUtils
    {
        public static ConcurrentDictionary<string, string> CurrentWaitingFuncDict = new ConcurrentDictionary<string, string>();
        private const int DefaultCheckInterval = 300; // 默认检查间隔

        //public static async Task WaitExpectedResultAsync<T>(Func<Task<T>> func, T expectedResult, TimeSpan? checkInterval = null, CancellationToken cancellationToken = default)
        //{
        //    ArgumentNullException.ThrowIfNull(func);
        //    var tcs = new TaskCompletionSource<bool>();
        //    var interval = checkInterval ?? TimeSpan.FromMilliseconds(DefaultCheckInterval);
        //    var timer = new System.Timers.Timer()
        //    {
        //        Interval = interval.TotalMilliseconds
        //    };
        //    async void Timer_Tick(object? sender, EventArgs e)
        //    {
        //        try
        //        {
        //            cancellationToken.ThrowIfCancellationRequested();
        //            // 确保在正确的Dispatcher上执行func
        //            //var result = await dispatcher.InvokeAsync(func, DispatcherPriority.Normal);
        //            var result = await func.Invoke();
        //            if (EqualityComparer<T>.Default.Equals(result, expectedResult))
        //            {
        //                timer.Stop();
        //                tcs.TrySetResult(true);
        //            }
        //        }
        //        catch (OperationCanceledException)
        //        {
        //            timer.Stop();
        //            tcs.TrySetCanceled(cancellationToken);
        //        }
        //        catch (Exception ex)
        //        {
        //            timer.Stop();
        //            tcs.TrySetException(ex);
        //        }
        //    }
        //    timer.Elapsed += Timer_Tick;
        //    timer.Start();
        //    string uuid = Guid.NewGuid().ToString("N");
        //    try
        //    {
        //        using (cancellationToken.Register(() =>
        //        {
        //            timer.Stop();
        //            tcs.TrySetCanceled(cancellationToken);
        //        }))
        //        {
        //            CurrentWaitingFuncDict.TryAdd(uuid, func.Method.Name);
        //            await tcs.Task;
        //        }
        //    }
        //    finally
        //    {
        //        timer.Elapsed -= Timer_Tick;
        //        CurrentWaitingFuncDict.TryRemove(uuid, out _);
        //    }
        //}

        //public static async Task WaitExpectedResultAsync(Func<Task<bool>> func, TimeSpan? checkInterval = null, CancellationToken cancellationToken = default)
        //{
        //    ArgumentNullException.ThrowIfNull(func);
        //    var tcs = new TaskCompletionSource<bool>();
        //    var interval = checkInterval ?? TimeSpan.FromMilliseconds(DefaultCheckInterval);
        //    var timer = new System.Timers.Timer()
        //    {
        //        Interval = interval.TotalMilliseconds
        //    };
        //    async void Timer_Tick(object? sender, EventArgs e)
        //    {
        //        try
        //        {
        //            cancellationToken.ThrowIfCancellationRequested();
        //            if (await func.Invoke())
        //            {
        //                timer.Stop();
        //                tcs.TrySetResult(true);
        //            }
        //        }
        //        catch (OperationCanceledException)
        //        {
        //            timer.Stop();
        //            tcs.TrySetCanceled(cancellationToken);
        //        }
        //        catch (Exception ex)
        //        {
        //            timer.Stop();
        //            tcs.TrySetException(ex);
        //        }
        //    }
        //    timer.Elapsed += Timer_Tick;
        //    timer.Start();
        //    string uuid = Guid.NewGuid().ToString("N");
        //    try
        //    {
        //        using (cancellationToken.Register(() =>
        //        {
        //            timer.Stop();
        //            tcs.TrySetCanceled(cancellationToken);
        //        }))
        //        {
        //            CurrentWaitingFuncDict.TryAdd(uuid, func.Method.Name);
        //            await tcs.Task;
        //        }
        //    }
        //    finally
        //    {
        //        timer.Elapsed -= Timer_Tick;
        //        CurrentWaitingFuncDict.TryRemove(uuid, out _);
        //    }
        //}

        public static async Task WaitExpectedResultAsync<T>(Func<Task<T>> func, T expectedResult, TimeSpan? checkInterval = null, CancellationToken cancellationToken = default)
        {
            var interval = checkInterval ?? TimeSpan.FromMilliseconds(DefaultCheckInterval);
            string uuid = Guid.NewGuid().ToString("N");
            try
            {
                CurrentWaitingFuncDict.TryAdd(uuid, func.Method.Name);
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        T result = await func().ConfigureAwait(false);
                        if (EqualityComparer<T>.Default.Equals(result, expectedResult))
                        {
                            return; // 达到预期结果，完成等待
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // 如果是取消操作，直接抛出
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Tools.LogError($"检查异常（继续重试）: {ex.Message}");
                    }
                    try
                    {
                        await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        throw new OperationCanceledException(cancellationToken);
                    }
                }
            }
            finally
            {
                CurrentWaitingFuncDict.TryRemove(uuid, out _);
            }
        }

        public static async Task WaitExpectedResultAsync(Func<Task<bool>> func, TimeSpan? checkInterval = null, CancellationToken cancellationToken = default)
        {
            var interval = checkInterval ?? TimeSpan.FromMilliseconds(DefaultCheckInterval);
            string uuid = Guid.NewGuid().ToString("N");
            try
            {
                CurrentWaitingFuncDict.TryAdd(uuid, func.Method.Name);
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        if (await func())
                        {
                            return; // 条件满足，直接返回
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Tools.LogError($"检查异常（继续重试）: {ex.Message}");
                    }
                    try
                    {
                        await Task.Delay(interval, cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        throw new OperationCanceledException(cancellationToken);
                    }
                }
            }
            finally
            {
                // 清理记录
                CurrentWaitingFuncDict.TryRemove(uuid, out _);
            }
        }

        public static TimeoutToken GetTimeoutCancellationToken(
            TimeSpan timeout = default,
            CancellationToken linkedToken = default)
        {
            timeout = timeout == default ? TimeSpan.FromSeconds(1) : timeout;
            return new TimeoutToken(timeout, linkedToken);
        }
    }
}