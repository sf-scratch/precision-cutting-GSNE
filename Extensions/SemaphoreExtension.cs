using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Helpers;

namespace 精密切割系统.Extensions
{
    public static class SemaphoreExtension
    {
        private static readonly ConditionalWeakTable<SemaphoreSlim, SemaphoreState> _stateTable =
            new ConditionalWeakTable<SemaphoreSlim, SemaphoreState>();

        // 异步版本
        public static async Task ExecuteAsync(
            this SemaphoreSlim semaphore,
            Func<Task> asyncAction,
            string? busyTaskName = null,
            TimeSpan timeout = default)
        {
            var state = _stateTable.GetValue(semaphore, _ => new SemaphoreState());

            string currentTaskName;
            lock (state.Lock)
            {
                currentTaskName = state.CurrentMessage;
            }

            if (!await semaphore.WaitAsync(timeout).ConfigureAwait(false))
            {
                if (!string.IsNullOrEmpty(currentTaskName))
                {
                    MaterialSnackUtils.MaterialSnack(
                        $"{currentTaskName}中，请勿进行其他操作！",
                        MaterialSnackUtils.SnackType.WARNING,
                        0);
                }
                return;
            }

            try
            {
                lock (state.Lock)
                {
                    state.CurrentMessage = busyTaskName ?? string.Empty;
                }

                await asyncAction().ConfigureAwait(false);
                MaterialSnackUtils.MaterialSnack(
                    $"{busyTaskName}已完成！",
                    MaterialSnackUtils.SnackType.SUCCESS);
            }
            finally
            {
                lock (state.Lock)
                {
                    state.CurrentMessage = string.Empty;
                }
                semaphore.Release();
            }
        }

        // 同步版本
        public static void Execute(
            this SemaphoreSlim semaphore,
            Action action,
            string? busyTaskName = null,
            TimeSpan timeout = default)
        {
            var state = _stateTable.GetValue(semaphore, _ => new SemaphoreState());

            string currentTaskName;
            lock (state.Lock)
            {
                currentTaskName = state.CurrentMessage;
            }

            bool lockAcquired;
            if (timeout == default)
            {
                semaphore.Wait();
                lockAcquired = true;
            }
            else
            {
                lockAcquired = semaphore.Wait(timeout);
            }

            if (!lockAcquired)
            {
                if (!string.IsNullOrEmpty(currentTaskName))
                {
                    MaterialSnackUtils.MaterialSnack(
                        $"{currentTaskName}中，请勿进行其他操作！",
                        MaterialSnackUtils.SnackType.WARNING,
                        0);
                }
                return;
            }

            try
            {
                lock (state.Lock)
                {
                    state.CurrentMessage = busyTaskName ?? string.Empty;
                }

                action();
            }
            finally
            {
                lock (state.Lock)
                {
                    state.CurrentMessage = string.Empty;
                }
                semaphore.Release();
            }
        }

        private class SemaphoreState
        {
            public readonly object Lock = new object();
            public string CurrentMessage = string.Empty;
        }
    }
}
