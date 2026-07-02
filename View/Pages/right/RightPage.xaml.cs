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
using 精密切割系统.Helpers;
using 精密切割系统.Helpers.GTN;
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

        private async Task RefreshDeviceStatusAsync()
        {
            await Task.Delay(5000);
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
                bool tempVacuumStateStatus = await IoAlarm.Instance.CheckWorkpieceVacuumDetectAlarmAsync();
                bool tempSpindleAirStatus = await IoAlarm.Instance.CheckAirFloatPressureAlarmAsync();
                bool tempSpindleCoolingWaterStatus = await IoAlarm.Instance.CheckCoolWaterDetectAlarmAsync();
                bool tempSpindleCuttingWaterStatus = await OutputConfig.Instance.GetCutWaterOpenAsync();
                int? tempSpindleSpeedPlcValue = await SpindleMotionSet.Instance.SpindleSpeedDisplayAsync();
                //var temperatures = await PlcControl.tagControl.wholeDevice.GetTemperatureSensorsAsync();
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
                    if ((spindleSpeedValue != null && !String.IsNullOrEmpty(spindleSpeedPlcValue) && tempSpindleSpeedPlcValue is not null))
                    {
                        spindleSpeedPlcValue = tempSpindleSpeedPlcValue.Value.ToString();
                        spindleSpeedValue.Content = spindleSpeedPlcValue;
                    }
                    //if (temperatures is not null && temperatures.Length >= 5)
                    //{
                    //    temperatureSensor1.Content = temperatures[0].ToString("F1");
                    //    temperatureSensor2.Content = temperatures[1].ToString("F1");
                    //    temperatureSensor3.Content = temperatures[2].ToString("F1");
                    //    temperatureSensor4.Content = temperatures[3].ToString("F1");
                    //    temperatureSensor5.Content = temperatures[4].ToString("F1");
                    //}
                });
                firstFlag = false;
                await Task.Delay(500);
            }
        }

        // 当Frame的Source属性变化时调用
        public void OnFrameSourceChanged(object sender, EventArgs e)
        {
            PanelAction.Visibility = Visibility.Collapsed;
            // 切割相关======
            btnCutStart.Visibility = Visibility.Collapsed;
            btnCutReStart.Visibility = Visibility.Collapsed;
            btnCutReStartCurY.Visibility = Visibility.Collapsed;
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
            btnClear.Visibility = Visibility.Collapsed;
            //告警提示
            // ShowTemplate.Visibility = Visibility.Collapsed;
            MachinePanel.Visibility = Visibility.Collapsed;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (GlobalParams.OnlineFlag)
            {
                // 刷新主轴转速等状态信息
                Task.Factory.StartNew(RefreshDeviceStatusAsync, TaskCreationOptions.LongRunning);
            }
        }

        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 调试补偿
            // Debug.WriteLine(PlcControl.GetCompensate("35.5", DeviceKey.yName, 0));
        }

        private void customPlot_MouseDown(object sender, MouseButtonEventArgs e)
        {
            customPlot.Clear();
        }
    }
}