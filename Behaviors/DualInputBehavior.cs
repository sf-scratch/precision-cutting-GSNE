using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace 精密切割系统.Behaviors
{
    public class DualInputBehavior : Behavior<UIElement>
    {
        // 依赖属性，可用于绑定命令
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

        // 是否已触发事件
        private volatile bool _isRaiseTouchDownEvent = false;
        // 是否已触发命令
        private volatile bool _isRaiseCommand = false;

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

        private void AssociatedObject_TouchDown(object? sender, TouchEventArgs e)
        {
            _isRaiseCommand = true;
            _isRaiseTouchDownEvent = true;
            if (StartCommand?.CanExecute(e) == true)
            {
                StartCommand.Execute(e);
            }
        }

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isRaiseCommand = true;
            // 执行触摸命令
            if (!_isRaiseTouchDownEvent && StartCommand?.CanExecute(e) == true)
            {
                StartCommand.Execute(e);
            }
        }

        private void AssociatedObject_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isRaiseCommand && StopCommand?.CanExecute(e) == true)
            {
                StopCommand.Execute(e);
                _isRaiseCommand = false;
                _isRaiseTouchDownEvent = false;
            }
        }

        private void AssociatedObject_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isRaiseCommand && StopCommand?.CanExecute(e) == true)
            {
                StopCommand.Execute(e);
                _isRaiseCommand = false;
                _isRaiseTouchDownEvent = false;
            }
        }

        private void AssociatedObject_TouchUp(object? sender, TouchEventArgs e)
        {
            if (_isRaiseCommand && StopCommand?.CanExecute(e) == true)
            {
                StopCommand.Execute(e);
                _isRaiseCommand = false;
                _isRaiseTouchDownEvent = false;
            }
        }

        private void AssociatedObject_TouchLeave(object? sender, TouchEventArgs e)
        {
            if (_isRaiseCommand && StopCommand?.CanExecute(e) == true)
            {
                StopCommand.Execute(e);
                _isRaiseCommand = false;
                _isRaiseTouchDownEvent = false;
            }
        }
    }
}
