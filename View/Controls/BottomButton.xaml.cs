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
using System.Windows.Navigation;
using System.Windows.Shapes;
using 精密切割系统.Helpers;

namespace 精密切割系统.View.Controls
{
    /// <summary>
    /// BottomButton.xaml 的交互逻辑
    /// </summary>
    public partial class BottomButton : UserControl
    {
        //文字
        public static readonly DependencyProperty ContentTextProperty =
        DependencyProperty.Register("ContentText", typeof(string), typeof(BottomButton), new PropertyMetadata(null));

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
            DependencyProperty.Register("ContentTextFontSize", typeof(double), typeof(BottomButton), new PropertyMetadata(0d));

        // 是否返回 false不返回 true 返回上一级
        public static readonly DependencyProperty BackFlagProperty =
        DependencyProperty.Register("BackFlag", typeof(bool), typeof(BottomButton), new PropertyMetadata(null));

        public bool BackFlag
        {
            get { return (bool)GetValue(BackFlagProperty); }
            set { SetValue(BackFlagProperty, value); }
        }
        // 全局有操作时，是否可以点击
        public static readonly DependencyProperty GlobalRunOperateFlagProperty =
        DependencyProperty.Register("GlobalRunOperateFlag", typeof(bool), typeof(BottomButton), new PropertyMetadata(null));

        public bool GlobalRunOperateFlag
        {
            get { return (bool)GetValue(GlobalRunOperateFlagProperty); }
            set { SetValue(GlobalRunOperateFlagProperty, value); }
        }
        //图片ICON
        public static readonly DependencyProperty ImagePathProperty =
       DependencyProperty.Register("ImagePath", typeof(string), typeof(BottomButton), new PropertyMetadata(null));

        public string ImagePath
        {
            get { return (string)GetValue(ImagePathProperty); }
            set { SetValue(ImagePathProperty, value); }
        }

        //按钮背景色
        public static readonly DependencyProperty BackgroundDefColorProperty =
        DependencyProperty.Register("BackgroundDefColor", typeof(Brush), typeof(BottomButton), new PropertyMetadata(Brushes.Silver));


        public Brush BackgroundDefColor
        {
            get { return (Brush)GetValue(BackgroundDefColorProperty); }
            set { SetValue(BackgroundDefColorProperty, value); }
        }

        //按钮按下背景色
        public static readonly DependencyProperty BackgroundDownColorProperty =
        DependencyProperty.Register("BackgroundDownColor", typeof(Brush), typeof(BottomButton), new PropertyMetadata(Brushes.Silver));

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
            DependencyProperty.Register("ButtonWidth", typeof(double), typeof(BottomButton), new PropertyMetadata(226d));

        public double ButtonHeight
        {
            get { return (double)GetValue(ButtonHeightProperty); }
            set { SetValue(ButtonHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ButtonHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ButtonHeightProperty =
            DependencyProperty.Register("ButtonHeight", typeof(double), typeof(BottomButton), new PropertyMetadata(72d));

        public Visibility OpenOrCloseVisibility
        {
            get { return (Visibility)GetValue(OpenOrCloseVisibilityProperty); }
            set { SetValue(OpenOrCloseVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OpenOrCloseVisibility.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OpenOrCloseVisibilityProperty =
            DependencyProperty.Register("OpenOrCloseVisibility", typeof(Visibility), typeof(BottomButton), new PropertyMetadata(Visibility.Collapsed));

        public bool IsOpen
        {
            get { return (bool)GetValue(IsOpenProperty); }
            set { SetValue(IsOpenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsOpen.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register("IsOpen", typeof(bool), typeof(BottomButton), new PropertyMetadata(false));



        public BottomButton()
        {
            InitializeComponent();
        }
    }
}
