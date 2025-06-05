using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace 精密切割系统.Behaviors
{
    public class TouchAndClickBehavior : Behavior<UIElement>
    {// 依赖属性，可用于绑定命令
        public static readonly DependencyProperty TouchAndClickCommandProperty =
            DependencyProperty.Register("TouchAndClickCommand", typeof(ICommand), typeof(TouchAndClickBehavior));

        public ICommand TouchAndClickCommand
        {
            get => (ICommand)GetValue(TouchAndClickCommandProperty);
            set => SetValue(TouchAndClickCommandProperty, value);
        }

        // 是否已触发事件
        private volatile bool _isRaiseDownEvent = false;

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
        }

        protected override void OnDetaching()
        {
            // 清理事件订阅
            //AssociatedObject.MouseDown -= OnMouseDown;
            AssociatedObject.PreviewMouseDown -= OnPreviewMouseDown;
            AssociatedObject.PreviewMouseUp -= AssociatedObject_PreviewMouseUp;
            AssociatedObject.MouseLeave -= AssociatedObject_MouseLeave;
            AssociatedObject.TouchDown -= AssociatedObject_TouchDown;
            AssociatedObject.TouchUp -= AssociatedObject_TouchUp;
            AssociatedObject.TouchLeave -= AssociatedObject_TouchLeave;
            base.OnDetaching();
        }

        private void AssociatedObject_TouchDown(object? sender, TouchEventArgs e)
        {
            _isRaiseDownEvent = true;
        }

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isRaiseDownEvent = true;
        }

        private void AssociatedObject_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isRaiseDownEvent) return;
            _isRaiseDownEvent = false;
            if (TouchAndClickCommand?.CanExecute(e) == true)
            {
                TouchAndClickCommand.Execute(e);
            }
        }

        private void AssociatedObject_TouchUp(object? sender, TouchEventArgs e)
        {
            if (!_isRaiseDownEvent) return;
            _isRaiseDownEvent = false;
            if (TouchAndClickCommand?.CanExecute(e) == true)
            {
                TouchAndClickCommand.Execute(e);
            }
        }

        private void AssociatedObject_TouchLeave(object? sender, TouchEventArgs e)
        {
            if (!_isRaiseDownEvent) return;
            _isRaiseDownEvent = false;
            if (TouchAndClickCommand?.CanExecute(e) == true)
            {
                TouchAndClickCommand.Execute(e);
            }
        }

        private void AssociatedObject_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isRaiseDownEvent) return;
            _isRaiseDownEvent = false;
            if (TouchAndClickCommand?.CanExecute(e) == true)
            {
                TouchAndClickCommand.Execute(e);
            }
        }
    }
}
