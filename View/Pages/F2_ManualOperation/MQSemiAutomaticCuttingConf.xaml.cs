using System.Diagnostics;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Controls;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
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
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;
        bool runFlag = false;
        public MQSemiAutomaticCuttingConf()
        {
            InitializeComponent();
            mainWindow = Application.Current.MainWindow as MainWindow;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            rightPage = mainWindow.rightFrame.Content as RightPage;
            operatePage = mainWindow.operateFrame.Content as OperatePage;

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
            alignmentStatus = false;
            string type = QueryUtils.GetValueFromQueryParams(this, "type");
            if (!string.IsNullOrEmpty(type) && "1".Equals(type))
            {
                Tools.LogInfo("切割模式进入...");
            } else
            {
                // 进入半自动切割模式
                MaterialSnack("进入切割模式成功！", SnackType.WARNING);
                CutOperateUtils.thetaAlignFlag = false;
            }
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


        bool alignmentStatus = false;
        bool precutFlag = false;
        private void OperateClickHandler(object sender, int code)
        {
            switch (code)
            {
                case 2401:
                    float tempDepthCompensation = Tools.GetFloatStringValue(viewModel.DepthCompensation);
                    // 高度补偿
                    GlobalParams.depthComp = tempDepthCompensation;
                    MaterialSnack("刀片高度补偿设置成功！", SnackType.SUCCESS);
                    /*if (tempDepthCompensation != 0)
                    {
                        CutOperateUtils.SetBladeHeightComp(tempDepthCompensation);
                        MaterialSnack("刀片高度补偿设置成功！", SnackType.SUCCESS);
                    }*/
                    break;
                case 2403:
                    float tempChangeFeedSpeed = Tools.GetFloatStringValue(viewModel.ChangeFeedSpeed);
                    // 速度更改
                    CutOperateUtils.SetFeedSpeedComp(tempChangeFeedSpeed);
                    MaterialSnack("变更进刀速度成功！", SnackType.SUCCESS);
                    break;
                case 2023:
                    alignmentStatus = true;
                    // 手动校准 type 
                    mainWindow.NavigateToPage("Pages/F2_ManualOperation/MQManualAlignmentConf", "type=1");
                    break;
                case 2404:
                    if (precutFlag)
                    {
                        MaterialSnack("关闭预切割！", SnackType.SUCCESS);
                    } else
                    {
                        MaterialSnack("开启预切割！", SnackType.SUCCESS);
                    }
                    // 预切启动
                    CutOperateUtils.precutFlag = !precutFlag;
                    break;
                case 2405:
                    // 进入型号参数
                    alignmentStatus = true;
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
        // 关门状态 0 初始状态 1 按了一次
        int closeDoor2Status = 0;
        // theta校准状态 0 初始 1 按了一次
        int thetaAlignStatus = 0;
        // 开始切割
        private void StartCut(object sender, bool e)
        {
            if (runFlag)
            {
                Tools.LogWarning("切割操作中....");
                return;
            }
            runFlag = true;
            
            // 检查切割条件是否满足
            if (CommonCheck.CutStatusCheck(1))
            {
                // 判断是否已准备好切割
                if (!CutOperateUtils.IsReadyToCut())
                {
                    MaterialSnack("切割未准备好！", SnackType.WARNING);
                    runFlag = false;
                    return;
                }

                // 判断切割方向
                if (CutOperateUtils.cutDirection == -1)
                {
                    MaterialSnack("请设置切割方向！", SnackType.WARNING);
                    runFlag = false;
                    return;
                }
                // 判断预切割配置是否存在，不存在则提示
                if (CutOperateUtils.precutFlag)
                {
                    // 查询当前配置获取预切割开始编号
                    FileTableItemModel fileTableItemModel = CurrentUtils.GetFileTableItemModel();
                    // 查询当前预切割流程信息
                    PreCutModel preCutModel = CurrentUtils.GetPreCutModel();
                    if (preCutModel.Id == 0)
                    {
                        MaterialSnack("预切割参数没找到！", SnackType.WARNING);
                        runFlag = false;
                        return;
                    }
                }
                // 如果没有theta轴校准 则提示一次
                if (!CutOperateUtils.thetaAlignFlag)
                {
                    if (thetaAlignStatus == 0)
                    {
                        MaterialSnack("请先进行校准，再次按下开始将强制切割！", SnackType.WARNING);
                        thetaAlignStatus = 1;
                        runFlag = false;
                        return;
                    }
                    thetaAlignStatus = 0;
                }
                startCut();
                // 判断门是否开启状态 如果是，则二次提示自动关门
                /*if (!CommonCheck.GetDoorStatus(DeviceKey.securityDoor2StatusKey))
                {
                    if (closeDoor2Status == 0)
                    {
                        MaterialSnack("请再次点击开始按钮，将自动关闭推拉门，请注意安全！", SnackType.WARNING);
                        closeDoor2Status = 1;
                        thetaAlignStatus = 1;
                        runFlag = false;
                        return;
                    }
                    closeDoor2Status = 0;
                    runFlag = true;
                    PlcControl.tagControl.wholeDevice.OperateSecurityDoor2(0);
                    // 如果是自动关门，则判断门是否已经关闭，如果关闭，则自动开始切割
                    Task.Run(() =>
                    {
                        GlobalParams.globalRunFlag = true;
                        bool status = Tools.WaitForValue(DeviceKey.securityDoor2StatusKey, 1, 10);
                        GlobalParams.globalRunFlag = false;
                        if (status)
                        {
                            startCut();
                        } else
                        {
                            MaterialSnack("自动关闭推拉门失败！", SnackType.ERROR);
                        }

                    });
                } else
                {
                    startCut();
                }*/
            }
            else
            {
                runFlag = false;
            }
            
        }

        private void startCut()
        {
            if (!CutOperateUtils._disposed)
            {
                Debug.WriteLine("开始切割....");
                GlobalParams.globalRunFlag = true;
                runFlag = true;
                // 设置切割模式
                CutOperateUtils.cutMethod = CutOperateUtils.GetCutMethod(chModel.CutMode);
                // 设置是否交换位置
                CutOperateUtils.exchangeXPosition = false;
                CutOperateUtils.runCut(viewModel.CutLine);
                runFlag = false;
            }
        }

        private void CutBack(object sender, bool e)
        {
            // 回复切割面到Ch 1
            CurrentUtils.InitCutCh();
            // 退出切割模式
            PlcControl.tagControl.cutting.EnterFullAutoInit(0);
            mainWindow.NavigateToPage("MainMenu");
        }

        private void CutFront(object sender, bool e)
        {
            SetCutDirection(0);
        }
        private void CutBackward(object sender, bool e)
        {
            SetCutDirection(1);
        }

        /// <summary>
        /// 切割方向 0 前切 1 后切
        /// </summary>
        private void SetCutDirection(int cutDirection)
        {
            viewModel.CutDirection = cutDirection == 0 ? "向前切" : "向后切";
            CutOperateUtils.cutDirection = cutDirection;
        }

        MQSemiAutomaticCuttingConfViewModel viewModel;
        FileTableItemChModel chModel;
        private async void LoadConfigInfo()
        {
            // 查询当前配置信息

            FileTableItemModel _model = CurrentUtils.GetFileTableItemModel();
            BladeHeightModel bladeHeightModel = CurrentUtils.GetBladeHeightModel();
            // 获取当前channel
            chModel = CurrentUtils.GetFileTableItemChModel();
            // 设置当前配置信息的切割方法
            PlcControl.tagControl.cutting.StartCutMethod(CutOperateUtils.GetCutMethod(chModel.CutMode));
            // 获取刀片高度、进刀速度
            string bladeHeightStr = chModel.BladeHeight;
            string feedSpeedStr = chModel.FeedSpeed;
            string bladeHeight = bladeHeightStr.Split(",")[0];
            string feedSpeed = feedSpeedStr.Split(",")[0];
            viewModel = new MQSemiAutomaticCuttingConfViewModel();
            viewModel.DeviceDataNo = _model.DeviceDataNo + "";
            viewModel.DeviceDataId = _model.DeviceDataId;
            viewModel.ChannelNum = CurrentUtils.GetCurrentConfiguration().ChannelNum;
            viewModel.BladeHeight = bladeHeight;
            viewModel.FeedSpeed = feedSpeed;
            viewModel.CutLine = 0;
            viewModel.CutDepthOffset = "0.000";
            viewModel.ChangeFeedSpeed = "0.000";
            viewModel.DepthCompensation = GlobalParams.depthComp.ToString("F3");
            viewModel.CutDirection = "----";
            viewModel.SpindleRev = _model.SpindleRev;
            DataContext = viewModel;
            // 设置切割初始参数
            CutOperateUtils.InitParams(1, mainWindow);

        }

        

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (!alignmentStatus)
            {
                // CutBack(null, true);
            }
        }
        /// <summary>
         /// 设置当前通道
         /// </summary>
         /// <param name="channelNoValue"></param>
        public void SetChannelNo(string channelNoValue)
        {
            viewModel.ChannelNum = channelNoValue;
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