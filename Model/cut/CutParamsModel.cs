using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut
{
    public class CutParamsModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _deviceDataNo;
        public string DeviceDataNo
        {
            get { return _deviceDataNo; }
            set { _deviceDataNo = value; OnPropertyChanged(); }
        }

        private string _tapeThickness;
        public string TapeThickness
        {
            get { return _tapeThickness; }
            set { _tapeThickness = value; OnPropertyChanged(); }
        }

        private int _spindleRev;
        public int SpindleRev
        {
            get { return _spindleRev; }
            set { _spindleRev = value; OnPropertyChanged(); }
        }

        private float _cutHeight;
        public float CutHeight
        {
            get { return _cutHeight; }
            set { _cutHeight = value; OnPropertyChanged(); }
        }

        private string _precutProcessNo;
        public string PrecutProcessNo
        {
            get { return _precutProcessNo; }
            set { _precutProcessNo = value; OnPropertyChanged(); }
        }

        private string _maxCutSpeed;
        public string MaxCutSpeed
        {
            get { return _maxCutSpeed; }
            set { _maxCutSpeed = value; OnPropertyChanged(); }
        }

        private int _cutNum;
        public int CutNum
        {
            get { return _cutNum; }
            set { _cutNum = value; OnPropertyChanged(); }
        }

        private string _workThickness;
        public string WorkThickness
        {
            get { return _workThickness; }
            set { _workThickness = value; OnPropertyChanged(); }
        }

        private float _offsetX;
        public float OffsetX
        {
            get { return _offsetX; }
            set { _offsetX = value; OnPropertyChanged(); }
        }
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
