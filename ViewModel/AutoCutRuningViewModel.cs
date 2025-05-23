using Newtonsoft.Json.Linq;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using 精密切割系统.Driver;
using 精密切割系统.DTOs;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.HttpClients;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.plc;
using 精密切割系统.PubSubEvent;
using 精密切割系统.Utils;
using 精密切割系统.View.common;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.View.Pages.F4_BladeMaintenance;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.ViewModel
{
    public class AutoCutRuningViewModel : CustomBindableBase
    {
        public RelayCommand InitCommand { get; set; }
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly SharpenService _sharpenService;
        private readonly CutService _cutService;
        private CancellationTokenSource _pauseCts;
        private CancellationTokenSource _monitoringAlarmCts;
        // 控制右侧按钮
        public ObservableCollection<RightButtonParams> RightButtonParamsCollection { get; set; }
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
        public string LunguId { get; set; }

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

        private static int _afterReplaceBladeCutTimes;
        /// <summary>
        /// 自更换刀片起刀片切了几道
        /// </summary>
        public int AfterReplaceBladeCutTimes
        {
            get { return _afterReplaceBladeCutTimes; }
            set { _afterReplaceBladeCutTimes = value; RaisePropertyChanged(); }
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

        public AutoCutRuningViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            RightButtonParamsCollection = WindowLayout.RightPageButtons;
            MessageList = new ObservableCollection<MessageModel>();
            _sharpenService = SharpenService.Instance;
            _cutService = CutService.Instance;
            _pauseCts = new CancellationTokenSource();
            _monitoringAlarmCts = new CancellationTokenSource();
            InitCommand = new RelayCommand(Init);
            _eventAggregator.GetEvent<AutoRuningMessageEvent>().Subscribe(message =>
            {
                Tools.LogDebug(message.Message);
                Application.Current.Dispatcher.Invoke(() => MessageList.Add(message));
            });
        }

        public AutoCutRuningViewModel()
        {
        }

        private void InitRightButton()
        {
            RightButtonParamsCollection.Add(RightButtonParams.YelloRightButton("暂停", "/Assets/icon/right/stop.png", Pause));
        }

        private async void Init()
        {
            if (!GlobalParams.onlineFlag)
            {
                RunStatus = AutoRunStatus.SharpeningInProgress;
                return;
            }
            //暂停页面跳转回来会触发InitCommand，调继续切割
            if (_pauseCts.IsCancellationRequested && !_monitoringAlarmCts.IsCancellationRequested)
            {
                await ContinueAsync();
                return;
            }
            //theta轴中心点位置
            DataPoint<float> thetaCenterPoint = GlobalParams.ThetaCenterPoint;
            //相机中心点位置
            DataPoint<float> cameraCenterPoint = GlobalParams.CameraCenterPoint;
            //相机相对theta轴中心点位置
            DataPoint<float> cameraRelativeBladePosition = Appsettings.CameraRelativeBladePosition;
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
            //单刀磨损量
            float singleBladeWear = GlobalParams.SingleBladeWear;
            //开始监控报警
            Task monitorTask = StartMonitoringAlarmAsync(_monitoringAlarmCts.Token);
            try
            {
                //PDA上机操作
                bool isSuccess = await PdaUtils.ComputerPracticeAsync(LunguId);
                if (!isSuccess)
                {
                    MaterialSnackUtils.MaterialSnack("上机失败！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                    return;
                }
                LunguSksjDTO? lunguSksj = new LunguSksjDTO();
                //LunguSksjDTO? lunguSksj = await HttpUtils.GetLunguSksjAsync(LunguId);
                //if (lunguSksj == null)
                //{
                //    MaterialSnackUtils.MaterialSnack("轮毂信息获取错误！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                //    return;
                //}
                RunStatus = AutoRunStatus.HeightMeasurementInProgress;
                HeightMeasurementMode heightMeasurementMode = HeightMeasurementMode.NoContact;
                // 开始测高
                float? firstHeightMeasurementZ = await AutoCutUtils.ProcessMeasureHeightAsync(heightMeasurementMode, _pauseCts.Token, _eventAggregator);
                if (firstHeightMeasurementZ == null)
                {
                    MaterialSnackUtils.MaterialSnack("测高失败，没有测高数据！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                    return;
                }
                AfterHeightMeasurementZ = firstHeightMeasurementZ.Value;
                RunStatus = AutoRunStatus.AutoFocus;
                await AutoCutUtils.WorkpieceBlowingAsync(_eventAggregator, _pauseCts.Token);
                //对焦
                await PlcControl.tagControl.cutting.RunMotionAsync(cameraCenterPoint.X, cameraCenterPoint.Y + 20, _pauseCts.Token);
                float? focusClearZ = await AutoCutUtils.AutoFocusAsync(_eventAggregator, _pauseCts.Token);
                if (focusClearZ == null)
                {
                    MaterialSnackUtils.MaterialSnack("对焦失败！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                    return;
                }
                Appsettings.FocusClearZ = focusClearZ.Value;
                RunStatus = AutoRunStatus.SharpenCalibrat;
                // 磨刀校准
                float sharpenCalibratTheta = await AutoCutUtils.CalibratSharpenAsync(sharpenRect.Clone().Translate(cameraRelativeBladePosition.X, cameraRelativeBladePosition.Y), _pauseCts.Token);
                RunStatus = AutoRunStatus.CutingCalibrat;
                // 切割校准
                float cutCalibratTheta = await AutoCutUtils.CalibratCutAsync(new DataPoint<float>(cameraCenterPoint.X, cameraCenterPoint.Y + GlobalParams.CenterDistance), workpieceRadius, _pauseCts.Token);

                _sharpenService.SharpenServiceProcessChanged += SharpenService_SharpenServiceProcessChanged;
                _sharpenService.RemindReplaceSharpenBoard += SharpenService_RemindReplaceSharpenBoard;
                _sharpenService.SharpenServicePaused += SharpenService_SharpenServicePaused;
                //int sharpenTimes = CalculateSharpenTimes(lunguSksj, singleBladeWear);
                int sharpenTimes = 3;
                float? curHeightZ;
                while (true)
                {
                    RunStatus = AutoRunStatus.SharpeningInProgress;
                    float sharpenContactWorkingDiscZ1 = CalculateBladeContactWorkingDiscZ1(heightMeasurementMode, AfterHeightMeasurementZ, nonContactHeightMeasurementToWorkbenchZ1);
                    // 开始磨刀
                    RunResult sharpenResult = await _sharpenService.Run(lunguSksj, sharpenContactWorkingDiscZ1, bladeLiftingHeight, SharpenParams.RotateSpeed, SharpenParams.CoOffsetX, sharpenCalibratTheta, sharpenTimes, _pauseCts.Token);
                    if (!sharpenResult.IsSuccess)
                    {
                        if (sharpenResult.Type == RunExceptionType.BladeScrap)
                        {
                            //提示更换刀片
                            MaterialSnackUtils.MaterialSnack($"刀片报废，请更换刀片！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                        }
                        else if (sharpenResult.Type != RunExceptionType.Stop)
                        {
                            //提示磨刀错误
                            MaterialSnackUtils.MaterialSnack($"磨刀失败：{sharpenResult.Message}", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                        }
                        return;
                    }

                    RunStatus = AutoRunStatus.HeightMeasurementInProgress;
                    // 开始测高
                    curHeightZ = await AutoCutUtils.ProcessMeasureHeightAsync(heightMeasurementMode, _pauseCts.Token, _eventAggregator);
                    if (curHeightZ == null)
                    {
                        MaterialSnackUtils.MaterialSnack("测高失败，没有测高数据！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                        return;
                    }
                    TotalWearAmount = curHeightZ.Value - firstHeightMeasurementZ.Value;
                    //上传磨刀数据到MES
                    PdaUtils.AddSharpen(curHeightZ.Value - AfterHeightMeasurementZ, sharpenTimes);
                    AfterHeightMeasurementZ = curHeightZ.Value;
                    if (AutoCutUtils.CheckIsMeetsCuttingConditions(lunguSksj, firstHeightMeasurementZ.Value, curHeightZ.Value))
                    {
                        MaterialSnackUtils.MaterialSnack("刀片满足进入切割的条件！", MaterialSnackUtils.SnackType.SUCCESS, 0, _eventAggregator);
                        await Task.Delay(1000);
                        break;
                    }
                    sharpenTimes = CalculateSharpenTimes(lunguSksj, singleBladeWear, firstHeightMeasurementZ.Value, curHeightZ.Value);
                }
                RunStatus = AutoRunStatus.CutingInProgress;
                _cutService.CutServiceProcessChanged += CutService_CutServiceProcessChanged;
                _cutService.CutServicePaused += CutService_CutServicePaused;
                float cutContactWorkingDiscZ1 = CalculateBladeContactWorkingDiscZ1(heightMeasurementMode, AfterHeightMeasurementZ, nonContactHeightMeasurementToWorkbenchZ1);
                //开始切割
                RunResult cutResult = await _cutService.Run(lunguSksj, cutContactWorkingDiscZ1, bladeLiftingHeight, CutParams.CutNum, CutParams.SpindleRev, CutParams.OffsetX, cutCalibratTheta, _eventAggregator, _pauseCts.Token);
                if (!cutResult.IsSuccess)
                {
                    if (cutResult.Type == RunExceptionType.BladeScrap)
                    {
                        //提示更换刀片
                        MaterialSnackUtils.MaterialSnack($"刀片报废，请更换刀片！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                    }
                    else if (cutResult.Type != RunExceptionType.Stop)
                    {
                        //提示磨刀错误
                        MaterialSnackUtils.MaterialSnack($"切割失败：{cutResult.Message}", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
                    }
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                // 取消订阅事件
                _sharpenService.SharpenServiceProcessChanged -= SharpenService_SharpenServiceProcessChanged;
                _sharpenService.RemindReplaceSharpenBoard -= SharpenService_RemindReplaceSharpenBoard;
                _sharpenService.SharpenServicePaused -= SharpenService_SharpenServicePaused;
                _cutService.CutServiceProcessChanged -= CutService_CutServiceProcessChanged;
                _cutService.CutServicePaused -= CutService_CutServicePaused;
                _pauseCts.Cancel();
                _monitoringAlarmCts.Cancel();
                await StopAsync();
                await PdaUtils.UpdateFlowValues();
            }
        }

        private async void SharpenService_SharpenServicePaused(LineSegment? line)
        {
            await PauseThenMoveToPosition(line);
        }

        private async void CutService_CutServicePaused(LineSegment? line)
        {
            await PauseThenMoveToPosition(line);
        }

        private async Task PauseThenMoveToPosition(LineSegment? line)
        {
            int runTime = 20;
            _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create($"暂停超时时间：{runTime}"));
            try
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(runTime)); // 超时自动取消
                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(cts.Token);
                if (line != null)
                {
                    var offsetPos = Appsettings.CameraRelativeBladePosition;
                    await PlcControl.tagControl.cutting.RunMotionAsync((line.StartPoint.X + line.EndPoint.X) / 2 + offsetPos.X, line.StartPoint.Y + offsetPos.Y, default);
                    await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(Appsettings.FocusClearZ ?? 0);
                }
                MaterialSnackUtils.MaterialSnack("已暂停切割", MaterialSnackUtils.SnackType.SUCCESS, 0, _eventAggregator);
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
                NavigationParameters parameters = new NavigationParameters { { "AutoCutRuningViewModel", this } };
                _regionManager.RequestNavigate(RegionName.MainRegion, nameof(AutoCutPausing), parameters);
            }
        }

        private void SharpenService_RemindReplaceSharpenBoard()
        {
            Pause();
            MaterialSnackUtils.MaterialSnack("请更换磨刀板！", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
        }

        public async Task StartMonitoringAlarmAsync(CancellationToken token)
        {
            try
            {
                await MonitoringAlarmAsync(token);
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

        private async Task MonitoringAlarmAsync(CancellationToken token)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
            while (await timer.WaitForNextTickAsync(token))
            {
                try
                {
                    if (AlarmConfig.Instance.HasActiveErrorAlarm())
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

        private void Pause()
        {
            if (!GlobalParams.onlineFlag)
            {
                NavigationParameters parameters = new NavigationParameters{{ "AutoCutRuningViewModel", this } };
                _regionManager.RequestNavigate(RegionName.MainRegion, nameof(AutoCutPausing), parameters);
                return;
            }
            if (_pauseCts.IsCancellationRequested)
            {
                _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("操作频繁！"));
                return;
            }
            MaterialSnackUtils.MaterialSnack("正在暂停切割...", MaterialSnackUtils.SnackType.WARNING, 0, _eventAggregator);
            // 暂停token
            _pauseCts.Cancel();
            if (RunStatus != AutoRunStatus.CutingInProgress && RunStatus != AutoRunStatus.SharpeningInProgress)
            {
                _eventAggregator?.GetEvent<AutoRuningMessageEvent>().Publish(MessageModel.Create("自动切割结束，当前状态不允许暂停！"));
            }
        }

        private async Task ContinueAsync()
        {
            _pauseCts = new CancellationTokenSource();
            _sharpenService.Continue(_pauseCts.Token);
            _cutService.Continue(_pauseCts.Token);
            await PlcControl.tagControl.cutting.StartCutAsync();
            UpdateMaterialSnack();
        }

        public async Task StopAsync()
        {
            if (!GlobalParams.onlineFlag)
            {

                _regionManager.RequestNavigate(RegionName.MainRegion, nameof(BladeReplacementConfiguration));
                return;
            }
            //中止监控报警线程
            _monitoringAlarmCts.Cancel();
            _sharpenService.Stop();
            _cutService.Stop();
            if (RunStatus == AutoRunStatus.HeightMeasurementInProgress)
            {
                //结束测高
                await PlcControl.tagControl.bladeMantance.HeightMeasurementEarlyEndAsync();
                //等待完成测高信号
                await PlcControl.tagControl.bladeMantance.WaitHeightMeasurementCompletedAsync(default);
            }
            else if (RunStatus == AutoRunStatus.SharpeningInProgress || RunStatus == AutoRunStatus.CutingInProgress)
            {
                //结束切割
                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(default);
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
                default:
                    //MaterialSnackUtils.MaterialSnack("未知状态", MaterialSnackUtils.SnackType.WARNING, 0);
                    break;
            }
        }

        private void CutService_CutServiceProcessChanged(CutServiceProcess process)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CutBladeHeight = process.CutBladeHeight;
                CutSpeed = process.CutSpeed;
                CutProgress = string.Format("{0}/{1}", process.CurCutTimes, process.TotalCutTimes);
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
                SharpenBladeHeight = process.SharpenBladeHeight;
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

        private int CalculateSharpenTimes(LunguSksjDTO lunguSksj, float singleBladeWear, float? firstHeightMeasurementZ = null, float? curHeightZ = null)
        {
            float abAverageThickness = lunguSksj.ABAverageThickness / 1000;
            float bladeLength = lunguSksj.LongestBlade / 1000;
            if (firstHeightMeasurementZ != null && curHeightZ != null)
            {
                float wearAmount = Math.Abs(curHeightZ.Value - firstHeightMeasurementZ.Value);
                bladeLength -= wearAmount;
            }
            float bladeExposedMax = AutoCutUtils.GetBladeExposedMax(abAverageThickness);
            return AutoCutUtils.GetNeedSharpenTimes(bladeLength, bladeExposedMax, singleBladeWear);
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            InitRightButton();
            LunguId = navigationContext.Parameters.GetValue<string>("LunguId");
            SharpenParams = navigationContext.Parameters.GetValue<SharpenParamsModel>("SharpenParams");
            CutParams = navigationContext.Parameters.GetValue<CutParamsModel>("CutParams");
            _deviceDataNo = CutParams.DeviceDataNo;
        }

        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return !_pauseCts.IsCancellationRequested || !_monitoringAlarmCts.IsCancellationRequested;
        }
    }
}
