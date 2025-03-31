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
using 精密切割系统.Utils;
using 精密切割系统.View.Pages.common;

namespace 精密切割系统.View.Controls
{
    /// <summary>
    /// KeyboardBtn.xaml 的交互逻辑
    /// </summary>
    public partial class KeyboardBtn : UserControl
    {
        public KeyboardBtn()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty BtnValueProperty =
        DependencyProperty.Register("BtnValue", typeof(string), typeof(KeyboardBtn), new PropertyMetadata(null, OnBtnValueChanged));

        public string BtnValue
        {
            get { return (string)GetValue(BtnValueProperty); }
            set { SetValue(BtnValueProperty, value); }
        }
        public static readonly DependencyProperty BtnTypeProperty =
        DependencyProperty.Register("BtnType", typeof(string), typeof(KeyboardBtn), new PropertyMetadata(null, OnBtnTypeChanged));

        public string BtnType
        {
            get { return (string)GetValue(BtnTypeProperty); }
            set { SetValue(BtnTypeProperty, value); }
        }
        private static void OnBtnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (KeyboardBtn)d;
            if (control.keyboardBorderBtn != null)
            {
                control.keyboardText.Text = e.NewValue + "";
            }
        }

        public event EventHandler<string> KeyPressed;

        private static void OnBtnTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 处理显示大小和位置
            var control = (KeyboardBtn)d;
        }
        public void SetClick(EventHandler<string> clickHandler)
        {
            // keyboardBorderBtn.Click += clickHandler
        }
        private void keyboardBorderBtn_TouchDown(object sender, TouchEventArgs e)
        {
            if (!BtnValue.Equals("Down"))
            {
                // 阻止焦点转移
                e.Handled = true;
                // 获取当前聚焦的元素
                var focusedElement = Keyboard.FocusedElement as TextBox;
                if (focusedElement != null)
                {
                    // 保持输入框焦点
                    focusedElement.Focus();
                    Thread.Sleep(50);
                }
            }
            KeyDown(sender);
        }
        private void KeyDown(object sender)
        {
            
            KeyPressed?.Invoke(this, BtnValue);
            keyboardBorderBtn.Background = new SolidColorBrush(Color.FromRgb(80, 135, 203));
        }
        private void keyboardBtn_Loaded(object sender, RoutedEventArgs e)
        {
            if (BtnType != null)
            {
                if ("0".Equals(BtnType))
                {
                    keyboardBtn.FontSize = 22;
                }
                else if (BtnType.Equals("2"))
                {
                    keyboardBtn.FontSize = 18;
                    keyboardText.Padding = new Thickness(3, 2, 1, 1);
                }
                else if (BtnType.Equals("3"))
                {
                    keyboardBorderBtn.BorderThickness = new Thickness(0);
                    keyboardBtn.FontSize = 26;
                    keyboardText.HorizontalAlignment = HorizontalAlignment.Center;
                    keyboardText.VerticalAlignment = VerticalAlignment.Center;
                }
            } else
            {
                keyboardBtn.FontSize = 22;
            }
            keyboardBorderBtn.TouchUp += keyboardBtn_TouchUp;
            keyboardBorderBtn.TouchLeave += keyboardBtn_TouchLeave;
            keyboardBorderBtn.TouchDown += keyboardBorderBtn_TouchDown;
            // keyboardBorderBtn.PreviewMouseDown += KeyboardBorderBtn_PreviewMouseDown;
            // keyboardBorderBtn.PreviewMouseUp += KeyboardBorderBtn_PreviewMouseUp;
        }

        private void KeyboardBorderBtn_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!BtnValue.Equals("Down"))
            {
                // 阻止焦点转移
                e.Handled = true;
                // 获取当前聚焦的元素
                var focusedElement = Keyboard.FocusedElement as TextBox;
                if (focusedElement != null)
                {
                    // 保持输入框焦点
                    focusedElement.Focus();
                    Thread.Sleep(50);
                }
            }
            KeyDown(sender);
        }

        private void KeyboardBorderBtn_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            KeyUp();
        }

        private void keyboardBtn_TouchLeave(object sender, TouchEventArgs e)
        {
            KeyUp();
        }

        private void keyboardBtn_TouchUp(object sender, TouchEventArgs e)
        {
            KeyUp();
        }

        private void KeyUp()
        {
            keyboardBorderBtn.Background = new SolidColorBrush(Color.FromRgb(135, 182, 211));
        }
    }
}
