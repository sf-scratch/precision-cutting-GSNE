using Newtonsoft.Json;
using NPOI.POIFS.Crypt.Dsig;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.HttpClients;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.cut.Workpieces;
using 精密切割系统.Model.MeasureHeight;
using 精密切割系统.Model.plc;
using 精密切割系统.PubSubEvent;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.View.Pages.F4_BladeMaintenance;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;
using static 精密切割系统.Helpers.MaterialSnackUtils;

namespace 精密切割系统.View.Pages.F2_ManualOperation
{
    /// <summary>
    /// MQSemiAutomaticCuttingRun.xaml 的交互逻辑
    /// </summary>
    public partial class MQSemiAutomaticCuttingRun : Page
    {
        private MQSemiAutomaticCuttingRunViewModel _viewModel;
        private readonly SemiAutoCutService _semiAutoCutService;
        private readonly MainWindow _mainWindow;
        private RightPage _rightPage;
        private static CancellationTokenSource _pauseCts;
        private CancellationTokenSource _monitoringCts;

        public MQSemiAutomaticCuttingRun()
        {
            InitializeComponent();
            _semiAutoCutService = SemiAutoCutService.Instance;
            _pauseCts = new CancellationTokenSource();
            _monitoringCts = new CancellationTokenSource();
            _mainWindow = Application.Current.MainWindow as MainWindow ?? new MainWindow();
            _mainWindow.UpdateOperatePage([], null);

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _rightPage = _mainWindow.rightFrame.Content as RightPage ?? new RightPage();
            _rightPage.PanelAction.Visibility = Visibility.Visible;
            _rightPage.btnCutPause.Visibility = Visibility.Visible;
            _rightPage.btnCutPause.GlobalRunOperateFlag = true;
            _rightPage.btnCutPause.SetRightClickedHandler(PauseHandler);
            // 加载参数
            FileTableItemModel fileTableItem = CurrentUtils.GetFileTableItemModel();
            _viewModel = new MQSemiAutomaticCuttingRunViewModel
            {
                DeviceDataNo = fileTableItem.DeviceDataNo,
                DeviceDataId = fileTableItem.DeviceDataId,
                ChannelNum = CurrentUtils.GetCurrentConfiguration().ChannelNum,
                ChangeFeedSpeed = _semiAutoCutService.FeedSpeedCompCompensationValue.ToString(),
                DepthCompensation = _semiAutoCutService.DepthCompensationValue.ToString(),
            };
            DataContext = _viewModel;
            UpdateDefineDataModel();
            // 调用开始切割
            StartCut();
        }

        private void UpdateDefineDataModel()
        {
            UserDefineDataModel userDefineModel = CurrentUtils.getUserDefineDataModel();
            bool isSpeedChange = "NO".Equals(userDefineModel.SpeedChange);
            bool isHeightChange = "NO".Equals(userDefineModel.HeightChange);
            if (isSpeedChange)//速度变更
            {
                ChangeFeedSpeed1.Visibility = Visibility.Hidden;
                ChangeFeedSpeed2.Visibility = Visibility.Hidden;
                ChangeFeedSpeed3.Visibility = Visibility.Hidden;
            }
            if (isHeightChange)//高度补偿
            {
                HeightChange1.Visibility = Visibility.Hidden;
                HeightChange2.Visibility = Visibility.Hidden;
                HeightChange3.Visibility = Visibility.Hidden;
            }
        }

