using HslCommunication.Profinet.OpenProtocol;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Text;
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
using 精密切割系统.Assets.config.menu;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Utils;
using 精密切割系统.View;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.Hader;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;
using System.Data;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using System.Globalization;
using System.Diagnostics;
using 精密切割系统.View.Pages.common;
using 精密切割系统.View.Controls;
using 精密切割系统.database.db.modle;
using 精密切割系统.Model.bunkering;
using 精密切割系统.Model.sqlite;
using System.Text.Json.Nodes;
using System.Text.Json;
using 精密切割系统.Model.logs;
using static SQLite.SQLite3;
using NPOI.OpenXmlFormats.Dml.Diagram;
using 精密切割系统.Model.plc;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.Model.cut;
using System.Windows.Threading;
using 精密切割系统.View.common;
using OpenCvSharp;
using System.IO;

namespace 精密切割系统
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SourceChanged();
        }

        private Thread initThread;

        private static event EventHandler<int> onClicked;

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            if (!App.MUTEX.WaitOne(TimeSpan.Zero, true))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("精密切割系统正在运行中。", "提示", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.ServiceNotification);
                });
                Environment.Exit(0);
            }
        }

        public void NavigateToPage(string pageName, string paramsStr = "")
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ContainerLocator.Container.Resolve<IRegionManager>().RequestNavigate(RegionName.MainRegion, nameof(EmptyView));
                mainFrame.Navigate(new Uri($"View/{pageName}.xaml" + (string.IsNullOrEmpty(paramsStr) ? "" : "?" + paramsStr), UriKind.Relative));
            });
        }

        public void NavigateToPage(string pageName, object paramsStr)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ContainerLocator.Container.Resolve<IRegionManager>().RequestNavigate(RegionName.MainRegion, nameof(EmptyView));
                mainFrame.Navigate(new Uri($"View/{pageName}.xaml", UriKind.Relative), paramsStr);
            });
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

            RightPage rightPage = rightFrame.Content as RightPage;
            rightPage.OnFrameSourceChanged(null, null);
            /*Task task = Task.Run(() =>
            {
                CameraUtils.StopAcquisition();
            });*/
            NavigateToPage("main");
        }
        OperatePage operatePage;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 禁用触摸到鼠标事件的转换
            Touch.FrameReported += (s, e) => { /* 防止触摸触发鼠标事件 */ };
            AlarmConfig alarmConfig = AlarmConfig.Instance;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                string logDirectory = "logs";
                int daysThreshold = 30; // 清理超过 30 天的日志
                TimeSpan interval = TimeSpan.FromDays(1); // 每天触发一次
                LogCleaner.StartLogCleanup(logDirectory, daysThreshold, interval);

                operatePage = operateFrame.Content as OperatePage;
                operatePage.SetOperateShowType(0);
                operatePage.UpdateOperate(OperateData.GetTab01Operate());
                InitializeData.initSystemData();
                if (!GlobalParams.onlineFlag)
                {
                    GlobalParams.systemInitFlag = true;
                }
                // 初始化设备
                initThread = new Thread(InitDevice);
                initThread.IsBackground = true;
                initThread.Start();
                operateFrame.Navigated += OperateFrame_Navigated;
                if (DevicesUtis.IsTouchSupported())
                {
                    shortcutDirectBtn.TouchDown += shortcutDirectBtn_TouchDown;
                    shortcutTopBtn.TouchDown += shortcutTopBtn_TouchDown;
                }
                else
                {
                    shortcutDirectBtn.MouseDown += shortcutDirectBtn_MouseDown;
                    shortcutTopBtn.MouseDown += shortcutTopBtn_MouseDown;
                }
                GlobalParams.ValueChanged += (sender, args) =>
                {
                    List<CommonDirection> commonDirectionList = Tools.GetChildrenOfType<CommonDirection>(mainFrame);
                    if (commonDirectionList != null && commonDirectionList.Count > 0)
                    {
                        commonDirectionList[0].SetHighBtnStatus(GlobalParams.heightSpeedStatus);
                    }
                    List<DirectOperate> directOperateList = Tools.GetChildrenOfType<DirectOperate>(operateFrame);
                    if (directOperateList != null && directOperateList.Count > 0)
                    {
                        directOperateList[0].SetHighBtnStatus(GlobalParams.heightSpeedStatus);
                    }
                };

                RunLogsCommon.LogEvent(LogType.INIT, new List<RunLogsViewModel>
                {
                    new RunLogsViewModel(LogType.INIT, "初始化"),
                    new RunLogsViewModel("结果", "初始化成功！")
                });
            }), DispatcherPriority.ContextIdle);
            
        }

        private void ShortcutDirectBtn_TouchUp(object? sender, TouchEventArgs e)
        {
            CommonEvent.BtnScaleDown(sender, 1);
        }

        public void GotoF5()
        {
            MainMenu m = new MainMenu();
            m.UpdateMenu(MenuData.GetF5Menu());
            mainFrame.Source = new Uri("View/MainMenu.xaml", UriKind.Relative);
        }

        public void GotoF7()
        {
            MainMenu m = new MainMenu();
            m.UpdateMenu(MenuData.GetF7Menu());
            mainFrame.Source = new Uri("View/MainMenu.xaml", UriKind.Relative);
        }

        private PlcControl mainPlc;

        private void InitDevice()
        {
            // SetInputLanguageToEnglishUS();
            KeyboardSimulator.SimulateKeyPress("capslock");
            GlobalParams.globalRunFlag = true;
            var taskStartTime = DateTime.Now;
            // 设备初始化重试3分钟
            var maxExecutionTime = TimeSpan.FromMinutes(3);
            // PlcControl初始化后，如果plc连接成功会一直循环更新所有tags
            mainPlc = PlcControl.GetInstance();
            // 加载配置参数
            CurrentUtils.UpdateParams();
            if (!GlobalParams.onlineFlag)
            {
                GlobalParams.globalRunFlag = false;
                return;
            }
            
            while ((DateTime.Now - taskStartTime) < maxExecutionTime)
            {
                try
                {
                    if (!PlcControl.connectionStatus)
                    {
                        MaterialSnackUtils.MaterialSnack("PLC连接中...", MaterialSnackUtils.SnackType.INFO);
                        bool res = mainPlc.ConnectPlc();
                        if (!res)
                        {
                            MaterialSnackUtils.MaterialSnack("PLC连接失败，重试中...", MaterialSnackUtils.SnackType.WARNING, 0);
                            Thread.Sleep(2000);
                        }
                    }
                    if (!CameraUtils.m_bDeviceOpened)
                    {
                        MaterialSnackUtils.MaterialSnack("相机连接中...", MaterialSnackUtils.SnackType.INFO, 0);
                        CameraUtils.connectDevice();
                        if (!CameraUtils.m_bDeviceOpened)
                        {
                            MaterialSnackUtils.MaterialSnack($"相机连接失败: {CameraUtils.errorMessage}", MaterialSnackUtils.SnackType.WARNING);
                            Thread.Sleep(2000);
                        }
                    }
                    if (!CameraUtils.l_lightConnectStatus)
                    {
                        MaterialSnackUtils.MaterialSnack("光源连接中...", MaterialSnackUtils.SnackType.INFO, 0);
                        CameraUtils.ConnectLight();
                        if (!CameraUtils.l_lightConnectStatus)
                        {
                            MaterialSnackUtils.MaterialSnack("光源连接失败，重试中..." + CameraUtils.l_errorMessage, MaterialSnackUtils.SnackType.WARNING);
                            Thread.Sleep(2000);
                        }
                    }
                    // if (PlcControl.connectionStatus && CameraUtils.m_bDeviceOpened && CameraUtils.l_lightConnectStatus)
                    if (PlcControl.connectionStatus)
                    {
                        MaterialSnackUtils.MaterialSnack("设备加载完成！", MaterialSnackUtils.SnackType.SUCCESS, 0);
                        break;
                    }
                    else
                    {
                        MaterialSnackUtils.MaterialSnack("设备加载失败，重试中...", MaterialSnackUtils.SnackType.SUCCESS);
                        Thread.Sleep(2000);
                    }
                }
                catch (Exception ex)
                {
                    // 模拟重试逻辑
                    MaterialSnackUtils.MaterialSnack($"设备连接异常: {ex.Message}", MaterialSnackUtils.SnackType.ERROR);
                    Tools.LogError($"设备连接异常: {ex.Message}");
                    Thread.Sleep(2000); // 等待2秒后重试
                }
            }
            // 退出所有模式
            PlcControl.plc.exitAllModel();
            GlobalParams.globalRunFlag = false;
            
            Thread.Sleep(1000);
            CurrentUtils.initPlcPosition();
            BunkeringHandler.AddBunkeringRecord();
            // 设置面板禁用
            PlcControl.tagControl.wholeDevice.SetPanelButtonsStauts(0);
            
            // 关闭Y轴光栅尺校准
            PlcControl.tagControl.cutting.SetYAxisCompStatus(0);

            // 记录异常日志
            //PlcControl.AddAlarmLog();
        }

        private void SetInputLanguageToEnglishUS()
        {
            try
            {
                // 创建 CultureInfo 对象用于表示英语（美国）
                CultureInfo englishUS = new CultureInfo("en-US");

                // 检查系统是否支持该输入法
                if (InputLanguageManager.Current.AvailableInputLanguages != null)
                {
                    foreach (var language in InputLanguageManager.Current.AvailableInputLanguages)
                    {
                        if (language is CultureInfo culture && culture.Name == englishUS.Name)
                        {
                            InputLanguageManager.Current.CurrentInputLanguage = englishUS;
                            Console.WriteLine("输入法已切换为：英语（美国）");
                            return;
                        }
                    }
                }

                Console.WriteLine("英语（美国）输入法未安装。");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发生异常: {ex.Message}");
            }
        }
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                Osklib.OnScreenKeyboard.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public bool isNavigating = false;
        // 事件处理程序
        private void OperateFrame_Navigated(object sender, NavigationEventArgs e)
        {
            SetOperateBtn(operatePage);
        }

        public bool shortcutTopBtnSel = false;
        public bool shortcutBottomBtnSel = false;

        private void shortcutTopBtn_TouchDown(object? sender, TouchEventArgs e)
        {
            shortcutTopBtnSel = !shortcutTopBtnSel;
            shortcutBottomBtnSel = false;
            ShortcutBtnClick();
            if (shortcutTopBtnSel)
            {
                // 显示方向界面
                operatePage.SetOperateShowType(1);
            } 
            else
            {
                if (WindowLayout.OperatePageButtons.Count != 0)
                {
                    operatePage.SetOperateShowType(3);
                }
                else
                {
                    operatePage.SetOperateShowType(0);
                }
            }
            CommonEvent.BtnScaleDown(sender, 1);
            CommonEvent.BtnScaleDown(shortcutDirectBtn, 0);
        }

        public void UpdateOperatePage(List<OperateBean> operateBeans, EventHandler<int> _onClicked, EventHandler<int> _touchLeave = null, EventHandler<int> _touchDown = null)
        {
            operatePage.SetOperateShowType(0);
            shortcutTopBtnSel = false;
            operatePage.UpdateOperate(operateBeans);
            operatePage.SetOnClickedHandler(_onClicked, _touchLeave, _touchDown);
        }

        public void SetOperateBtn(OperatePage operatePage)
        {
            operatePage.SetOperateShowType(0);
            if (shortcutBottomBtnSel)
            {
                operatePage.UpdateOperate(OperateData.GetTab01Operate());
            }
            else
            {
                // 显示当前页面
                operatePage.UpdateOperate(GlobalParams.currentOperateBeanList);
            }
            ShortcutBtnClick();
        }
        /// <summary>
        /// 显示/隐藏 键盘
        /// </summary>
        /// <param name="status"> 1 显示 0 隐藏</param>
        public void ShowKeyboardPage(int status)
        {
            if (status == 1)
            {
                shortcutTopBtnSel = false;
                shortcutBottomBtnSel = false;
                // operateFrame.Source = new Uri("View/Pages/common/CustomKeyboard.xaml", UriKind.Relative);
                operatePage.SetOperateShowType(2);
                CommonEvent.BtnScaleDown(shortcutDirectBtn, 0);
                CommonEvent.BtnScaleDown(shortcutTopBtn, 0);
            }
            else if (status == 0) 
            {
                // operateFrame.Navigate(new Uri("View/Pages/operate/OperatePage.xaml", UriKind.Relative));
                operatePage.SetOperateShowType(0);
                // 判断是否有上一个页面
                isNavigating = true;
            }
            ShortcutBtnClick();
        }

        private void shortcutDirectBtn_TouchDown(object? sender, TouchEventArgs e)
        {
            operatePage.SetOperateShowType(0);
            shortcutBottomBtnSel = !shortcutBottomBtnSel;
            shortcutTopBtnSel = false;
            ShortcutBtnClick();
            CommonEvent.BtnScaleDown(sender, 1);
            CommonEvent.BtnScaleDown(shortcutTopBtn, 0);
            if (shortcutBottomBtnSel)
            {
                operatePage.UpdateOperate(OperateData.GetTab01Operate());
            }
            else
            {
                if (GlobalParams.currentOperateBeanList.Count != 0)
                {
                    operatePage.UpdateOperate(GlobalParams.currentOperateBeanList);
                    return;
                }
                if (WindowLayout.OperatePageButtons.Count != 0)
                {
                    operatePage.SetOperateShowType(3);
                    return;
                }
                operatePage.UpdateOperate(GlobalParams.currentOperateBeanList);
            }
        }


        public void SetShortcutBtnStatus(bool _shortcutTopBtnSel, bool _shortcutBottomBtnSel)
        {
            shortcutTopBtnSel = _shortcutTopBtnSel;
            shortcutBottomBtnSel = _shortcutBottomBtnSel;
            ShortcutBtnClick();
        }

        public void ShortcutBtnClick()
        {
            // 设置按钮和图标的动态效果
            AnimateButtonAppearance(shortcutTopBtn, shortcutTopIcon,
                shortcutTopBtnSel,
                Color.FromRgb(49, 91, 143), "/Assets/picture/shortcut_top_btn_icon_sel.png",
                Color.FromRgb(255, 255, 255), "/Assets/picture/shortcut_top_btn_icon.png");

            AnimateButtonAppearance(shortcutDirectBtn, shortcutDirectIcon,
                shortcutBottomBtnSel,
                Color.FromRgb(49, 91, 143), "/Assets/picture/shortcut_direct_sel.png",
                Color.FromRgb(255, 255, 255), "/Assets/picture/shortcut_direct.png");
        }

        // 工具方法：动态设置按钮背景颜色和图标
        private void AnimateButtonAppearance(Border button, Image icon, bool isSelected,
            Color selectedColor, string selectedIconPath,
            Color unselectedColor, string unselectedIconPath)
        {
            // 动画时长
            var duration = TimeSpan.FromMilliseconds(0);

            // 背景颜色动画
            ColorAnimation colorAnimation = new ColorAnimation
            {
                From = ((SolidColorBrush)button.Background).Color, // 当前颜色
                To = isSelected ? selectedColor : unselectedColor, // 目标颜色
                Duration = duration
            };
            button.Background = new SolidColorBrush(((SolidColorBrush)button.Background).Color); // 确保动画可用
            ((SolidColorBrush)button.Background).BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            // 切换图标
            icon.Source = Tools.BitmapImageToBitmap(isSelected ? selectedIconPath : unselectedIconPath);
            
        }


        //监听页面是否改变，然后关闭消息提示
        private void SourceChanged()
        {
            DependencyPropertyDescriptor descriptor = DependencyPropertyDescriptor.FromProperty(Frame.SourceProperty, typeof(Frame));
            descriptor.AddValueChanged(mainFrame, OnFrameSourceChanged);
        }

        // 当Frame的Source属性变化时调用
        private void OnFrameSourceChanged(object sender, EventArgs e) {
            HeaderPage headerPage  = headerFrame.Content as HeaderPage;
            headerPage.messgePanel.Visibility = Visibility.Hidden;
        }

        private void shortcutTopBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            shortcutTopBtnSel = !shortcutTopBtnSel;
            shortcutBottomBtnSel = false;
            ShortcutBtnClick();
            if (shortcutTopBtnSel)
            {
                // 显示方向界面
                operatePage.SetOperateShowType(1);
            }
            else
            {
                if (WindowLayout.OperatePageButtons.Count != 0)
                {
                    operatePage.SetOperateShowType(3);
                }
                else
                {
                    operatePage.SetOperateShowType(0);
                }
            }
        }

        private void shortcutDirectBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            operatePage.SetOperateShowType(0);
            shortcutBottomBtnSel = !shortcutBottomBtnSel;
            shortcutTopBtnSel = false;
            ShortcutBtnClick();
            if (shortcutBottomBtnSel)
            {
                operatePage.UpdateOperate(OperateData.GetTab01Operate());
            }
            else
            {
                if (GlobalParams.currentOperateBeanList.Count != 0)
                {
                    operatePage.UpdateOperate(GlobalParams.currentOperateBeanList);
                    return;
                }
                if (WindowLayout.OperatePageButtons.Count != 0)
                {
                    operatePage.SetOperateShowType(3);
                    return;
                }
                operatePage.UpdateOperate(GlobalParams.currentOperateBeanList);
            }
        }
    }
}