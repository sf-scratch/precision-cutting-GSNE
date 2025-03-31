using System.ComponentModel;

namespace 精密切割系统.ViewModel;

/// <summary>
/// 
/// </summary>
public class MQSemiAutomaticCuttingConfViewModel: INotifyPropertyChanged
{
    private string _directoryId;
    private string _deviceDataNo;
    private string _deviceDataId;
    private string _channelNum;
    private string _bladeHeight;
    private string _feedSpeed;
    private int _spindleRev;
    private string _depthCompensation;
    private int _cutLine;
    private string _cutDepthOffset;
    private string _cutDirection;
    private string _changeFeedSpeed;

    
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

    // BladeHeight
    public string BladeHeight
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
    public string FeedSpeed
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
    public int SpindleRev
    {
        get { return _spindleRev; }
        set
        {
            if (_spindleRev != value)
            {
                _spindleRev = value;
                OnPropertyChanged("SpindleRev");
            }
        }
    }

    // CutLine
    public int CutLine
    {
        get { return _cutLine; }
        set
        {
            if (_cutLine != value)
            {
                _cutLine = value;
                OnPropertyChanged("CutLine");
            }
        }
    }

    // CutDepthOffset
    public string CutDepthOffset
    {
        get { return _cutDepthOffset; }
        set
        {
            if (_cutDepthOffset != value)
            {
                _cutDepthOffset = value;
                OnPropertyChanged("CutDepthOffset");
            }
        }
    }

    // CutDirection
    public string CutDirection
    {
        get { return _cutDirection; }
        set
        {
            if (_cutDirection != value)
            {
                _cutDirection = value;
                OnPropertyChanged("CutDirection");
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
    
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}