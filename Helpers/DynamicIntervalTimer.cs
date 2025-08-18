

using System;
using System.Windows;
using System.Timers;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace 精密切割系统.Helpers
{

    /// <summary>
    /// 动态间隔定时器（支持同步/异步操作，自动管理生命周期）
    /// </summary>
    public sealed class DynamicIntervalTimer : IDisposable
    {
        private readonly object _locker = new();
        private System.Timers.Timer? _timer;
        private ElapsedEventHandler? _currentActionHandler;
        private bool _isActive;
        private readonly Dispatcher _dispatcher;
        private readonly TimeSpan _initialDelay;
        private readonly TimeSpan _subsequentInterval;

        /// <param name="initialDelay">初始延迟</param>
        /// <param name="subsequentInterval">后续间隔</param>
        /// <param name="dispatcher">UI调度器（默认使用Application.Current.Dispatcher）</param>
        public DynamicIntervalTimer(
            TimeSpan initialDelay,
            TimeSpan subsequentInterval,
            Dispatcher? dispatcher = null)
        {
            _initialDelay = initialDelay;
            _subsequentInterval = subsequentInterval;
            _dispatcher = dispatcher ?? Application.Current.Dispatcher;
            _isActive = true;
            InitializeTimer();
            _timer!.Elapsed += RestartTimerHandler; // 固定注册重启逻辑
        }

        private void InitializeTimer()
        {
            lock (_locker)
            {
                _timer?.Dispose();
                _timer = new System.Timers.Timer(_initialDelay.TotalMilliseconds)
                {
                    AutoReset = false
                };
            }
        }

        /// <summary>注册同步操作（替换旧处理器）</summary>
        public void RegisterAction(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            RegisterHandler((_, _) => _dispatcher.Invoke(action));
        }

        /// <summary>注册异步操作（替换旧处理器）</summary>
        public void RegisterAsyncAction(Func<Task> asyncAction)
        {
            ArgumentNullException.ThrowIfNull(asyncAction);
            RegisterHandler(async (_, _) => await _dispatcher.InvokeAsync(asyncAction));
        }

        private void RegisterHandler(ElapsedEventHandler handler)
        {
            lock (_locker)
            {
                if (_timer == null) throw new ObjectDisposedException(nameof(DynamicIntervalTimer));

                // 移除旧处理器
                if (_currentActionHandler != null)
                    _timer.Elapsed -= _currentActionHandler;

                // 添加新处理器
                _timer.Elapsed += handler;
                _currentActionHandler = handler;
            }
        }

        private void RestartTimerHandler(object? sender, ElapsedEventArgs e)
        {
            lock (_locker)
            {
                if (!_isActive || _timer == null) return;
                _timer.Interval = _subsequentInterval.TotalMilliseconds;
                _timer.Start();
            }
        }

        /// <summary>启动定时器（使用初始延迟）</summary>
        public void Start()
        {
            lock (_locker)
            {
                if (_timer == null) throw new ObjectDisposedException(nameof(DynamicIntervalTimer));
                _isActive = true;
                _timer.Interval = _initialDelay.TotalMilliseconds;
                _timer.Start();
            }
        }

        /// <summary>停止定时器（可重新Start）</summary>
        public void Stop()
        {
            lock (_locker)
            {
                _isActive = false;
                _timer?.Stop();
            }
        }

        public void Dispose()
        {
            lock (_locker)
            {
                _isActive = false;
                if (_timer != null)
                {
                    _timer.Elapsed -= _currentActionHandler;
                    _timer.Elapsed -= RestartTimerHandler;
                    _timer.Dispose();
                    _timer = null;
                }
                _currentActionHandler = null;
            }
        }
    }
}
