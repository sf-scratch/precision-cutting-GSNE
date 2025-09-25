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
        private readonly SolidColorBrush LeaveBackground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        private readonly SolidColorBrush DownBackground = new SolidColorBrush(Color.FromRgb(23, 124, 250));

        //图片ICON
        public static readonly DependencyProperty ImagePathProperty =
       DependencyProperty.Register("ImagePath", typeof(string), typeof(MenuButton), new PropertyMetadata(null));

        public string ImagePath
        {
            get { return (string)GetValue(ImagePathProperty); }
            set { SetValue(ImagePathProperty, value); }
        }

        public MenuButton(MenuBean bean)
        {
            InitializeComponent();
            ImagePath = bean.BlackIcon;
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

        private void Down(object? sender)
        {
            if (sender is Border menuButton && menuButton.Tag is MenuBean bean)
            {
                menuButton.Background = DownBackground;
                ImagePath = bean.BlackIcon;
            }
        }

        private void Leave(object? sender)
        {
            if (sender is Border menuButton && menuButton.Tag is MenuBean bean)
            {
                menuButton.Background = LeaveBackground;
                ImagePath = bean.BlackIcon;
            }
        }

        private void Up(object? sender)
        {
            if (sender is Border menuButton && menuButton.Tag is MenuBean bean)
            {
                menuButton.Background = LeaveBackground;
                ImagePath = bean.BlackIcon;
                OnClick(bean);
            }
        }

        private void MenuBorder_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Up(sender);
        }

        private void MenuBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Down(sender);
        }

        private void MenuBorder_TouchLeave(object? sender, TouchEventArgs e)
        {
            Leave(sender);
        }

        private void MenuBorder_TouchUp(object? sender, TouchEventArgs e)
        {
            Up(sender);
        }

        private void MenuBorder_TouchDown(object? sender, TouchEventArgs e)
        {
            Down(sender);
        }

        private void OnClick(MenuBean bean)
        {
            MenuClicked?.Invoke(this, bean);
        }

        public event EventHandler<MenuBean> MenuClicked;
    }
}