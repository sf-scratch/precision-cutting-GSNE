using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.common;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.common;
using 精密切割系统.View.Pages.F4_BladeMaintenance;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.Auto
{
    /// <summary>
    /// AutoCutPausing.xaml 的交互逻辑
    /// </summary>
    public partial class AutoCutPausing : UserControl
    {
        private MainWindow mainWindow;
        private bool runFlag = false;
        private bool stopCheckFlag = false;
        private bool cameraOffsetStatus = false;
        private float cameraCutOffset = 0;

        // 手动调整基准线标识
        private bool adjustDatumLineFlag = false;

        // 创建一个定时器
        private System.Timers.Timer timer = null;

        private static CameraCommon cameraCommon;

        public AutoCutPausing()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mainWindow = Application.Current.MainWindow as MainWindow;
            NavigateUtils.ClearOperatePage();
            //updateDefineDataModel();
        }

        //根据默认配置控制对应显示和隐藏
        private void updateDefineDataModel()
        {
            UserDefineDataModel userDefineModel = CurrentUtils.getUserDefineDataModel();
            bool isSpeedChange = "NO".Equals(userDefineModel.SpeedChange);
            bool isHeightChange = "NO".Equals(userDefineModel.HeightChange);
            //if (isSpeedChange)//速度变更
            //{
            //    SpeedChangePanel.Visibility = Visibility.Collapsed;
            //}
            //if (isHeightChange)//高度补偿
            //{
            //    HeightChangePanel.Visibility = Visibility.Collapsed;
            //}
            mainWindow.UpdateOperatePage(OperateData.GetSemiAutoCuttingStopOperate(!isSpeedChange, !isHeightChange)
                , OperateClickHandler, OperateTouchLeaveHandler, OperateTouchDownHandler);
        }

        private void UpdateMenu2()
        {
            mainWindow.UpdateOperatePage(OperateData.GetSemiAutoCuttingStopTwoOperate(), OperateClickHandler);
        }

        private void DisposeDatumLine(int code)
        {
            if (code == 23040)
            {
                cameraCommon?.SetEdgeWidth(-1, 2);
            }
            else if (code == 23041)
            {
                cameraCommon?.SetEdgeWidth(1, 2);
            }
            else if (code == 23407)
            {
                cameraCommon?.SetCutMarkWidth(-1, 2);
            }
            else if (code == 23408)
            {
                cameraCommon?.SetCutMarkWidth(1, 2);
            }

            //viewModel.CutWidth = Tools.GetDoubleStringValue(Tools.FormatDecimalString((cameraCommon._cutMarkWidth / 1000).ToString(), 4));
            //viewModel.DdgesWidth = Tools.GetDoubleStringValue(Tools.FormatDecimalString((cameraCommon._edgeChipWidth / 1000).ToString(), 4));
        }

        public void OperateTouchDownHandler(object sender, int code)
        {
            switch (code)
            {
                case 23407:
                // 基准线调窄
                case 23408:
                // 基准线调宽
                case 23040:
                // 崩边调窄
                case 23041:
                    DisposeDatumLine(code); // 初始调用 DisposeDatumLine
                    adjustDatumLineFlag = true;
                    if (timer != null)
                    {
                        timer.Stop();
                    }
                    // 创建定时器
                    timer = new System.Timers.Timer
                    {
                        Interval = 500, // 初始延迟 500 毫秒
                        AutoReset = false // 每次触发后需要手动重新启动
                    };
                    timer.Elapsed += (sender, e) =>
                    {
                        if (timer != null)
                        {
                            if (!adjustDatumLineFlag)
                            {
                                timer.Stop();
                                timer.Dispose(); // 释放资源
                                return;
                            }

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                DisposeDatumLine(code);
                            });

                            // 重新设置间隔为 100 毫秒并重新启动定时器
                            timer.Interval = 100;
                            timer.Start();
                        }
                    };

                    timer.Start(); // 启动定时器
                    break;

                default:
                    break;
            }
        }

        public void OperateTouchLeaveHandler(object sender, int code)
        {
            switch (code)
            {
                case 23407:
                // 基准线调窄
                case 23408:
                // 基准线调宽
                case 23040:
                // 崩边调窄
                case 23041:
                    adjustDatumLineFlag = false;
                    if (timer != null)
                    {
                        timer.Stop();
                        timer.Dispose();
                        timer = null;
                    }
                    break;

                default:
                    break;
            }
        }

        public void OperateClickHandler(object sender, int code)
        {
            switch (code)
            {
                case 2409:
                    // 确认基准线
                    string yCurrentPosition = PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey);
                    float offset = CutOperateUtils.yStopLocation - float.Parse(yCurrentPosition);
                    GlobalParams.cameraOffsetY = cameraCutOffset + offset;
                    MaterialSnackUtils.MaterialSnack("基准线已确认！", MaterialSnackUtils.SnackType.SUCCESS);
                    Tools.LogInfo($"最新基准线：{GlobalParams.cameraOffsetY}");
                    cameraOffsetStatus = true;
                    break;

                case 2401:
                    //float tempDepthCompensation = Tools.GetFloatStringValue(viewModel.DepthCompensation);
                    //// 高度补偿
                    //GlobalParams.depthComp = tempDepthCompensation;
                    // CutOperateUtils.SetBladeHeightComp(tempDepthCompensation);
                    MaterialSnackUtils.MaterialSnack("刀片高度补偿设置成功！", MaterialSnackUtils.SnackType.SUCCESS);
                    break;

                case 2403:
                    //float tempChangeFeedSpeed = Tools.GetFloatStringValue(viewModel.ChangeFeedSpeed);
                    //// 速度更改
                    //CutOperateUtils.SetFeedSpeedComp(tempChangeFeedSpeed);
                    MaterialSnackUtils.MaterialSnack("变更进刀速度成功！", MaterialSnackUtils.SnackType.SUCCESS);
                    break;

                case 2442:
                    // 聚焦
                    if (!CommonCheck.FocusStatsCheck())
                    {
                        break;
                    }
                    CommonOperate.GetInstance().AutoFocus(2, mainWindow, null);
                    break;

                case 2412:
                    UpdateMenu2();
                    // 调光
                    ShowDimming(1);
                    break;

                case 2411:
                    updateDefineDataModel();
                    // 调光
                    ShowDimming(0);
                    break;

                case 2422:
                    // 刀片状态信息
                    mainWindow.NavigateToPage("Pages/F4_BladeMaintenance/BladeInfo", "pageName=Pages/F2_ManualOperation/MQSemiAutomaticCuttingStop");
                    break;

                case 2405:
                    mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCDeviceDataListConf");
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// 显示调光
        /// </summary>
        /// <param name="status">0 不显示 1 显示</param>
        public void ShowDimming(int status)
        {
            if (status == 0)
            {
                //dimmingGrid.Visibility = Visibility.Collapsed;
                //cutLineWidthGrid.Visibility = Visibility.Visible;
                //linesRecordGrid.Visibility = Visibility.Visible;
                //compGrid.Visibility = Visibility.Visible;
            }
            else
            {
                //dimmingGrid.Visibility = Visibility.Visible;
                //cutLineWidthGrid.Visibility = Visibility.Collapsed;
                //linesRecordGrid.Visibility = Visibility.Collapsed;
                //compGrid.Visibility = Visibility.Collapsed;
            }
        }

        // 继续切割
        private void ReCutStart(object sender, bool e)
        {
            if (runFlag)
            {
                // MaterialSnackUtils.showOperateLimitMsg();
                return;
            }
            if (!cameraOffsetStatus)
            {
                MaterialSnackUtils.MaterialSnack("请先进行基准线校准！", MaterialSnackUtils.SnackType.WARNING, 0);
                return;
            }
            cameraOffsetStatus = false;
            runFlag = true;
            // 设置是否交换位置
            CutOperateUtils.exchangeXPosition = false;
            GlobalParams.globalRunFlag = true;
            CutOperateUtils.stopCheckFlag = stopCheckFlag;
            CutOperateUtils.pauseFlag = false;
            Thread.Sleep(10);
            runFlag = false;
            CutOperateUtils.checkStatus = false;
        }

        // 执行校准检查
        private double[] PerformAlignmentCheck()
        {
            // 调用刀痕和崩边识别，获取刀痕宽度、角度等信息
            Tools.WaitForValue(PlcControl.allTags[DeviceKey.z2CurSpeedKey], "0");
            // 对焦完成后，测量刀痕宽度
            double[] widthInfo = [0, 0];
            try
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                string fileName = $"check_{timestamp}.png";
                bool flag = cameraCommon.SaveWriteableBitmap(fileName);
                if (flag)
                {
                    Thread.Sleep(500);
                    widthInfo = CommonOperate.GetCutEdgeWidth(fileName);
                    Task.Run(() =>
                    {
                        Thread.Sleep(2000);
                        // 删除照片
                        Tools.DeleteFile(fileName);
                    });
                }
            }
            catch (Exception e)
            {
                Tools.LogError("识别失败！原因：" + e.Message);
            }
            if (widthInfo == null || widthInfo[0] == 0 || widthInfo[1] == 0)
            {
                return widthInfo;
            }
            Thread.Sleep(100);
            // 设置识别出来的刀痕线
            double cutWidth = CameraOperateUtils.ConvertToPictureBoxSize(widthInfo[0]);
            double edgesWidth = CameraOperateUtils.ConvertToPictureBoxSize(widthInfo[1]);
            cameraCommon.DrawLineForWidth((float)cutWidth, (float)edgesWidth);
            //viewModel.CutWidth = Tools.GetDoubleStringValue(Tools.FormatDecimalString((cameraCommon._cutMarkWidth / 1000).ToString(), 4));
            //viewModel.DdgesWidth = Tools.GetDoubleStringValue(Tools.FormatDecimalString((cameraCommon._edgeChipWidth / 1000).ToString(), 4));
            return widthInfo;
        }

        private void cutRecognition_Click(object sender, RoutedEventArgs e)
        {
            MaterialSnackUtils.MaterialSnack("识别中...", MaterialSnackUtils.SnackType.WARNING, 0);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string fileName = $"check_{timestamp}.png";
            cameraCommon.SaveWriteableBitmap(fileName);
            Thread.Sleep(1000);
            double[] widthInfo = CommonOperate.GetCutEdgeWidth(fileName);
            if (widthInfo == null || widthInfo[0] == 0 || widthInfo[1] == 0)
            {
                return;
            }
            double cutWidthValue = CameraOperateUtils.ConvertToPictureBoxSize(widthInfo[0]);
            double edgesWidthValue = CameraOperateUtils.ConvertToPictureBoxSize(widthInfo[1]);
            cameraCommon.DrawLineForWidth((float)cutWidthValue, (float)edgesWidthValue);
            //viewModel.CutWidth = Tools.GetDoubleStringValue(Tools.FormatDecimalString((cameraCommon._cutMarkWidth / 1000).ToString(), 4));
            //viewModel.DdgesWidth = Tools.GetDoubleStringValue(Tools.FormatDecimalString((cameraCommon._edgeChipWidth / 1000).ToString(), 4));
            MaterialSnackUtils.MaterialSnack("识别完成！", MaterialSnackUtils.SnackType.SUCCESS);
        }

        private void stopCheckCheckbox_Click(object sender, RoutedEventArgs e)
        {
            //stopCheckFlag = stopCheckCheckbox.IsChecked == true;
        }

        private void repeatedCheckbox_Click(object sender, RoutedEventArgs e)
        {
            //CutOperateUtils.repeatedFlag = repeatedCheckbox.IsChecked == true;
        }
    }
}