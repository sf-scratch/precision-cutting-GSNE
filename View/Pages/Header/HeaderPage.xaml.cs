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
using System.Windows.Threading;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.system;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.Hader
{
    /// <summary>
    /// HeaderPage.xaml 的交互逻辑
    /// </summary>
    public partial class HeaderPage : Page
    {
        private DispatcherTimer timer;
        MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        public HeaderPage()
        {
            InitializeComponent();
        }
        //  
        static int showType = 0;
        RightPage? rightPage;
        private void HeaderPage_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateTime();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1); // 每秒更新一次
            timer.Tick += Timer_Tick;
            timer.Start();
            rightPage = mainWindow.rightFrame.Content as RightPage;
            if (DevicesUtis.IsTouchSupported())
            {
                sensorBtn.TouchUp += SensorBtn_TouchUp;
            }
            else
            {
                sensorBtn.MouseUp += SensorBtn_MouseUp;
            }
                
        }

        private void SensorBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {

            DisposeShowType();
        }

        private void SensorBtn_TouchUp(object? sender, TouchEventArgs e)
        {
            DisposeShowType();
        }
        public void DisposeShowType()
        {
            if (showType == 0)
            {
                showType = 1;
                rightPage.PanelAction.Visibility = Visibility.Collapsed;
                rightPage.ShowTemplate.Visibility = Visibility.Visible;
            }
            else if (showType == 1)
            {
                showType = 0;
                rightPage.PanelAction.Visibility = Visibility.Visible;
                rightPage.ShowTemplate.Visibility = Visibility.Collapsed;
            }
            if (GlobalParams.currentPageIsHome) //主页面不要返回
            {
                rightPage.btnBack.Visibility = Visibility.Collapsed;
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateTime();
        }

        private void UpdateTime()
        {
            DateTime now = DateTime.Now;
            labelDay.Content = now.ToString("yyyy-MM-dd");
            labelTime.Content = DateTime.Now.ToString("HH:mm:ss");
            string[] daysOfWeekZH = { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };
            labelWeek.Content = daysOfWeekZH[(int)now.DayOfWeek];
        }

        private void BtnQuit_Click(object sender, RoutedEventArgs e)
        {
            CameraUtils.CloseDevice();
            Application.Current.Shutdown();
        }

        private void Camera_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow.mainFrame.Navigate(new Uri($"View/camera/Camera.xaml", UriKind.Relative));
        }


        private void StackPanel_TouchDown(object sender, TouchEventArgs e)
        {
            ClearAlarmInfo();
        }

        private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ClearAlarmInfo();
        }

        private async void ClearAlarmInfo()
        {
            await PlcControl.tagControl.wholeDevice.CloseBuzzerAsync();
            await PlcControl.tagControl.wholeDevice.AlarmResetAsync();
        }

        //退出系统；连续10下退出系统或最小化窗口
        private int clickCount = 0;
        private long clickTime = 0;//第一次点击时间
        private void Image_TouchDown(object sender, TouchEventArgs e)
        {
            dowm();
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            dowm();
        }

        private void dowm()
        {
            //第一次点击时间
            if (clickCount == 0)
            {
                clickTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            else
            {
                long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                //10秒内点击10次
                if (currentTimestamp - clickTime > 1000 * 5)
                {
                    clickCount = 0; // 重置点击次数
                    clickTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }
            }
            clickCount++; // 增加点击次数
            if (clickCount >= 10) // 如果点击次数达到10次
            {
                clickCount = 0; // 重置点击次数
                if (havePassWord())
                {
                    mainWindow.NavigateToPage("Pages/system/SystemPage");
                }
                else
                {
                    SystemDialog dialog = new SystemDialog();
                    dialog.ShowDialog();
                }
            }
        }

        //是否需要密码
        private Boolean havePassWord()
        {
            UserDefineDataModel userDefineDataModel=null;
            //查询用不基础配置信息
            var list =  SqlHelper.Table<UserDefineDataModel>()
                   .Where(t => t.Id == 1).ToList();
            //数据不存在，则初始化数据
            if (list.Count() > 0)
            {
                userDefineDataModel = list[0];
                if (!string.IsNullOrEmpty(userDefineDataModel.SystemPassword))
                {
                    //查询录入的密码时间戳
                    if (userDefineDataModel.SystemPasswordTime == 0)
                    {
                        return true;
                    }
                    long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    //判断当前录入时间是否间隔2小时
                    if (currentTimestamp - userDefineDataModel.SystemPasswordTime > 1000 * 60 * 60 * 2)
                    {
                        return true;
                    }
                    return false;
                }
                return false;
            }
            return false;
        }

        private async void urgentRaiseBtn_TouchDown(object sender, TouchEventArgs e)
        {
            await PlcControl.tagControl.wholeDevice.UrgentRaise();
        }

        private async void urgentRaiseBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await PlcControl.tagControl.wholeDevice.UrgentRaise();
        }
    }
}
