using NPOI.OpenXmlFormats.Dml.Diagram;
using System.Diagnostics;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.MeasureHeight;
using 精密切割系统.Model.plc;
using 精密切割系统.PubSubEvent;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;
using static 精密切割系统.Helpers.MaterialSnackUtils;

namespace 精密切割系统.View.Pages.F2_ManualOperation
{
    /// <summary>
    /// MQSemiAutomaticCuttingConf.xaml 的交互逻辑
    /// </summary>
    public partial class MQSemiAutomaticCuttingConf : Page
    {
        private MQSemiAutomaticCuttingConfViewModel _viewModel;
        private readonly SemiAutoCutService _semiAutoCutService;
        private CancellationTokenSource _pauseCts;
        private CancellationTokenSource _monitoringAlarmCts;

        private MainWindow mainWindow;
        private RightPage rightPage;
        private OperatePage operatePage;

        public MQSemiAutomaticCuttingConf()
        {
            InitializeComponent();
            _semiAutoCutService = SemiAutoCutService.Instance;
            _pauseCts = new CancellationTokenSource();
            _monitoringAlarmCts = new CancellationTokenSource();
            mainWindow = Application.Current.MainWindow as MainWindow ?? new MainWindow();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            rightPage = mainWindow.rightFrame.Content as RightPage ?? new RightPage();
            operatePage = mainWindow.operateFrame.Content as OperatePage ?? new OperatePage();
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.SetRightClickedHandler(CutBack);
            rightPage.btnCutStart.Visibility = Visibility.Visible;
            rightPage.btnCutStart.SetRightClickedHandler(StartCut);
            rightPage.btnCutBackward.Visibility = Visibility.Visible;
            rightPage.btnCutBackward.SetRightClickedHandler(CutBackward);
            rightPage.btnCutFront.Visibility = Visibility.Visible;
            rightPage.btnCutFront.SetRightClickedHandler(CutFront);
            GlobalParams.cutStatusInfo = 0;
            updateDefineDataModel();
            // 初始化配置
            LoadConfigInfo();
        }
        
        //根据默认配置控制对应显示和隐藏
        private void updateDefineDataModel()
        {
            UserDefineDataModel userDefineModel = CurrentUtils.getUserDefineDataModel();
            bool isSpeedChange = "NO".Equals(userDefineModel.SpeedChange);
            bool isHeightChange = "NO".Equals(userDefineModel.HeightChange);
            if (isSpeedChange)//速度变更
            {
                SpeedChangePanel.Visibility = Visibility.Collapsed;
            }
            if (isHeightChange)//高度补偿
            {
                HeightChangePanel.Visibility = Visibility.Collapsed;
            }
            mainWindow.UpdateOperatePage(OperateData.GetSemiAutoCuttingOperate(!isSpeedChange, !isHeightChange), OperateClickHandler);
        }

        private void OperateClickHandler(object sender, int code)
        {
            switch (code)
            {
                case 2401:
                    float tempDepthCompensation = Tools.GetFloatStringValue(_viewModel.DepthCompensation);
                    // 高度补偿
                    GlobalParams.depthComp = tempDepthCompensation;
                    MaterialSnack("刀片高度补偿设置成功！", SnackType.SUCCESS);
                    break;
                case 2403:
                    float tempChangeFeedSpeed = Tools.GetFloatStringValue(_viewModel.ChangeFeedSpeed);
                    // 速度更改
                    CutOperateUtils.SetFeedSpeedComp(tempChangeFeedSpeed);
                    MaterialSnack("变更进刀速度成功！", SnackType.SUCCESS);
                    break;
                case 2023:
                    // 手动校准 type 
                    mainWindow.NavigateToPage("Pages/F2_ManualOperation/MQManualAlignmentConf", "type=1");
                    break;
                case 2404:
                    if (CutOperateUtils.precutFlag)
                    {
                        MaterialSnack("关闭预切割！", SnackType.SUCCESS);
                    } 
                    else
                    {
                        MaterialSnack("开启预切割！", SnackType.SUCCESS);
                    }
                    // 预切启动
                    CutOperateUtils.precutFlag = !CutOperateUtils.precutFlag;
                    break;
                case 2405:
                    // 进入型号参数
                    // 查询当前配置,跳转到型号参数目录
                    mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCDeviceDataConf", "id=" + CurrentUtils.GetCurrentConfiguration().DeviceDataId + "&url=Pages/F2_ManualOperation/MQSemiAutomaticCuttingConf");
                    break;
                case 2422:
                    // 刀片状态信息
                    mainWindow.NavigateToPage("Pages/F4_BladeMaintenance/BladeInfo", "pageName=Pages/F2_ManualOperation/MQSemiAutomaticCuttingConf");
                    break;
                default:
                    break;
            }
        }

