using System.ComponentModel;

namespace 精密切割系统.ViewModel;

/// <summary>
///
/// </summary>
public class MQSemiAutomaticCuttingConfViewModel : BindableBase
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
        set { SetProperty(ref _directoryId, value); }
    }

    // DeviceDataNo
    public string DeviceDataNo
    {
        get { return _deviceDataNo; }
        set { SetProperty(ref _deviceDataNo, value); }
    }

    // DeviceDataId
    public string DeviceDataId
    {
        get { return _deviceDataId; }
        set { SetProperty(ref _deviceDataId, value); }
    }

    // DepthCompensation
    public string DepthCompensation
    {
        get { return _depthCompensation; }
        set { SetProperty(ref _depthCompensation, value); }
    }

    // ChannelNum
    public string ChannelNum
    {
        get { return _channelNum; }
        set { SetProperty(ref _channelNum, value); }
    }

    // BladeHeight
    public string BladeHeight
    {
        get { return _bladeHeight; }
        set { SetProperty(ref _bladeHeight, value); }
    }

    // FeedSpeed
    public string FeedSpeed
    {
        get { return _feedSpeed; }
        set { SetProperty(ref _feedSpeed, value); }
    }

    // SpindleRev
    public int SpindleRev
    {
        get { return _spindleRev; }
        set { SetProperty(ref _spindleRev, value); }
    }

    // CutLine
    public int CutLine
    {
        get { return _cutLine; }
        set { SetProperty(ref _cutLine, value); }
    }

    // CutDepthOffset
    public string CutDepthOffset
    {
        get { return _cutDepthOffset; }
        set { SetProperty(ref _cutDepthOffset, value); }
    }

    // CutDirection
    public string CutDirection
    {
        get { return _cutDirection; }
        set { SetProperty(ref _cutDirection, value); }
    }

    // ChangeFeedSpeed
    public string ChangeFeedSpeed
    {
        get { return _changeFeedSpeed; }
        set { SetProperty(ref _changeFeedSpeed, value); }
    }

    private bool _isOpenCutWaterAfterCuttingCompleted;

    public bool IsOpenCutWaterAfterCuttingCompleted
    {
        get { return _isOpenCutWaterAfterCuttingCompleted; }
        set { SetProperty(ref _isOpenCutWaterAfterCuttingCompleted, value); }
    }
}