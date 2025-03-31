using Emgu.Util;
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
using 精密切割系统.Assets.config.menu;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;

namespace 精密切割系统.View.Controls
{
    /// <summary>
    /// MenuButton.xaml 的交互逻辑
    /// </summary>
    public partial class MenuButton : UserControl
    {

        private SolidColorBrush LeaveBackground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        private SolidColorBrush DownBackground = new SolidColorBrush(Color.FromRgb(23, 124, 250));
        //private bool resetState = true;
        public MenuButton(MenuBean bean)
        {
            InitializeComponent();
            menuIcon.Source = bean.BlackIcon;
            menuText.Text = bean.Title;
            menuBorder.Background = LeaveBackground;
            menuBorder.Tag = bean;
            
            if (DevicesUtis.IsTouchSupported())
            {
                //触摸屏上使用
                menuBorder.TouchDown += MenuBorder_TouchDown;
                menuBorder.TouchLeave += MenuBorder_TouchLeave;
                menuBorder.TouchUp += MenuBorder_TouchUp;
            }
            else
            {
                menuBorder.MouseDown += MenuBorder_MouseDown;
                menuBorder.MouseUp += MenuBorder_MouseUp;
            }    
        }


        private void MenuBorder_MouseUp(object sender, MouseButtonEventArgs e)
        {
            
            Border menuButton = sender as Border;
            MenuBean bean = menuButton.Tag as MenuBean;
            menuButton.Background = LeaveBackground;
            menuIcon.Source = bean.BlackIcon;
            menuText.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            onClick(bean);
        }


        private void MenuBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Border menuButton = sender as Border;
            MenuBean bean = menuButton.Tag as MenuBean;
            menuButton.Background = DownBackground;
            menuIcon.Source = bean.WhiteIcon;
            menuText.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        }

        private void MenuBorder_TouchLeave(object? sender, TouchEventArgs e)
        {
            if (resetState)
            {
                Border menuButton = sender as Border;
                MenuBean bean = menuButton.Tag as MenuBean;
                menuButton.Background = LeaveBackground;
                menuIcon.Source = bean.BlackIcon;
                menuText.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            }
            resetState = true;
        }


        private void MenuBorder_TouchUp(object? sender, TouchEventArgs e)
        {
            Border menuButton = sender as Border;
            MenuBean bean = menuButton.Tag as MenuBean;
            onClick(bean);
        }



        private void MenuBorder_TouchDown(object? sender, TouchEventArgs e)
        {
            Debug.WriteLine("MenuBorder_TouchDown");
            Border menuButton = sender as Border;
            MenuBean bean = menuButton.Tag as MenuBean;
            menuButton.Background = DownBackground;
            menuIcon.Source = bean.WhiteIcon;
            menuText.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        }

        private void onClick(MenuBean bean)
        {
            MenuClicked?.Invoke(this, bean);
            //MenuBean bean = menuBtn.Tag as MenuBean;
        }
        public event EventHandler<MenuBean> MenuClicked;

        public bool resetState { get; set; }  =true;

        public void resetBg()
        {
        }

    }
}
