using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut
{
    public class SharpenParamsModel : BindableBase
    {
        private int _rotateSpeed;
        public int RotateSpeed
        {
            get { return _rotateSpeed; }
            set { _rotateSpeed = value; RaisePropertyChanged(); }
        }

        private string _cutThickness;
        public string CutThickness
        {
            get { return _cutThickness; }
            set { _cutThickness = value; RaisePropertyChanged(); }
        }

        private float _coJiaoHeight;
        public float CoJiaoHeight
        {
            get { return _coJiaoHeight; }
            set { _coJiaoHeight = value; RaisePropertyChanged(); }
        }

        private float _cutHeight;
        public float CutHeight
        {
            get { return _cutHeight; }
            set { _cutHeight = value; RaisePropertyChanged(); }
        }

        private float _cutSize;
        public float CutSize
        {
            get { return _cutSize; }
            set { _cutSize = value; RaisePropertyChanged(); }
        }

        private float _cutNum;
        public float CutNum
        {
            get { return _cutNum; }
            set { _cutNum = value; RaisePropertyChanged(); }
        }

        private float _coOffsetX;
        public float CoOffsetX
        {
            get { return _coOffsetX; }
            set { _coOffsetX = value; RaisePropertyChanged(); }
        }

        private float _hightestCutSpeed;
        public float HightestCutSpeed
        {
            get { return _hightestCutSpeed; }
            set { _hightestCutSpeed = value; RaisePropertyChanged(); }
        }

        private float _cutNum1;
        public float CutNum1
        {
            get { return _cutNum1; }
            set { _cutNum1 = value; RaisePropertyChanged(); }
        }

        private float _cutNum2;
        public float CutNum2
        {
            get { return _cutNum2; }
            set { _cutNum2 = value; RaisePropertyChanged(); }
        }

        private bool _isExecuteSharpen;

        public bool IsExecuteSharpen
        {
            get { return _isExecuteSharpen; }
            set { _isExecuteSharpen = value; RaisePropertyChanged(); }
        }
    }
}