        private async Task MonitoringAlarmAsync(CancellationToken token)
        {
            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
                while (await timer.WaitForNextTickAsync(token))
                {
                    try
                    {
                        if (AlarmConfig.Instance.HasActiveErrorAlarm("MR60408", "MR61000", "MR61100", "MR61200", "MR61300", "MR61400"))
                        {
                            if (!_pauseCts.IsCancellationRequested)
                            {
                                Pause();
                            }
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task MonitoringCutProgressAsync(CancellationToken token)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
            while (await timer.WaitForNextTickAsync(token))
            {
                try
                {
                    _viewModel.ChannelNum = CurrentUtils.GetCurrentConfiguration().ChannelNum;
                    float curX = MathF.Round(await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync() ?? 0, 3);
                    float curY = MathF.Round(await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync() ?? 0, 3);
                    float curZ1 = MathF.Round(await PlcControl.tagControl.Z1axis.GetCurrentLocationAsync() ?? 0, 3);
                    float curZ2 = MathF.Round(await PlcControl.tagControl.Z2axis.GetCurrentLocationAsync() ?? 0, 3);
                    float curTheta = MathF.Round(await PlcControl.tagControl.ThetaAxis.GetCurrentLocationAsync() ?? 0, 3);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        yAxisCutPosition.Text = CutOperateUtils.globalYCutPosition.ToString();
                        xAxisCurrentPosition.Text = curX.ToString();
                        yAxisCurrentPosition.Text = curY.ToString();
                        zAxisCurrentPosition.Text = curZ1.ToString();
                        z2AxisCurrentPosition.Text = curZ2.ToString();
                        thetaAxisCurrentPosition.Text = curTheta.ToString();
                    });
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// 开始切割
        /// </summary>
        private async void StartCut()
        {
            if (!GlobalParams.onlineFlag)
            {
                return;
            }
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                if (!_semiAutoCutService.IsReady)
                {
                    return;
                }
                CommonResult checkResult = await SemiAutoCutService.CheckCutAsync();
                if (!checkResult.IsSuccess)
                {
                    MaterialSnack(checkResult.Message, SnackType.WARNING);
                    return;
                }
                CommonResult<List<CutStep>> cutStepResult = await GenerateCutStepListAsync(_semiAutoCutService.IsOpenPrecut);
                if (!cutStepResult.IsSuccess || cutStepResult.Data is null)
                {
                    MaterialSnack(cutStepResult.Message, SnackType.WARNING);
                    return;
                }
                CommonResult<FileTableItemModel> fileTableItemResult = await GetFileTableItemModelAsync();
                if (!fileTableItemResult.IsSuccess || fileTableItemResult.Data is null)
                {
                    MaterialSnack(fileTableItemResult.Message, SnackType.WARNING);
                    return;
                }
                _ = MonitoringAlarmAsync(_monitoringCts.Token);
                _ = MonitoringCutProgressAsync(_monitoringCts.Token);
                float cutY = (await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync() ?? 0) - Appsettings.CameraRelativeBladePosition.Y;
                int spindleRev = fileTableItemResult.Data.SpindleRev;
                await PlcControl.tagControl.bladeMantance.SetSetupParamsAsync(CurrentUtils.GetBladeHeightModel());
                await PlcControl.tagControl.bladeMantance.SetZAxisMaxDistanceAsync(AutoCutUtils.CaculateZAxisMaxDistance(55.1f));
                CommonResult<float> curHeightZ = await AutoCutUtils.ProcessMeasureHeightAsync(HeightMeasurementMode.Contact, default, default, _pauseCts.Token);
                if (!curHeightZ.IsSuccess)
                {
                    MaterialSnack(curHeightZ.Message, SnackType.WARNING, 0);
                    return;
                }
                CircularWorkpiece workpiece = new(GlobalParams.ThetaCenterPoint, GlobalParams.WorkpieceRadius, cutY)
                {
                    WorkThickness = float.Parse(fileTableItemResult.Data.WorkThickness),
                    TapeThickness = float.Parse(fileTableItemResult.Data.TapeThickness)
                };
                _semiAutoCutService.CutServiceProcessChanged += CutService_CutServiceProcessChanged;
                _semiAutoCutService.CutServicePaused += CutService_CutServicePaused;
                MaterialSnack($"切割中...", SnackType.SUCCESS, 0);
                RunResult cutResult = await _semiAutoCutService.RunAsync(cutStepResult.Data, workpiece, 30, spindleRev, curHeightZ.Data, GlobalParams.BladeLiftingHeight, _pauseCts.Token);
                if (!cutResult.IsSuccess)
                {
                    MaterialSnack($"切割失败：{cutResult.Message}", SnackType.WARNING, 0);
                    return;
                }
                stopwatch.Stop();
                TimeSpan timeSpan = TimeSpan.FromSeconds(stopwatch.Elapsed.TotalSeconds);
                string formattedTime = $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
                MaterialSnack($"切割完成！ 总用时：{formattedTime}", SnackType.SUCCESS, 0);
            }
            catch (Exception ex)
            {
                MaterialSnack($"切割异常：{ex.Message}", SnackType.WARNING, 0);
            }
            finally
            {
                _semiAutoCutService.CutServiceProcessChanged -= CutService_CutServiceProcessChanged;
                _semiAutoCutService.CutServicePaused -= CutService_CutServicePaused;
                stopwatch.Stop();
                _monitoringCts.Cancel();
                _pauseCts.Cancel();
                await StopAsync(ServicePauseResult.Stop);
            }
        }

        private void PauseHandler(object? sender, bool e)
        {
            Pause();
        }

        private void Pause()
        {
            if (!GlobalParams.onlineFlag)
            {
                // 暂停token
                _pauseCts.Cancel();
                _mainWindow.NavigateToPage("Pages/F2_ManualOperation/MQSemiAutomaticCuttingStop", JsonConvert.SerializeObject(_viewModel));
                return;
            }
            if (_pauseCts.IsCancellationRequested)
            {
                MaterialSnack("操作频繁！", SnackType.WARNING, 0);
                return;
            }
            // 暂停token
            _pauseCts.Cancel();
        }

        public static async Task ContinueAsync()
        {
            if (!GlobalParams.onlineFlag)
            {
                SemiAutoCutService.Instance.Continue(_pauseCts.Token);
                return;
            }
            MaterialSnack("正在继续切割...", SnackType.WARNING, 0);
            _pauseCts = new CancellationTokenSource();
            await PlcControl.tagControl.cutting.EnterCuttingModeAsync(_pauseCts.Token);
            SemiAutoCutService.Instance.Continue(_pauseCts.Token);
            MaterialSnack("切割中...", SnackType.WARNING, 0);
        }

        public static async Task StopAsync(ServicePauseResult pauseResult)
        {
            MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;
            if (!GlobalParams.onlineFlag)
            {
                mainWindow?.NavigateToPage("Pages/F2_ManualOperation/MQSemiAutomaticCuttingConf");
                return;
            }
            SemiAutoCutService.Instance.Stop(pauseResult);
            //结束切割
            await PlcControl.tagControl.cutting.ExitCuttingModeAsync(default);
            mainWindow?.NavigateToPage("Pages/F2_ManualOperation/MQSemiAutomaticCuttingConf");
        }

        private async void CutService_CutServicePaused(LineSegment? line, string? message)
        {
            await AfterPauseThenMoveToPosition(line, message);
        }

        private async Task AfterPauseThenMoveToPosition(LineSegment? line, string? message)
        {
            MaterialSnack("正在暂停切割...", SnackType.WARNING, 0);
            int runTime = 40;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(runTime));// 超时自动取消
                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(cts.Token);
                // 轴不报警时移动到指定位置
                if (line != null)
                {
                    // 执行默认动作
                    var offsetPos = Appsettings.CameraRelativeBladePosition;
                    Task z1Task = PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0, default, cts.Token);
                    Task z2Task = PlcControl.tagControl.Z2axis.StartAbsoluteAsync(Appsettings.FocusClearZ ?? 0, default, cts.Token);
                    await Task.WhenAll(z1Task, z2Task);
                    await AutoCutUtils.WorkpieceBlowingAsync(default, cts.Token);
                    await PlcControl.tagControl.cutting.RunMotionAsync((line.StartPoint.X + line.EndPoint.X) / 2 + offsetPos.X, line.StartPoint.Y + offsetPos.Y, cts.Token);
                }
                await AutoCutUtils.FineTuneAxisYAsync();
                await AutoCutUtils.UpdateCameraCommonLineAsync();
                MaterialSnack(message ?? "暂停中...", SnackType.WARNING, 0);
            }
            catch (OperationCanceledException)
            {
                MaterialSnack("暂停切割超时", SnackType.WARNING, 0);
            }
            catch (Exception ex)
            {
                MaterialSnack($"暂停切割时遇到其他错误: {ex.Message}", SnackType.WARNING, 0);
            }
            finally
            {
                _mainWindow.NavigateToPage("Pages/F2_ManualOperation/MQSemiAutomaticCuttingStop", JsonConvert.SerializeObject(_viewModel));
            }
        }

