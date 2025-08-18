using NPOI.SS.Formula.Functions;
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
using System.Xml.Linq;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.Pages.common;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Controls
{
    /// <summary>
    /// RightButton.xaml 的交互逻辑
    /// </summary>
    public partial class RightButton : UserControl
    {
        //文字
        public static readonly DependencyProperty ContentTextProperty =
        DependencyProperty.Register("ContentText", typeof(string), typeof(RightButton), new PropertyMetadata(null));

        public string ContentText
        {
            get { return (string)GetValue(ContentTextProperty); }
            set { SetValue(ContentTextProperty, value); }
        }

        public double ContentTextFontSize
        {
            get { return (double)GetValue(ContentTextFontSizeProperty); }
            set { SetValue(ContentTextFontSizeProperty, value); }
        }

        // 文字大小
        public static readonly DependencyProperty ContentTextFontSizeProperty =
            DependencyProperty.Register("ContentTextFontSize", typeof(double), typeof(RightButton), new PropertyMetadata(0d));

        // 是否返回 false不返回 true 返回上一级
        public static readonly DependencyProperty BackFlagProperty =
        DependencyProperty.Register("BackFlag", typeof(bool), typeof(RightButton), new PropertyMetadata(null));

        public bool BackFlag
        {
            get { return (bool)GetValue(BackFlagProperty); }
            set { SetValue(BackFlagProperty, value); }
        }
        // 全局有操作时，是否可以点击
        public static readonly DependencyProperty GlobalRunOperateFlagProperty =
        DependencyProperty.Register("GlobalRunOperateFlag", typeof(bool), typeof(RightButton), new PropertyMetadata(null));

        public bool GlobalRunOperateFlag
        {
            get { return (bool)GetValue(GlobalRunOperateFlagProperty); }
            set { SetValue(GlobalRunOperateFlagProperty, value); }
        }
        //图片ICON
        public static readonly DependencyProperty ImagePathProperty =
       DependencyProperty.Register("ImagePath", typeof(string), typeof(RightButton), new PropertyMetadata(null));

        public string ImagePath
        {
            get { return (string)GetValue(ImagePathProperty); }
            set { SetValue(ImagePathProperty, value); }
        }

        //按钮背景色
       public static readonly DependencyProperty BackgroundDefColorProperty =
       DependencyProperty.Register("BackgroundDefColor", typeof(Brush), typeof(RightButton), new PropertyMetadata(Brushes.Silver));


        public Brush BackgroundDefColor
        {
            get { return (Brush)GetValue(BackgroundDefColorProperty); }
            set { SetValue(BackgroundDefColorProperty, value); }
        }

        //按钮按下背景色
        public static readonly DependencyProperty BackgroundDownColorProperty =
        DependencyProperty.Register("BackgroundDownColor", typeof(Brush), typeof(RightButton), new PropertyMetadata(Brushes.Silver));

        public Brush BackgroundDownColor
        {
            get { return (Brush)GetValue(BackgroundDownColorProperty); }
            set { SetValue(BackgroundDownColorProperty, value); }
        }

        public double ButtonWidth
        {
            get { return (double)GetValue(ButtonWidthProperty); }
            set { SetValue(ButtonWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ButtonWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ButtonWidthProperty =
            DependencyProperty.Register("ButtonWidth", typeof(double), typeof(RightButton), new PropertyMetadata(226d));

        public double ButtonHeight
        {
            get { return (double)GetValue(ButtonHeightProperty); }
            set { SetValue(ButtonHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ButtonHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ButtonHeightProperty =
            DependencyProperty.Register("ButtonHeight", typeof(double), typeof(RightButton), new PropertyMetadata(72d));


        public RightButton()
        {
            InitializeComponent();
            if (DevicesUtis.IsTouchSupported()) {
                btnBorder.TouchDown += BtnBorder_TouchDown;
                btnBorder.TouchLeave += BtnBorder_TouchLeave;
                btnBorder.TouchUp += BtnBorder_TouchUp;
            }
            else
            {
                btnBorder.MouseDown += BtnBorder_MouseDown;
                btnBorder.MouseUp += BtnBorder_MouseUp;
                btnBorder.MouseLeave += BtnBorder_MouseLeave;
                btnBorder.PreviewMouseDown += BtnBorder_PreviewMouseDown;
                btnBorder.PreviewMouseUp += BtnBorder_PreviewMouseUp;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            btnBorder.Background = BackgroundDefColor;
        }

        private void BtnBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            btnBorder.Background = BackgroundDefColor;
        }

        private void BtnBorder_TouchLeave(object? sender, TouchEventArgs e)
        {
            //btnBorder.Background = BackgroundDefColor;
        }

        private void BtnBorder_TouchUp(object? sender, TouchEventArgs e)
        {
            btnBorder.Background = BackgroundDefColor;
            onClick(true);
        }

        private void BtnBorder_MouseUp(object sender, MouseButtonEventArgs e)
        {
            btnBorder.Background = BackgroundDefColor;
            onClick(true);
        }

        private void BtnBorder_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            btnBorder.Background = BackgroundDefColor;
        }

        private void BtnBorder_TouchDown(object? sender, TouchEventArgs e)
        {
            btnBorder.Background = BackgroundDownColor;
        }

        private void BtnBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            btnBorder.Background = BackgroundDownColor;
        }


        private void BtnBorder_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            btnBorder.Background = BackgroundDownColor;
        }


        private void onClick(Boolean isOK)
        {
            RightClicked?.Invoke(this, isOK);
        }
        // 设置事件处理器的方法
        public void SetRightClickedHandler(EventHandler<Boolean> handler)
        {
            // 移除所有现有的处理器
            RightClicked -= RightClicked;
            // 添加新的处理器
            RightClicked += handler;
        }
        public event EventHandler<Boolean> RightClicked;
    }
}
