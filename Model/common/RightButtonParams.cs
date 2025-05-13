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

namespace 精密切割系统.Model.common
{
    public class RightButtonParams : BindableBase
    {
        public DelegateCommand RightClickCommand { get; set; }
        public DelegateCommand StartCommand { get; }
        public DelegateCommand StopCommand { get; }
        private CancellationTokenSource _cts;

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

        public RightButtonParams(Brush backgroundDefColor, Brush backgroundDownColor, string contentText, double fontSize, string imagePath, Action? action = null, Action? continuousAction = null, bool backFlag = false, Visibility visibility = Visibility.Visible)
        {
            _backgroundDefColor = backgroundDefColor;
            _backgroundDownColor = backgroundDownColor;
            _contentText = contentText;
            _imagePath = imagePath;
            _backFlag = backFlag;
            _visibility = visibility;
            _contentTextFontSize = fontSize;
            RightClickCommand = new DelegateCommand(() => action?.Invoke()); 
            StartCommand = new DelegateCommand(() => StartContinuousAction(continuousAction));
            StopCommand = new DelegateCommand(StopContinuousAction);
        }

        private async void StartContinuousAction(Action? action)
        {
            _cts = new CancellationTokenSource();
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    action?.Invoke();
                    await Task.Delay(100, _cts.Token); // 控制触发频率
                }
            }
            catch (OperationCanceledException) { /* 正常停止 */ }
        }

        private void StopContinuousAction()
        {
            _cts?.Cancel();
        }

        public static RightButtonParams GreenRightButton(string contentText, string imagePath, Action? action, Action? continuousAction = null, double fontSize = 10)
        {
            return new RightButtonParams(new SolidColorBrush(Color.FromRgb(0x39, 0xD1, 0x1A)), new SolidColorBrush(Color.FromRgb(0x39, 0xB4, 0x1A)), contentText, fontSize, imagePath, action, continuousAction);
        }

        public static RightButtonParams YelloRightButton(string contentText, string imagePath, Action? action, Action? continuousAction = null, double fontSize = 10)
        {
            return new RightButtonParams(new SolidColorBrush(Color.FromRgb(0xFF, 0xAD, 0x00)), new SolidColorBrush(Color.FromRgb(0xC8, 0xAD, 0x00)), contentText, fontSize, imagePath, action, continuousAction);
        }

        public static RightButtonParams BlueRightButton(string contentText, string imagePath, Action? action, Action? continuousAction = null, double fontSize = 10)
        {
            return new RightButtonParams(new SolidColorBrush(Color.FromRgb(0x50, 0x87, 0xcb)), new SolidColorBrush(Color.FromRgb(0x17, 0x7C, 0xfa)), contentText, fontSize, imagePath, action, continuousAction);
        }

        public static RightButtonParams RedRightButton(string contentText, string imagePath, Action? action, Action? continuousAction = null, double fontSize = 10)
        {
            return new RightButtonParams(Brushes.Red, Brushes.DarkRed, contentText, fontSize, imagePath, action, continuousAction);
        }
    }
}
