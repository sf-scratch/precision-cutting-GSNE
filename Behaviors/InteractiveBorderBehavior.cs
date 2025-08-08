using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace 精密切割系统.Behaviors
{
    public class InteractiveBorderBehavior : Behavior<Border>
    {
        public Brush DownColor
        {
            get { return (Brush)GetValue(DownColorProperty); }
            set { SetValue(DownColorProperty, value); }
        }

        public static readonly DependencyProperty DownColorProperty =
            DependencyProperty.Register("DownColor", typeof(Brush), typeof(InteractiveBorderBehavior), new PropertyMetadata(Brushes.Silver));

        public Brush DefaultColor
        {
            get { return (Brush)GetValue(DefaultColorProperty); }
            set { SetValue(DefaultColorProperty, value); }
        }

        public static readonly DependencyProperty DefaultColorProperty =
            DependencyProperty.Register("DefaultColor", typeof(Brush), typeof(InteractiveBorderBehavior), new PropertyMetadata(Brushes.Silver));

        protected override void OnAttached()
        {
            AssociatedObject.PreviewMouseDown += OnPressed;
            AssociatedObject.PreviewMouseUp += OnReleased;
            AssociatedObject.MouseLeave += OnReleased;
            AssociatedObject.TouchDown += OnPressed;
            AssociatedObject.TouchUp += OnReleased;
            AssociatedObject.TouchLeave += OnReleased;
            AssociatedObject.PreviewTouchDown += OnPressed;
            AssociatedObject.PreviewTouchUp += OnReleased;
        }

        private void OnPressed(object? sender, EventArgs e)
        {
            AssociatedObject.Background = DownColor;
        }

        private void OnReleased(object? sender, EventArgs e)
        {
            AssociatedObject.Background = DefaultColor;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseDown -= OnPressed;
            AssociatedObject.PreviewMouseUp -= OnReleased;
            AssociatedObject.MouseLeave -= OnReleased;
            AssociatedObject.TouchDown -= OnPressed;
            AssociatedObject.TouchUp -= OnReleased;
            AssociatedObject.TouchLeave -= OnReleased;
            AssociatedObject.PreviewTouchDown -= OnPressed;
            AssociatedObject.PreviewTouchUp -= OnReleased;
        }
    }
}
