using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut
{
    public class SemiAutomaticCutParamModel : BindableBase
    {
        private string _directoryId;
        private string _deviceDataNo;
        private string _deviceDataId;
        private string _channelNum;
        private string _bladeHeight;
        private string _feedSpeed;
        private string _depthCompensation;
        private string _changeFeedSpeed;
        private int _allCutLine;
        private string _allCutLineLength;
        private double _cutWidth;
        private double _edgesWidth;

        public string DirectoryId
        {
            get => _directoryId;
            set => SetProperty(ref _directoryId, value);
        }

        // DeviceDataNo
        public string DeviceDataNo
        {
            get => _deviceDataNo;
            set => SetProperty(ref _deviceDataNo, value);
        }

        // DeviceDataId
        public string DeviceDataId
        {
            get => _deviceDataId;
            set => SetProperty(ref _deviceDataId, value);
        }

        // DepthCompensation
        public string DepthCompensation
        {
            get => _depthCompensation;
            set => SetProperty(ref _depthCompensation, value);
        }

        // ChannelNum
        public string ChannelNum
        {
            get => _channelNum;
            set => SetProperty(ref _channelNum, value);
        }

        // BladeHeight
        public string BladeHeight
        {
            get => _bladeHeight;
            set => SetProperty(ref _bladeHeight, value);
        }

        // FeedSpeed
        public string FeedSpeed
        {
            get => _feedSpeed;
            set => SetProperty(ref _feedSpeed, value);
        }

        // CutWidth
        public double CutWidth
        {
            get => _cutWidth;
            set => SetProperty(ref _cutWidth, value);
        }

        // DdgesWidth
        public double DdgesWidth
        {
            get => _edgesWidth;
            set => SetProperty(ref _edgesWidth, value);
        }

        // ChangeFeedSpeed
        public string ChangeFeedSpeed
        {
            get => _changeFeedSpeed;
            set => SetProperty(ref _changeFeedSpeed, value);
        }

        private int _runCutLine;
        public int RunCutLine
        {
            get => _runCutLine;
            set => SetProperty(ref _runCutLine, value);
        }

        private int _allRunCutLine;
        public int AllRunCutLine
        {
            get => _allRunCutLine;
            set => SetProperty(ref _allRunCutLine, value);
        }

        private string _expectedProcessingEndTime;

        public string ExpectedProcessingEndTime
        {
            get => _expectedProcessingEndTime;
            set => SetProperty(ref _expectedProcessingEndTime, value);
        }

        public int AllCutLine
        {
            get => _allCutLine;
            set => SetProperty(ref _allCutLine, value);
        }

        public string AllCutLineLength
        {
            get => _allCutLineLength;
            set => SetProperty(ref _allCutLineLength, value);
        }
    }
}
