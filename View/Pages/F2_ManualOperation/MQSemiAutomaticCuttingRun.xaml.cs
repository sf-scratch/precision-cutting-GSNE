using System.Diagnostics;
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

namespace 精密切割系统.View.Pages.F2_ManualOperation
{
    /// <summary>
    /// MQSemiAutomaticCuttingRun.xaml 的交互逻辑
    /// </summary>
    public partial class MQSemiAutomaticCuttingRun : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;
        MQSemiAutomaticCuttingRunViewModel viewModel;
        bool runFlag = false;
        bool monitorFlag = false;
        public MQSemiAutomaticCuttingRun()
        {
            InitializeComponent();
            mainWindow = Application.Current.MainWindow as MainWindow;
            
        }
        int repeatedCount = 1;
        int allRunCutLine = 0;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            rightPage = mainWindow.rightFrame.Content as RightPage;
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnCutPause.Visibility = Visibility.Visible;
            rightPage.btnCutPause.GlobalRunOperateFlag = true;
            rightPage.btnCutPause.SetRightClickedHandler(PauseCut);
            GlobalParams.cutStatusInfo = 1;
            // 加载参数
            FileTableItemModel _model = CurrentUtils.GetFileTableItemModel();
            BladeHeightModel bladeHeightModel = CurrentUtils.GetBladeHeightModel();
            // 获取当前channel
            FileTableItemChModel chModel = CurrentUtils.GetFileTableItemChModel();
            // 获取刀片高度、进刀速度
            string bladeHeightStr = chModel.BladeHeight;
            string feedSpeedStr = chModel.FeedSpeed;
            string bladeHeight = bladeHeightStr.Split(",")[0];
            string feedSpeed = feedSpeedStr.Split(",")[0];
            viewModel = new MQSemiAutomaticCuttingRunViewModel();
            viewModel.DeviceDataNo = _model.DeviceDataNo + "";
            viewModel.DeviceDataId = _model.DeviceDataId;
            viewModel.ChannelNum = CurrentUtils.GetCurrentConfiguration().ChannelNum;
            
