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
        #region 拓展方法

        /// <summary>
        /// 执行一个异步任务，并设置超时时间。
        /// </summary>
        /// <typeparam name="TResult">任务返回值类型</typeparam>
        /// <param name="task">要执行的异步任务</param>
        /// <param name="timeout">超时时间（默认不超时）</param>
        /// <param name="timeoutException">超时时抛出的异常（默认TimeoutException）</param>
        /// <param name="cancellationToken">可选的取消令牌</param>
        /// <returns>任务的结果（如果未超时）</returns>
        /// <exception cref="TimeoutException">任务超时</exception>
        public static async Task<TResult> WithTimeout<TResult>(this Task<TResult> task, TimeSpan timeout, Exception? timeoutException = null, CancellationToken cancellationToken = default)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            // 创建超时任务
            Task timeoutTask = Task.Delay(timeout, cancellationToken);

            // 等待任意一个任务完成
            Task completedTask = await Task.WhenAny(task, timeoutTask);

            // 处理超时
            if (completedTask == timeoutTask)
            {
                throw timeoutException ?? new TimeoutException($"操作超时（{timeout.TotalSeconds}秒）");
            }

            // 返回原始任务的结果（确保异常正确传播）
            return await task;
        }
        #endregion


        /// <summary>
        /// 异步等待直到函数返回预期结果（WPF版本，保持同步上下文）
        /// </summary>
        /// <typeparam name="T">结果类型</typeparam>
        /// <param name="func">要执行的函数</param>
        /// <param name="expectedResult">预期结果</param>
        /// <param name="checkInterval">检查间隔（默认100ms）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns></returns>
        public static async Task WaitExpectedResultAsync<T>(Func<T> func, T expectedResult, CancellationToken cancellationToken = default, TimeSpan? checkInterval = null)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));
            var tcs = new TaskCompletionSource<bool>();
            var interval = checkInterval ?? TimeSpan.FromMilliseconds(500);
            var timer = new System.Timers.Timer()
            {
                Interval = interval.TotalMilliseconds
            };
            void Timer_Tick(object? sender, EventArgs e)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // 确保在正确的Dispatcher上执行func
                    //var result = await dispatcher.InvokeAsync(func, DispatcherPriority.Normal);
                    var result = func.Invoke();
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

        public static async Task WaitExpectedResultAsync<T>(Func<Task<T>> func, T expectedResult, CancellationToken cancellationToken = default, TimeSpan? checkInterval = null)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));
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
            if (func == null)
                throw new ArgumentNullException(nameof(func));
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

        public static async Task WaitExpectedResultAsync<T>(Func<T,Task<bool>> func, T param, CancellationToken cancellationToken = default, TimeSpan? checkInterval = null)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));
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
                    if (await func.Invoke(param))
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
        /// <summary>
        /// 异步等待直到函数返回结果变化（WPF版本，保持同步上下文）
        /// </summary>
        /// <typeparam name="T">结果类型</typeparam>
        /// <param name="func">要执行的函数</param>
        /// <param name="checkInterval">检查间隔（默认100ms）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns></returns>
        public static async Task WaitResultUpdateAsync<T>(Func<T> func, T expectedResult, CancellationToken cancellationToken = default, TimeSpan? checkInterval = null)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));
            var tcs = new TaskCompletionSource<bool>();
            var interval = checkInterval ?? TimeSpan.FromMilliseconds(500);
            var timer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = interval
            };
            void Timer_Tick(object? sender, EventArgs e)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // 确保在正确的Dispatcher上执行func
                    //var result = await dispatcher.InvokeAsync(func, DispatcherPriority.Normal);
                    var result = func.Invoke();
                    if (!EqualityComparer<T>.Default.Equals(result, expectedResult))
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
            timer.Tick += Timer_Tick;
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
                timer.Tick -= Timer_Tick;
            }
        }

        /// <summary>
        /// 执行一个无返回值的异步任务，并设置超时时间。
        /// </summary>
        public static async Task WithTimeout(
            this Task task,
            TimeSpan timeout,
            Exception? timeoutException = null,
            CancellationToken cancellationToken = default)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            // 转换为 Task<bool> 以复用泛型方法
            await (((Task<bool>)task).WithTimeout(timeout, timeoutException, cancellationToken));
        }
    }
}
