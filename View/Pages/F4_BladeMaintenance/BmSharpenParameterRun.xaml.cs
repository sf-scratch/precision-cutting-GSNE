using Emgu.CV.Bioinspired;
using NPOI.OpenXmlFormats.Dml.Diagram;
using NPOI.OpenXmlFormats.Wordprocessing;
using NPOI.SS.Formula.Functions;
using Osklib.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.cut.Workpieces;
using 精密切割系统.Model.MeasureHeight;
using 精密切割系统.Model.plc;
using 精密切割系统.Model.sqlite;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.F4_BladeMaintenance
{
    /// <summary>
    /// BmSharpenParameterRun.xaml 的交互逻辑
    /// </summary>
    public partial class BmSharpenParameterRun : Page
    {
        private readonly SemiAutoCutService _semiAutoCutService;
        private CancellationTokenSource _pauseCts;
        private CancellationTokenSource _monitoringAlarmCts;
        private MainWindow _mainWindow;
        private RightPage _rightPage;

        public BmSharpenParameterRun()
        {
            InitializeComponent();
            _mainWindow = Application.Current.MainWindow as MainWindow ?? new MainWindow();
            _semiAutoCutService = SemiAutoCutService.Instance;
            _pauseCts = new CancellationTokenSource();
            _monitoringAlarmCts = new CancellationTokenSource();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            _rightPage = _mainWindow.rightFrame.Content as RightPage ?? new RightPage();
            _rightPage.btnCutPause.SetRightClickedHandler(Pause);
            _rightPage.btnCutStop.SetRightClickedHandler(Stop);
            _rightPage.btnCutReStart.SetRightClickedHandler(ReStart);
            _rightPage.PanelAction.Visibility = Visibility.Visible;
            UpdateToRunStatus();
            StartCut();
        }

        private async void StartCut()
        {
            if (!GlobalParams.OnlineFlag)
            {
                return;
            }
            if (AlarmConfig.Instance.HasActiveErrorAlarm())
            {
                MaterialSnack(AlarmConfig.HasErrorAlarmMessage, SnackType.WARNING);
                return;
            }
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
            CommonResult<BmSharpenParameterModel> sharpenParamResult = await GetBmSharpenParameterModelAsync();
            if (!sharpenParamResult.IsSuccess || sharpenParamResult.Data is null)
            {
                MaterialSnack(sharpenParamResult.Message, SnackType.WARNING);
                return;
            }
            CommonResult<List<ChCutStep>> cutStepResult = await GenerateCutStepListAsync();
            if (!cutStepResult.IsSuccess || cutStepResult.Data is null)
            {
                MaterialSnack(cutStepResult.Message, SnackType.WARNING);
                return;
            }
            if (Appsettings.BladeOuterDiameter is null)
            {
                MaterialSnack("未设置刀片外径！", SnackType.WARNING);
                return;
            }
            BmSharpenParameterModel bmSharpenParameter = sharpenParamResult.Data;
            // 初始化数据
            bladeHeight.Text = bmSharpenParameter.CutHeight.ToString();
            FeedSpeed.Text = bmSharpenParameter.MoCutOneSpeed;
            currentCutNum.Text = "0";
            totalCutNum.Text = cutStepResult.Data.First().CutSteps.Count.ToString();
            dirLightRatio.Text = Math.Round(GlobalParams.intensityRatio * 100, 2).ToString();
            _ = MonitoringAlarmAsync(_monitoringAlarmCts.Token);
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                CommonResult<float> firstHeightZ = await AutoCutUtils.ProcessCombineMeasureHeightAsync(default, _pauseCts.Token);
                RectangleWorkpiece workpiece = new(Appsettings.ThetaCenterPoint, GlobalParams.SharpenRect.Width, GlobalParams.SharpenRect.Height, (await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync() ?? 0).ToActualY())
                {
                    WorkThickness = float.Parse(bmSharpenParameter.CutThickness),
                    TapeThickness = bmSharpenParameter.CoJiaoHeight
                };
                _semiAutoCutService.CutServiceProcessChanged += CutService_CutServiceProcessChanged;
                _semiAutoCutService.CutServicePaused += CutService_CutServicePaused;
                RunResult cutResult = await _semiAutoCutService.RunAsync(cutStepResult.Data, workpiece, 30, firstHeightZ.Data, Appsettings.SafetyMarginZ1 ?? GlobalParams.BladeLiftingHeight, default, _pauseCts.Token);
                if (!cutResult.IsSuccess)
                {
                    MaterialSnack($"磨刀失败：{cutResult.Message}", SnackType.WARNING, 0);
                    return;
                }
                CommonResult<float> lastHeightZ = await AutoCutUtils.ProcessCombineMeasureHeightAsync(default, _pauseCts.Token);
                if (!lastHeightZ.IsSuccess)
                {
                    MaterialSnack(lastHeightZ.Message, SnackType.WARNING, 0);
                    return;
                }
                float wearAmount = MathF.Round(lastHeightZ.Data - firstHeightZ.Data, 3);
                stopwatch.Stop();
                TimeSpan timeSpan = TimeSpan.FromSeconds(stopwatch.Elapsed.TotalSeconds);
                string formattedTime = $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
                MaterialSnack($"磨刀完成！ 磨损量{wearAmount}mm 总用时：{formattedTime}", SnackType.SUCCESS, 0);
            }
            catch (Exception ex)
            {
                MaterialSnack($"磨刀异常：{ex.Message}", SnackType.WARNING, 0);
            }
            finally
            {
                _semiAutoCutService.CutServiceProcessChanged -= CutService_CutServiceProcessChanged;
                _semiAutoCutService.CutServicePaused -= CutService_CutServicePaused;
                _monitoringAlarmCts.Cancel();
                _pauseCts.Cancel();
                stopwatch.Stop();
                await StopAsync();
            }
        }

        private async void ReStart(object? sender, bool e)
        {
            if (!GlobalParams.OnlineFlag)
            {
                UpdateToRunStatus();
                return;
            }
            MaterialSnack("正在准备继续磨刀...", SnackType.WARNING, 0);
            _pauseCts = new CancellationTokenSource();
            await PlcControl.tagControl.cutting.EnterCuttingModeAsync(_pauseCts.Token);
            _semiAutoCutService.Continue(_pauseCts.Token);
            UpdateToRunStatus();
        }

        private void Pause(object? sender, bool e)
        {
            if (!GlobalParams.OnlineFlag)
            {
                UpdateToPauseStatus();
                return;
            }
            if (_pauseCts.IsCancellationRequested)
            {
                MaterialSnack("操作频繁！", SnackType.WARNING);
                return;
            }
            // 暂停token
            _pauseCts.Cancel();
        }

        private async void Stop(object? sender, bool e)
        {
            await StopAsync();
        }

        private async Task StopAsync()
        {
            string flag = QueryUtils.GetValueFromQueryParams(this, "Flag");
            string bladeLotID = QueryUtils.GetValueFromQueryParams(this, "BladeLotID");
            string idStr = QueryUtils.GetValueFromQueryParams(this, "Id");
            MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;
            if (!GlobalParams.OnlineFlag)
            {
                mainWindow?.NavigateToPage("Pages/F4_BladeMaintenance/BmSharpenParameterForm", "Id=" + idStr + "&Flag=" + flag + "&BladeLotID=" + bladeLotID);
                return;
            }
            _semiAutoCutService.Stop(ServicePauseResult.Stop);
            //结束切割
            await PlcControl.tagControl.cutting.ExitCuttingModeAsync(default);
            mainWindow?.NavigateToPage("Pages/F4_BladeMaintenance/BmSharpenParameterForm", "Id=" + idStr + "&Flag=" + flag + "&BladeLotID=" + bladeLotID);
        }

        private void UpdateToRunStatus()
        {
            MaterialSnack("磨刀中...", SnackType.WARNING, 0);
            _rightPage.btnCutReStart.Visibility = Visibility.Collapsed;
            _rightPage.btnCutPause.Visibility = Visibility.Visible;
            _rightPage.btnCutStop.Visibility = Visibility.Collapsed;
            stopGrid.Visibility = Visibility.Collapsed;
            sharpenTitle.Content = "磨刀进行状态";
        }

        private void UpdateToPauseStatus()
        {
            MaterialSnack("暂停中...", SnackType.WARNING, 0);
            _rightPage.btnCutReStart.Visibility = Visibility.Visible;
            _rightPage.btnCutPause.Visibility = Visibility.Collapsed;
            _rightPage.btnCutStop.Visibility = Visibility.Visible;
            stopGrid.Visibility = Visibility.Visible;
            _mainWindow.UpdateOperatePage(OperateData.GetTab4403Operate(), OperatePage_onClicked);
            sharpenTitle.Content = "磨刀暂停状态";
        }

        private async void CutService_CutServicePaused(CutServicePauseData pauseData)
        {
            await AfterPauseThenMoveToPosition(pauseData.Line, pauseData.Message);
        }

        private async Task AfterPauseThenMoveToPosition(LineSegment? line, string? message)
        {
            MaterialSnack("正在暂停磨刀...", SnackType.WARNING, 0);
            int runTime = 60;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(runTime)); // 超时自动取消
                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(cts.Token);
                // 轴不报警时移动到指定位置
                if (line != null && !AlarmConfig.Instance.HasAxisErrorAlarms())
                {
                    // 执行默认动作
                    await PlcControl.tagControl.Z1axis.StartAbsoluteAsync(0, default, cts.Token);
                    await AutoCutUtils.WorkpieceBlowingAsync(default, default, true, default, cts.Token);
                    await PlcControl.tagControl.cutting.RunMotionAsync(((line.StartPoint.X + line.EndPoint.X) / 2).ToCameraX(), line.StartPoint.Y.ToCameraY(), cts.Token);
                    await AutoFocusService.GlobalFocusAsync(default, cts.Token);
                    await AutoCutUtils.FineTuneAxisYAsync();
                    await AutoCutUtils.UpdateCameraCommonLineAsync();
                }
                MaterialSnack(message ?? "暂停中...", SnackType.WARNING, 0);
            }
            catch (OperationCanceledException)
            {
                MaterialSnack("暂停磨刀超时", SnackType.WARNING, 0);
            }
            catch (Exception ex)
            {
                MaterialSnack($"暂停磨刀时遇到其他错误: {ex.Message}", SnackType.WARNING, 0);
            }
            finally
            {
                UpdateToPauseStatus();
            }
        }

        private void CutService_CutServiceProcessChanged(CutServiceProcess process)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                bladeHeight.Text = process.CutBladeHeight.ToString();
                FeedSpeed.Text = process.CutSpeed.ToString();
                currentCutNum.Text = process.CutTimes.ToString();
                totalCutNum.Text = process.TotalCutTimes.ToString();
            });
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
                        if (AlarmConfig.Instance.HasAutoRunUnexpectedAlarms())
                        {
                            if (!_pauseCts.IsCancellationRequested)
                            {
                                await StopAsync();
                            }
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task<CommonResult<List<ChCutStep>>> GenerateCutStepListAsync()
        {
            CommonResult<BmSharpenParameterModel> result = await GetBmSharpenParameterModelAsync();
            if (!result.IsSuccess || result.Data is null)
            {
                return CommonResult<List<ChCutStep>>.Failure(result.Message);
            }
            BmSharpenParameterModel bmSharpenParameter = result.Data;
            var cutDataList = new List<(int, float)>()
                {
                    (Tools.GetIntStringValue(bmSharpenParameter.MoCutOneNo), Tools.GetFloatStringValue(bmSharpenParameter.MoCutOneSpeed)),
                    (Tools.GetIntStringValue(bmSharpenParameter.MoCutTwoNo), Tools.GetFloatStringValue(bmSharpenParameter.MoCutTwoSpeed)),
                    (Tools.GetIntStringValue(bmSharpenParameter.MoCutThreeNo), Tools.GetFloatStringValue(bmSharpenParameter.MoCutThreeSpeed)),
                    (Tools.GetIntStringValue(bmSharpenParameter.MoCutFourNo), Tools.GetFloatStringValue(    bmSharpenParameter.MoCutFourSpeed)),
                    (Tools.GetIntStringValue(bmSharpenParameter.MoCutFiveNo), Tools.GetFloatStringValue(bmSharpenParameter.MoCutFiveSpeed)),
                    (Tools.GetIntStringValue(bmSharpenParameter.MoCutSixNo), Tools.GetFloatStringValue(bmSharpenParameter.MoCutSixSpeed)),
                    (Tools.GetIntStringValue(bmSharpenParameter.MoCutSevenNo), Tools.GetFloatStringValue(bmSharpenParameter.MoCutSevenSpeed)),
                    (Tools.GetIntStringValue(bmSharpenParameter.MoCutEightNo), Tools.GetFloatStringValue(bmSharpenParameter.MoCutEightSpeed)),
                    (Tools.GetIntStringValue(bmSharpenParameter.MoCutNineNo), Tools.GetFloatStringValue(bmSharpenParameter.MoCutNineSpeed)),
                    (Tools.GetIntStringValue(bmSharpenParameter.MoCutTenNo), Tools.GetFloatStringValue(bmSharpenParameter.MoCutTenSpeed)),
                };
            List<CutStep> cutSteps = [];
            foreach (var cutData in cutDataList)
            {
                for (int i = 0; i < cutData.Item1; i++)
                {
                    if (cutData.Item1 == 0 || cutData.Item2 == 0)
                    {
                        break;
                    }
                    cutSteps.Add(new CutStep(bmSharpenParameter.CutHeight, cutData.Item2, bmSharpenParameter.CoCutSize, 0, false, 0));
                }
            }
            int chCutLines = bmSharpenParameter.CoCutNum;
            if (chCutLines != 0)
            {
                if (chCutLines > cutSteps.Count)
                {
                    cutSteps = Enumerable.Range(0, chCutLines).Select(i => cutSteps[i % cutSteps.Count]).ToList();
                }
                else
                {
                    cutSteps = cutSteps.GetRange(0, chCutLines);
                }
            }

            return CommonResult<List<ChCutStep>>.Success(new List<ChCutStep>() { new ChCutStep(GlobalParams.CH1, cutSteps) });
        }

        private async Task<CommonResult<BmSharpenParameterModel>> GetBmSharpenParameterModelAsync()
        {
            long id = long.Parse(QueryUtils.GetValueFromQueryParams(this, "Id"));
            List<BmSharpenParameterModel> list = await SqlHelper.TableAsync<BmSharpenParameterModel>().Where(t => t.Id == id).ToListAsync();
            if (list.Count > 0)
            {
                return CommonResult<BmSharpenParameterModel>.Success(list[0]);
            }
            return CommonResult<BmSharpenParameterModel>.Failure("磨刀配置获取失败！");
        }

        private async void OperatePage_onClicked(object? sender, int code)
        {
            switch (code)
            {
                case 2023:
                    // 手动校准 type
                    _mainWindow.mainFrame.Source = new Uri($"View/Pages/F2_ManualOperation/MQManualAlignmentConf.xaml?type=2", UriKind.Relative);
                    break;

                case 2442:
                    var timeoutToken = TaskUtils.GetTimeoutCancellationToken(TimeSpan.FromSeconds(120));
                    var result = await AutoFocusService.GlobalFocusAsync(default, timeoutToken.Token);
                    if (!result.IsSuccess)
                    {
                        MaterialSnack(result.Message, SnackType.WARNING);
                        return;
                    }
                    break;

                default:
                    break;
            }
        }

        ////换刀后修改高度数据为0，测量后修改为测量值
        //public async void updateData()
        //{
        //    BladeHeightModel _bmSharpenParameter;
        //    var list = await SqlHelper.TableAsync<BladeHeightModel>()
        //            .Where(t => t.Id == 1).ToListAsync();
        //    //数据不存在，则初始化数据
        //    if (list.Count() == 0)
        //    {
        //        _bmSharpenParameter = new BladeHeightModel();
        //        await SqlHelper.AddAsync(_bmSharpenParameter);
        //    }
        //    else
        //    {
        //        _bmSharpenParameter = list[0];

        //    }
        //    _bmSharpenParameter.BladeHeight = "0";
        //    //保存数据
        //    await SqlHelper.UpdateAsync(_bmSharpenParameter);
        //}

        private void SubFive_OnClick(object sender, RoutedEventArgs e)
        {
            AdjustIntensity(-0.05m);
        }

        private void SubOne_OnClick(object sender, RoutedEventArgs e)
        {
            AdjustIntensity(-0.01m);
        }

        private void AddOne_OnClick(object sender, RoutedEventArgs e)
        {
            AdjustIntensity(0.01m);
        }

        private void AddFive_OnClick(object sender, RoutedEventArgs e)
        {
            AdjustIntensity(0.05m);
        }

        private void SetLightRatio(decimal t_intensity)
        {
            double intensity = Convert.ToDouble(t_intensity);
            intensity = Math.Clamp(intensity, 0.01, 1);
            dirLightRatio.Text = (Math.Round(intensity * 100, 2)).ToString();
        }

        /// <summary>
        /// adjustment 是小数，表示百分比的小数 比如0.05表示 百分之5
        /// </summary>
        /// <param name="adjustment"></param>
        private void AdjustIntensity(decimal adjustment)
        {
            decimal t_intensity = Convert.ToDecimal(GlobalParams.intensityRatio);
            t_intensity += adjustment;
            // 设置初始光源亮度v_intensity = 255*0.85 = 216.75
            int v_intensity = (int)Math.Ceiling(t_intensity * 255);
            int reNum = Math.Clamp(v_intensity, 1, 255); //值在这个区间
            CameraUtils.SetLightIntensity(reNum, GlobalParams.LightIntensityChannel);
            SetLightRatio(t_intensity);
        }
    }
}