using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.Hader;


namespace 精密切割系统.Helpers
{
    //头部告警类
    internal class MaterialSnackUtils
    {
        private static DispatcherTimer timer = null;
        public enum SnackType
        {
            SUCCESS = 0,//成功提示
            INFO = 1,//消息提示
            WARNING = 2,//警告提示
            ERROR = 3//错误提示
        }
        static MainWindow mainWindow;
        static HeaderPage headerPage;
        public static void initPage()
        {
            if (mainWindow == null)
            {
                mainWindow = Application.Current.MainWindow as MainWindow;
                headerPage = mainWindow.headerFrame.Content as HeaderPage;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="type"></param>
        /// <param name="hideTime">隐藏时间 毫秒</param>
        public static void MaterialSnack(string msg, SnackType type, int hideMilliseconds = 10)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                // 更新UI控件的代码
                initPage();
                headerPage.messgePanel.Visibility = Visibility.Visible;
                headerPage.messgeLabel.Content = msg;
                switch (type)
                {
                    case SnackType.SUCCESS:
                        headerPage.messgesBorder.Background = new SolidColorBrush(Color.FromArgb(8, 76, 215, 21));
                        headerPage.messgesBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(76, 215, 21));
                        headerPage.messgeLabel.Foreground = new SolidColorBrush(Color.FromRgb(76, 215, 21));
                        Tools.LogInfo(msg);
                        break;
                    case SnackType.WARNING:
                        headerPage.messgesBorder.Background = new SolidColorBrush(Color.FromArgb(8, 255, 180, 0));
                        headerPage.messgesBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 180, 0));
                        headerPage.messgeLabel.Foreground = new SolidColorBrush(Color.FromRgb(255, 180, 0));
                        Tools.LogWarning(msg);
                        break;
                    case SnackType.ERROR:
                        headerPage.messgesBorder.Background = new SolidColorBrush(Color.FromArgb(8, 255, 69, 69));
                        headerPage.messgesBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 69, 69));
                        headerPage.messgeLabel.Foreground = new SolidColorBrush(Color.FromRgb(255, 69, 69));
                        Tools.LogWarning(msg);
                        break;
                    case SnackType.INFO:
                        headerPage.messgesBorder.Background = new SolidColorBrush(Color.FromArgb(8, 163, 163, 163));
                        headerPage.messgesBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(163, 163, 163));
                        headerPage.messgeLabel.Foreground = new SolidColorBrush(Color.FromRgb(163, 163, 163));
                        Tools.LogInfo(msg);
                        break;
                    default:
                        headerPage.messgesBorder.Background = new SolidColorBrush(Color.FromArgb(8, 163, 163, 163));
                        headerPage.messgesBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(163, 163, 163));
                        headerPage.messgeLabel.Foreground = new SolidColorBrush(Color.FromRgb(163, 163, 163));
                        break;
                }
                if (hideMilliseconds>0)
                {
                    if (timer != null)
                    {
                        timer.Stop();
                        timer = null;
                    }
                    timer = new DispatcherTimer();
                    Timer(headerPage, hideMilliseconds);
                } else
                {
                    if (timer != null)
                    {
                        timer.Stop();
                        timer = null;
                    }
                }
               
            }));
        }

        public static void hideMessage()
        {
            headerPage.messgePanel.Visibility = Visibility.Hidden;

        }

        public static void showOperateLimitMsg()
        {
            MaterialSnack("操作频繁，请稍后再试！", SnackType.WARNING);
        }

        private static bool isTimerRunning = false;
        //只显示3秒
        private static void Timer(HeaderPage headerPage, int hideMilliseconds)
        {
            if (hideMilliseconds > 0)
            {
                // 如果已经有定时器在运行，则停止并不执行新的定时器
                if (isTimerRunning)
                {
                    timer.Stop();
                }

                // 设置新的定时器间隔
                timer.Interval = TimeSpan.FromSeconds(hideMilliseconds);
                timer.Tick += (s, e) =>
                {
                    // 你想要定期执行的代码
                    headerPage.messgePanel.Visibility = Visibility.Hidden;
                    // 执行完后，更新定时器状态
                    isTimerRunning = false;
                };

                // 启动定时器并标记为正在运行
                isTimerRunning = true;
                timer.Start();
            }
        }
    }
}