        // 开始切割
        private async void StartCut(object? sender, bool e)
        {
            CommonResult checkResult = await CheckCutAsync();
            if (!checkResult.IsSuccess)
            {
                MaterialSnack(checkResult.Message, SnackType.WARNING);
                return;
            }
            // 判断预切割配置是否存在，不存在则提示
            if (CutOperateUtils.precutFlag)
            {
                // 查询当前预切割流程信息
                PreCutModel preCutModel = CurrentUtils.GetPreCutModel();
                if (preCutModel.Id == 0)
                {
                    MaterialSnack("预切割参数没找到！", SnackType.WARNING);
                    return;
                }
            }
            CommonResult<List<CutStep>> cutStepResult = await GenerateCutStepListAsync();
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
            FileTableItemModel fileTableItem = fileTableItemResult.Data;
            try
            {
                await PlcControl.tagControl.bladeMantance.SetSetupParamsAsync(CurrentUtils.GetBladeHeightModel());
                await PlcControl.tagControl.bladeMantance.SetZAxisMaxDistanceAsync(55.5f / 2 - 10.2f);
                CommonResult<float> curHeightZ = await AutoCutUtils.ProcessMeasureHeightAsync(HeightMeasurementMode.Contact, default, default, _pauseCts.Token);
                if (!curHeightZ.IsSuccess)
                {
                    MaterialSnack(curHeightZ.Message, SnackType.WARNING, 0);
                    return;
                }
                _semiAutoCutService.CutServiceProcessChanged += CutService_CutServiceProcessChanged;
                _semiAutoCutService.CutServicePaused += CutService_CutServicePaused;
                await _semiAutoCutService.RunAsync(cutStepResult.Data, 30, fileTableItem.SpindleRev, curHeightZ.Data, GlobalParams.BladeLiftingHeight);
            }
            catch (Exception ex)
            {

            }
            finally
            {

            }
        }

        private async void CutService_CutServicePaused(LineSegment? line, string? message)
        {
            await AfterPauseThenMoveToPosition(line, message);
        }

