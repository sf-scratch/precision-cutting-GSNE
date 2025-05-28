using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.common;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;
using static 精密切割系统.Helpers.MaterialSnackUtils;

namespace 精密切割系统.View.Pages.F2_ManualOperation
{
    /// <summary>
    /// MQManualAlignmentConf.xaml 的交互逻辑
    /// </summary>
    public partial class MQManualAlignmentConf : Page
    {
        public MQManualAlignmentConf()
        {
            InitializeComponent();
            mainWindow = Application.Current.MainWindow as MainWindow;
        }
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;
        // hi-speed状态 0 低速 1 高速
        private int hiSpeedStatus = 0;
        // 高速的倍率
        private double multipleNum = 0.1;
        // 扫描速度
        private double scanSpeed = 1;
        // 操作类型 0 菜单进入 1 半自动进入 2 磨刀进入
        private int operateType = 0;
        // 相机操作对象
        CameraCommon cameraCommon;
        // 手动调整基准线标识
        bool adjustDatumLineFlag = false;
        // 创建一个定时器
        System.Timers.Timer timer = null;
        //获取参数
        string IdStr;
        string Flag;
        string BladeLotID;
        // 当前页面状态 0 校准 1 对焦 2 测量 根据状态不同，退出的操作不同
        int pageStatus = 0;
        // 读取轴实时位置标识
        bool axisRealTimeFlag = true;
        // 清零后X位置
        string cleanXPosition = "";
        // 清零后Y位置
        string cleanYPosition = "";



        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // 加载右边和底部按钮
            rightPage = mainWindow.rightFrame.Content as RightPage;
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(bakClickHandle);

            rightPage.btnSure.Visibility = Visibility.Visible;
            rightPage.btnSure.SetRightClickedHandler(sureHandler);
            mainWindow.UpdateOperatePage(OperateData.GetManualAlignmentOperate(), ClickHandler, TouchLeaveHandler, TouchDownHandler);
            string type = QueryUtils.GetValueFromQueryParams(this, "type");
            if (!string.IsNullOrEmpty(type))
            {
                operateType = int.Parse(type);
            }
            if ("2".Equals(type))
            {
                //获取参数
                IdStr = QueryUtils.GetValueFromQueryParams(this, "Id");
                Flag = QueryUtils.GetValueFromQueryParams(this, "Flag");
                BladeLotID = QueryUtils.GetValueFromQueryParams(this, "BladeLotID");
            }
            // 设置相关参数
            channelNo.Text = CurrentUtils.GetCurrentConfiguration().ChannelNum;
            // 获取相机操作对象
            // 获取相机页面
            List<CameraCommon> cameraCommons = Tools.GetChildrenOfType<CameraCommon>(mainWindow.mainFrame);
            if (cameraCommons.Count == 0)
            {
                MaterialSnack("相机获取失败！", SnackType.WARNING);
                return;
            }
            cameraCommon = cameraCommons[0];
            if (operateType == 0)
            {
                MaterialSnack("进入校准模式成功！", SnackType.WARNING);
            } else
            {
                // 其它模式進入后，自动打开门
                // PlcControl.tagControl.wholeDevice.OperateSecurityDoor2(1);
            }
            CommonOperate.xLocation = 0;
            cutWidth.Text = Tools.FormatDecimalString((cameraCommon._cutMarkWidth / 1000).ToString(), 4);
            edgesWidth.Text = Tools.FormatDecimalString((cameraCommon._edgeChipWidth / 1000).ToString(), 4);
            // 开启插补
            PlcControl.tagControl.wholeDevice.SetInterpositionStatus(1);
            LoadPosition();
        }
        bool confirmFlag = false;
        private void sureHandler(object sender, bool e)
        {
            // 判断是否Theta轴拉直 等于0 说明没有做Theta轴校准
            if (CommonOperate.xLocation == 0 && CommonOperate.xVerticalLocation == 0 && !confirmFlag)
            {
                MaterialSnack("请先进行校准，再次按下开始将强制切割！", SnackType.WARNING);
                confirmFlag = true;
                return;
            }
            if (CommonOperate.xLocation == 1 || CommonOperate.xVerticalLocation == 1)
            {
                MaterialSnack("请再次点击Theta轴校准，完成校准！", SnackType.WARNING);
                return;
            }
            // 根据当前的切割面，设置开始切割位置
            SetChCutStartPosition();
            ToNextPage();
        }

