using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using 精密切割系统.Helpers;
using 精密切割系统.View.Controls;

namespace 精密切割系统.Model.common
{
    public class ButtonParams : BindableBase
    {
        public DelegateCommand RightClickCommand { get; set; }
        public DelegateCommand StartCommand { get; set; }
        public DelegateCommand StopCommand { get; set; }

        private bool _backFlag;

        public bool BackFlag
        {
            get { return _backFlag; }
            set { _backFlag = value; RaisePropertyChanged(); }
        }

        private Brush _backgroundDefColor;

        public Brush BackgroundDefColor
        {
            get { return _backgroundDefColor; }
            set { _backgroundDefColor = value; RaisePropertyChanged(); }
        }

        private Brush _backgroundDownColor;

        public Brush BackgroundDownColor
        {
            get { return _backgroundDownColor; }
            set { _backgroundDownColor = value; RaisePropertyChanged(); }
        }

        private string _contentText;

        public string ContentText
        {
            get { return _contentText; }
            set { _contentText = value; RaisePropertyChanged(); }
        }

        private double _contentTextFontSize;

        public double ContentTextFontSize
        {
            get { return _contentTextFontSize; }
            set { _contentTextFontSize = value; RaisePropertyChanged(); }
        }

        private string _imagePath;

        public string ImagePath
        {
            get { return _imagePath; }
            set { _imagePath = value; RaisePropertyChanged(); }
        }

        private Visibility _visibility;

        public Visibility Visibility
        {
            get { return _visibility; }
            set { _visibility = value; RaisePropertyChanged(); }
        }

        private Visibility _openOrCloseVisibility;

        public Visibility OpenOrCloseVisibility
        {
            get { return _openOrCloseVisibility; }
            set { _openOrCloseVisibility = value; RaisePropertyChanged(); }
        }

        private Visibility _buttonVisibility;

        public Visibility ButtonVisibility
        {
            get { return _buttonVisibility; }
            set { SetProperty(ref _buttonVisibility, value); }
        }

        private bool _isOpen;

        public bool IsOpen
        {
            get { return _isOpen; }
            set { _isOpen = value; RaisePropertyChanged(); }
        }

        public ButtonParams(Brush backgroundDefColor, Brush backgroundDownColor, string contentText, double fontSize, string imagePath, Action? action, Action? start, Action? stop, Visibility openOrCloseVisibility, Visibility buttonVisibility)
        {
            _backgroundDefColor = backgroundDefColor;
            _backgroundDownColor = backgroundDownColor;
            _contentText = contentText;
            _imagePath = imagePath;
            _backFlag = false;
            _visibility = Visibility.Visible;
            _contentTextFontSize = fontSize;
            _openOrCloseVisibility = openOrCloseVisibility;
            _buttonVisibility = buttonVisibility;
            _isOpen = false;
            RightClickCommand = new DelegateCommand(() => action?.Invoke());
            StartCommand = new DelegateCommand(() => start?.Invoke());
            StopCommand = new DelegateCommand(() => stop?.Invoke());
        }

        public ButtonParams(Brush backgroundDefColor, Brush backgroundDownColor, string contentText, double fontSize, string imagePath, Func<Task>? action, Func<Task>? start, Func<Task>? stop, Visibility openOrCloseVisibility, Visibility buttonVisibility)
        {
            _backgroundDefColor = backgroundDefColor;
            _backgroundDownColor = backgroundDownColor;
            _contentText = contentText;
            _imagePath = imagePath;
            _backFlag = false;
            _visibility = Visibility.Visible;
            _contentTextFontSize = fontSize;
            _openOrCloseVisibility = openOrCloseVisibility;
            _buttonVisibility = buttonVisibility;
            _isOpen = false;
            RightClickCommand = new DelegateCommand(() => action?.Invoke());
            StartCommand = new DelegateCommand(() => start?.Invoke());
            StopCommand = new DelegateCommand(() => stop?.Invoke());
        }

        public static ButtonParams GreenRightButton(string contentText, string imagePath, Action? action, Action? start = null, Action? stop = null)
        {
            return new ButtonParams(new SolidColorBrush(Color.FromRgb(0x39, 0xD1, 0x1A)), new SolidColorBrush(Color.FromRgb(0x39, 0xB4, 0x1A)), contentText, 12, imagePath, action, start, stop, Visibility.Collapsed, Visibility.Visible);
        }

        public static ButtonParams YelloRightButton(string contentText, string imagePath, Action? action, Action? start = null, Action? stop = null)
        {
            return new ButtonParams(new SolidColorBrush(Color.FromRgb(0xFF, 0xAD, 0x00)), new SolidColorBrush(Color.FromRgb(0xC8, 0xAD, 0x00)), contentText, 12, imagePath, action, start, stop, Visibility.Collapsed, Visibility.Visible);
        }

        public static ButtonParams BlueButton(string contentText, string imagePath, Action? action, Action? start = null, Action? stop = null, Visibility openOrCloseVisibility = Visibility.Collapsed, Visibility buttonVisibility = Visibility.Visible)
        {
            return new ButtonParams(new SolidColorBrush(Color.FromRgb(0x50, 0x87, 0xcb)), new SolidColorBrush(Color.FromRgb(0x17, 0x7C, 0xfa)), contentText, 22, imagePath, action, start, stop, openOrCloseVisibility, buttonVisibility);
        }

        public static ButtonParams RedRightButton(string contentText, string imagePath, Action? action, Action? start = null, Action? stop = null)
        {
            return new ButtonParams(Brushes.Red, Brushes.DarkRed, contentText, 12, imagePath, action, start, stop, Visibility.Collapsed, Visibility.Visible);
        }

        public static ButtonParams GreenRightButton(string contentText, string imagePath, Func<Task>? action, Func<Task>? start = null, Func<Task>? stop = null)
        {
            return new ButtonParams(new SolidColorBrush(Color.FromRgb(0x39, 0xD1, 0x1A)), new SolidColorBrush(Color.FromRgb(0x39, 0xB4, 0x1A)), contentText, 12, imagePath, action, start, stop, Visibility.Collapsed, Visibility.Visible);
        }

        public static ButtonParams YelloRightButton(string contentText, string imagePath, Func<Task>? action, Func<Task>? start = null, Func<Task>? stop = null)
        {
            return new ButtonParams(new SolidColorBrush(Color.FromRgb(0xFF, 0xAD, 0x00)), new SolidColorBrush(Color.FromRgb(0xC8, 0xAD, 0x00)), contentText, 12, imagePath, action, start, stop, Visibility.Collapsed, Visibility.Visible);
        }

        public static ButtonParams BlueButton(string contentText, string imagePath, Func<Task>? action, Func<Task>? start = null, Func<Task>? stop = null, Visibility openOrCloseVisibility = Visibility.Collapsed, Visibility buttonVisibility = Visibility.Visible)
        {
            return new ButtonParams(new SolidColorBrush(Color.FromRgb(0x50, 0x87, 0xcb)), new SolidColorBrush(Color.FromRgb(0x17, 0x7C, 0xfa)), contentText, 22, imagePath, action, start, stop, openOrCloseVisibility, buttonVisibility);
        }

        public static ButtonParams RedRightButton(string contentText, string imagePath, Func<Task>? action, Func<Task>? start = null, Func<Task>? stop = null)
        {
            return new ButtonParams(Brushes.Red, Brushes.DarkRed, contentText, 12, imagePath, action, start, stop, Visibility.Collapsed, Visibility.Visible);
        }
    }
}