using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.Threading;
using NPOI.SS.Formula.Functions;
using System.Diagnostics;

namespace 精密切割系统.Behaviors
{
    public class DualInputBehavior : Behavior<UIElement>
    {
        public static readonly DependencyProperty PromptCommandProperty =
            DependencyProperty.Register("PromptCommand", typeof(AsyncDelegateCommand), typeof(DualInputBehavior));

        public static readonly DependencyProperty StartCommandProperty =
            DependencyProperty.Register("StartCommand", typeof(ICommand), typeof(DualInputBehavior));

        public static readonly DependencyProperty StopCommandProperty =
            DependencyProperty.Register("StopCommand", typeof(ICommand), typeof(DualInputBehavior));

        public ICommand StartCommand
        {
            get => (ICommand)GetValue(StartCommandProperty);
            set => SetValue(StartCommandProperty, value);
        }

        public ICommand StopCommand
        {
            get => (ICommand)GetValue(StopCommandProperty);
            set => SetValue(StopCommandProperty, value);
        }

        public AsyncDelegateCommand PromptCommand
        {
            get => (AsyncDelegateCommand)GetValue(PromptCommandProperty);
            set => SetValue(PromptCommandProperty, value);
        }

        private readonly object _ctsLock = new();
        private int _isRaiseCommand = 0; // 0表示未触发，1表示已触发
        private CancellationTokenSource? _delayCts;

        protected override void OnAttached()
        {
            base.OnAttached();
            // 订阅事件
            AssociatedObject.TouchDown += AssociatedObject_TouchDown;
            //AssociatedObject.MouseDown += OnMouseDown;
            AssociatedObject.PreviewMouseDown += OnPreviewMouseDown;
            AssociatedObject.TouchLeave += AssociatedObject_TouchLeave;
            AssociatedObject.TouchUp += AssociatedObject_TouchUp;
            AssociatedObject.PreviewMouseUp += AssociatedObject_PreviewMouseUp;
            AssociatedObject.MouseLeave += AssociatedObject_MouseLeave;
        }

        protected override void OnDetaching()
        {
            // 清理事件订阅
            AssociatedObject.TouchDown -= AssociatedObject_TouchDown;
            //AssociatedObject.MouseDown -= OnMouseDown;
            AssociatedObject.PreviewMouseDown -= OnPreviewMouseDown;
            AssociatedObject.TouchLeave -= AssociatedObject_TouchLeave;
            AssociatedObject.TouchUp -= AssociatedObject_TouchUp;
            AssociatedObject.PreviewMouseUp -= AssociatedObject_PreviewMouseUp;
            AssociatedObject.MouseLeave -= AssociatedObject_MouseLeave;
            base.OnDetaching();
        }

        private async void AssociatedObject_TouchDown(object? sender, TouchEventArgs e)
        {
            lock (_ctsLock)
            {
                if (_delayCts is not null && !_delayCts.IsCancellationRequested)
                    return;
                _delayCts?.Dispose();
                _delayCts = new CancellationTokenSource();
            }
            try
            {
                await ExecutePrompt(_delayCts.Token);
                if (_delayCts?.IsCancellationRequested ?? true) return;
                ExecuteStart(sender, e);
            }
            catch (TaskCanceledException) { }
        }

        private async void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            lock (_ctsLock)
            {
                if (_delayCts is not null && !_delayCts.IsCancellationRequested)
                    return;
                _delayCts?.Dispose();
                _delayCts = new CancellationTokenSource();
            }
            try
            {
                await ExecutePrompt(_delayCts.Token);
                if (_delayCts?.IsCancellationRequested ?? true) return;
                ExecuteStart(sender, e);
            }
            catch (TaskCanceledException) { }
        }

        private void AssociatedObject_MouseLeave(object sender, MouseEventArgs e)
        {
            ExecuteStop(sender, e);
        }

        private void AssociatedObject_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            ExecuteStop(sender, e);
        }

        private void AssociatedObject_TouchUp(object? sender, TouchEventArgs e)
        {
            ExecuteStop(sender, e);
        }

        private void AssociatedObject_TouchLeave(object? sender, TouchEventArgs e)
        {
            ExecuteStop(sender, e);
        }

        private async Task ExecutePrompt(CancellationToken token)
        {
            if (Volatile.Read(ref _isRaiseCommand) == 0 && PromptCommand?.CanExecute() == true)
            {
                await PromptCommand.Execute(token);
            }
        }

        private void ExecuteStart(object? sender, object e)
        {
            if (Interlocked.CompareExchange(ref _isRaiseCommand, 1, 0) == 0 && StartCommand?.CanExecute(e) == true)
            {
                StartCommand.Execute(e);
            }
        }

        private void ExecuteStop(object? sender, object e)
        {
            CancellationTokenSource? ctsToCancel;
            lock (_ctsLock)
            {
                ctsToCancel = _delayCts;
                _delayCts = null;
            }
            ctsToCancel?.Cancel();
            ctsToCancel?.Dispose();
            if (Interlocked.CompareExchange(ref _isRaiseCommand, 0, 1) == 1 && StopCommand?.CanExecute(e) == true)
            {
                StopCommand.Execute(e);
            }
        }
    }
}
