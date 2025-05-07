using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut
{
    public class SharpenParamsModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private int _rotateSpeed;
        public int RotateSpeed
        {
            get { return _rotateSpeed; }
            set { _rotateSpeed = value; OnPropertyChanged(); }
        }

        private string _cutThickness;
        public string CutThickness
        {
            get { return _cutThickness; }
            set { _cutThickness = value; OnPropertyChanged(); }
        }

        private float _coJiaoHeight;
        public float CoJiaoHeight
        {
            get { return _coJiaoHeight; }
            set { _coJiaoHeight = value; OnPropertyChanged(); }
        }

        private float _cutHeight;
        public float CutHeight
        {
            get { return _cutHeight; }
            set { _cutHeight = value; OnPropertyChanged(); }
        }

        private float _cutSize;
        public float CutSize
        {
            get { return _cutSize; }
            set { _cutSize = value; OnPropertyChanged(); }
        }

        private float _cutNum;
        public float CutNum
        {
            get { return _cutNum; }
            set { _cutNum = value; OnPropertyChanged(); }
        }

        private float _coOffsetX;
        public float CoOffsetX
        {
            get { return _coOffsetX; }
            set { _coOffsetX = value; OnPropertyChanged(); }
        }



        private float _cutSpeed1;
        public float CutSpeed1
        {
            get { return _cutSpeed1; }
            set { _cutSpeed1 = value; OnPropertyChanged(); }
        }

        private float _cutSpeed2;
        public float CutSpeed2
        {
            get { return _cutSpeed2; }
            set { _cutSpeed2 = value; OnPropertyChanged(); }
        }

        private float _cutNum1;
        public float CutNum1
        {
            get { return _cutNum1; }
            set { _cutNum1 = value; OnPropertyChanged(); }
        }

        private float _cutNum2;
        public float CutNum2
        {
            get { return _cutNum2; }
            set { _cutNum2 = value; OnPropertyChanged(); }
        }
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