        private void SetChCutStartPosition()
        {
            string currentCh = CurrentUtils.GetCurrentChNo();
            string currentYPositionStr = PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey);
            float currentYPosition = Tools.GetFloatStringValue(currentYPositionStr);
            switch (currentCh)
            {
                case "Ch 1":
                    GlobalParams.ch1CutStartPosition = currentYPosition;
                    break;
                case "Ch 2":
                    GlobalParams.ch2CutStartPosition = currentYPosition;
                    break;
                case "Ch 3":
                    GlobalParams.ch3CutStartPosition = currentYPosition;
                    break;
                case "Ch 4":
                    GlobalParams.ch4CutStartPosition = currentYPosition;
                    break;
                default:
                    break;
            }
            Tools.LogInfo($"GlobalParams.ch1CutStartPosition:{GlobalParams.ch1CutStartPosition}");
            Tools.LogInfo($"GlobalParams.ch2CutStartPosition:{GlobalParams.ch2CutStartPosition}");
            Tools.LogInfo($"GlobalParams.ch3CutStartPosition:{GlobalParams.ch3CutStartPosition}");
            Tools.LogInfo($"GlobalParams.ch4CutStartPosition:{GlobalParams.ch4CutStartPosition}");
        }

        private void bakClickHandle(object sender, bool e)
        {
            SetChCutStartPosition();
            ToNextPage();
        }

        private void ToNextPage()
        {
            if (pageStatus == 0)
            {
                // 等于0 则跳回菜单 等于1 则跳回切割
                if (operateType == 0)
                {
                    if (!GlobalParams.onlineFlag)
                    {
                        mainWindow.NavigateToPage("MainMenu");
                        return;
                    }
                    // 退出校准模式
                    PlcControl.tagControl.calibration.AlignInit(0);

                    mainWindow.NavigateToPage("MainMenu");
                }
                else if (operateType == 1)
                {
                    mainWindow.NavigateToPage("Pages/F2_ManualOperation/MQSemiAutomaticCuttingConf", "type=1"); // type = 1 校准跳转
                }
                else if (operateType == 2)
                {
                    mainWindow.NavigateToPage("Pages/F4_BladeMaintenance/BmSharpenParameterForm", "Id=" + IdStr + "&Flag=" + Flag + "&BladeLotID=" + BladeLotID);
                }
                else if (operateType == 3)
                {
                    mainWindow.NavigateToPage("Pages/F7_ElectricSpark/AutoAlignPosition");
                }
            } else if (pageStatus == 2) {
                // absolutePositionPanel.Visibility = Visibility.Collapsed;
                cleanPositonPanel.Visibility = Visibility.Collapsed;
                channelPanel.Visibility = Visibility.Visible;
                channelTipsPanel.Visibility = Visibility.Visible;
                mainWindow.UpdateOperatePage(OperateData.GetManualAlignmentOperate(), ClickHandler, TouchLeaveHandler, TouchDownHandler);
                pageStatus = 0;
                titleName.Content = "单一切割面校准 (1.1)";
                rightPage.btnSure.Visibility = Visibility.Visible;
            }

        }

        public void ClickHandler(object sender, int code)
        {
            Debug.WriteLine("ClickHandler");
            switch (code)
            {
                case 2442:
                    if (!GlobalParams.onlineFlag)
                    {
                        return;
                    }
                    // 聚焦
                    if (!CommonCheck.FocusStatsCheck())
                    {
                        break;
                    }
                    int focusType = 1;
                    if (operateType == 2)
                    {
                        focusType = 4;
                    }
                    CommonOperate.GetInstance().AutoFocus(focusType, mainWindow, BladeLotID);
                    break;
                case 2443:
                    if (!GlobalParams.onlineFlag)
                    {
                        return;
                    }
                    if (!CommonCheck.ThetaAlignStatsCheck())
                    {
                        break;
                    }
                    CommonOperate.GetInstance().ThetaAlign();
                    break;
                case 2453:
                    if (!CommonCheck.ThetaAlignStatsCheck())
                    {
                        break;
                    }
                    CommonOperate.GetInstance().ThetaAlign1();
                    break;
                case 2479:
                    // 倍率变更
                    cameraCommon.ChangeCamera();
                    Thread.Sleep(100);
                    // Tools.GetChildObject<CommonDimming>(this, "commonDimming");
                    commonDimming.InitData();
                    break;
                case 2050:
                    // 测量
                    // absolutePositionPanel.Visibility = Visibility.Visible;
                    cleanPositonPanel.Visibility = Visibility.Visible;
                    channelPanel.Visibility = Visibility.Collapsed;
                    channelTipsPanel.Visibility = Visibility.Collapsed;
                    mainWindow.UpdateOperatePage(OperateData.GetMeasurementOperate(), ClickHandler, TouchLeaveHandler, TouchDownHandler);
                    pageStatus = 2;
                    cleanXPosition = PlcControl.plc.GetPlcValueString(DeviceKey.curLocationKey);
                    cleanYPosition = PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey);
                    titleName.Content = "测量";
                    rightPage.btnSure.Visibility = Visibility.Collapsed;
                    
                    break;
                case 2570:
                    // 位置清零
                    cleanXPosition = PlcControl.plc.GetPlcValueString(DeviceKey.curLocationKey);
                    cleanYPosition = PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey);
                    break;
                default:
                    break;
            }
        }

        private void LoadPosition()
        {
            Task.Run(() =>
            {
                while (axisRealTimeFlag)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // 显示实时位置
                        xAbsolutePosition.Text = (Tools.GetDoubleStringValue(PlcControl.plc.GetPlcValueString(DeviceKey.curLocationKey))).ToString("F4");
                        yAbsolutePosition.Text = (Tools.GetDoubleStringValue(PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey))).ToString("F4");
                        zAbsolutePosition.Text = (Tools.GetDoubleStringValue(PlcControl.plc.GetPlcValueString(DeviceKey.z1CurLocationKey))).ToString("F4");
                        thetaAbsolutePosition.Text = (Tools.GetDoubleStringValue(PlcControl.plc.GetPlcValueString(DeviceKey.thetaCurLocationKey))).ToString("F4");
                        // 显示清零后位置
                        xCleanPosition.Text = (Tools.GetDoubleStringValue(xAbsolutePosition.Text)
                            - Tools.GetDoubleStringValue(cleanXPosition)).ToString("F4");
                        yCleanPosition.Text = (Tools.GetDoubleStringValue(yAbsolutePosition.Text)
                            - Tools.GetDoubleStringValue(cleanYPosition)).ToString("F4");
                    });
                    Thread.Sleep(100);
                }
            });
        }
        private void DisposeDatumLine(int code)
        {
            if (code == 2040)
            {
                cameraCommon?.SetEdgeWidth(-1, 2);
            }
            else if (code == 2041)
            {
                cameraCommon?.SetEdgeWidth(1, 2);
            }
            else if (code == 2407)
            {
                cameraCommon?.SetCutMarkWidth(-1, 2);
            }
            else if (code == 2408)
            {
                cameraCommon?.SetCutMarkWidth(1, 2);
            }
            cutWidth.Text = Tools.FormatDecimalString((cameraCommon._cutMarkWidth / 1000).ToString(), 4);
            edgesWidth.Text = Tools.FormatDecimalString((cameraCommon._edgeChipWidth / 1000).ToString(), 4);
        }
        private void TouchLeaveHandler(object sender, int code)
        {
            switch (code)
            {
                case 2407:
                // 基准线调窄
                case 2408:
                // 基准线调宽
                case 2040:
                // 崩边调窄
                case 2041:
                    adjustDatumLineFlag = false;
                    if (timer != null)
                    {
                        timer.Stop();
                        timer.Dispose();
                        timer = null;
                    }
                    break;
                case 2466:
                case 2477:
                    PlcControl.tagControl.Z2axis.StopMove();
                    break;
            }
        }
        private void TouchDownHandler(object sender, int code)
        {
            switch (code)
            {
                case 2407:
                // 基准线调窄
                case 2408:
                // 基准线调宽
                case 2040:
                // 崩边调窄
                case 2041:
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
                case 2466:
                case 2477:
                    // Z2轴上升
                    // Z2轴上升
                    // 设置Z轴为低速
                    PlcControl.tagControl.Z2axis.SetHighSpeed("0");
                    PlcControl.tagControl.Z2axis.SetRelativeSpeed("0.2");
                    PlcControl.tagControl.Z2axis.StartJog(code == 2466 ? 1 : 0);
                    break;
                default:
                    break;
            }
        }

        public void SetBtnImage(Image image, string direction, bool isSelected, int type)
        {
            string resourceName = null;
            if (type == 1)
            {
                resourceName = isSelected ? $"scr_{direction}_sel" : $"scr_{direction}";
            }
            else if (type == 2)
            {
                resourceName = isSelected ? $"scan_{direction}_sel" : $"scan_{direction}";
            }
            image.Source = Tools.BitmapImageToBitmap("/Assets/picture/" + resourceName + ".png");
        }
        /// <summary>
        /// 设置当前通道
        /// </summary>
        /// <param name="channelNoValue"></param>
        public void SetChannelNo(string channelNoValue)
        {
            channelNo.Text = channelNoValue;
        }

        private void cutRecognition_Click(object sender, RoutedEventArgs e)
        {
            if (!GlobalParams.onlineFlag)
            {
                return;
            }
            Task.Run(() =>
            {
                MaterialSnack("识别中...", SnackType.WARNING, 0);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                string fileName = $"check_{timestamp}.png";
                cameraCommon.SaveWriteableBitmap(fileName);
                Thread.Sleep(1000);
                double[] widthInfo = CommonOperate.GetCutEdgeWidth(fileName);
                if (widthInfo == null || widthInfo[0] == 0 || widthInfo[1] == 0)
                {
                    MaterialSnack("识别失败！", SnackType.WARNING, 0);
                    return;
                }
                double cutWidthValue = CameraOperateUtils.ConvertToPictureBoxSize(widthInfo[0]);
                double edgesWidthValue = CameraOperateUtils.ConvertToPictureBoxSize(widthInfo[1]);
                cameraCommon.DrawLineForWidth((float)cutWidthValue, (float)edgesWidthValue);
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    cutWidth.Text = Tools.FormatDecimalString((cameraCommon._cutMarkWidth / 1000).ToString(), 4);
                    edgesWidth.Text = Tools.FormatDecimalString((cameraCommon._edgeChipWidth / 1000).ToString(), 4);
                }));
                MaterialSnack("识别完成！", SnackType.SUCCESS);
            });
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // 关闭插补
            PlcControl.tagControl.wholeDevice.SetInterpositionStatus(0);
            axisRealTimeFlag = false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Mat mat = cameraCommon.localBitmap.ToMat();
            // 保存Mat到本地文件
            bool success = Cv2.ImWrite($"C:\\Users\\17632\\Desktop\\image\\{DateTime.Now.Ticks}_mat.jpg", mat);
            Mat cropMat = AutoCutUtils.CropHorizontalCenter(mat, (int)(mat.Height * 0.05));
            // 保存Mat到本地文件
            bool success2 = Cv2.ImWrite($"C:\\Users\\17632\\Desktop\\image\\{DateTime.Now.Ticks}_cropMat.jpg", cropMat);
            Mat cropMatJpg = AutoCutUtils.JpegStreamToMat(AutoCutUtils.MatToJpegStream(cropMat));
            // 保存Mat到本地文件
            bool success3 = Cv2.ImWrite($"C:\\Users\\17632\\Desktop\\image\\{DateTime.Now.Ticks}_cropMatJpg.jpg", cropMatJpg);
            var (bladeWidthMm, collapseWidthMm, bladeTop, bladeBottom, collapseTop, collapseBottom) = VisionAnalyzer.ProcessImage(cropMatJpg);
        }
    }
}
