using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut
{
    public class KnifeWearModel : BindableBase
    {
        private DateTime _startTime;
        public DateTime StartTime
        {
            get { return _startTime; }
            set { _startTime = value; RaisePropertyChanged(); }
        }

        private DateTime _endTime;
        public DateTime EndTime
        {
            get { return _endTime; }
            set { _endTime = value; RaisePropertyChanged(); }
        }

        private int _sharpenCount;
        public int SharpenCount
        {
            get { return _sharpenCount; }
            set { _sharpenCount = value; RaisePropertyChanged(); }
        }

        private float _wearAmount;
        public float WearAmount
        {
            get { return _wearAmount; }
            set { _wearAmount = value; RaisePropertyChanged(); }
        }

        private int _lastSharpenCount;
        public int LastSharpenCount
        {
            get { return _lastSharpenCount; }
            set { _lastSharpenCount = value; RaisePropertyChanged(); }
        }

        private float _lastWearAmount;
        public float LastWearAmount
        {
            get { return _lastWearAmount; }
            set { _lastWearAmount = value; RaisePropertyChanged(); }
        }

        private int _cutCount;
        public int CutCount
        {
            get { return _cutCount; }
            set { _cutCount = value; RaisePropertyChanged(); }
        }

        private string _firstCutImage;
        public string FirstCutImage
        {
            get { return _firstCutImage; }
            set { _firstCutImage = value; RaisePropertyChanged(); }
        }

        private string _secondCutImage;
        public string SecondCutImage
        {
            get { return _secondCutImage; }
            set { _secondCutImage = value; RaisePropertyChanged(); }
        }

        private string _lastCutImage;
        public string LastCutImage
        {
            get { return _lastCutImage; }
            set { _lastCutImage = value; RaisePropertyChanged(); }
        }
    }
}
