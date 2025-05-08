using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using 精密切割系统.Helpers;

namespace 精密切割系统.Model.common
{
    public class RightButtonParams : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public RelayCommand RightClickCommand { get; set; }

        private bool _backFlag;
        public bool BackFlag
        {
            get { return _backFlag; }
            set { _backFlag = value; OnPropertyChanged(); }
        }

        private Brush _backgroundDefColor;
        public Brush BackgroundDefColor
        {
            get { return _backgroundDefColor; }
            set { _backgroundDefColor = value; OnPropertyChanged(); }
        }

        private Brush _backgroundDownColor;
        public Brush BackgroundDownColor
        {
            get { return _backgroundDownColor; }
            set { _backgroundDownColor = value; OnPropertyChanged(); }
        }

        private string _contentText;
        public string ContentText
        {
            get { return _contentText; }
            set { _contentText = value; OnPropertyChanged(); }
        }

        private string _imagePath;
        public string ImagePath
        {
            get { return _imagePath; }
            set { _imagePath = value; OnPropertyChanged(); }
        }

        private Visibility _visibility;
        public Visibility Visibility
        {
            get { return _visibility; }
            set { _visibility = value; OnPropertyChanged(); }
        }

        public RightButtonParams(Brush backgroundDefColor, Brush backgroundDownColor, string contentText, string imagePath, Action action, bool backFlag = false, Visibility visibility = Visibility.Visible)
        {
            _backgroundDefColor = backgroundDefColor;
            _backgroundDownColor = backgroundDownColor;
            _contentText = contentText;
            _imagePath = imagePath;
            _backFlag = backFlag;
            _visibility = visibility;
            RightClickCommand = new RelayCommand(action);
        }

        public static RightButtonParams GreenRightButton(string contentText, string imagePath, Action action)
        {
            return new RightButtonParams(new SolidColorBrush(Color.FromRgb(0x39, 0xD1, 0x1A)), new SolidColorBrush(Color.FromRgb(0x39, 0xB4, 0x1A)), contentText, imagePath, action);
        }

        public static RightButtonParams YelloRightButton(string contentText, string imagePath, Action action)
        {
            return new RightButtonParams(new SolidColorBrush(Color.FromRgb(0xFF, 0xAD, 0x00)), new SolidColorBrush(Color.FromRgb(0xC8, 0xAD, 0x00)), contentText, imagePath, action);
        }

        public static RightButtonParams BlueRightButton(string contentText, string imagePath, Action action)
        {
            return new RightButtonParams(new SolidColorBrush(Color.FromRgb(0x50, 0x87, 0xcb)), new SolidColorBrush(Color.FromRgb(0x50, 0x87, 0xcb)), contentText, imagePath, action);
        }

        public static RightButtonParams RedRightButton(string contentText, string imagePath, Action action)
        {
            return new RightButtonParams(Brushes.Red, Brushes.DarkRed, contentText, imagePath, action);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
