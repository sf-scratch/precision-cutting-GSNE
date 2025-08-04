using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.ViewModel
{
    public class ThetaCenterAlignConfViewModel : BindableBase
    {
        private long _id;
        public long Id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _workSize;
        public string WorkSize
        {
            get { return _workSize; }
            set { SetProperty(ref _workSize, value); }
        }

        private string _workThickness;
        public string WorkThickness
        {
            get { return _workThickness; }
            set { SetProperty(ref _workThickness, value); }
        }

        private string _tapeThickness;
        public string TapeThickness
        {
            get { return _tapeThickness; }
            set { SetProperty(ref _tapeThickness, value); }
        }

        private string _bladeHeight;
        public string BladeHeight
        {
            get { return _bladeHeight; }
            set { SetProperty(ref _bladeHeight, value); }
        }

        private string _cutSpeed;
        public string CutSpeed
        {
            get { return _cutSpeed; }
            set { SetProperty(ref _cutSpeed, value); }
        }

        private string _spindleSpeed;
        public string SpindleSpeed
        {
            get { return _spindleSpeed; }
            set { SetProperty(ref _spindleSpeed, value); }
        }
    }
}
