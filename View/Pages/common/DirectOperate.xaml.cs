using Emgu.CV.Dnn;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.Pages.common;
using 精密切割系统.View.Pages.F2_ManualOperation;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Controls
{
    /// <summary>
    /// DirectOperate.xaml 的交互逻辑
    /// </summary>
    public partial class DirectOperate : UserControl
    {
        private MainWindow? mainWindow;
        public DirectOperate()
        {
            InitializeComponent();
            mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        }
        // 快速操作类型 0 idx 1 scr
        private int directStatus = 0;
        // hi-speed状态 0 低速 1 高速
        private int hiSpeedStatus = 0;
        private System.Timers.Timer timer = new System.Timers.Timer
        {
            Interval = 500, // 初始延迟 500 毫秒
            AutoReset = false // 每次触发后需要手动重新启动
        };
        private string relativeDistance = "0.005";
        // 轴按下运动状态
        private bool downRunStatus;

        // idx
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            directStatus = 0;
            SetDirectImage();
        }
        // scr
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            directStatus = 1;
            SetDirectImage();
        }

        private void DisposeTimer()
        {
            timer = null;
            timer = new System.Timers.Timer
            {
                Interval = 800, // 初始延迟 500 毫秒
                AutoReset = false // 每次触发后需要手动重新启动
            };
            timer.Elapsed += null;
        }

        public void SetDirectImage()
        {
            if (directStatus == 0)
            {
                scanBorder.Background = new SolidColorBrush(Color.FromRgb(23, 124, 250));
                scrBorder.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                scanIcon.Source = Tools.BitmapImageToBitmap("/Assets/picture/IDX_icon.png");
                scrIcon.Source = Tools.BitmapImageToBitmap("/Assets/picture/SCR_icon.png");
                // 设置方向图片按钮
                idxLeftIcon.Source = Tools.BitmapImageToBitmap("/Assets/picture/idx_left.png");
                idxTopIcon.Source = Tools.BitmapImageToBitmap("/Assets/picture/idx_top.png");
                idxRightIcon.Source = Tools.BitmapImageToBitmap("/Assets/picture/idx_right.png");
                idxBottomIcon.Source = Tools.BitmapImageToBitmap("/Assets/picture/idx_bottom.png");
                idxRightBottomIcon.Source = Tools.BitmapImageToBitmap("/Assets/picture/idx_right_bottom.png");
                idxLeftBottomIcon.Source = Tools.BitmapImageToBitmap("/Assets/picture/idx_left_bottom.png");
            }
            else 
            {

                scanBorder.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                scrBorder.Background = new SolidColorBrush(Color.FromRgb(23, 124, 250));
                scanIcon.Source = Tools.BitmapImageToBitmap("/Assets/picture/IDX_icon_sel.png");
                scrIcon.Source = Tools.BitmapImageToBitmap("/Assets/picture/SCR_icon_sel.png");

                idxLeftIcon.Source = Tools.BitmapImageToBitmap("/Assets/picture/scr_left.png");
                idxTopIcon.Source = Tools.BitmapImageToBitmap("/Assets/picture/scr_top.png");
                idxRightIcon.Source = Tools.BitmapImageToBitmap("/Assets/picture/scr_right.png");
                idxBottomIcon.Source = Tools.BitmapImageToBitmap("/Assets/picture/scr_bottom.png");
                idxRightBottomIcon.Source = Tools.BitmapImageToBitmap("/Assets/picture/scr_right_bottom.png");
                idxLeftBottomIcon.Source = Tools.BitmapImageToBitmap("/Assets/picture/scr_left_bottom.png");
            }
        }

        private void idxLeftBottom_TouchDown(object sender, TouchEventArgs e)
        {
            if (CommonCheck.AxisRunStatusCheck())
            {
                return;
            }
            // 相对运动90°
            // PlcControl.tagControl.ThetaAxis.StartRelative(GlobalParams.thetaScreenSpeed, GlobalParams.thetaScreenIndex, 0);
            RotationAngle(0);
            SetBtnImage(idxLeftBottomIcon, "left_bottom", true, 1);
        }

        private void idxLeftBottom_TouchUp(object sender, TouchEventArgs e)
        {
            if (CommonCheck.AxisRunStatusCheck())
            {
                return;
            }
            SetBtnImage(idxLeftBottomIcon, "left_bottom", false, 1);
        }

        public void SetBtnImage(Image image, string direction, bool isSelected, int type)
        {
            string resourceName = null;
            if (type == 1)
            {
                resourceName = isSelected ? $"idx_{direction}_sel" : $"idx_{direction}";
                if (directStatus == 1)
                {
                    resourceName = isSelected ? $"scr_{direction}_sel" : $"scr_{direction}";
                }
            }
            else if (type == 2) {
                resourceName = isSelected ? $"scan_{direction}_sel" : $"scan_{direction}";
            }
            image.Source = Tools.BitmapImageToBitmap("/Assets/picture/" + resourceName + ".png");
        }

        private void idxTop_TouchDown(object sender, TouchEventArgs e)
        {
            if (CommonCheck.AxisRunStatusCheck())
            {
                return;
            }
            if (downRunStatus)
            {
                return;
            }
            // Y轴向上运动 directStatus 如果为0 则是步进 如果为1 则scr 距离为步进的2倍
            downRunStatus = true;
            // 创建并启动新任务
            Task.Run(() => ScrOrIndexScreen(1, 1));
            SetBtnImage(idxTopIcon, "top", true, 1);
        }

        private void idxTop_TouchUp(object sender, TouchEventArgs e)
        {
            SetBtnImage(idxTopIcon, "top", false, 1);
        }

        private void idxLeftBtn_TouchDown(object sender, TouchEventArgs e)
        {
            if (CommonCheck.AxisRunStatusCheck())
            {
                return;
            }
            downRunStatus = true;
            // 创建并启动新任务
            Task.Run(() => ScrOrIndexScreen(1, 0));

            downRunStatus = false;
            SetBtnImage(idxLeftIcon, "left", true, 1);
        }

        private void idxLeftBtn_TouchUp(object sender, TouchEventArgs e)
        {
            SetBtnImage(idxLeftIcon, "left", false, 1);
        }

        private void idxRight_TouchDown(object sender, TouchEventArgs e)
        {
            if (CommonCheck.AxisRunStatusCheck())
            {
                return;
            }
            downRunStatus = true;
            // 创建并启动新任务
            Task.Run(() => ScrOrIndexScreen(0, 0));
            downRunStatus = false;
            SetBtnImage(idxRightIcon, "right", true, 1);
        }

        private void idxRight_TouchUp(object sender, TouchEventArgs e)
        {
            SetBtnImage(idxRightIcon, "right", false, 1);
        }

        private void idxBottomBtn_TouchDown(object sender, TouchEventArgs e)
        {
            if (CommonCheck.AxisRunStatusCheck())
            {
                return;
            }
            downRunStatus = true;
            // 创建并启动新任务
            Task.Run(() => ScrOrIndexScreen(0, 1));

            downRunStatus = false;
            SetBtnImage(idxBottomIcon, "bottom", true, 1);
        }

        private void idxBottomBtn_TouchUp(object sender, TouchEventArgs e)
        {
            SetBtnImage(idxBottomIcon, "bottom", false, 1);
        }

        private void idxRightBottomBtn_TouchDown(object sender, TouchEventArgs e)
        {
            if (CommonCheck.AxisRunStatusCheck())
            {
                return;
            }
            // 相对运动-90° 这个角度可能是根据ch1 ch2 ch3 ch4来的
            // PlcControl.tagControl.ThetaAxis.StartRelative(GlobalParams.thetaScreenSpeed, GlobalParams.thetaScreenIndex, 1);
            SetBtnImage(idxRightBottomIcon, "right_bottom", true, 1);
            RotationAngle(1);
        }

        private void idxRightBottomBtn_TouchUp(object sender, TouchEventArgs e)
        {
            SetBtnImage(idxRightBottomIcon, "right_bottom", false, 1);
        }

        private void scanLeftBottomBtn_TouchDown(object sender, TouchEventArgs e)
        {
            scanLeftBottomBtnDown(0, 0);
        }

        private void scanLeftBottomBtn_TouchUp(object sender, TouchEventArgs e)
        {
            scanLeftBottomBtnDown(0, 1);
        }

        private void scanTopBtn_TouchDown(object sender, TouchEventArgs e)
        {
            scanLeftBottomBtnDown(2, 0);
        }

        private void scanTopBtn_TouchUp(object sender, TouchEventArgs e)
        {
            scanLeftBottomBtnDown(2, 1);
        }

        private void scanLeftBtn_TouchDown(object sender, TouchEventArgs e)
        {
            scanLeftBottomBtnDown(4, 0);
        }

        private void scanLeftBtn_TouchUp(object sender, TouchEventArgs e)
        {
            scanLeftBottomBtnDown(4, 1);
        }

        private void scanRightBtn_TouchDown(object sender, TouchEventArgs e)
        {
            scanLeftBottomBtnDown(5, 0);
        }

        private void scanRightBtn_TouchUp(object sender, TouchEventArgs e)
        {
            scanLeftBottomBtnDown(5, 1);
        }

        private void stopTimer()
        {
            timer.Stop();
            timer.Dispose();
        }
        private void scanBottomBtn_TouchDown(object sender, TouchEventArgs e)
        {
            scanLeftBottomBtnDown(3, 0);
        }

        private void scanBottomBtn_TouchUp(object sender, TouchEventArgs e)
        {
            scanLeftBottomBtnDown(3, 1);
        }

        private void scanRightBottomBtn_TouchDown(object sender, TouchEventArgs e)
        {
            scanLeftBottomBtnDown(1, 0);
        }

        private void scanRightBottomBtn_TouchUp(object sender, TouchEventArgs e)
        {
            scanLeftBottomBtnDown(1, 1);
        }

        // 运行方向 0 正 1 负
        // axisType 0 X 轴 1 Y轴
        private void ScrOrIndexScreen(int jogDirection, int axisType)
        {
            // 轴步进距离
            float tempScreenIndex = Tools.GetFloatStringValue(GlobalParams.xScreenIndex);
            string tempScreenSpeed = GlobalParams.xScreenSpeed;
            if (axisType == 1)
            {
                tempScreenIndex = Tools.GetFloatStringValue(GlobalParams.yScreenIndex);
                tempScreenSpeed = GlobalParams.yScreenSpeed;
            }
            if (tempScreenIndex == null || tempScreenSpeed == null)
            {
                return;
            }
            // 如果是0 则是步进
            if (directStatus == 1)
            {
                tempScreenIndex = (tempScreenIndex * 2);
            }
            if (axisType == 0)
            {
                // 获取当前位置
                float currentPosition = Tools.GetFloatStringValue(PlcControl.plc.GetPlcValueString(DeviceKey.curLocationKey));
                float targetPosition = jogDirection == 0 ? currentPosition + tempScreenIndex : currentPosition - tempScreenIndex;
                PlcControl.tagControl.Xaxis.StartAbsolute(tempScreenSpeed, targetPosition + "");
                // PlcControl.tagControl.Xaxis.StartRelative(tempScreenSpeed, tempScreenIndex, jogDirection);
                Thread.Sleep(50);
                // 是否运动完成 应该读取plc的值 true是运行中，false是已停止
                Tools.WaitForValue(PlcControl.allTags[DeviceKey.curSpeedKey], "0");

            } else
            {
                // 获取当前位置
                float currentPosition = Tools.GetFloatStringValue(PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey));
                float targetPosition = jogDirection == 0 ? currentPosition + tempScreenIndex : currentPosition - tempScreenIndex;
                PlcControl.tagControl.Yaxis.StartAbsolute(tempScreenSpeed, targetPosition + "");
                // PlcControl.tagControl.Yaxis.StartRelative(tempScreenSpeed, tempScreenIndex, jogDirection);
                Thread.Sleep(50);
                // 是否运动完成 应该读取plc的值 true是运行中，false是已停止
                Tools.WaitForValue(PlcControl.allTags[DeviceKey.yCurSpeedKey], "0");
            }
            // Y轴向上运动结束
            downRunStatus = false;
        }


        private void scrHighSpeedBtn_TouchDown(object sender, TouchEventArgs e)
        {
            if (hiSpeedStatus == 1)
            {
                hiSpeedStatus = 0;
            }
            else
            {
                hiSpeedStatus = 1;
            }
            SetHighBtnStatus(hiSpeedStatus);
            GlobalParams.heightSpeedStatus = hiSpeedStatus;
        }

        public void SetHighBtnStatus(int tempHiSpeedStatus)
        {
            if (tempHiSpeedStatus == 0)
            {
                highSpeedIcon.Source = Tools.BitmapImageToBitmap("/Assets/picture/scan_hi_speed_icon.png");
                highSpeedBorder.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                GlobalParams.multipleNum = 0.1;
                // 设置为低速
                PlcControl.tagControl.Xaxis.SetHighSpeed("0");
                PlcControl.tagControl.Yaxis.SetHighSpeed("0");
            }
            else
            {
                highSpeedIcon.Source = Tools.BitmapImageToBitmap("/Assets/picture/scan_hi_speed_icon_sel.png");
                highSpeedBorder.Background = new SolidColorBrush(Color.FromRgb(23, 124, 250));
                GlobalParams.multipleNum = 1;
                // 设置为高速
                PlcControl.tagControl.Xaxis.SetHighSpeed("1");
                PlcControl.tagControl.Yaxis.SetHighSpeed("1");
            }
        }
        bool rotationAngleFlag = false;
        //旋转角度
        private void RotationAngle(int jogDirection)
        {
            if (rotationAngleFlag)
            {
                return;
            }
            string newChannelNum = "";
            string currentChannelNum = "";
            //获取当前是否转动过
            CurrentConfigurationModel _model= CurrentUtils.GetCurrentConfiguration();
            if (string.IsNullOrEmpty(_model.ChannelNum))
            {
                currentChannelNum = GlobalParams.CH1;
            }
            else
            {
                currentChannelNum = _model.ChannelNum;
            }
            // 左转是减，右转是加
            if (jogDirection == 0)//左转
            {
                if (GlobalParams.CH1.Equals(currentChannelNum)) return;
                switch (currentChannelNum)
                {
                    case "Ch 2":
                        newChannelNum = GlobalParams.CH1;
                        break;
                    case "Ch 3":
                        newChannelNum = GlobalParams.CH2;
                        break;
                    case "Ch 4":
                        newChannelNum = GlobalParams.CH3;
                        break;
                    default:
                        break;
                }
            }
            else//右转
            {
                if (GlobalParams.CH4.Equals(currentChannelNum)) return;
                switch (currentChannelNum)
                {
                    case "Ch 1":
                        newChannelNum = GlobalParams.CH2;
                        break;
                    case "Ch 2":
                        newChannelNum = GlobalParams.CH3;
                        break;
                    case "Ch 3":
                        newChannelNum = GlobalParams.CH4;
                        break;
                    default:
                        break;
                }
            }
            rotationAngleFlag = true;
            //查询对应的值
            //查询数据
            string deviceDataId = _model.DeviceDataId.ToString();
            if (string.IsNullOrEmpty(deviceDataId)) return;
            var tableList = SqlHelper.Table<FileTableItemModel>()
                   .Where(t => t.Id == _model.DeviceDataId)
                   .ToList();
            if (tableList.Count>0)
            {
                long id = tableList[0].Id;
                var listOldCh =  SqlHelper.Table<FileTableItemChModel>().Where(t => t.ItemId == id).Where(t=>t.ChName == currentChannelNum).ToList();
                var listNewCh = SqlHelper.Table<FileTableItemChModel>().Where(t => t.ItemId == id).Where(t => t.ChName == newChannelNum).ToList();
                if (listOldCh != null&& listOldCh.Count > 0&& listNewCh != null && listNewCh.Count > 0)
                {
                    FileTableItemChModel newChModel = listNewCh[0];
                    float[] feedSpeeds = Tools.StringToFloatArray(newChModel.FeedSpeed); // 获取进给速度
                    float[] yIndexs = Tools.StringToFloatArray(newChModel.YIndex);       // 获取Y轴偏移
                    float[] setBladeHeight = Tools.StringToFloatArray(newChModel.BladeHeight);         // 设置的刀片高度
                    // 判断新的CH的切刀数大于0且seq至少有1个
                    if (Tools.GetIntStringValue(newChModel.CutLine) == 0 
                        || feedSpeeds[0] <= 0 || yIndexs[0] <= 0 || setBladeHeight[0] <= 0)
                    {
                        rotationAngleFlag = false;
                        return;
                    }

                    FileTableItemChModel currentChModel = listOldCh[0];
                    double oldThetaDeg = double.Parse(currentChModel.ThetaDeg);
                    double newThetaDeg = double.Parse(newChModel.ThetaDeg);
                    double thetaDeg = oldThetaDeg - newThetaDeg;
                    if (jogDirection==0)
                    {
                         thetaDeg = newThetaDeg - oldThetaDeg; 
                    }
                    //Debug.WriteLine("转动："+ GlobalParams.thetaScreenSpeed+"=="+ thetaDeg.ToString());
                    // 相对运动-90° 这个角度可能是根据ch1 ch2 ch3 ch4来的
                    PlcControl.tagControl.ThetaAxis.StartRelative(GlobalParams.thetaScreenSpeed, thetaDeg.ToString(), jogDirection);
                    CurrentUtils.UpdateCurrentCh(newChannelNum);
                    // 获取当前主窗口是哪个页面 如果是切割或者校准，则更新当前CH值
                    DisposeChChange(newChannelNum);
                }
            }
            rotationAngleFlag = false;
        }

        private void DisposeChChange(string newChannelNum)
        {
            List<MQManualAlignmentConf> manualAlignmentConfs = Tools.GetChildrenOfType<MQManualAlignmentConf>(mainWindow);
            if (manualAlignmentConfs != null && manualAlignmentConfs.Count > 0)
            {
                manualAlignmentConfs[0].SetChannelNo(newChannelNum);
            }
            List<MQSemiAutomaticCuttingConf> semiAutomaticCuttingConfs = Tools.GetChildrenOfType<MQSemiAutomaticCuttingConf>(mainWindow);
            if (semiAutomaticCuttingConfs != null && semiAutomaticCuttingConfs.Count > 0)
            {
                semiAutomaticCuttingConfs[0].SetChannelNo(newChannelNum);
            }
        }

        private void scanLeftBtn_TouchLeave(object sender, TouchEventArgs e)
        {
            scanLeftBottomBtnDown(4, 1);
        }

        private void scanBottomBtn_TouchLeave(object sender, TouchEventArgs e)
        {
            scanLeftBottomBtnDown(3, 1);
        }

        private void scanRightBtn_TouchLeave(object sender, TouchEventArgs e)
        {
            scanLeftBottomBtnDown(5, 1);
        }

        private void scanRightBottomBtn_TouchLeave(object sender, TouchEventArgs e)
        {
            scanLeftBottomBtnDown(1, 1);
        }

        private void idxLeftBtn_TouchLeave(object sender, TouchEventArgs e)
        {
            SetBtnImage(idxLeftIcon, "left", false, 1);
        }

        private void idxRight_TouchLeave(object sender, TouchEventArgs e)
        {
            SetBtnImage(idxRightIcon, "right", false, 1);
        }

        private void idxBottomBtn_TouchLeave(object sender, TouchEventArgs e)
        {
            SetBtnImage(idxBottomIcon, "bottom", false, 1);
        }

        private void idxTop_TouchLeave(object sender, TouchEventArgs e)
        {
            SetBtnImage(idxTopIcon, "top", false, 1);
        }

        private void idxLeftBottom_TouchLeave(object sender, TouchEventArgs e)
        {
            SetBtnImage(idxLeftBottomIcon, "left_bottom", false, 1);
        }

        private void idxRightBottomBtn_TouchLeave(object sender, TouchEventArgs e)
        {

            SetBtnImage(idxRightBottomIcon, "right_bottom", false, 1);
        }

        private void scanLeftBottomBtn_TouchLeave(object sender, TouchEventArgs e)
        {
            scanLeftBottomBtnDown(0, 1);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            
            // scrHighSpeedBtn_TouchDown(null, null);
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool isVisible)
            {
                if (isVisible)
                {
                    hiSpeedStatus = GlobalParams.heightSpeedStatus;
                    SetHighBtnStatus(hiSpeedStatus);
                }
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        private void scanTopBtn_TouchLeave(object sender, TouchEventArgs e)
        {
            scanLeftBottomBtnDown(2, 1);
        }

        private void scanRightBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            scanLeftBottomBtnDown(5, 0);
        }

        private void scanRightBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            scanLeftBottomBtnDown(5, 1);
        }

        private void idxLeftBottom_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            RotationAngle(0);
        }

        private void idxRightBottomBtn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            RotationAngle(1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">0 左下 1 右下 2 上 3 下 4 左 5 右</param>
        /// <param name="operateType">操作类型 0 down 1 up</param>
        private void scanLeftBottomBtnDown(int type, int operateType)
        {
            if (operateType == 0)
            {
                if (CommonCheck.AxisRunStatusCheck())
                {
                    return;
                }
                switch (type)
                {
                    case 0:
                        PlcControl.tagControl.ThetaAxis.StartJog(1);
                        SetBtnImage(scanLeftBottomIcon, "left_bottom", true, 2);
                        break;
                    case 1:
                        PlcControl.tagControl.ThetaAxis.StartJog(0);
                        SetBtnImage(scanRightBottomIcon, "right_bottom", true, 2);
                        break;
                    case 2:
                        // 先执行相对运动
                        PlcControl.tagControl.Yaxis.StartRelative(GlobalParams.xScanSpeed, relativeDistance, 1);
                        DisposeTimer();
                        timer.Elapsed += (sender, e) =>
                        {
                            PlcControl.tagControl.Yaxis.StartJog(1);
                        };
                        timer.Start();
                        SetBtnImage(scanTopIcon, "top", true, 2);
                        break;
                    case 3:
                        // 先执行相对运动
                        PlcControl.tagControl.Yaxis.StartRelative(GlobalParams.xScanSpeed, relativeDistance, 0);
                        DisposeTimer();
                        timer.Elapsed += (sender, e) =>
                        {
                            PlcControl.tagControl.Yaxis.StartJog(0);
                        };
                        timer.Start();
                        SetBtnImage(scanBottomIcon, "bottom", true, 2);
                        break;
                    case 4:
                        // 先执行相对运动
                        PlcControl.tagControl.Xaxis.StartRelative(GlobalParams.xScanSpeed, relativeDistance, 0);
                        DisposeTimer();
                        timer.Elapsed += (sender, e) =>
                        {
                            PlcControl.tagControl.Xaxis.StartJog(0);
                        };
                        timer.Start();
                        SetBtnImage(scanLeftIcon, "left", true, 2);
                        break;
                    case 5:
                        // 先执行相对运动
                        PlcControl.tagControl.Xaxis.StartRelative(GlobalParams.xScanSpeed, relativeDistance, 1);
                        DisposeTimer();
                        timer.Elapsed += (sender, e) =>
                        {
                            PlcControl.tagControl.Xaxis.StartJog(1);
                        };
                        timer.Start();

                        SetBtnImage(scanRightIcon, "right", true, 2);
                        break;
                    default:
                        break;
                }
            } else if (operateType == 1)
            {
                stopTimer();
                switch (type)
                {
                    case 0:

                        PlcControl.tagControl.ThetaAxis.StopMove();
                        SetBtnImage(scanLeftBottomIcon, "left_bottom", false, 2);
                        break;
                    case 1:
                        PlcControl.tagControl.ThetaAxis.StopMove();
                        SetBtnImage(scanRightBottomIcon, "right_bottom", false, 2);
                        break;
                    case 2:
                        PlcControl.tagControl.Yaxis.StopMove();
                        SetBtnImage(scanTopIcon, "top", false, 2);
                        break;
                    case 3:
                        PlcControl.tagControl.Yaxis.StopMove();
                        SetBtnImage(scanBottomIcon, "bottom", false, 2);
                        break;
                    case 4:
                        PlcControl.tagControl.Xaxis.StopMove();
                        SetBtnImage(scanLeftIcon, "left", false, 2);
                        break;
                    case 5:
                        PlcControl.tagControl.Xaxis.StopMove();
                        SetBtnImage(scanRightIcon, "right", false, 2);
                        break;
                    default:
                        break;
                }
            }
        }

        private void scanLeftBottomBtn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            scanLeftBottomBtnDown(0, 0);
        }

        private void scanLeftBottomBtn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            scanLeftBottomBtnDown(0, 1);
        }

        private void scanRightBottomBtn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            scanLeftBottomBtnDown(1, 0);
        }

        private void scanRightBottomBtn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            scanLeftBottomBtnDown(1, 1);
        }

        private void scanTopBtn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            scanLeftBottomBtnDown(2, 0);
        }

        private void scanTopBtn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            scanLeftBottomBtnDown(2, 1);
        }

        private void scanBottomBtn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            scanLeftBottomBtnDown(3, 0);
        }

        private void scanBottomBtn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            scanLeftBottomBtnDown(3, 1);
        }

        private void scanLeftBtn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            scanLeftBottomBtnDown(4, 0);
        }

        private void scanLeftBtn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            scanLeftBottomBtnDown(4, 1);
        }
    }
}
