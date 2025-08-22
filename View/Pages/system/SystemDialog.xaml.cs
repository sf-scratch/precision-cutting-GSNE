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
using System.Windows.Shapes;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;

namespace 精密切割系统.View.Pages.system
{
    /// <summary>
    /// SystemDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SystemDialog : Window
    {
        public SystemDialog()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DevicesUtis.IsTouchSupported()) {
                btnExit.TouchDown += BtnExit_TouchDown;
                btnCanle.TouchDown += BtnCanle_TouchDown;
                btnMinimized.TouchDown += BtnMinimized_TouchDown;
            }
            else
            {
                btnCanle.MouseDown += BtnCanle_MouseDown;
                btnExit.MouseDown += BtnExit_MouseDown;
                btnMinimized.MouseDown += BtnMinimized_MouseDown;
            }
        }

        private void BtnMinimized_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Application.Current.MainWindow.WindowState = System.Windows.WindowState.Minimized;
            Close();
        }

        private void BtnMinimized_TouchDown(object? sender, TouchEventArgs e)
        {
            Application.Current.MainWindow.WindowState = System.Windows.WindowState.Minimized;
            Close();
        }

        private void BtnExit_TouchDown(object? sender, TouchEventArgs e)
        {
            CameraUtils.CloseDevice();
            Application.Current.Shutdown();
        }

        private void BtnExit_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CameraUtils.CloseDevice();
            Application.Current.Shutdown();
        }

        private void BtnCanle_TouchDown(object? sender, TouchEventArgs e)
        {
            Close();
        }

        private void BtnCanle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }
    }
}
