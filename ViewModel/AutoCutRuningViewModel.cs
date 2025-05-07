using Newtonsoft.Json.Linq;
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
using System.Windows.Media.Media3D;
using 精密切割系统.Driver;
using 精密切割系统.DTOs;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.HttpClients;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.ViewModel
{
    public class AutoCutRuningViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public RelayCommand InitCommand { get; set; }

        private SharpenService _sharpenService;
        private CutService _cutService;
        private CancellationTokenSource _pauseCts;
        private CancellationTokenSource _monitoringAlarmCts;
        // 控制右侧按钮
        public ObservableCollection<RightButtonParams> RightButtonParamsCollection;

        /// <summary>
        /// 磨刀参数
        /// </summary>
        private SharpenParamsModel _sharpenParams;

        /// <summary>
        /// 切割参数
        /// </summary>
        private CutParamsModel _cutParams;

        private float _afterHeightMeasurementZ;
        /// <summary>
        /// 测高位置
        /// </summary>
        public float AfterHeightMeasurementZ
        {
            get { return _afterHeightMeasurementZ; }
            set { _afterHeightMeasurementZ = value; OnPropertyChanged(); }
        }

        private float _sharpenBladeHeight;
        /// <summary>
        /// 磨刀片高度
        /// </summary>
        public float SharpenBladeHeight
        {
            get { return _sharpenBladeHeight; }
            set { _sharpenBladeHeight = value; OnPropertyChanged(); }
        }

        private float _sharpenSpeed;
        /// <summary>
        /// 磨刀速度
        /// </summary>
        public float SharpenSpeed
        {
            get { return _sharpenSpeed; }
            set { _sharpenSpeed = value; OnPropertyChanged(); }
        }

        private string _sharpenProgress;
        /// <summary>
        /// 磨刀进度
        /// </summary>
        public string SharpenProgress
        {
            get { return _sharpenProgress; }
            set { _sharpenProgress = value; OnPropertyChanged(); }
        }

        private string _deviceDataNo;
        /// <summary>
        /// 型号参数No
        /// </summary>
        public string DeviceDataNo
        {
            get { return _deviceDataNo; }
            set { _deviceDataNo = value; OnPropertyChanged(); }
        }

        private float _cutBladeHeight;
        /// <summary>
        /// 切割刀片高度
        /// </summary>
        public float CutBladeHeight
        {
            get { return _cutBladeHeight; }
            set { _cutBladeHeight = value; OnPropertyChanged(); }
        }

        private float _cutSpeed;
        /// <summary>
        /// 磨刀速度
        /// </summary>
        public float CutSpeed
        {
            get { return _cutSpeed; }
            set { _cutSpeed = value; OnPropertyChanged(); }
        }

        private string _cutProgress;
        /// <summary>
        /// 切割进度
        /// </summary>
        public string CutProgress
        {
            get { return _cutProgress; }
            set { _cutProgress = value; OnPropertyChanged(); }
        }

        private static int _afterReplaceBladeCutTimes;
        /// <summary>
        /// 自更换刀片起刀片切了几道
        /// </summary>
        public int AfterReplaceBladeCutTimes
        {
            get { return _afterReplaceBladeCutTimes; }
            set { _afterReplaceBladeCutTimes = value; OnPropertyChanged(); }
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

        public AutoCutRuningViewModel(SharpenParamsModel sharpenParams, CutParamsModel cutParams)
        {
            RightButtonParamsCollection = RightPageViewModel.RightButtonParams;
            InitRightButton();
            _sharpenParams = sharpenParams;
            _cutParams = cutParams;
            _deviceDataNo = _cutParams.DeviceDataNo;
            _sharpenService = SharpenService.Instance;
            _cutService = CutService.Instance;
            _pauseCts = new CancellationTokenSource();
            _monitoringAlarmCts = new CancellationTokenSource();
            InitCommand = new RelayCommand(Init);
        }

        public AutoCutRuningViewModel() { }

        private void InitRightButton()
        {
            RightButtonParamsCollection.Clear();
            RightButtonParamsCollection.Add(RightButtonParams.YelloRightButton("暂停", "/Assets/icon/right/stop.png", async () => { await PauseAsync(); }));
        }

        private async void Init()
        {
            if (!GlobalParams.onlineFlag)
            {
                InitRightButton();
                RunStatus = AutoRunStatus.SharpeningInProgress;
                return;
            }
            //暂停页面跳转回来会触发InitCommand，调继续切割
            if (_pauseCts.IsCancellationRequested)
            {
                InitRightButton();
                await ContinueAsync();
                return;
            }
            //PDA上机操作
            bool tuple = await HttpUtils.ComputerPracticeAsync();
            if (!tuple)
            {
                MaterialSnackUtils.MaterialSnack("上机失败！", MaterialSnackUtils.SnackType.WARNING, 0);
                return;
            }
            HttpUtils.AddSharpen(9, 90, 3.5f, 7);
            HttpUtils.AddToolMarks("11", "22", "33", "C:\\Users\\17632\\Desktop\\image\\screenshot-20250421-212840.png",
                "C:\\Users\\17632\\Desktop\\image\\008ijAxUly1hx0v35k265j33b04eox6s.jpg", "0.5", "0.5");
            await HttpUtils.UpdateFlowValues();
            //theta轴中心点位置
            DataPoint<float> thetaCenterPoint = GlobalParams.ThetaCenterPoint;
            //相机中心点位置
            DataPoint<float> cameraCenterPoint = GlobalParams.CameraCenterPoint;
            //相机相对theta轴中心点位置
            DataPoint<float> cameraRelativeBladePosition = GlobalParams.CameraRelativeBladePosition;
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
            Task monitorTask = StartMonitoringAlarmAsync(_monitoringAlarmCts.Token);
            try
            {
                HeightMeasurementMode heightMeasurementMode = HeightMeasurementMode.NoContact;
                string lunguId = CameraUtils.GetLunguId();
                LunguSksjDTO? lunguSksj = await HttpUtils.GetLunguSksjAsync(lunguId);
                if (lunguSksj == null)
                {
                    MaterialSnackUtils.MaterialSnack("轮毂信息获取错误！", MaterialSnackUtils.SnackType.WARNING, 0);
                    return;
                }
                RunStatus = AutoRunStatus.HeightMeasurementInProgress;
                // 开始测高
                float? firstHeightMeasurementZ = await AutoCutUtils.ProcessMeasureHeightAsync(heightMeasurementMode, _pauseCts.Token);
                //float? firstHeightMeasurementZ = 10;
                if (firstHeightMeasurementZ == null)
                {
                    MaterialSnackUtils.MaterialSnack("测高失败！", MaterialSnackUtils.SnackType.WARNING, 0);
                    return;
                }
                AfterHeightMeasurementZ = firstHeightMeasurementZ.Value;
                RunStatus = AutoRunStatus.AutoFocus;
                //对焦
                await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(10, GlobalParams.CameraCenterPoint.X, _pauseCts.Token);
                await PlcControl.tagControl.Yaxis.StartAbsoluteAsync(10, GlobalParams.CameraCenterPoint.Y + 20, _pauseCts.Token);
                await AutoCutUtils.AutoFocusAsync(_pauseCts.Token);
                float? focusClearZ = await PlcControl.tagControl.Z2axis.GetCurrentLocationAsync();
                //float? focusClearZ = 20;

                RunStatus = AutoRunStatus.SharpenCalibrat;
                //// 磨刀校准
                float sharpenCalibratTheta = await AutoCutUtils.CalibratSharpenAsync(sharpenRect.Clone().Translate(cameraRelativeBladePosition.X, cameraRelativeBladePosition.Y), _pauseCts.Token);
                RunStatus = AutoRunStatus.CutingCalibrat;
                // 切割校准
                float cutCalibratTheta = await AutoCutUtils.CalibratCutAsync(new DataPoint<float>(cameraCenterPoint.X, cameraCenterPoint.Y + GlobalParams.CenterDistance), workpieceRadius, _pauseCts.Token);

                _sharpenService.SharpenServiceProcessChanged += SharpenService_SharpenServiceProcessChanged;
                _sharpenService.RemindReplaceSharpenBoard += SharpenService_RemindReplaceSharpenBoard;
                float? curHeightZ;
                //do
                //{
                //    RunStatus = AutoRunStatus.SharpeningInProgress;
                //    // 开始磨刀
                //    RunResult sharpenResult = await _sharpenService.Run(lunguSksj, CalculateBladeContactWorkingDiscZ1(heightMeasurementMode, AfterHeightMeasurementZ, nonContactHeightMeasurementToWorkbenchZ1), 
                //        bladeLiftingHeight, _sharpenParams.RotateSpeed, _sharpenParams.CoOffsetX, sharpenCalibratTheta, _pauseCts.Token);
                //    if (!sharpenResult.IsSuccess)
                //    {
                //        if (sharpenResult.Type == RunExceptionType.BladeScrap)
                //        {
                //            //提示更换刀片
                //            MaterialSnackUtils.MaterialSnack($"刀片报废，请更换刀片！", MaterialSnackUtils.SnackType.WARNING, 0);
                //        }
                //        else if (sharpenResult.Type != RunExceptionType.Stop)
                //        {
                //            //提示磨刀错误
                //            MaterialSnackUtils.MaterialSnack($"磨刀失败：{sharpenResult.Message}", MaterialSnackUtils.SnackType.WARNING, 0);
                //        }
                //        return;
                //    }

                //    RunStatus = AutoRunStatus.HeightMeasurementInProgress;
                //    await Task.Delay(1000);
                //    // 开始测高
                //    curHeightZ = await AutoCutUtils.ProcessMeasureHeightAsync(heightMeasurementMode, _pauseCts.Token);
                //    if (curHeightZ == null)
                //    {
                //        MaterialSnackUtils.MaterialSnack("测高失败，没有测高数据！", MaterialSnackUtils.SnackType.WARNING, 0);
                //        return;
                //    }
                //    //上传磨刀数据到MES
                //    //if (sharpenResult.SharpenTimesList != null)
                //    //{
                //    //    HttpUtils.AddSharpen(tuple.Item2, tuple.Item1, curHeightZ.Value - AfterHeightMeasurementZ, sharpenResult.SharpenTimesList[0], 0, 0);
                //    //}
                //    AfterHeightMeasurementZ = curHeightZ.Value;
                //} while (!AutoCutUtils.CheckIsMeetsCuttingConditions(lunguSksj, firstHeightMeasurementZ.Value, curHeightZ.Value));
                MaterialSnackUtils.MaterialSnack("刀片满足进入切割的条件！", MaterialSnackUtils.SnackType.SUCCESS, 0);
                await Task.Delay(1000);

                RunStatus = AutoRunStatus.CutingInProgress;
                _cutService.CutServiceProcessChanged += CutService_CutServiceProcessChanged;
                //开始切割
                RunResult cutResult = await _cutService.Run(lunguSksj, CalculateBladeContactWorkingDiscZ1(heightMeasurementMode, AfterHeightMeasurementZ, nonContactHeightMeasurementToWorkbenchZ1), 
                    focusClearZ.Value, bladeLiftingHeight, _cutParams.CutNum, _cutParams.SpindleRev, _cutParams.OffsetX, cutCalibratTheta, _pauseCts.Token);
                if (!cutResult.IsSuccess)
                {
                    if (cutResult.Type == RunExceptionType.BladeScrap)
                    {
                        //提示更换刀片
                        MaterialSnackUtils.MaterialSnack($"刀片报废，请更换刀片！", MaterialSnackUtils.SnackType.WARNING, 0);
                    }
                    else if (cutResult.Type != RunExceptionType.Stop)
                    {
                        //提示磨刀错误
                        MaterialSnackUtils.MaterialSnack($"切割失败：{cutResult.Message}", MaterialSnackUtils.SnackType.WARNING, 0);
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
                _cutService.CutServiceProcessChanged -= CutService_CutServiceProcessChanged;
                _pauseCts.Cancel();
                _monitoringAlarmCts.Cancel();
                await StopAsync();
            }
        }

        private async void SharpenService_RemindReplaceSharpenBoard()
        {
            if (await PauseAsync())
            {
                MaterialSnackUtils.MaterialSnack("已暂停，请更换磨刀板！", MaterialSnackUtils.SnackType.WARNING, 0);
            }
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
                Tools.LogError($"监控异常: {ex.Message}");
            }
        }

        private async Task MonitoringAlarmAsync(CancellationToken token)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
            while (await timer.WaitForNextTickAsync(token))
            {
                try
                {
                    ushort? axisAlarm = await PlcControl.plc.ReadDataAsync<ushort>("DM1000");
                    ushort? deviceAlarm = await PlcControl.plc.ReadDataAsync<ushort>("DM1010");
                    if (axisAlarm != null && axisAlarm != 0 || deviceAlarm != null && deviceAlarm != 0)
                    {
                        if (!_pauseCts.IsCancellationRequested)
                        {
                            await PauseAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Tools.LogError($"循环内异常: {ex.Message}");
                }
            }
        }

        private async Task<bool> PauseAsync()
        {
            if (!GlobalParams.onlineFlag)
            {
                NavigateUtils.NavigateToPage("Pages/Auto/AutoCutPausing", this);
                return false;
            }
            if (_pauseCts.IsCancellationRequested)
            {
                Tools.LogWarning("操作频繁！");
                return false;
            }
            // 暂停token
            _pauseCts.Cancel();
            if (RunStatus == AutoRunStatus.CutingInProgress || RunStatus == AutoRunStatus.SharpeningInProgress)
            {
                MaterialSnackUtils.MaterialSnack("正在暂停切割...", MaterialSnackUtils.SnackType.WARNING, 0);
                // 设置暂停超时时间 根据切割速度来计算 
                float cutSpeed = 50f;
                int runTime = (int)(150 / cutSpeed);
                // 运行时间 + 10秒动作时间 + 20秒余量时间
                runTime += 10 + 20;
                PlcControl.tagControl.cutting.SetCutStopDelayTime(runTime);
                Tools.LogInfo($"暂停超时时间：{runTime}");
                // 发送结束信号
                await PlcControl.tagControl.cutting.EndFullAutoCutAsync();
                try
                {
                    using var cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromSeconds(runTime)); // 超时自动取消
                    await PlcControl.tagControl.cutting.WaitReadyToCutAsync(cts.Token);
                    MaterialSnackUtils.MaterialSnack("已暂停切割", MaterialSnackUtils.SnackType.SUCCESS, 0);
                    return true;
                }
                catch (OperationCanceledException)
                {
                    MaterialSnackUtils.MaterialSnack("暂停切割超时", MaterialSnackUtils.SnackType.WARNING, 0);
                }
                catch (Exception ex)
                {
                    MaterialSnackUtils.MaterialSnack($"暂停切割时遇到其他错误: {ex.Message}", MaterialSnackUtils.SnackType.WARNING, 0);
                }
                finally
                {
                    NavigateUtils.NavigateToPage("Pages/Auto/AutoCutPausing", this);
                }
            }
            else
            {
                MaterialSnackUtils.MaterialSnack("停止自动切割，当前状态不允许暂停！", MaterialSnackUtils.SnackType.WARNING, 0);
            }
            return false;
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
                NavigateUtils.NavigateToPage("Pages/F4_BladeMaintenance/BladeReplacementConfiguration");
            }
            //中止监控报警线程
            _monitoringAlarmCts.Cancel();
            _sharpenService.Stop();
            _cutService.Stop();
            if (RunStatus == AutoRunStatus.SharpeningInProgress || RunStatus == AutoRunStatus.CutingInProgress)
            {
                //结束切割
                await PlcControl.tagControl.cutting.EnterFullAutoInitAsync(1);
                await PlcControl.tagControl.cutting.EndFullAutoCutAsync();
                //等待切割准备完成信号
                await PlcControl.tagControl.cutting.WaitReadyToCutAsync(default);
                await PlcControl.tagControl.cutting.EnterFullAutoInitAsync(0);
            }
            else
            {
                //其他模式停止，手动回到原点位置
                //await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(20, 0, default);
                //await PlcControl.tagControl.Yaxis.StartAbsoluteAsync(20, 0, default);
                //await PlcControl.tagControl.Z1axis.StartAbsoluteAsync(20, 0, default);
                //await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(20, 0, default);
            }
            NavigateUtils.NavigateToPage("Pages/F4_BladeMaintenance/BladeReplacementConfiguration");
        }

        private void UpdateMaterialSnack()
        {
            switch (_autoRunStatus)
            {
                case AutoRunStatus.HeightMeasurementInProgress:
                    MaterialSnackUtils.MaterialSnack("测高进行中...", MaterialSnackUtils.SnackType.SUCCESS, 0);
                    break;
                case AutoRunStatus.AutoFocus:
                    MaterialSnackUtils.MaterialSnack("自动聚焦中....", MaterialSnackUtils.SnackType.SUCCESS, 0);
                    break;
                case AutoRunStatus.SharpenCalibrat:
                    MaterialSnackUtils.MaterialSnack("磨刀校准中...", MaterialSnackUtils.SnackType.SUCCESS, 0);
                    break;
                case AutoRunStatus.CutingCalibrat:
                    MaterialSnackUtils.MaterialSnack("切割校准中...", MaterialSnackUtils.SnackType.SUCCESS, 0);
                    break;
                case AutoRunStatus.SharpeningInProgress:
                    MaterialSnackUtils.MaterialSnack("磨刀进行中...", MaterialSnackUtils.SnackType.SUCCESS, 0);
                    break;
                case AutoRunStatus.CutingInProgress:
                    MaterialSnackUtils.MaterialSnack("切割进行中...", MaterialSnackUtils.SnackType.SUCCESS, 0);
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

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
