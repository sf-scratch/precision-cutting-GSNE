using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using 精密切割系统.Model.common;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;

namespace 精密切割系统.Behaviors
{
    public class TouchAndClickBehavior : Behavior<UIElement>
    {
        // 依赖属性，可用于绑定命令
        public static readonly DependencyProperty TouchAndClickCommandProperty =
            DependencyProperty.Register("TouchAndClickCommand", typeof(ICommand), typeof(TouchAndClickBehavior));

        public ICommand TouchAndClickCommand
        {
            get => (ICommand)GetValue(TouchAndClickCommandProperty);
            set => SetValue(TouchAndClickCommandProperty, value);
        }

        public object? CommandParameter { get; set; }

        private int _isRaiseCommand = 1; // 0表示未触发，1表示已触发

        protected override void OnAttached()
        {
            base.OnAttached();
            // 订阅事件
            //AssociatedObject.MouseDown += OnMouseDown;
            AssociatedObject.PreviewMouseDown += OnPreviewMouseDown;
            AssociatedObject.PreviewMouseUp += AssociatedObject_PreviewMouseUp;
            AssociatedObject.MouseLeave += AssociatedObject_MouseLeave;
            AssociatedObject.TouchDown += AssociatedObject_TouchDown;
            AssociatedObject.TouchUp += AssociatedObject_TouchUp;
            AssociatedObject.TouchLeave += AssociatedObject_TouchLeave;
            AssociatedObject.PreviewTouchDown += AssociatedObject_PreviewTouchDown;
            AssociatedObject.PreviewTouchUp += AssociatedObject_PreviewTouchUp;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            // 清理事件订阅
            //AssociatedObject.MouseDown -= OnMouseDown;
            AssociatedObject.PreviewMouseDown -= OnPreviewMouseDown;
            AssociatedObject.PreviewMouseUp -= AssociatedObject_PreviewMouseUp;
            AssociatedObject.MouseLeave -= AssociatedObject_MouseLeave;
            AssociatedObject.TouchDown -= AssociatedObject_TouchDown;
            AssociatedObject.TouchUp -= AssociatedObject_TouchUp;
            AssociatedObject.TouchLeave -= AssociatedObject_TouchLeave;
            AssociatedObject.PreviewTouchDown -= AssociatedObject_PreviewTouchDown;
            AssociatedObject.PreviewTouchUp -= AssociatedObject_PreviewTouchUp;
        }

        private void AssociatedObject_TouchDown(object? sender, TouchEventArgs e)
        {
            Interlocked.CompareExchange(ref _isRaiseCommand, 0, 1);
        }

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Interlocked.CompareExchange(ref _isRaiseCommand, 0, 1);
        }

        private void AssociatedObject_PreviewTouchDown(object? sender, TouchEventArgs e)
        {
            Interlocked.CompareExchange(ref _isRaiseCommand, 0, 1);
        }

        private void AssociatedObject_PreviewTouchUp(object? sender, TouchEventArgs e)
        {
            if (sender is not null && sender is RightButton rightButton && rightButton.DataContext is RightButtonParams rightButtonParams)
            {
                rightButton.btnBorder.Background = rightButtonParams.BackgroundDefColor;
            }
            if (Interlocked.CompareExchange(ref _isRaiseCommand, 1, 0) == 0 && TouchAndClickCommand?.CanExecute(e) == true)
            {
                TouchAndClickCommand.Execute(CommandParameter);
            }
        }

        private void AssociatedObject_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not null && sender is RightButton rightButton && rightButton.DataContext is RightButtonParams rightButtonParams)
            {
                rightButton.btnBorder.Background = rightButtonParams.BackgroundDefColor;
            }
            if (Interlocked.CompareExchange(ref _isRaiseCommand, 1, 0) == 0 && TouchAndClickCommand?.CanExecute(e) == true)
            {
                TouchAndClickCommand.Execute(CommandParameter);
            }
        }

        private void AssociatedObject_TouchUp(object? sender, TouchEventArgs e)
        {
            AssociatedObject.ReleaseTouchCapture(e.TouchDevice);
            if (sender is not null && sender is RightButton rightButton && rightButton.DataContext is RightButtonParams rightButtonParams)
            {
                rightButton.btnBorder.Background = rightButtonParams.BackgroundDefColor;
            }
            if (Interlocked.CompareExchange(ref _isRaiseCommand, 1, 0) == 0 && TouchAndClickCommand?.CanExecute(e) == true)
            {
                TouchAndClickCommand.Execute(CommandParameter);
            }
        }

        private void AssociatedObject_TouchLeave(object? sender, TouchEventArgs e)
        {
            //Interlocked.CompareExchange(ref _isRaiseCommand, 0, 1);
        }

        private void AssociatedObject_MouseLeave(object sender, MouseEventArgs e)
        {
            //Interlocked.CompareExchange(ref _isRaiseCommand, 0, 1);
        }
    }
}