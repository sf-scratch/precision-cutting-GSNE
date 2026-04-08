using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace 精密切割系统.Behaviors
{

    public class MultiClickBehavior : Behavior<FrameworkElement>
    {
        private int _clickCount = 0;
        private DispatcherTimer _timer;

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(MultiClickBehavior));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty RequiredClicksProperty =
            DependencyProperty.Register(nameof(RequiredClicks), typeof(int), typeof(MultiClickBehavior), new PropertyMetadata(5));

        public int RequiredClicks
        {
            get => (int)GetValue(RequiredClicksProperty);
            set => SetValue(RequiredClicksProperty, value);
        }

        public static readonly DependencyProperty TimeoutProperty =
            DependencyProperty.Register(nameof(Timeout), typeof(double), typeof(MultiClickBehavior), new PropertyMetadata(500.0));

        public double Timeout
        {
            get => (double)GetValue(TimeoutProperty);
            set => SetValue(TimeoutProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseLeftButtonDown += OnMouseLeftButtonDown;
            _timer = new DispatcherTimer();
            _timer.Tick += OnTimerTick;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            _timer.Tick -= OnTimerTick;
            _timer.Stop();
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _clickCount++;

            _timer.Stop();
            _timer.Interval = TimeSpan.FromMilliseconds(Timeout);
            _timer.Start();

            if (_clickCount >= RequiredClicks)
            {
                _timer.Stop();
                if (Command != null && Command.CanExecute(null))
                {
                    Command.Execute(null);
                }
                _clickCount = 0;
            }
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            _timer.Stop();
            _clickCount = 0;
        }
    }
}
