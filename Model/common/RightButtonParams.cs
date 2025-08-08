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
    public class RightButtonParams : BindableBase
    {
        public DelegateCommand RightClickCommand { get; set; }

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

        private bool _isOpen;
        public bool IsOpen
        {
            get { return _isOpen; }
            set { _isOpen = value; RaisePropertyChanged(); }
        }

        public RightButtonParams(Brush backgroundDefColor, Brush backgroundDownColor, string contentText, double fontSize, string imagePath, Action action, Visibility openOrCloseVisibility)
        {
            _backgroundDefColor = backgroundDefColor;
            _backgroundDownColor = backgroundDownColor;
            _contentText = contentText;
            _imagePath = imagePath;
            _backFlag = false;
            _visibility = Visibility.Visible;
            _contentTextFontSize = fontSize;
            _openOrCloseVisibility = openOrCloseVisibility;
            _isOpen = false;
            RightClickCommand = new DelegateCommand(() => action?.Invoke()); 
        }

        public static RightButtonParams GreenRightButton(string contentText, string imagePath, Action action, double fontSize = 12)
        {
            return new RightButtonParams(new SolidColorBrush(Color.FromRgb(0x39, 0xD1, 0x1A)), new SolidColorBrush(Color.FromRgb(0x39, 0xB4, 0x1A)), contentText, fontSize, imagePath, action, Visibility.Collapsed);
        }

        public static RightButtonParams YelloRightButton(string contentText, string imagePath, Action action, double fontSize = 12)
        {
            return new RightButtonParams(new SolidColorBrush(Color.FromRgb(0xFF, 0xAD, 0x00)), new SolidColorBrush(Color.FromRgb(0xC8, 0xAD, 0x00)), contentText, fontSize, imagePath, action, Visibility.Collapsed);
        }

        public static RightButtonParams BlueButton(string contentText, string imagePath, Action action, Visibility openOrCloseVisibility = Visibility.Collapsed, double fontSize = 22)
        {
            return new RightButtonParams(new SolidColorBrush(Color.FromRgb(0x50, 0x87, 0xcb)), new SolidColorBrush(Color.FromRgb(0x17, 0x7C, 0xfa)), contentText, fontSize, imagePath, action, openOrCloseVisibility);
        }

        public static RightButtonParams RedRightButton(string contentText, string imagePath, Action action, double fontSize = 12)
        {
            return new RightButtonParams(Brushes.Red, Brushes.DarkRed, contentText, fontSize, imagePath, action, Visibility.Collapsed);
        }
    }
}
