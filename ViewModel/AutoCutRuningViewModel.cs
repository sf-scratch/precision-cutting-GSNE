using Newtonsoft.Json.Linq;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using Prism.Dialogs;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.DTOs;
using 精密切割系统.Extensions;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.HttpClients;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.MeasureHeight;
using 精密切割系统.Model.plc;
using 精密切割系统.PubSubEvent;
using 精密切割系统.Utils;
using 精密切割系统.View.common;
using 精密切割系统.View.Dialogs;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.View.Pages.common;
using 精密切割系统.View.Pages.F4_BladeMaintenance;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.ViewModel
{
    public class AutoCutRuningViewModel : CustomBindableBase
    {
        public DelegateCommand RunAutoCutCommand { get; set; }
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogService _dialogService;
        private readonly FullyAutoSharpenService _sharpenService;
        private readonly FullyAutoCutService _cutService;
        private CancellationTokenSource _pauseCts;
        private CancellationTokenSource _monitoringAlarmCts;
        public ObservableCollection<MessageModel> MessageList { get; set; }

        /// <summary>
        /// 磨刀参数
        /// </summary>
        public SharpenParamsModel SharpenParams { get; set; }

        /// <summary>
        /// 切割参数
        /// </summary>
        public CutParamsModel CutParams { get; set; }

        /// <summary>
        /// 轮毂ID
        /// </summary>
        public LunguSksjModel LunguSksj { get; set; }

        private float _afterHeightMeasurementZ;
        /// <summary>
        /// 测高位置
        /// </summary>
        public float AfterHeightMeasurementZ
        {
            get { return _afterHeightMeasurementZ; }
            set { _afterHeightMeasurementZ = value; RaisePropertyChanged(); }
        }

        private float _sharpenBladeHeight;
        /// <summary>
        /// 磨刀片高度
        /// </summary>
        public float SharpenBladeHeight
        {
            get { return _sharpenBladeHeight; }
            set { _sharpenBladeHeight = value; RaisePropertyChanged(); }
        }

        private float _sharpenSpeed;
        /// <summary>
        /// 磨刀速度
        /// </summary>
        public float SharpenSpeed
        {
            get { return _sharpenSpeed; }
            set { _sharpenSpeed = value; RaisePropertyChanged(); }
        }

        private string _sharpenProgress;
        /// <summary>
        /// 磨刀进度
        /// </summary>
        public string SharpenProgress
        {
            get { return _sharpenProgress; }
            set { _sharpenProgress = value; RaisePropertyChanged(); }
        }

        private float _totalWearAmount;
        /// <summary>
        /// 总磨损量
        /// </summary>
        public float TotalWearAmount
        {
            get { return _totalWearAmount; }
            set { _totalWearAmount = value; RaisePropertyChanged(); }
        }

        private string _deviceDataNo;
        /// <summary>
        /// 型号参数No
        /// </summary>
        public string DeviceDataNo
        {
            get { return _deviceDataNo; }
            set { _deviceDataNo = value; RaisePropertyChanged(); }
        }

        private float _cutBladeHeight;
        /// <summary>
        /// 切割刀片高度
        /// </summary>
        public float CutBladeHeight
        {
            get { return _cutBladeHeight; }
            set { _cutBladeHeight = value; RaisePropertyChanged(); }
        }

        private float _cutSpeed;
        /// <summary>
        /// 磨刀速度
        /// </summary>
        public float CutSpeed
        {
            get { return _cutSpeed; }
            set { _cutSpeed = value; RaisePropertyChanged(); }
        }

        private string _cutProgress;
        /// <summary>
        /// 切割进度
        /// </summary>
        public string CutProgress
        {
            get { return _cutProgress; }
            set { _cutProgress = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 自更换刀片起刀片切了几道
        /// </summary>
        public int AfterReplaceBladeCutTimes
        {
            get { return Appsettings.AfterReplaceBladeCutTimes ?? 0; }
            set { Appsettings.AfterReplaceBladeCutTimes = value; RaisePropertyChanged(); }
        }

        private AutoRunStatus _autoRunStatus;
        public AutoRunStatus RunStatus
        {
            get { return _autoRunStatus; }
            set
            {
                _autoRunStatus = value;
                UpdateMaterialSnack();
            }
        }

        public AutoCutRuningViewModel(IRegionManager regionManager, IEventAggregator eventAggregator, IDialogService dialogService)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            _dialogService = dialogService;
            MessageList = new ObservableCollection<MessageModel>();
            _sharpenService = FullyAutoSharpenService.Instance;
            _cutService = FullyAutoCutService.Instance;
            _pauseCts = new CancellationTokenSource();
            _monitoringAlarmCts = new CancellationTokenSource();
            RunAutoCutCommand = new DelegateCommand(RunAutoCut);
        }

        public AutoCutRuningViewModel()
        {
        }

        private void InitRightButton()
        {
            RightButtonCollection.Add(RightButtonParams.YelloRightButton("暂停", "/Assets/icon/right/stop.png", () => Pause()));
        }

        private async void RunAutoCut()
        {
            if (!GlobalParams.onlineFlag)
            {
                RunStatus = AutoRunStatus.SharpeningInProgress;
                return;
            }
            //清报警列表
            await PlcControl.tagControl.wholeDevice.AlarmResetAsync();
            //暂停页面跳转回来会触发InitCommand，调继续切割
            if (RunStatus == AutoRunStatus.SharpeningInProgress || RunStatus == AutoRunStatus.CutingInProgress || RunStatus == AutoRunStatus.ReplaceSharpenBoard || RunStatus == AutoRunStatus.ReplaceWafer)
            {
                Continue();
                return;
            }
            //相机中心点位置
            DataPoint<float> cameraThetaCenterPoint = Appsettings.CameraThetaCenterPoint;
            //工件半径
            float workpieceRadius = GlobalParams.WorkpieceRadius;
            //工件中心点到theta轴中心点距离
            float centerDistance = GlobalParams.CenterDistance;
            // 磨刀板尺寸
            DataRectangleF sharpenRect = GlobalParams.SharpenRect;
            //刀片切一刀后抬起高度
            float bladeLiftingHeight = GlobalParams.BladeLiftingHeight;
            //非接触测高位置到工作台的z1轴高度
            float nonContactHeightMeasurementToWorkbenchZ1 = GlobalParams.NonContactHeightMeasurementToWorkbenchZ1;
            //开始监控报警
            _ = StartMonitoringAlarmAsync(_monitoringAlarmCts.Token);
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                if (LunguSksj.BladeOuterDiameter <= 0)
                {
                    MaterialSnackUtils.MaterialSnack("轮毂信息刀片外径异常，请检查或手动输入！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                    return;
                }
                //PDA上机操作
                CommonResult computerPractice = await PdaUtils.ComputerPracticeAsync(LunguSksj.LunguId);
                if (!computerPractice.IsSuccess)
                {
                    MaterialSnackUtils.MaterialSnack(computerPractice.Message, MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                    return;
                }
                AutoCutHistoryUtils.SetStartTime();
                //检查预切割
                CommonResult<List<float>> cutListResult = await AutoCutUtils.GetCutListAsync(CutParams);
                if (!cutListResult.IsSuccess)
                {
                    MaterialSnackUtils.MaterialSnack($"切割序列获取失败，请检查切割参数配置！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                    return;
                }
                List<float> cutSpeedList = cutListResult.Data ?? [];
                PdaUtils.AddStandardCutSpeed(cutSpeedList.Last());
                PdaUtils.AddMaxCutSpeed(cutSpeedList.Last());
                // 测高的同时移动相机位置
                RunStatus = AutoRunStatus.HeightMeasurementInProgress;
                HeightMeasurementMode heightMeasurementMode = HeightMeasurementMode.Contact;
                // 设置测高参数
                await PlcControl.tagControl.bladeMantance.SetSetupParamsAsync(CurrentUtils.GetBladeHeightModel());
                await PlcControl.tagControl.bladeMantance.SetZAxisMaxDistanceAsync(AutoCutUtils.CaculateZAxisMaxDistance(LunguSksj.BladeOuterDiameter));
                Task zAxisTask = PlcControl.tagControl.Z2axis.StartAbsoluteAsync(Appsettings.FocusClearZ ?? 0, 1, _pauseCts.Token);
                Task<CommonResult<float>> measureHeightTask = AutoCutUtils.ProcessMeasureHeightAsync(heightMeasurementMode, _dialogService, _eventAggregator, _pauseCts.Token);
                await Task.WhenAll(zAxisTask, measureHeightTask);
                // 开始测高
                CommonResult<float> firstHeightMeasurementZ = measureHeightTask.Result;
                if (!firstHeightMeasurementZ.IsSuccess)
                {
                    MaterialSnackUtils.MaterialSnack(firstHeightMeasurementZ.Message, MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                    return;
                }
                AfterHeightMeasurementZ = firstHeightMeasurementZ.Data;
                //RunStatus = AutoRunStatus.AutoFocus;
                //对焦
                //await AutoCutUtils.GoPreCutLineAsync(_pauseCts.Token);
                //CommonResult<float> focusClearZ = await AutoCutUtils.AutoFocusAsync(default, _pauseCts.Token);
                //if (!focusClearZ.IsSuccess)
                //{
                //    MaterialSnackUtils.MaterialSnack(focusClearZ.Message, MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                //    return;
                //}
                //Appsettings.FocusClearZ = focusClearZ.Data;
                RunStatus = AutoRunStatus.SharpenCalibrat;
                // 磨刀校准
                float sharpenCalibratTheta = await AutoCutUtils.CalibratSharpenAsync(sharpenRect.Clone(), _pauseCts.Token);
                RunStatus = AutoRunStatus.CutingCalibrat;
                PdaUtils.AddStandardSharpenSpeed(SharpenParams.HightestCutSpeed);
                // 切割校准
                float cutCalibratTheta = await AutoCutUtils.CalibratCutAsync(new DataPoint<float>(cameraThetaCenterPoint.X, cameraThetaCenterPoint.Y + GlobalParams.CenterDistance), workpieceRadius, _pauseCts.Token);
                // 开始磨刀
                int sharpenTimes = SharpenParams.CutNum; // 默认磨刀次数
                if (sharpenTimes > 0)
                {
                    _sharpenService.SharpenServiceProcessChanged += SharpenService_SharpenServiceProcessChanged;
                    _sharpenService.RemindReplaceSharpenBoard += SharpenService_RemindReplaceSharpenBoard;
                    _sharpenService.SharpenServicePaused += SharpenService_SharpenServicePaused;
                    float singleBladeWear = 0.0002f;
                    RunStatus = AutoRunStatus.SharpeningInProgress;
                    float sharpenContactWorkingDiscZ1 = CalculateBladeContactWorkingDiscZ1(heightMeasurementMode, AfterHeightMeasurementZ, nonContactHeightMeasurementToWorkbenchZ1);
                    RunResult sharpenResult = await _sharpenService.Run(LunguSksj, SharpenParams, sharpenContactWorkingDiscZ1, bladeLiftingHeight, sharpenCalibratTheta, sharpenTimes, singleBladeWear, _pauseCts.Token);
                    if (!sharpenResult.IsSuccess)
                    {
                        MaterialSnackUtils.MaterialSnack($"磨刀失败：{sharpenResult.Message}", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                        return;
                    }
                    RunStatus = AutoRunStatus.HeightMeasurementInProgress;
                    CommonResult<float>? curHeightZ = null;
                    float wearAmount = 0;
                    for (int failTimes = 1; failTimes <= 5; failTimes++)
                    {
                        // 开始测高
                        curHeightZ = await AutoCutUtils.ProcessMeasureHeightAsync(heightMeasurementMode, _dialogService, _eventAggregator, _pauseCts.Token);
                        if (!curHeightZ.IsSuccess)
                        {
                            MaterialSnackUtils.MaterialSnack(curHeightZ.Message, MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                            return;
                        }
                        wearAmount = curHeightZ.Data - AfterHeightMeasurementZ;
                        _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"本次磨损量: {wearAmount}"));
                        // 如果磨损量小于0，说明测高数据有问题，继续测高
                        if (wearAmount > 0)
                        {
                            TotalWearAmount = MathF.Round(curHeightZ.Data - firstHeightMeasurementZ.Data, 4);
                            break;
                        }
                        curHeightZ = null;
                        _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("测高数据异常，重新测高"));
                    }
                    if (curHeightZ == null)
                    {
                        MaterialSnackUtils.MaterialSnack("测高失败，请检查设备！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                        return;
                    }
                    AfterHeightMeasurementZ = curHeightZ.Data;
                    singleBladeWear = TotalWearAmount / sharpenTimes;
                    _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"单刀磨损量: {singleBladeWear}"));
                    string drsmdj = await FullyAutoCutService.GetDrsmdjAsync(LunguSksj.LunguId, (float)Math.Round(singleBladeWear * 1000, 1));
                    PdaUtils.AddBladeLifeGrade(drsmdj);
                    PdaUtils.AddSharpen(wearAmount, sharpenTimes);
                    PdaUtils.AddResidueSharpenTimes(0);
                    PdaUtils.AddResidueBlade(LunguSksj.LongestBlade - (TotalWearAmount * 1000));
                    PdaUtils.AddTotalSharpenTimes(sharpenTimes);
                    AutoCutHistoryUtils.SetSharpen(wearAmount, sharpenTimes);
                }
                // 开始切割
                RunStatus = AutoRunStatus.CutingInProgress;
                if (cutSpeedList.Count > 0)
                {
                    _cutService.CutServiceProcessChanged += CutService_CutServiceProcessChanged;
                    _cutService.CutServicePaused += CutService_CutServicePaused;
                    _cutService.RemindReplaceWafer += CutService_RemindReplaceWafer;
                    float cutContactWorkingDiscZ1 = CalculateBladeContactWorkingDiscZ1(heightMeasurementMode, AfterHeightMeasurementZ, nonContactHeightMeasurementToWorkbenchZ1);
                    RunResult cutResult = await _cutService.Run(LunguSksj, cutSpeedList, CutParams, cutContactWorkingDiscZ1, bladeLiftingHeight, cutCalibratTheta, _eventAggregator, _pauseCts.Token);
                    if (!cutResult.IsSuccess)
                    {
                        MaterialSnackUtils.MaterialSnack($"切割失败：{cutResult.Message}", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                        return;
                    }
                }

                if (SharpenParams.IsExecuteLastSharpen)
                {
                    int defaultSharpenTimes = 10; // 默认磨刀次数
                    float singleBladeWear = 0.0002f;
                    // 开始磨刀
                    RunStatus = AutoRunStatus.SharpeningInProgress;
                    float sharpenContactWorkingDiscZ1 = CalculateBladeContactWorkingDiscZ1(heightMeasurementMode, AfterHeightMeasurementZ, nonContactHeightMeasurementToWorkbenchZ1);
                    RunResult sharpenResult = await _sharpenService.Run(LunguSksj, SharpenParams, sharpenContactWorkingDiscZ1, bladeLiftingHeight, sharpenCalibratTheta, defaultSharpenTimes, singleBladeWear, _pauseCts.Token);
                    if (!sharpenResult.IsSuccess)
                    {
                        MaterialSnackUtils.MaterialSnack($"磨刀失败：{sharpenResult.Message}", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                        return;
                    }

                    RunStatus = AutoRunStatus.HeightMeasurementInProgress;
                    CommonResult<float>? curHeightZ = null;
                    float wearAmount = 0;
                    for (int failTimes = 1; failTimes <= 5; failTimes++)
                    {
                        // 开始测高
                        curHeightZ = await AutoCutUtils.ProcessMeasureHeightAsync(heightMeasurementMode, _dialogService, _eventAggregator, _pauseCts.Token);
                        if (!curHeightZ.IsSuccess)
                        {
                            MaterialSnackUtils.MaterialSnack(curHeightZ.Message, MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                            return;
                        }
                        wearAmount = curHeightZ.Data - AfterHeightMeasurementZ;
                        _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"本次磨损量: {wearAmount}"));
                        // 如果磨损量小于0，说明测高数据有问题，继续测高
                        if (wearAmount > 0)
                        {
                            TotalWearAmount = MathF.Round(curHeightZ.Data - firstHeightMeasurementZ.Data, 4);
                            break;
                        }
                        curHeightZ = null;
                        _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("测高数据异常，重新测高"));
                    }
                    if (curHeightZ == null)
                    {
                        MaterialSnackUtils.MaterialSnack("测高失败，请检查设备！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                        return;
                    }
                    AfterHeightMeasurementZ = curHeightZ.Data;
                    PdaUtils.AddWearAmountAfterCut(wearAmount, defaultSharpenTimes);
                    AutoCutHistoryUtils.SetLastSharpen(wearAmount, defaultSharpenTimes);
                }

                stopwatch.Stop();
                TimeSpan timeSpan = TimeSpan.FromSeconds(stopwatch.Elapsed.TotalSeconds);
                string formattedTime = $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
                MaterialSnackUtils.MaterialSnack($"切割完成，请更换刀片！  总用时：{formattedTime}", MaterialSnackUtils.SnackType.SUCCESS, 0, _eventAggregator);
                await PlcControl.tagControl.wholeDevice.OpenBuzzerAsync();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                // 取消订阅事件
                _sharpenService.SharpenServiceProcessChanged -= SharpenService_SharpenServiceProcessChanged;
                _sharpenService.SharpenServicePaused -= SharpenService_SharpenServicePaused;
                _sharpenService.RemindReplaceSharpenBoard -= SharpenService_RemindReplaceSharpenBoard;
                _cutService.CutServiceProcessChanged -= CutService_CutServiceProcessChanged;
                _cutService.CutServicePaused -= CutService_CutServicePaused;
                _cutService.RemindReplaceWafer -= CutService_RemindReplaceWafer;
                _pauseCts.Cancel();
                _monitoringAlarmCts.Cancel();
                await StopAsync(ServicePauseResult.Stop);
                await PdaUtils.UpdateFlowValuesAsync();
                if (!stopwatch.IsRunning)
                {
                    await PdaUtils.QualifiedAsync();
                }
                else
                {
                    await PdaUtils.SetCompletedAsync();
                }
                stopwatch.Stop();
                AutoCutHistoryUtils.SetEndTime();
                RunStatus = AutoRunStatus.End;
            }
        }

        //private async void RunAutoCut_WearAmount()
        //{
        //    if (!GlobalParams.onlineFlag)
        //    {
        //        RunStatus = AutoRunStatus.SharpeningInProgress;
        //        return;
        //    }
        //    //清报警列表
        //    await PlcControl.tagControl.wholeDevice.AlarmResetAsync();
        //    //暂停页面跳转回来会触发InitCommand，调继续切割
        //    if (RunStatus == AutoRunStatus.SharpeningInProgress || RunStatus == AutoRunStatus.CutingInProgress)
        //    {
        //        await ContinueAsync();
        //        return;
        //    }
        //    //theta轴中心点位置
        //    DataPoint<float> thetaCenterPoint = GlobalParams.ThetaCenterPoint;
        //    //相机中心点位置
        //    DataPoint<float> cameraCenterPoint = GlobalParams.CameraCenterPoint;
        //    //相机相对theta轴中心点位置
        //    DataPoint<float> cameraRelativeBladePosition = Appsettings.CameraRelativeBladePosition;
        //    //工件半径
        //    float workpieceRadius = GlobalParams.WorkpieceRadius;
        //    //工件中心点到theta轴中心点距离
        //    float centerDistance = GlobalParams.CenterDistance;
        //    // 磨刀板尺寸
        //    DataRectangleF sharpenRect = GlobalParams.SharpenRect;
        //    //刀片切一刀后抬起高度
        //    float bladeLiftingHeight = GlobalParams.BladeLiftingHeight;
        //    //非接触测高位置到工作台的z1轴高度
        //    float nonContactHeightMeasurementToWorkbenchZ1 = GlobalParams.NonContactHeightMeasurementToWorkbenchZ1;
        //    //单刀磨损量
        //    float singleBladeWear = GlobalParams.SingleBladeWear;
        //    //开始监控报警
        //    Task monitorTask = StartMonitoringAlarmAsync(_monitoringAlarmCts.Token);
        //    try
        //    {
        //        //PDA上机操作
        //        CommonResult computerPractice = await PdaUtils.ComputerPracticeAsync(LunguSksj.LunguId);
        //        //PDA上机操作
        //        if (!computerPractice.IsSuccess)
        //        {
        //            MaterialSnackUtils.MaterialSnack(computerPractice.Message, MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
        //            return;
        //        }
        //        // 测高的同时移动相机位置
        //        RunStatus = AutoRunStatus.HeightMeasurementInProgress;
        //        HeightMeasurementMode heightMeasurementMode = HeightMeasurementMode.NoContact;
        //        Task zAxisTask = PlcControl.tagControl.Z2axis.StartAbsoluteAsync(Appsettings.FocusClearZ ?? 0, 1, _pauseCts.Token);
        //        Task<CommonResult<float>> measureHeightTask = AutoCutUtils.ProcessMeasureWearAmountAsync(heightMeasurementMode, true, _dialogService, _eventAggregator, _pauseCts.Token);
        //        await Task.WhenAll(zAxisTask, measureHeightTask);
        //        // 开始测高
        //        CommonResult<float> firstWearAmount = measureHeightTask.Result;
        //        if (!firstWearAmount.IsSuccess)
        //        {
        //            MaterialSnackUtils.MaterialSnack(firstWearAmount.Message, MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
        //            return;
        //        }
        //        AfterHeightMeasurementZ = GlobalParams.ZAxisZeroToWorkingDiscDistance - LunguSksj.BladeOuterDiameter / 2;
        //        RunStatus = AutoRunStatus.AutoFocus;
        //        //对焦
        //        await AutoCutUtils.WorkpieceBlowingAsync(_eventAggregator, _pauseCts.Token);
        //        await AutoCutUtils.GoPreCutLineAsync(_pauseCts.Token);
        //        CommonResult<float> focusClearZ = await AutoCutUtils.AutoFocusAsync(_eventAggregator, _pauseCts.Token);
        //        if (!focusClearZ.IsSuccess)
        //        {
        //            MaterialSnackUtils.MaterialSnack(focusClearZ.Message, MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
        //            return;
        //        }
        //        Appsettings.FocusClearZ = focusClearZ.Data;
        //        RunStatus = AutoRunStatus.SharpenCalibrat;
        //        // 磨刀校准
        //        float sharpenCalibratTheta = await AutoCutUtils.CalibratSharpenAsync(sharpenRect.Clone().Translate(cameraRelativeBladePosition.X, cameraRelativeBladePosition.Y), _pauseCts.Token);
        //        RunStatus = AutoRunStatus.CutingCalibrat;
        //        PdaUtils.AddStandardSharpenSpeed(SharpenParams.HightestCutSpeed);
        //        // 切割校准
        //        float cutCalibratTheta = await AutoCutUtils.CalibratCutAsync(new DataPoint<float>(cameraCenterPoint.X, cameraCenterPoint.Y + GlobalParams.CenterDistance), workpieceRadius, _pauseCts.Token);
        //        //新刀才磨刀
        //        if (LunguSksj.BladeType == "新刀")
        //        {
        //            _sharpenService.SharpenServiceProcessChanged += SharpenService_SharpenServiceProcessChanged;
        //            _sharpenService.RemindReplaceSharpenBoard += SharpenService_RemindReplaceSharpenBoard;
        //            _sharpenService.SharpenServicePaused += SharpenService_SharpenServicePaused;
        //            int defaultSharpenTimes = 10; // 默认磨刀次数
        //            var sharpenDatas = new List<(int sharpenTimes, float wearAmount)>(); // 记录每次磨刀的次数和磨损量
        //            bool isGetSingleBladeWear = true; // 是否获取单刀磨损量
        //            CommonResult<float>? curTotalWearAmount = null;
        //            while (true)
        //            {
        //                bool isEndSharpen;
        //                int sharpenTimes;
        //                if (isGetSingleBladeWear)
        //                {
        //                    isEndSharpen = false;
        //                    sharpenTimes = defaultSharpenTimes; // 默认磨刀次数
        //                }
        //                else
        //                {
        //                    var (times, isEnd) = CalculateSharpenTimesByWearAmount(LunguSksj, singleBladeWear, curTotalWearAmount?.Data);
        //                    isEndSharpen = isEnd;
        //                    sharpenTimes = times;
        //                }
        //                // 开始磨刀
        //                RunStatus = AutoRunStatus.SharpeningInProgress;
        //                RunResult sharpenResult = await _sharpenService.Run(LunguSksj, AfterHeightMeasurementZ, bladeLiftingHeight, SharpenParams.RotateSpeed, SharpenParams.CoOffsetX, sharpenCalibratTheta, sharpenTimes, _pauseCts.Token);
        //                if (!sharpenResult.IsSuccess)
        //                {
        //                    MaterialSnackUtils.MaterialSnack($"磨刀失败：{sharpenResult.Message}", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
        //                    return;
        //                }
        //                RunStatus = AutoRunStatus.HeightMeasurementInProgress;
        //                // 记录上次测高的磨损量
        //                float preTotalWearAmount = curTotalWearAmount?.Data ?? 0;
        //                float thisTimeWearAmount = 0;
        //                for (int failTimes = 1; failTimes <= 10; failTimes++)
        //                {
        //                    // 开始测高
        //                    curTotalWearAmount = await AutoCutUtils.ProcessMeasureWearAmountAsync(heightMeasurementMode, false, _dialogService, _eventAggregator, _pauseCts.Token);
        //                    if (!curTotalWearAmount.IsSuccess)
        //                    {
        //                        MaterialSnackUtils.MaterialSnack(curTotalWearAmount.Message, MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
        //                        return;
        //                    }
        //                    thisTimeWearAmount = curTotalWearAmount.Data - preTotalWearAmount;
        //                    _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"本次磨损量: {thisTimeWearAmount}"));
        //                    // 如果磨损量小于0，说明测高数据有问题，继续测高
        //                    if (thisTimeWearAmount > 0)
        //                    {
        //                        TotalWearAmount += thisTimeWearAmount;
        //                        break;
        //                    }
        //                    curTotalWearAmount = null;
        //                    if (failTimes % 3 == 0)
        //                    {
        //                        //测高多次失败，手动吹水
        //                        await AutoCutUtils.WaitManualBlowing(_dialogService, _pauseCts.Token);
        //                    }
        //                    _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("测高数据异常，重新测高"));
        //                }
        //                if (curTotalWearAmount == null)
        //                {
        //                    MaterialSnackUtils.MaterialSnack("测高失败次数过多，请检查设备！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
        //                    return;
        //                }
        //                AfterHeightMeasurementZ = AfterHeightMeasurementZ + thisTimeWearAmount;
        //                // 记录磨刀数据
        //                sharpenDatas.Add((sharpenTimes, thisTimeWearAmount));
        //                if (isGetSingleBladeWear)
        //                {
        //                    isGetSingleBladeWear = false;
        //                    singleBladeWear = TotalWearAmount / defaultSharpenTimes;
        //                    _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"单刀磨损量: {singleBladeWear}"));
        //                }
        //                if (isEndSharpen)
        //                {
        //                    string drsmdj = await CutService.GetDrsmdjAsync(LunguSksj.LunguId, singleBladeWear);
        //                    PdaUtils.AddBladeLifeGrade(drsmdj);
        //                    PdaUtils.AddWearAmountBeforeCircle(sharpenDatas.SkipLast(1).Sum(p => p.wearAmount));
        //                    PdaUtils.AddWearAmountAfterCircle(thisTimeWearAmount, sharpenTimes);
        //                    break;
        //                }
        //                //上传磨刀数据
        //                PdaUtils.AddSharpen(thisTimeWearAmount, sharpenTimes);
        //            }
        //            PdaUtils.AddResidueSharpenTimes(0);
        //            PdaUtils.AddResidueBlade(LunguSksj.LongestBlade - (TotalWearAmount * 1000));
        //            PdaUtils.AddTotalSharpenTimes(sharpenDatas.Sum(p => p.sharpenTimes));

        //            // 开始切割
        //            RunStatus = AutoRunStatus.CutingInProgress;
        //            _cutService.CutServiceProcessChanged += CutService_CutServiceProcessChanged;
        //            _cutService.CutServicePaused += CutService_CutServicePaused;
        //            _cutService.RemindReplaceWafer += CutService_RemindReplaceWafer;
        //            List<float>? cutSpeedList = await AutoCutUtils.GetCutListAsync(LunguSksj.LunguId, LunguSksj.LongestBlade - TotalWearAmount * 1000);
        //            if (cutSpeedList is null)
        //            {
        //                MaterialSnackUtils.MaterialSnack($"切割序列获取失败，请检查切割参数配置！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
        //                return;
        //            }
        //            PdaUtils.AddStandardCutSpeed(cutSpeedList.Last());
        //            PdaUtils.AddMaxCutSpeed(cutSpeedList.Last());
        //            RunResult cutResult = await _cutService.Run(LunguSksj, cutSpeedList, AfterHeightMeasurementZ, bladeLiftingHeight, CutParams.SpindleRev, CutParams.OffsetX, cutCalibratTheta, _eventAggregator, _pauseCts.Token);
        //            if (!cutResult.IsSuccess)
        //            {
        //                MaterialSnackUtils.MaterialSnack($"切割失败：{cutResult.Message}", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
        //                return;
        //            }
        //            MaterialSnackUtils.MaterialSnack($"切割完成！请更换刀片", MaterialSnackUtils.SnackType.SUCCESS, 0, _eventAggregator);
        //        }
        //        else
        //        {
        //            RunStatus = AutoRunStatus.CutingInProgress;
        //            _cutService.CutServiceProcessChanged += CutService_CutServiceProcessChanged;
        //            _cutService.CutServicePaused += CutService_CutServicePaused;
        //            _cutService.RemindReplaceWafer += CutService_RemindReplaceWafer;
        //            float cutContactWorkingDiscZ1 = CalculateBladeContactWorkingDiscZ1(heightMeasurementMode, AfterHeightMeasurementZ, nonContactHeightMeasurementToWorkbenchZ1);
        //            List<float> cutSpeedList = new List<float>() { 60 };
        //            PdaUtils.AddStandardCutSpeed(cutSpeedList.Last());
        //            PdaUtils.AddMaxCutSpeed(cutSpeedList.Last());
        //            RunResult cutResult = await _cutService.Run(LunguSksj, cutSpeedList, cutContactWorkingDiscZ1, bladeLiftingHeight, CutParams.SpindleRev, CutParams.OffsetX, cutCalibratTheta, _eventAggregator, _pauseCts.Token);
        //            if (!cutResult.IsSuccess)
        //            {
        //                MaterialSnackUtils.MaterialSnack($"切割失败：{cutResult.Message}", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
        //                return;
        //            }
        //            MaterialSnackUtils.MaterialSnack($"切割完成！请更换刀片", MaterialSnackUtils.SnackType.SUCCESS, 0, _eventAggregator);
        //            await PlcControl.tagControl.wholeDevice.OpenBuzzerAsync();
        //        }
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        return;
        //    }
        //    finally
        //    {
        //        // 取消订阅事件
        //        _sharpenService.SharpenServiceProcessChanged -= SharpenService_SharpenServiceProcessChanged;
        //        _sharpenService.SharpenServicePaused -= SharpenService_SharpenServicePaused;
        //        _sharpenService.RemindReplaceSharpenBoard -= SharpenService_RemindReplaceSharpenBoard;
        //        _cutService.CutServiceProcessChanged -= CutService_CutServiceProcessChanged;
        //        _cutService.CutServicePaused -= CutService_CutServicePaused;
        //        _cutService.RemindReplaceWafer -= CutService_RemindReplaceWafer;
        //        _pauseCts.Cancel();
        //        _monitoringAlarmCts.Cancel();
        //        await StopAsync(ServicePauseResult.Stop);
        //        await PdaUtils.UpdateFlowValuesAsync();
        //        await PdaUtils.SetCompletedAsync();
        //        //await PdaUtils.QualifiedAsync();
        //        RunStatus = AutoRunStatus.End;
        //    }
        //}

        private async void SharpenService_SharpenServicePaused(LineSegment? line)
        {
            await AfterPauseThenMoveToPosition(line, null);
        }

        private async void CutService_CutServicePaused(LineSegment? line, string? message)
        {
            await AfterPauseThenMoveToPosition(line, message);
        }

        private async Task AfterPauseThenMoveToPosition(LineSegment? line, string? message)
        {
            MaterialSnackUtils.MaterialSnack("正在暂停切割...", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
            int runTime = 60;
            _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"暂停超时时间：{runTime}"));
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(runTime)); // 超时自动取消
                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(cts.Token);
                switch (RunStatus)
                {
                    case AutoRunStatus.ReplaceWafer:
                        await AutoCutUtils.ReplaceWaferAsync(_eventAggregator, cts.Token);
                        break;
                    case AutoRunStatus.ReplaceSharpenBoard:
                        await AutoCutUtils.ReplaceSharpeningBoardAsync(_eventAggregator, cts.Token);
                        break;
                    default:
                        // 轴不报警时移动到指定位置
                        if (line != null)
                        {
                            // 执行默认动作
                            Task z1Task = PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0, default, cts.Token);
                            Task z2Task = PlcControl.tagControl.Z2axis.StartAbsoluteAsync(Appsettings.FocusClearZ ?? 0, default, cts.Token);
                            await Task.WhenAll(z1Task, z2Task);
                            await AutoCutUtils.WorkpieceBlowingAsync(_eventAggregator, cts.Token);
                            await PlcControl.tagControl.cutting.RunMotionAsync(((line.StartPoint.X + line.EndPoint.X) / 2).ToCameraX(), line.StartPoint.Y.ToCameraY(), cts.Token);
                        }
                        await AutoFocusService.GlobalFocusAsync(_eventAggregator, cts.Token);
                        await AutoCutUtils.FineTuneAxisYAsync();
                        await AutoCutUtils.UpdateCameraCommonLineAsync();
                        MaterialSnackUtils.MaterialSnack(message ?? "暂停中...", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                MaterialSnackUtils.MaterialSnack("暂停切割超时", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
            }
            catch (Exception ex)
            {
                MaterialSnackUtils.MaterialSnack($"暂停切割时遇到其他错误: {ex.Message}", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
            }
            finally
            {
                NavigationParameters parameters = new NavigationParameters { { nameof(AutoCutRuningViewModel), this } };
                _regionManager.RequestNavigate(RegionName.AutoCutStateRegion, nameof(AutoCutPausing), parameters);
            }
        }

        private void SharpenService_RemindReplaceSharpenBoard()
        {
            RunStatus = AutoRunStatus.ReplaceSharpenBoard;
        }

        private void CutService_RemindReplaceWafer()
        {
            RunStatus = AutoRunStatus.ReplaceWafer;
        }

        public async Task StartMonitoringAlarmAsync(CancellationToken token)
        {
            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
                while (await timer.WaitForNextTickAsync(token))
                {
                    try
                    {
                        if (AlarmConfig.Instance.HasAutoRunUnexpectedAlarms())
                        {
                            if (!_pauseCts.IsCancellationRequested)
                            {
                                Pause();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"监控任务内异常: {ex.Message}"));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消，无需处理
            }
            catch (Exception ex)
            {
                _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"监控异常: {ex.Message}"));
            }
        }

        private bool _isForcedPause = false;

        private void Pause()
        {
            if (!GlobalParams.onlineFlag)
            {
                NavigationParameters parameters = new NavigationParameters { { "AutoCutRuningViewModel", this } };
                _regionManager.RequestNavigate(RegionName.AutoCutStateRegion, nameof(AutoCutPausing), parameters);
                return;
            }
            if (_pauseCts.IsCancellationRequested)
            {
                _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("操作频繁！"));
                return;
            }
            if (RunStatus != AutoRunStatus.CutingInProgress && RunStatus != AutoRunStatus.SharpeningInProgress)
            {
                if (!_isForcedPause)
                {
                    _isForcedPause = true;
                    MaterialSnackUtils.MaterialSnack("当前状态不能暂停，再次点击暂停将退出自动执行！", MaterialSnackUtils.SnackType.WARNING, 5, _eventAggregator);
                    return;
                }
            }
            _isForcedPause = false;
            // 暂停token
            _pauseCts.Cancel();
        }

        private async void Continue()
        {
            MaterialSnackUtils.MaterialSnack("正在继续切割...", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
            _pauseCts = new CancellationTokenSource();
            await PlcControl.tagControl.cutting.EnterCuttingModeAsync(_pauseCts.Token);
            _sharpenService.Continue(_pauseCts.Token);
            _cutService.Continue(_pauseCts.Token);
            if (RunStatus == AutoRunStatus.ReplaceSharpenBoard)
            {
                RunStatus = AutoRunStatus.SharpeningInProgress;
            }
            else if (RunStatus == AutoRunStatus.ReplaceWafer)
            {
                RunStatus = AutoRunStatus.CutingInProgress;
            }
            else
            {
                UpdateMaterialSnack();
            }
        }

        public async Task StopAsync(ServicePauseResult pauseResult)
        {
            if (!GlobalParams.onlineFlag)
            {
                _regionManager.RequestNavigate(RegionName.MainRegion, nameof(BladeReplacementConfiguration));
                return;
            }
            //中止监控报警线程
            _monitoringAlarmCts.Cancel();
            _sharpenService.Stop();
            _cutService.Stop(pauseResult);
            if (RunStatus == AutoRunStatus.SharpeningInProgress || RunStatus == AutoRunStatus.CutingInProgress)
            {
                //结束切割
                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(default);
            }
            else
            {
                if (RunStatus == AutoRunStatus.HeightMeasurementInProgress)
                {
                    //结束测高
                    await PlcControl.tagControl.bladeMantance.HeightMeasurementEarlyEndAsync();
                    //等待完成测高信号
                    await PlcControl.tagControl.bladeMantance.WaitHeightMeasurementCompletedAsync(default);
                }
            }
            _regionManager.RequestNavigate(RegionName.MainRegion, nameof(BladeReplacementConfiguration));
        }

        private void UpdateMaterialSnack()
        {
            switch (_autoRunStatus)
            {
                case AutoRunStatus.HeightMeasurementInProgress:
                    MaterialSnackUtils.MaterialSnack("测高进行中...", MaterialSnackUtils.SnackType.SUCCESS, 0, _eventAggregator);
                    break;
                case AutoRunStatus.AutoFocus:
                    MaterialSnackUtils.MaterialSnack("自动聚焦中....", MaterialSnackUtils.SnackType.SUCCESS, 0, _eventAggregator);
                    break;
                case AutoRunStatus.SharpenCalibrat:
                    MaterialSnackUtils.MaterialSnack("磨刀校准中...", MaterialSnackUtils.SnackType.SUCCESS, 0, _eventAggregator);
                    break;
                case AutoRunStatus.CutingCalibrat:
                    MaterialSnackUtils.MaterialSnack("切割校准中...", MaterialSnackUtils.SnackType.SUCCESS, 0, _eventAggregator);
                    break;
                case AutoRunStatus.SharpeningInProgress:
                    MaterialSnackUtils.MaterialSnack("磨刀进行中...", MaterialSnackUtils.SnackType.SUCCESS, 0, _eventAggregator);
                    break;
                case AutoRunStatus.CutingInProgress:
                    MaterialSnackUtils.MaterialSnack("切割进行中...", MaterialSnackUtils.SnackType.SUCCESS, 0, _eventAggregator);
                    break;
                case AutoRunStatus.ReplaceSharpenBoard:
                    MaterialSnackUtils.MaterialSnack("替换磨刀板...", MaterialSnackUtils.SnackType.SUCCESS, 0, _eventAggregator);
                    break;
                case AutoRunStatus.ReplaceWafer:
                    MaterialSnackUtils.MaterialSnack("替换硅片...", MaterialSnackUtils.SnackType.SUCCESS, 0, _eventAggregator);
                    break;
                default:
                    //MaterialSnackUtils.MaterialSnack("未知状态", MaterialSnackUtils.SnackType.WARNING, 0);
                    break;
            }
        }

        private void CutService_CutServiceProcessChanged(CutServiceProcess process)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CutSpeed = process.CutSpeed;
                CutProgress = string.Format("{0}/{1}", process.CutTimes, process.TotalCutTimes);
                if (process.IsCompleted)
                {
                    AfterReplaceBladeCutTimes++;
                }
            });
        }

        private void SharpenService_SharpenServiceProcessChanged(SharpenServiceProcess process)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SharpenSpeed = process.SharpenSpeed;
                SharpenProgress = string.Format("{0}/{1}", process.CurSharpenTimes, process.TotalSharpenTimes);
                if (process.IsCompleted)
                {
                    AfterReplaceBladeCutTimes++;
                }
            });
        }

        private float CalculateBladeContactWorkingDiscZ1(HeightMeasurementMode mode, float afterHeightMeasurementZ, float nonContactHeightMeasurementToWorkbenchZ1)
        {
            return mode == HeightMeasurementMode.Contact ? afterHeightMeasurementZ : afterHeightMeasurementZ - nonContactHeightMeasurementToWorkbenchZ1; ;
        }

        private (int sharpenTimes, bool isEndSharpen) CalculateSharpenTimes(LunguSksjModel lunguSksj, float singleBladeWear, float firstHeightMeasurementZ, float? curHeightZ = null)
        {
            float needWearAmount = 2 * lunguSksj.Ymhtxd / 1000;
            float totalWearAmount = 0;
            if (curHeightZ != null)
            {
                totalWearAmount = Math.Abs(curHeightZ.Value - firstHeightMeasurementZ);
                needWearAmount -= totalWearAmount;
            }
            // 是否磨完真圆2a
            if (needWearAmount <= 0)
            {
                // 是否满足长宽比
                if ((lunguSksj.LongestBlade - totalWearAmount * 1000) / lunguSksj.ABAverageThickness <= 28)
                {
                    return (10, true);
                }
                else
                {
                    return ((int)Math.Ceiling(((lunguSksj.LongestBlade - lunguSksj.ABAverageThickness * 28) / 1000 - totalWearAmount) / singleBladeWear) + 5, false);
                }
            }
            else
            {
                //算出的磨刀数的基础上加5次，防止磨刀次数过少
                return ((int)Math.Ceiling(needWearAmount / singleBladeWear) + 5, false);
            }
        }

        private (int sharpenTimes, bool isEndSharpen) CalculateSharpenTimesByWearAmount(LunguSksjModel lunguSksj, float singleBladeWear, float? wearAmount)
        {
            float needWearAmount = 2 * lunguSksj.Ymhtxd / 1000;
            float totalWearAmount = wearAmount is null ? 0 : wearAmount.Value;
            needWearAmount -= totalWearAmount;
            // 是否磨完真圆2a
            if (needWearAmount <= 0)
            {
                // 是否满足长宽比
                if ((lunguSksj.LongestBlade - totalWearAmount) / lunguSksj.ABAverageThickness <= 28)
                {
                    return (10, true);
                }
                else
                {
                    return ((int)Math.Ceiling(lunguSksj.LongestBlade - lunguSksj.ABAverageThickness * 28 - totalWearAmount / singleBladeWear + 5) + 5, false);
                }
            }
            else
            {
                //算出的磨刀数的基础上加5次，防止磨刀次数过少
                return ((int)Math.Ceiling(needWearAmount / singleBladeWear) + 5, false);
            }
        }

        private void ReceivedAutoRuningMessage(MessageModel message)
        {
            Tools.LogDebug(message.Message); 
            MessageList.Add(message);
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            InitRightButton();
            LunguSksj = navigationContext.Parameters.GetValue<LunguSksjModel>("LunguSksj");
            SharpenParams = navigationContext.Parameters.GetValue<SharpenParamsModel>("SharpenParams");
            SharpenBladeHeight = MathF.Round(SharpenParams.CutHeight, 3);
            CutParams = navigationContext.Parameters.GetValue<CutParamsModel>("CutParams");
            CutBladeHeight = MathF.Round(CutParams.CutHeight, 3);
            _eventAggregator.GetEvent<AutoRuningMessageEvent>().Subscribe(ReceivedAutoRuningMessage, ThreadOption.UIThread);
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
            _eventAggregator.GetEvent<AutoRuningMessageEvent>().Unsubscribe(ReceivedAutoRuningMessage);
        }

        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return !_pauseCts.IsCancellationRequested || !_monitoringAlarmCts.IsCancellationRequested;
        }
    }
}