            viewModel.FeedSpeed = feedSpeed;
            viewModel.ChangeFeedSpeed = CutOperateUtils.feedSpeedComp + "";
            viewModel.DepthCompensation = GlobalParams.depthComp + "";
            viewModel.BladeHeight = (Tools.GetFloatStringValue(bladeHeight) + GlobalParams.depthComp).ToString("F4");
            viewModel.AllCutLine = GlobalParams.cutAllNum;
            viewModel.AllCutLineLength = GlobalParams.cutAllDistance + "";
            viewModel.RunCutLine = 1;
            int modelCutLine = Tools.GetIntStringValue(chModel.CutLine);
            // 根据自定义刀数设置运行刀数
            viewModel.AllRunCutLine = CutOperateUtils._cutLineNum > 0
                ? (CutOperateUtils._cutLineNum > modelCutLine ? modelCutLine : CutOperateUtils._cutLineNum)
                : Convert.ToInt32(chModel.CutLine);
            allRunCutLine = viewModel.AllRunCutLine;
            DataContext = viewModel;
            updateDefineDataModel();
            repeatedCheckbox.IsChecked = CutOperateUtils.repeatedFlag;
            // 调用开始切割
            StartCut();
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
            GlobalParams.globalRunEnableOperateBtnCodes.Add(24221);
            GlobalParams.globalRunEnableOperateBtnCodes.Add(24051);
            // 底部菜单
            mainWindow.UpdateOperatePage(OperateData.GetSemiAutoCuttingRunOperate(!isSpeedChange, !isHeightChange), OperateClickHandler);
        }

        public void OperateClickHandler(object sender, int code)
        {
            switch (code)
            {
                case 2401:
                    float tempDepthCompensation = Tools.GetFloatStringValue(viewModel.DepthCompensation);
                    // 高度补偿
                    GlobalParams.depthComp = tempDepthCompensation;
                    MaterialSnackUtils.MaterialSnack("刀片高度补偿设置成功！", MaterialSnackUtils.SnackType.SUCCESS);
                    break;
                case 2403:
                    float tempChangeFeedSpeed = Tools.GetFloatStringValue(viewModel.ChangeFeedSpeed);
                    // 速度更改
                    CutOperateUtils.SetFeedSpeedComp(tempChangeFeedSpeed);
                    viewModel.ChangeFeedSpeed = CutOperateUtils.feedSpeedComp + "";
                    MaterialSnackUtils.MaterialSnack("变更进刀速度成功！", MaterialSnackUtils.SnackType.SUCCESS);
                    break;
                case 24221:
                    // 刀片状态信息
                    mainWindow.NavigateToPage("Pages/F4_BladeMaintenance/BladeInfo", "pageName=Pages/F2_ManualOperation/MQSemiAutomaticCuttingRun");
                    break;
                case 24051:
                    mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCDeviceDataListConf");
                    break;
                default:
                    break;
            }

        }

        /// <summary>
        /// 开始切割
        /// </summary>
        private void StartCut()
        {
            monitorFlag = true;
            // 监听并更新刀数
            Task.Run(() => {
                do
                {
                    // 设置切割进度
                    viewModel.RunCutLine = CutOperateUtils.chCurrentCutLine;
                    viewModel.AllCutLine = GlobalParams.cutAllNum;
                    viewModel.AllCutLineLength = Tools.FormatDecimalString(GlobalParams.cutAllDistance.ToString(), 4);
                    viewModel.ChannelNum = CurrentUtils.GetCurrentConfiguration().ChannelNum;
                    // 设置相关信息
                    viewModel.FeedSpeed = CutOperateUtils.currentFeedSpeed + "";
                    
                    repeatedCount = CutOperateUtils.repeatedCount;
                    viewModel.AllRunCutLine = CutOperateUtils.allRunCutLine;
                    Application.Current.Dispatcher.Invoke(() =>
                    {

                        
                        yAxisCutPosition.Text = CutOperateUtils.globalYCutPosition.ToString();
                        zAxisCutPosition.Text = CutOperateUtils.globalZCutPosition.ToString();
                        double yCurrentPositionTemp = Tools.GetDoubleStringValue(PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey));
                        xAxisCutPosition.Text = (yCurrentPositionTemp - CutOperateUtils.globalYCutPosition).ToString("F5");
                        // 设置当前位置
                        xAxisCurrentPosition.Text = (Tools.GetDoubleStringValue(PlcControl.plc.GetPlcValueString(DeviceKey.curLocationKey))).ToString("F5");
                        yAxisCurrentPosition.Text = (yCurrentPositionTemp).ToString("F5");
                        zAxisCurrentPosition.Text = (Tools.GetDoubleStringValue(PlcControl.plc.GetPlcValueString(DeviceKey.z1CurLocationKey))).ToString("F5");
                        thetaAxisCurrentPosition.Text = (Tools.GetDoubleStringValue(PlcControl.plc.GetPlcValueString(DeviceKey.thetaCurLocationKey))).ToString("F5");
                    });
                    Thread.Sleep(100);
                } while (CutOperateUtils._disposed && monitorFlag);
            });
        }

        // 暂停切割
        private void PauseCut(object sender, bool e)
        {
            // 判断切割是否开始
            if (CutOperateUtils.IsReadyToCut())
            {
                return;
            }

            if (runFlag)
            {
                Tools.LogWarning("操作频繁！");
                // MaterialSnackUtils.showOperateLimitMsg();
                return;
            }
            // 设置全局运行参数为true 防止误操作
            runFlag = true;
            Debug.WriteLine("正在暂停切割...", 0);
            // 设置暂停超时时间 根据切割速度来计算 
            int runTime = (int)(150 / CutOperateUtils.currentFeedSpeed);
            // 运行时间 + 10秒动作时间 + 20秒余量时间
            runTime += 10 + 20;
            PlcControl.tagControl.cutting.SetCutStopDelayTime(runTime);
            CutOperateUtils.stopDelayTime = runTime;
            Tools.LogInfo($"暂停超时时间：{runTime}");
            // 发送暂停信号
            PlcControl.tagControl.cutting.StopCut(1);
            MaterialSnackUtils.MaterialSnack("正在暂停切割...", MaterialSnackUtils.SnackType.WARNING, 0);
            CutOperateUtils.checkStatus = true;
            Task.Run(() => {
                DateTime startTime = DateTime.Now;
                bool flag = CutOperateUtils.MonitorCutStatusFalse("False", runTime * 1000);
                runFlag = false;
            });
        }
        public static int FindLastNonZeroIndex(float[] array)
        {
            // 方法一：从后向前遍历
            for (int i = array.Length - 1; i >= 0; i--)
            {
                if (array[i] != 0)
                {
                    return i;
                }
            }

            // 如果所有元素都为 0，返回 -1 表示未找到
            return -1;
        }
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            monitorFlag = false;
        }

        private void repeatedCheckbox_Click(object sender, RoutedEventArgs e)
        {
            CutOperateUtils.repeatedFlag = repeatedCheckbox.IsChecked == true;
        }
    }
}
