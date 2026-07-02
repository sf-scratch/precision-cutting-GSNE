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
using 精密切割系统.Extensions;
using 精密切割系统.Helpers;
using 精密切割系统.Helpers.GTN;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.MeasureHeight;
using 精密切割系统.Model.plc;
using 精密切割系统.PubSubEvent;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.View.Pages.F4_BladeMaintenance;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.F2_ManualOperation
{
    /// <summary>
    /// MQSemiAutomaticCuttingConf.xaml 的交互逻辑
    /// </summary>
    public partial class MQSemiAutomaticCuttingConf : Page
    {
        private readonly SemiAutoCutService _semiAutoCutService;
        private MQSemiAutomaticCuttingConfViewModel _viewModel;

        private MainWindow mainWindow;
        private RightPage rightPage;
        private CancellationTokenSource _cts;

        public MQSemiAutomaticCuttingConf()
        {
            InitializeComponent();
            _semiAutoCutService = SemiAutoCutService.Instance;
            mainWindow = Application.Current.MainWindow as MainWindow ?? new MainWindow();
            _cts = new CancellationTokenSource();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_cts.IsCancellationRequested)
            {
                _cts = new CancellationTokenSource();
            }
            rightPage = mainWindow.rightFrame.Content as RightPage ?? new RightPage();
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.SetRightClickedHandler(CutBack);
            rightPage.btnCutStart.Visibility = Visibility.Visible;
            rightPage.btnCutStart.SetRightClickedHandler(StartCut);
            rightPage.btnCutBackward.Visibility = Visibility.Visible;
            rightPage.btnCutBackward.SetRightClickedHandler((a, b) => CutBackward());
            rightPage.btnCutFront.Visibility = Visibility.Visible;
            rightPage.btnCutFront.SetRightClickedHandler((a, b) => CutFront());
            GlobalParams.cutStatusInfo = 0;
            UpdateDefineDataModel();
            // 初始化配置
            LoadConfigInfo();
            _ = Task.Run(() => StartMonitorCurrentChAsync(_cts.Token));
        }

        private async Task StartMonitorCurrentChAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                string currentCh = CurrentUtils.GetCurrentCh();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_viewModel.ChannelNum != currentCh)
                    {
                        _viewModel.ChannelNum = currentCh;
                    }
                });
                await Task.Delay(500);
            }
        }

        //根据默认配置控制对应显示和隐藏
        private async void UpdateDefineDataModel()
        {
            UserDefineDataModel userDefineModel = await SqlHelper.GetOrCreateEntityAsync(() => new UserDefineDataModel());
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

        private void LoadConfigInfo()
        {
            // 查询当前配置信息
            FileTableItemModel _model = CurrentUtils.GetFileTableItemModel();
            BladeHeightModel bladeHeightModel = CurrentUtils.GetBladeHeightModel();
            // 获取当前channel
            FileTableItemChModel chModel = CurrentUtils.GetFileTableItemChModel();
            // 获取刀片高度、进刀速度
            string bladeHeightStr = chModel.BladeHeight;
            string feedSpeedStr = chModel.FeedSpeed;
            string bladeHeight = bladeHeightStr.Split(",")[0];
            string feedSpeed = feedSpeedStr.Split(",")[0];
            _viewModel = new MQSemiAutomaticCuttingConfViewModel();
            _viewModel.DeviceDataNo = _model.DeviceDataNo + "";
            _viewModel.DeviceDataId = _model.DeviceDataId;
            _viewModel.BladeHeight = bladeHeight;
            _viewModel.FeedSpeed = feedSpeed;
            _viewModel.CutLine = 0;
            _viewModel.CutDepthOffset = "0.000";
            _viewModel.ChangeFeedSpeed = _semiAutoCutService.FeedSpeedCompCompensationValue.ToString();
            _viewModel.DepthCompensation = _semiAutoCutService.DepthCompensationValue.ToString();
            _viewModel.CutDirection = "----";
            _viewModel.SpindleRev = _model.SpindleRev;
            DataContext = _viewModel;
            // 设置切割初始参数
            CutOperateUtils.InitParams(1, mainWindow);
            if (chModel.CutDir == "向前切")
            {
                CutFront();
            }
            else if (chModel.CutDir == "向后切")
            {
                CutBackward();
            }
        }

        private async void OperateClickHandler(object? sender, int code)
        {
            switch (code)
            {
                case 2401:
                    if (this.HasFormError())
                    {
                        MaterialSnack(RegionUtils.FormErrorMessage, SnackType.WARNING);
                        return;
                    }
                    _semiAutoCutService.DepthCompensationValue = _viewModel.DepthCompensation.ToFloat();
                    MaterialSnack($"刀片高度补偿设置为 {_semiAutoCutService.DepthCompensationValue}！", SnackType.SUCCESS);
                    break;

                case 2403:
                    if (this.HasFormError())
                    {
                        MaterialSnack(RegionUtils.FormErrorMessage, SnackType.WARNING);
                        return;
                    }
                    _semiAutoCutService.FeedSpeedCompCompensationValue = _viewModel.ChangeFeedSpeed.ToFloat();
                    MaterialSnack($"变更进刀速度设置为 {_semiAutoCutService.FeedSpeedCompCompensationValue}！", SnackType.SUCCESS);
                    break;

                case 2023:
                    if (mainWindow == null)
                    {
                        MaterialSnack($"{nameof(mainWindow)}为空", SnackType.WARNING);
                        return;
                    }
                    CommonResult result = await AutoCutUtils.EnterManualAlignmentAsync(mainWindow);
                    if (result.IsSuccess)
                    {
                        mainWindow.NavigateToPage("Pages/F2_ManualOperation/MQManualAlignmentConf", "type=1");
                    }
                    else
                    {
                        MaterialSnack(result.Message, SnackType.WARNING);
                    }
                    break;

                case 2404:
                    _semiAutoCutService.TriggerPrecut(true);
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

                case 5001:
                    // 暖机
                    _ = WarmUpHelper.TriggerWarmUpAsync();
                    break;

                default:
                    break;
            }
        }

        // 开始切割
        private async void StartCut(object? sender, bool e)
        {   //真空报警合集
            if (IoAlarm.Instance.HasAnyAlarm)
            {
                MaterialSnack(AlarmConfig.HasErrorAlarmMessage, SnackType.WARNING);
                return;
            }

            bool axisReady = await GsneMotion.Instance.WaitReadyCuttingAsync();
            if (!axisReady)
            {
                MaterialSnack("轴未就绪，请检查！", SnackType.WARNING);
                return;
            }
            if (WarmUpHelper.IsRuning)
            {
                MaterialSnack("请先结束暖机再开始切割！", SnackType.WARNING);
                return;
            }
            if (_viewModel.CutDirection == "----")
            {
                MaterialSnack("请选择切割方向！", SnackType.WARNING);
                return;
            }
            if (Appsettings.BladeOuterDiameter is null)
            {
                MaterialSnack("未设置刀片外径！", SnackType.WARNING);
                return;
            }
            CommonResult<ChCutStep> cutStepResult = await AutoCutUtils.GenerateSingleSideCutStepListAsync();
            if (!cutStepResult.IsSuccess || cutStepResult.Data is null)
            {
                MaterialSnack(cutStepResult.Message, SnackType.WARNING);
                return;
            }
            var currentY = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync();
            if (currentY is null)
            {
                if (GlobalParams.OnlineFlag)
                {
                    MaterialSnack("获取Y轴当前位置失败！", SnackType.WARNING);
                    return;
                }
                else
                {
                    currentY = 0;
                }
            }
            var userDefineData = await SqlHelper.GetOrCreateEntityAsync(() => new UserDefineDataModel());
            float cutYPositiveLimit = userDefineData.CutYPositiveLimit.ToFloat();
            float cutYNegativeLimit = userDefineData.CutYNegativeLimit.ToFloat();
            ChCutStep chCutStep = cutStepResult.Data;
            if (chCutStep is null || chCutStep.CutSteps == null || chCutStep.CutSteps.Count == 0)
            {
                MaterialSnack("生成切割步骤失败！", SnackType.WARNING);
                return;
            }
            CutStep firtStep = chCutStep.CutSteps.First();
            float yPositon = firtStep.IsAbsolute ? firtStep.ChannelStartY : currentY.Value.ToActualY() - firtStep.ChannelStartY;
            for (int i = 0; i < chCutStep.CutSteps.Count; i++)
            {
                if (yPositon > cutYPositiveLimit)
                {
                    if (userDefineData.IsAllowedCutting)
                    {
                        var newCutSteps = chCutStep.CutSteps.Take(i).ToList();
                        chCutStep.CutSteps.Clear();
                        chCutStep.CutSteps.AddRange(newCutSteps);
                        break;
                    }
                    else
                    {
                        MaterialSnack($"{chCutStep.ChName}面 第{i + 1}刀，将超出切割正限位！", SnackType.WARNING);
                        return;
                    }
                }
                else if (yPositon < cutYNegativeLimit)
                {
                    if (userDefineData.IsAllowedCutting)
                    {
                        var newCutSteps = chCutStep.CutSteps.Take(i).ToList();
                        chCutStep.CutSteps.Clear();
                        chCutStep.CutSteps.AddRange(newCutSteps);
                        break;
                    }
                    else
                    {
                        MaterialSnack($"{chCutStep.ChName}面 第{i + 1}刀，将超出切割负限位！", SnackType.WARNING);
                        return;
                    }
                }
                CutStep cutStep = chCutStep.CutSteps[i];
                switch (_semiAutoCutService.CutDirection)
                {
                    case CutDirection.Backward:
                        yPositon -= cutStep.NextStepDistance;
                        break;

                    case CutDirection.Forward:
                        yPositon += cutStep.NextStepDistance;
                        break;

                    default: break;
                }
            }
            _semiAutoCutService.CutLine = _viewModel.CutLine;
            _semiAutoCutService.SpindleRev = _viewModel.SpindleRev;
            ChCutStep updateChCutStep = chCutStep with { Direction = _semiAutoCutService.CutDirection };
            NavigationParameters parameters = new() { { "cutSteps", new List<ChCutStep>() { updateChCutStep } } };
            ContainerLocator.Container.Resolve<IRegionManager>().RequestNavigate(RegionName.MainRegion, nameof(MQSemiAutomaticCuttingRun), parameters);
        }

        private async void CutBack(object? sender, bool e)
        {
            var operationParams = await CurrentUtils.GetOperationParametersModelAsync();
            if (operationParams.IsExitCutClearManualCompensation)
            {
                _semiAutoCutService.DepthCompensationValue = 0;
            }
            _semiAutoCutService.FeedSpeedCompCompensationValue = 0;
            _cts.Cancel();
            mainWindow.NavigateToPage("MainMenu");
        }

        private void CutFront()
        {
            _viewModel.CutDirection = "向前切";
            _semiAutoCutService.CutDirection = CutDirection.Forward;
        }

        private void CutBackward()
        {
            _viewModel.CutDirection = "向后切";
            _semiAutoCutService.CutDirection = CutDirection.Backward;
        }

        private void repeatedCheckbox_Click(object sender, RoutedEventArgs e)
        {
            CutOperateUtils.repeatedFlag = repeatedCheckbox.IsChecked == true;
        }

        private void z1CompCheckbox_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void yCompCheckbox_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void yCompCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
        }

        private void z1CompCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
        }
    }
}