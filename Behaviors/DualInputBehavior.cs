using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.Threading;

namespace 精密切割系统.Behaviors
{
    public class DualInputBehavior : Behavior<UIElement>
    {
        public const float TouchDelaySeconds = 0.5f; // 触摸延迟时间，单位为秒

        public static readonly DependencyProperty PromptCommandProperty =
            DependencyProperty.Register("PromptCommand", typeof(ICommand), typeof(DualInputBehavior));

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

        public ICommand PromptCommand
        {
            get => (ICommand)GetValue(PromptCommandProperty);
            set => SetValue(PromptCommandProperty, value);
        }

        // 是否已触发命令
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
            if (_delayCts is not null && !_delayCts.IsCancellationRequested) return;
            _delayCts = new CancellationTokenSource();
            try
            {
                ExecutePrompt(sender, e);
                await Task.Delay(TimeSpan.FromSeconds(TouchDelaySeconds), _delayCts.Token);
                ExecuteStart(sender, e);
            }
            catch (TaskCanceledException) { }
        }

        private async void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_delayCts is not null && !_delayCts.IsCancellationRequested) return;
            _delayCts = new CancellationTokenSource();
            try
            {
                ExecutePrompt(sender, e);
                await Task.Delay(TimeSpan.FromSeconds(TouchDelaySeconds), _delayCts.Token);
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

        private void ExecutePrompt(object? sender, object e)
        {
            if (Volatile.Read(ref _isRaiseCommand) == 0 && PromptCommand?.CanExecute(e) == true)
            {
                PromptCommand.Execute(e);
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
            _delayCts?.Cancel();
            if (Interlocked.CompareExchange(ref _isRaiseCommand, 0, 1) == 1 && StopCommand?.CanExecute(e) == true)
            {
                StopCommand.Execute(e);
            }
        }
    }
}
