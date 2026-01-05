using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using 精密切割系统.Model.common;
using 精密切割系统.View.common;

namespace 精密切割系统.ViewModel
{
    internal class CtViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ButtonParams> RightButtonParams { get; set; } = WindowLayout.OperatePageButtons;

        private BitmapImage _imageSource01;
        public BitmapImage ImageSource01
        {
            get { return _imageSource01; }
            set
            {
                _imageSource01 = value;
                OnPropertyChanged(nameof(ImageSource01));
            }
        }

        private BitmapImage _imageSource02;
        public BitmapImage ImageSource02
        {
            get { return _imageSource02; }
            set
            {
                _imageSource02 = value;
                OnPropertyChanged(nameof(ImageSource02));
            }
        }

        private BitmapImage _imageSource03;
        public BitmapImage ImageSource03
        {
            get { return _imageSource03; }
            set
            {
                _imageSource03 = value;
                OnPropertyChanged(nameof(ImageSource03));
            }
        }


        private BitmapImage _imageSource04;
        public BitmapImage ImageSource04
        {
            get { return _imageSource04; }
            set
            {
                _imageSource04 = value;
                OnPropertyChanged(nameof(ImageSource04));
            }
        }

        private BitmapImage _imageSource05;
        public BitmapImage ImageSource05
        {
            get { return _imageSource05; }
            set
            {
                _imageSource05 = value;
                OnPropertyChanged(nameof(ImageSource05));
            }
        }

        private BitmapImage _imageSource06;
        public BitmapImage ImageSource06
        {
            get { return _imageSource06; }
            set
            {
                _imageSource06 = value;
                OnPropertyChanged(nameof(ImageSource06));
            }
        }

        private BitmapImage _imageSource07;
        public BitmapImage ImageSource07
        {
            get { return _imageSource07; }
            set
            {
                _imageSource07 = value;
                OnPropertyChanged(nameof(ImageSource07));
            }
        }

        private BitmapImage _imageSource08;
        public BitmapImage ImageSource08
        {
            get { return _imageSource08; }
            set
            {
                _imageSource08 = value;
                OnPropertyChanged(nameof(ImageSource08));
            }
        }

        private BitmapImage _imageSource09;
        public BitmapImage ImageSource09
        {
            get { return _imageSource09; }
            set
            {
                _imageSource09 = value;
                OnPropertyChanged(nameof(ImageSource09));
            }
        }

        private BitmapImage _imageSource10;
        public BitmapImage ImageSource10
        {
            get { return _imageSource10; }
            set
            {
                _imageSource10 = value;
                OnPropertyChanged(nameof(ImageSource09));
            }
        }

        private BitmapImage _imageSource8004;
        public BitmapImage ImageSource8004
        {
            get { return _imageSource8004; }
            set
            {
                _imageSource8004 = value;
                OnPropertyChanged(nameof(ImageSource8004));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void UpdateImage(bool type,int code)
        {
            string imagePath = type ? "/Assets/icon/right/open.png" : "/Assets/icon/right/close.png";
            switch (code)
            {
                case 1:
                    ImageSource01 = new BitmapImage(new Uri(imagePath, UriKind.Relative));
                    break;
                case 2:
                    ImageSource02 = new BitmapImage(new Uri(imagePath, UriKind.Relative));
                    break;
                case 3:
                    ImageSource03 = new BitmapImage(new Uri(imagePath, UriKind.Relative));
                    break;
                case 4:
                    ImageSource04 = new BitmapImage(new Uri(imagePath, UriKind.Relative));
                    break;
                case 5:
                    ImageSource05 = new BitmapImage(new Uri(imagePath, UriKind.Relative));
                    break;
                case 6:
                    ImageSource06 = new BitmapImage(new Uri(imagePath, UriKind.Relative));
                    break;
                case 7:
                    ImageSource07 = new BitmapImage(new Uri(imagePath, UriKind.Relative));
                    break;
                case 8:
                    ImageSource08 = new BitmapImage(new Uri(imagePath, UriKind.Relative));
                    break;
                case 9:
                    ImageSource09 = new BitmapImage(new Uri(imagePath, UriKind.Relative));
                    break;
                case 10:
                    ImageSource10 = new BitmapImage(new Uri(imagePath, UriKind.Relative));
                    break;
                case 8004:
                    ImageSource8004 = new BitmapImage(new Uri(imagePath, UriKind.Relative));
                    break;
            }
           

        }
    }
}
