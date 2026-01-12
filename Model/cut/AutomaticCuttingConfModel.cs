using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut
{
    internal class AutomaticCuttingConfModel : BindableBase
    {
        private string _directoryName;

        public string DirectoryName
        {
            get { return _directoryName; }
            set { SetProperty(ref _directoryName, value); }
        }

        private string _deviceDataNo;

        public string DeviceDataNo
        {
            get { return _deviceDataNo; }
            set { SetProperty(ref _deviceDataNo, value); }
        }

        private string _deviceDataId;

        public string DeviceDataId
        {
            get { return _deviceDataId; }
            set { SetProperty(ref _deviceDataId, value); }
        }

        private string _depthCompensation;

        public string DepthCompensation
        {
            get { return _depthCompensation; }
            set { SetProperty(ref _depthCompensation, value); }
        }

        private string _changeFeedSpeed;

        public string ChangeFeedSpeed
        {
            get { return _changeFeedSpeed; }
            set { SetProperty(ref _changeFeedSpeed, value); }
        }

        private string _afterReplaceBladeCutTimes;

        public string AfterReplaceBladeCutTimes
        {
            get { return _afterReplaceBladeCutTimes; }
            set { SetProperty(ref _afterReplaceBladeCutTimes, value); }
        }

        private string _afterReplaceBladeCutLength;

        public string AfterReplaceBladeCutLength
        {
            get { return _afterReplaceBladeCutLength; }
            set { SetProperty(ref _afterReplaceBladeCutLength, value); }
        }

        private string _afterMeasureHeightCutTimes;

        public string AfterMeasureHeightCutTimes
        {
            get { return _afterMeasureHeightCutTimes; }
            set { SetProperty(ref _afterMeasureHeightCutTimes, value); }
        }

        private string _afterMeasureHeightCutLength;

        public string AfterMeasureHeightCutLength
        {
            get { return _afterMeasureHeightCutLength; }
            set { SetProperty(ref _afterMeasureHeightCutLength, value); }
        }

        private string _afterClearDataCutTimes;

        public string AfterClearDataCutTimes
        {
            get { return _afterClearDataCutTimes; }
            set { SetProperty(ref _afterClearDataCutTimes, value); }
        }

        private string _afterClearDataCutLength;

        public string AfterClearDataCutLength
        {
            get { return _afterClearDataCutLength; }
            set { SetProperty(ref _afterClearDataCutLength, value); }
        }

        private string _spindleRev;

        public string SpindleRev
        {
            get { return _spindleRev; }
            set { SetProperty(ref _spindleRev, value); }
        }
    }
}