        private void CutService_CutServiceProcessChanged(CutServiceProcess process)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 设置切割进度
                _viewModel.RunCutLine = process.CutTimes;
                _viewModel.AllRunCutLine = process.TotalCutTimes;
                _viewModel.FeedSpeed = process.CutSpeed;
                _viewModel.BladeHeight = process.CutBladeHeight;
                _viewModel.ChannelNum = $"CH{process.ChannelNum}";
                if (process.IsCompleted)
                {
                    Appsettings.AfterReplaceBladeCutTimes++;
                    Appsettings.AfterReplaceBladeCutLength += process.CutLength;
                    _viewModel.AllCutLine = Appsettings.AfterReplaceBladeCutTimes ?? 0;
                    _viewModel.AllCutLineLength = MathF.Round(Appsettings.AfterReplaceBladeCutLength / 100 ?? 0, 3);
                    _viewModel.ExpectedProcessingEndTime = DateTime.Now.AddSeconds(process.RemainingTime).ToString("HH:mm:ss");
                }
            });
        }

        private async Task<CommonResult<List<CutStep>>> GenerateCutStepListAsync(bool isOpenPrecut)
        {
            //获取功能选择数据
            var selectionModels = await SqlHelper.TableAsync<FunctionSelectionModel>().Where(t => t.Id == 1).ToListAsync();
            if (selectionModels.Count <= 0)
            {
                return CommonResult<List<CutStep>>.Failure("功能选择配置异常！");
            }
            FunctionSelectionModel functionModel = selectionModels[0];
            bool isDeep = functionModel.DepthStepsFunction;
            bool isLoop = functionModel.LoopFunction;
            CommonResult<FileTableItemModel> fileTableItemResult = await GetFileTableItemModelAsync();
            if (!fileTableItemResult.IsSuccess || fileTableItemResult.Data is null)
            {
                return CommonResult<List<CutStep>>.Failure(fileTableItemResult.Message);
            }
            FileTableItemModel fileTableItem = fileTableItemResult.Data;
            string cuttingChSeq = fileTableItem.CuttingChSeq;
            // 参数校验
            if (fileTableItem.SpindleRev == 0 || fileTableItem.SpindleRev > 30000)
            {
                return CommonResult<List<CutStep>>.Failure("切割参数配置错误！");
            }
            List<CutStep> cutSteps = [];
            // 查询通道信息
            List<FileTableItemChModel> chModels = await SqlHelper.TableAsync<FileTableItemChModel>().Where(t => t.ItemId == fileTableItem.Id).ToListAsync();
            int[] chSeqs = Tools.StringToIntegerArray(cuttingChSeq);
            foreach (int chSeq in chSeqs)
            {
                FileTableItemChModel ch = chModels[chSeq - 1];
                float[] setBladeHeight = Tools.StringToFloatArray(ch.BladeHeight);// 设置的刀片高度
                float[] feedSpeeds = Tools.StringToFloatArray(ch.FeedSpeed); // 获取进给速度
                float[] yIndexs = Tools.StringToFloatArray(ch.YIndex);       // 获取Y轴偏移
                float[] repeatTimes = Tools.StringToFloatArray(ch.RepeatTimes); // 获取重复次数
                float[] cutDepths = Tools.StringToFloatArray(ch.DepthSteps); // 获取切割深度
                string[] loops = Tools.StringToStringArray(ch.Loop);         // 获取循环控制信息
                // 检查索引是否连续
                int maxIndex = AreIndexesContinuous(setBladeHeight, feedSpeeds, yIndexs, repeatTimes);
                if (maxIndex == -1)
                {
                    return CommonResult<List<CutStep>>.Failure("切割参数错误！");
                }
                if (cutDepths.Length <= maxIndex)
                {
                    return CommonResult<List<CutStep>>.Failure("切割深度参数错误！");
                }
                // 生成子序列
                List<string> repetitions = [.. loops];
                List<int> sequences = [.. Enumerable.Range(0, maxIndex + 1)];
                List<int> newSeq = CutUtils.CombineSequences(sequences, repetitions);
                List<CutStep> tempCutSteps = [];
                foreach (int index in newSeq)
                {
                    for (int i = 0; i < repeatTimes[index]; i++)
                    {
                        tempCutSteps.Add(new CutStep(setBladeHeight[index], feedSpeeds[index], yIndexs[index], float.Parse(ch.ThetaDeg), ch.CutMode == CutOperateUtils.B_ZKEEP, chSeq, isDeep ? cutDepths[index] : default));
                    }
                }
                int chCutLines = Tools.GetIntStringValue(ch.CutLine);
                if (chCutLines == 0)
                {
                    cutSteps.AddRange(tempCutSteps);
                }
                else if (chCutLines > tempCutSteps.Count)
                {
                    cutSteps.AddRange(Enumerable.Range(0, chCutLines).Select(i => tempCutSteps[i % tempCutSteps.Count]));
                }
                else
                {
                    cutSteps.AddRange(tempCutSteps.GetRange(0, chCutLines));
                }
            }
            if (isOpenPrecut)
            {
                CommonResult<List<float>> preCutSpeedResult = AutoCutUtils.GetPreCutSpeedList();
                if (!preCutSpeedResult.IsSuccess || preCutSpeedResult.Data is null)
                {
                    return CommonResult<List<CutStep>>.Failure(preCutSpeedResult.Message);
                }
                List<float> speeds = preCutSpeedResult.Data;
                if (speeds.Count > cutSteps.Count)
                {
                    speeds = speeds.GetRange(0, cutSteps.Count);
                }
                for (int i = 0; i < speeds.Count; i++)
                {
                    if (speeds[i] < cutSteps[i].Speed)
                    {
                        cutSteps[i] = cutSteps[i] with { Speed = speeds[i] };
                    }
                }
            }
            return CommonResult<List<CutStep>>.Success(cutSteps);
        }

        private async Task<CommonResult<FileTableItemModel>> GetFileTableItemModelAsync()
        {
            long id = CurrentUtils.GetCurrentConfiguration().DeviceDataId;
            // 判断是否确认配置信息
            if (id == 0)
            {
                return CommonResult<FileTableItemModel>.Failure("未确认配置信息！");
            }
            // 查询配置信息
            List<FileTableItemModel> listConf = await SqlHelper.TableAsync<FileTableItemModel>().Where(t => t.Id == id).ToListAsync();
            if (listConf.Count == 0)
            {
                return CommonResult<FileTableItemModel>.Failure("未确认配置信息！");
            }
            FileTableItemModel fileTableItem = listConf[0];
            return CommonResult<FileTableItemModel>.Success(fileTableItem);
        }

        public static int AreIndexesContinuous(float[] setBladeHeight, float[] feedSpeeds, float[] yIndexs, float[] repeatTimes)
        {
            // 获取满足条件的索引
            var validIndexes = setBladeHeight
                .Select((value, index) => new { Value = value, Index = index })
                .Where(x => x.Value > 0 && feedSpeeds[x.Index] > 0 && yIndexs[x.Index] != 0 && repeatTimes[x.Index] > 0)
                .Select(x => x.Index)
                .OrderBy(x => x)
                .ToList();
            // 检查是否有有效的索引
            if (!validIndexes.Any())
            {
                return 0; // 没有符合条件的索引
            }
            // 检查索引是否连续
            bool areIndexesContinuous = validIndexes.Zip(validIndexes.Skip(1), (current, next) => next - current == 1).All(x => x);

            // 如果有效索引是连续的，返回最大索引，否则返回0
            return areIndexesContinuous ? validIndexes.Max() : -1;
        }
    }
}
