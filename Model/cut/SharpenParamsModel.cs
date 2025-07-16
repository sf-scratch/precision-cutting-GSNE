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
        private long _id;

        public long Id
        {
            get { return _id; }
            set { _id = value; RaisePropertyChanged(); }
        }


        private int _spindleRev;
        public int SpindleRev
        {
            get { return _spindleRev; }
            set { _spindleRev = value; RaisePropertyChanged(); }
        }

        private float _sharpenThickness;
        public float SharpenThickness
        {
            get { return _sharpenThickness; }
            set { _sharpenThickness = value; RaisePropertyChanged(); }
        }

        private float _tapeThickness;
        public float TapeThickness
        {
            get { return _tapeThickness; }
            set { _tapeThickness = value; RaisePropertyChanged(); }
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

        private int _cutNum;
        public int CutNum
        {
            get { return _cutNum; }
            set { _cutNum = value; RaisePropertyChanged(); }
        }

        private float _offsetX;
        public float OffsetX
        {
            get { return _offsetX; }
            set { _offsetX = value; RaisePropertyChanged(); }
        }

        private float _hightestCutSpeed;
        public float HightestCutSpeed
        {
            get { return _hightestCutSpeed; }
            set { _hightestCutSpeed = value; RaisePropertyChanged(); }
        }

        private bool _isExecuteLastSharpen;

        public bool IsExecuteLastSharpen
        {
            get { return _isExecuteLastSharpen; }
            set { _isExecuteLastSharpen = value; RaisePropertyChanged(); }
        }
    }
}
