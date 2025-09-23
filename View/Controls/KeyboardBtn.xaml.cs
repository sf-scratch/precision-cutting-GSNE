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
            }
            else
            {
                keyboardBtn.FontSize = 22;
            }
        }

        private DelegateCommand _keyDownCommand;
        public DelegateCommand KeyDownCommand =>
            _keyDownCommand ?? (_keyDownCommand = new DelegateCommand(ExecuteKeyDownCommand));

        void ExecuteKeyDownCommand()
        {
            KeyPressed?.Invoke(this, BtnValue);
            keyboardBorderBtn.Background = new SolidColorBrush(Color.FromRgb(80, 135, 203));
        }

        private DelegateCommand _keyUpCommand;
        public DelegateCommand KeyUpCommand =>
            _keyUpCommand ?? (_keyUpCommand = new DelegateCommand(ExecuteKeyUpCommand));

        void ExecuteKeyUpCommand()
        {
            keyboardBorderBtn.Background = new SolidColorBrush(Color.FromRgb(135, 182, 211));
        }

        public event EventHandler<string> KeyPressed;

        private static void OnBtnTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 处理显示大小和位置
            var control = (KeyboardBtn)d;
        }
    }
}
