using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Xml.Linq;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.page.right
{
    /// <summary>
    /// RightPage.xaml 的交互逻辑
    /// </summary>
    public partial class RightPage : Page
    {
        public RightPage()
        {
            InitializeComponent();
            SourceChanged();
        }


        //监听Frame中Source改变
        private void SourceChanged()
        {
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow.mainFrame != null)
            {
                DependencyPropertyDescriptor descriptor = DependencyPropertyDescriptor.FromProperty(Frame.SourceProperty, typeof(Frame));
                descriptor.AddValueChanged(mainWindow.mainFrame, OnFrameSourceChanged);
            }
        }

        private void RefreshDeviceStatus()
        {
            Thread _thread = new Thread(() => {
                Thread.Sleep(5000);
                bool vacuumStateStatus = false;
                bool spindleAirStatus = false;
                bool spindleCoolingWaterStatus = false;
                bool spindleCuttingWaterStatus = false;
                string spindleSpeedPlcValue = "0";
                bool firstFlag = true;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    spindleSpeedValue.Content = spindleSpeedPlcValue;
                });
                while (true)
                {
                    // 需要修改为线程内可以操作的方式
                    bool tempVacuumStateStatus = Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.vacuumStateKey));
                    bool tempSpindleAirStatus = Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.spindleAirKey));
                    bool tempSpindleCoolingWaterStatus = Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.spindleCoolingWaterKey));
                    bool tempSpindleCuttingWaterStatus = Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.spindleCuttingWaterKey));
                    string tempSpindleSpeedPlcValue = PlcControl.plc.GetPlcValueString(DeviceKey.spindleSpeedStatusKey);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (vacuumStateStatus != tempVacuumStateStatus || firstFlag)
                        {
                            vacuumStateStatus = tempVacuumStateStatus;
                            vacuumStateStatusIcon.Source = Tools.BitmapImageToBitmap(vacuumStateStatus ? "/Assets/picture/lamp_normal.png"
                                : "/Assets/picture/lamp_error.png");
                        }
                        if (spindleAirStatus != tempSpindleAirStatus || firstFlag)
                        {
                            spindleAirStatus = tempSpindleAirStatus;
                            spindleAirStatusIcon.Source = Tools.BitmapImageToBitmap(spindleAirStatus ? "/Assets/picture/lamp_normal.png"
                                : "/Assets/picture/lamp_error.png");
                        }
                        if (spindleCoolingWaterStatus != tempSpindleCoolingWaterStatus || firstFlag)
                        {
                            spindleCoolingWaterStatus = tempSpindleCoolingWaterStatus;
                            spindleCoolingWaterStatusIcon.Source = Tools.BitmapImageToBitmap(spindleCoolingWaterStatus ? "/Assets/picture/lamp_normal.png"
                                : "/Assets/picture/lamp_error.png");
                        }
                        if (spindleCuttingWaterStatus != tempSpindleCuttingWaterStatus || firstFlag)
                        {
                            spindleCuttingWaterStatus = tempSpindleCuttingWaterStatus;
                            spindleCuttingWaterStatusIcon.Source = Tools.BitmapImageToBitmap(spindleCuttingWaterStatus ? "/Assets/picture/lamp_normal.png"
                                : "/Assets/picture/lamp_error.png");
                        }
                        if ((spindleSpeedValue != null && !String.IsNullOrEmpty(spindleSpeedPlcValue)
                        && !String.IsNullOrEmpty(tempSpindleSpeedPlcValue) && spindleSpeedPlcValue != tempSpindleSpeedPlcValue) || firstFlag)
                        {
                            spindleSpeedPlcValue = tempSpindleSpeedPlcValue;
                            spindleSpeedValue.Content = spindleSpeedPlcValue;
                        }

                    });
                    firstFlag = false;
                    Thread.Sleep(500);
                }
            });
            _thread.IsBackground = true;
            _thread.Start();
        }

        // 当Frame的Source属性变化时调用
        public void OnFrameSourceChanged(object sender, EventArgs e)
        {
            PanelAction.Visibility = Visibility.Collapsed;
            // 切割相关======
            btnCutStart.Visibility = Visibility.Collapsed;
            btnCutReStart.Visibility = Visibility.Collapsed;
            btnCutPause.Visibility = Visibility.Collapsed;
            btnCutStop.Visibility = Visibility.Collapsed;
            btnCutBackward.Visibility = Visibility.Collapsed;
            btnCutFront.Visibility = Visibility.Collapsed;
            // 切割相关end=======
            // 刀片更换
            // 刀片测高
            btnContactSetupSure.Visibility = Visibility.Collapsed;
            // 电火花修刀
            btnElectricalStart.Visibility = Visibility.Collapsed;
            btnElectricalPause.Visibility = Visibility.Collapsed;
            // 用户参数设定
            //还原所有右侧控件
            btnBack.Visibility = Visibility.Collapsed;
            btnSee.Visibility = Visibility.Collapsed;
            btnSure.Visibility = Visibility.Collapsed;
            btnStartSetup.Visibility = Visibility.Collapsed;
            btnSave.Visibility = Visibility.Collapsed;
            //告警提示
            // ShowTemplate.Visibility = Visibility.Collapsed;
            MachinePanel.Visibility = Visibility.Collapsed;

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (GlobalParams.onlineFlag)
            {
                // 刷新主轴转速等状态信息
                RefreshDeviceStatus();
            }
        }

        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 调试补偿
            // Debug.WriteLine(PlcControl.GetCompensate("35.5", DeviceKey.yName, 0));
        }
    }
}
