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
using 精密切割系统.Helpers;
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
        private OperatePage operatePage;

        public MQSemiAutomaticCuttingConf()
        {
            InitializeComponent();
            _semiAutoCutService = SemiAutoCutService.Instance;
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
            UpdateDefineDataModel();
            // 初始化配置
            LoadConfigInfo();
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
            _viewModel.ChangeFeedSpeed = _semiAutoCutService.FeedSpeedCompCompensationValue.ToString();
            _viewModel.DepthCompensation = _semiAutoCutService.DepthCompensationValue.ToString();
            _viewModel.CutDirection = "----";
            _viewModel.SpindleRev = _model.SpindleRev;
            DataContext = _viewModel;
            // 设置切割初始参数
            CutOperateUtils.InitParams(1, mainWindow);
        }

        private async void OperateClickHandler(object? sender, int code)
        {
            switch (code)
            {
                case 2401:
                    float tempDepthCompensation = Tools.GetFloatStringValue(_viewModel.DepthCompensation);
                    // 高度补偿
                    _semiAutoCutService.DepthCompensationValue = tempDepthCompensation;
                    MaterialSnack("刀片高度补偿设置成功！", SnackType.SUCCESS);
                    break;

                case 2403:
                    float tempChangeFeedSpeed = Tools.GetFloatStringValue(_viewModel.ChangeFeedSpeed);
                    // 速度更改
                    _semiAutoCutService.FeedSpeedCompCompensationValue = tempChangeFeedSpeed;
                    MaterialSnack("变更进刀速度成功！", SnackType.SUCCESS);
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
                    if (_semiAutoCutService.IsOpenPrecut)
                    {
                        MaterialSnack("关闭预切割！", SnackType.SUCCESS);
                    }
                    else
                    {
                        MaterialSnack("开启预切割！", SnackType.SUCCESS);
                    }
                    // 预切启动
                    _semiAutoCutService.IsOpenPrecut = !_semiAutoCutService.IsOpenPrecut;
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
        private void StartCut(object? sender, bool e)
        {
            if (AlarmConfig.Instance.HasActiveErrorAlarm())
            {
                MaterialSnack(AlarmConfig.HasErrorAlarmMessage, SnackType.WARNING);
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
            _semiAutoCutService.CutLine = _viewModel.CutLine;
            _semiAutoCutService.SpindleRev = _viewModel.SpindleRev;
            ContainerLocator.Container.Resolve<IRegionManager>().RequestNavigate(RegionName.MainRegion, nameof(MQSemiAutomaticCuttingRun));
        }

        private void CutBack(object? sender, bool e)
        {
            // 回复切割面到Ch 1
            //CurrentUtils.InitCutCh();
            WarmUpHelper.StopWarmUp();
            SemiAutoCutService.Instance.HasNotTakenOutWorkpiecesAfterCuttingCompleted = false;
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