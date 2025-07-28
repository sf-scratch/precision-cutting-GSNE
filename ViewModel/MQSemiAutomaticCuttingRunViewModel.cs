using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.ViewModel
{
    public class MQSemiAutomaticCuttingRunViewModel : INotifyPropertyChanged
    {
        private string _directoryId;
        private string _deviceDataNo;
        private string _deviceDataId;
        private string _channelNum;
        private float _bladeHeight;
        private float _feedSpeed;
        private string _depthCompensation;
        private string _changeFeedSpeed;
        private int _runCutLine;
        private int _allRunCutLine;
        private int _allCutLine; 
        private float _allCutLineLength; 


        public string DirectoryId
        {
            get { return _directoryId; }
            set
            {
                if (_directoryId != value)
                {
                    _directoryId = value;
                    OnPropertyChanged("DirectoryId");
                }
            }
        }

        // DeviceDataNo
        public string DeviceDataNo
        {
            get { return _deviceDataNo; }
            set
            {
                if (_deviceDataNo != value)
                {
                    _deviceDataNo = value;
                    OnPropertyChanged("DeviceDataNo");
                }
            }
        }

        // DeviceDataId
        public string DeviceDataId
        {
            get { return _deviceDataId; }
            set
            {
                if (_deviceDataId != value)
                {
                    _deviceDataId = value;
                    OnPropertyChanged("DeviceDataId");
                }
            }
        }

        // DepthCompensation
        public string DepthCompensation
        {
            get { return _depthCompensation; }
            set
            {
                if (_depthCompensation != value)
                {
                    _depthCompensation = value;
                    OnPropertyChanged("DepthCompensation");
                }
            }
        }

        // ChannelNum
        public string ChannelNum
        {
            get { return _channelNum; }
            set
            {
                if (_channelNum != value)
                {
                    _channelNum = value;
                    OnPropertyChanged("ChannelNum");
                }
            }
        }


        private string _expectedProcessingEndTime;

        public string ExpectedProcessingEndTime
        {
            get { return _expectedProcessingEndTime; }
            set
            {
                if (_expectedProcessingEndTime != value)
                {
                    _expectedProcessingEndTime = value;
                    OnPropertyChanged("ExpectedProcessingEndTime");
                }
            }
        }

        // BladeHeight
        public float BladeHeight
        {
            get { return _bladeHeight; }
            set
            {
                if (_bladeHeight != value)
                {
                    _bladeHeight = value;
                    OnPropertyChanged("BladeHeight");
                }
            }
        }

        // FeedSpeed
        public float FeedSpeed
        {
            get { return _feedSpeed; }
            set
            {
                if (_feedSpeed != value)
                {
                    _feedSpeed = value;
                    OnPropertyChanged("FeedSpeed");
                }
            }
        }

        // SpindleRev
        public int RunCutLine
        {
            get { return _runCutLine; }
            set
            {
                if (_runCutLine != value)
                {
                    _runCutLine = value;
                    OnPropertyChanged("RunCutLine");
                }
            }
        }

        // AllRunCutLine
        public int AllRunCutLine
        {
            get { return _allRunCutLine; }
            set
            {
                if (_allRunCutLine != value)
                {
                    _allRunCutLine = value;
                    OnPropertyChanged("AllRunCutLine");
                }
            }
        }

        // ChangeFeedSpeed
        public string ChangeFeedSpeed
        {
            get { return _changeFeedSpeed; }
            set
            {
                if (_changeFeedSpeed != value)
                {
                    _changeFeedSpeed = value;
                    OnPropertyChanged("ChangeFeedSpeed");
                }
            }
        }
        public int AllCutLine
        {
            get { return _allCutLine; }
            set
            {
                if (_allCutLine != value)
                {
                    _allCutLine = value;
                    OnPropertyChanged("AllCutLine");
                }
            }
        }
        public float AllCutLineLength
        {
            get { return _allCutLineLength; }
            set
            {
                if (_allCutLineLength != value)
                {
                    _allCutLineLength = value;
                    OnPropertyChanged("AllCutLineLength");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}