        private async Task AfterPauseThenMoveToPosition(LineSegment? line, string? message)
        {
            MaterialSnackUtils.MaterialSnack("正在暂停切割...", MaterialSnackUtils.SnackType.WARNING, 0);
            int runTime = 40;
            try
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(runTime)); // 超时自动取消
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
                MaterialSnackUtils.MaterialSnack(message ?? "暂停中...", MaterialSnackUtils.SnackType.WARNING, 0);
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
            }
        }

        private void CutService_CutServiceProcessChanged(CutServiceProcess process)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                //CutSpeed = process.CutSpeed;
                //CutProgress = string.Format("{0}/{1}", process.CurCutTimes, process.TotalCutTimes);
                //if (process.IsCompleted)
                //{
                //    AfterReplaceBladeCutTimes++;
                //}
            });
        }

        private async Task<CommonResult<List<CutStep>>> GenerateCutStepListAsync()
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
            foreach (int chIndex in chSeqs)
            {
                FileTableItemChModel ch = chModels[chIndex - 1];
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
                        tempCutSteps.Add(new CutStep(setBladeHeight[index], feedSpeeds[index], yIndexs[index], float.Parse(ch.ThetaDeg), isDeep ? cutDepths[index] : default));
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
                .Where(x => x.Value > 0 && feedSpeeds[x.Index] > 0 && yIndexs[x.Index] > 0 && repeatTimes[x.Index] > 0)
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

        private async Task<CommonResult> CheckCutAsync()
        {
            if (!GlobalParams.onlineFlag) return CommonResult.Success();
            if (!await PlcControl.tagControl.wholeDevice.IsCompletedSystemInitAsync())
            {
                return CommonResult.Failure("请完成系统初始化！");
            }
            if (!await PlcControl.tagControl.wholeDevice.IsOpenVacuumSwitchAsync())
            {
                return CommonResult.Failure("请打开工作盘真空！");
            }
            if (await PlcControl.tagControl.wholeDevice.IsOpenCutSecurityDoorAsync())
            {
                return CommonResult.Failure("请关闭切割安全门！");
            }
            if (await PlcControl.tagControl.wholeDevice.IsOpenCameraSecurityDoorAsync())
            {
                return CommonResult.Failure("请关闭相机安全门！");
            }
            return CommonResult.Success();
        }

        private void CutBack(object? sender, bool e)
        {
            // 回复切割面到Ch 1
            CurrentUtils.InitCutCh();
            // 退出切割模式
            PlcControl.tagControl.cutting.EnterFullAutoInit(0);
            mainWindow.NavigateToPage("MainMenu");
        }

        private void CutFront(object? sender, bool e)
        {
            _viewModel.CutDirection = "向前切";
            _semiAutoCutService.CutDirection = CutDirection.Forward;
            //CutOperateUtils.cutDirection = cutDirection;
        }
        private void CutBackward(object? sender, bool e)
        {
            _viewModel.CutDirection = "向后切";
            _semiAutoCutService.CutDirection = CutDirection.Backward;
        }

        private void LoadConfigInfo()
        {
            // 查询当前配置信息
            FileTableItemModel _model = CurrentUtils.GetFileTableItemModel();
            BladeHeightModel bladeHeightModel = CurrentUtils.GetBladeHeightModel();
            // 获取当前channel
            FileTableItemChModel chModel = CurrentUtils.GetFileTableItemChModel();
            // 设置当前配置信息的切割方法
            PlcControl.tagControl.cutting.StartCutMethod(CutOperateUtils.GetCutMethod(chModel.CutMode));
            // 获取刀片高度、进刀速度
            string bladeHeightStr = chModel.BladeHeight;
            string feedSpeedStr = chModel.FeedSpeed;
            string bladeHeight = bladeHeightStr.Split(",")[0];
            string feedSpeed = feedSpeedStr.Split(",")[0];
            _viewModel = new MQSemiAutomaticCuttingConfViewModel();
            _viewModel.DeviceDataNo = _model.DeviceDataNo + "";
            _viewModel.DeviceDataId = _model.DeviceDataId;
            _viewModel.ChannelNum = CurrentUtils.GetCurrentConfiguration().ChannelNum;
            _viewModel.BladeHeight = bladeHeight;
            _viewModel.FeedSpeed = feedSpeed;
            _viewModel.CutLine = 0;
            _viewModel.CutDepthOffset = "0.000";
            _viewModel.ChangeFeedSpeed = "0.000";
            _viewModel.DepthCompensation = GlobalParams.depthComp.ToString("F3");
            _viewModel.CutDirection = "----";
            _viewModel.SpindleRev = _model.SpindleRev;
            DataContext = _viewModel;
            // 设置切割初始参数
            CutOperateUtils.InitParams(1, mainWindow);
        }

        /// <summary>
         /// 设置当前通道
         /// </summary>
         /// <param name="channelNoValue"></param>
        public void SetChannelNo(string channelNoValue)
        {
            _viewModel.ChannelNum = channelNoValue;
        }

        private void repeatedCheckbox_Click(object sender, RoutedEventArgs e)
        {
            CutOperateUtils.repeatedFlag = repeatedCheckbox.IsChecked == true; 
        }

        private void z1CompCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            PlcControl.tagControl.cutting.SetZ1AxisCompStatus(0);
        }

        private void yCompCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            PlcControl.tagControl.cutting.SetYAxisCompStatus(1);
        }

        private void yCompCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            PlcControl.tagControl.cutting.SetYAxisCompStatus(0);
        }

        private void z1CompCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            PlcControl.tagControl.cutting.SetZ1AxisCompStatus(1);
        }
    }
}