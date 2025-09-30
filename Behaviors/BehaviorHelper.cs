using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace 精密切割系统.Behaviors
{
    // BehaviorHelper.cs
    public static class BehaviorHelper
    {
        public static bool GetIsABehaviorActive(DependencyObject obj)
            => (bool)obj.GetValue(IsABehaviorActiveProperty);

        public static void SetIsABehaviorActive(DependencyObject obj, bool value)
            => obj.SetValue(IsABehaviorActiveProperty, value);

        public static readonly DependencyProperty IsABehaviorActiveProperty =
            DependencyProperty.RegisterAttached(
                "IsABehaviorActive",
                typeof(bool),
                typeof(BehaviorHelper),
                new PropertyMetadata(false, OnIsABehaviorActiveChanged));

        public static ICommand GetStartCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(StartCommandProperty);
        }

        public static void SetStartCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(StartCommandProperty, value);
        }

        public static readonly DependencyProperty StartCommandProperty =
            DependencyProperty.RegisterAttached("StartCommand", typeof(ICommand), typeof(BehaviorHelper));

        public static ICommand GetStopCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(StopCommandProperty);
        }

        public static void SetStopCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(StopCommandProperty, value);
        }

        // Using a DependencyProperty as the backing store for StopCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StopCommandProperty =
            DependencyProperty.RegisterAttached("StopCommand", typeof(ICommand), typeof(BehaviorHelper));

        public static ICommand GetTouchAndClickCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(TouchAndClickCommandProperty);
        }

        public static void SetTouchAndClickCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(TouchAndClickCommandProperty, value);
        }

        // Using a DependencyProperty as the backing store for TouchAndClick.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TouchAndClickCommandProperty =
            DependencyProperty.RegisterAttached("TouchAndClickCommand", typeof(ICommand), typeof(BehaviorHelper));

        private static void OnIsABehaviorActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button button)
            {
                var behaviors = Interaction.GetBehaviors(button);

                // 清除所有相关行为
                behaviors.Clear();

                if ((bool)e.NewValue)
                {
                    // 添加 ABehavior
                    behaviors.Add(new DualInputBehavior { StartCommand = GetStartCommand(d), StopCommand = GetStopCommand(d) });
                }
                else
                {
                    // 添加 BBehavior
                    behaviors.Add(new TouchAndClickBehavior { TouchAndClickCommand = GetTouchAndClickCommand(d) });
                }
            }
        }
    }
}