using MathNet.Numerics;
using NPOI.SS.Formula.Functions;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using 精密切割系统.Helpers;
using 精密切割系统.Utils;
using static MaterialDesignThemes.Wpf.Theme;

namespace 精密切割系统.View.Controls
{
    /// <summary>
    /// 扩展输入框：可设置水印,可设置必填,可设置正则表达式验证
    /// </summary>
    public class InputTextBox : System.Windows.Controls.TextBox
    {
        #region 依赖属性

        public static readonly DependencyProperty InputTypeProperty;
        public static readonly DependencyProperty XWmkTextProperty;//水印文字
        public static readonly DependencyProperty XWmkForegroundProperty;//水印着色
        public static readonly DependencyProperty XAllowNullProperty;//是否允许为空
        public static readonly DependencyProperty XIsErrorProperty;//是否字段有误
        public static readonly DependencyProperty XRegExpProperty;//正则表达式

        public static readonly DependencyProperty XMinProperty;//最小值 针对数字小数
        public static readonly DependencyProperty XMaxProperty;//最大值 针对数字小数
        public static readonly DependencyProperty XPrecisionProperty;//小数位数 针对数字小数

        #endregion 依赖属性

        private static MainWindow? mainWindow;

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static InputTextBox()
        {
            InputTypeProperty = DependencyProperty.Register("InputType", typeof(InputTypement), typeof(InputTextBox), new PropertyMetadata(InputTypement.Default));
            //XWmkText 水印文字  比如 请输入内容
            XWmkTextProperty = DependencyProperty.Register("XWmkText", typeof(String), typeof(InputTextBox), new PropertyMetadata(null));
            XIsErrorProperty = DependencyProperty.Register("XIsError", typeof(bool), typeof(InputTextBox), new PropertyMetadata(false));
            XAllowNullProperty = DependencyProperty.Register("XAllowNull", typeof(bool), typeof(InputTextBox), new PropertyMetadata(false));
            XWmkForegroundProperty = DependencyProperty.Register("XWmkForeground", typeof(Brush), typeof(InputTextBox), new PropertyMetadata(Brushes.Silver));
            XRegExpProperty = DependencyProperty.Register("XRegExp", typeof(string), typeof(InputTextBox), new PropertyMetadata(""));
            FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(InputTextBox), new FrameworkPropertyMetadata(typeof(InputTextBox)));

            XMinProperty = DependencyProperty.Register("XMin", typeof(decimal), typeof(InputTextBox), new PropertyMetadata(decimal.MinValue));
            XMaxProperty = DependencyProperty.Register("XMax", typeof(decimal), typeof(InputTextBox), new PropertyMetadata(decimal.MaxValue));
            XPrecisionProperty = DependencyProperty.Register("XPrecision", typeof(int), typeof(InputTextBox), new PropertyMetadata(3));
        }

        #region 内部方法

        /// <summary>
        /// 注册事件
        /// </summary>
        public InputTextBox()
        {
            //this.Width = 300;
            //默认垂直居中
            //this.Height = 35;
            this.FontSize = 18;
            this.Padding = new Thickness(5, 0, 5, 0);
            this.VerticalContentAlignment = VerticalAlignment.Center;
            //事件
            this.Loaded += new RoutedEventHandler(InputTextBox_Loaded);
            // this.LostFocus -= new RoutedEventHandler(XTextBox_LostFocus);
            this.LostFocus += new RoutedEventHandler(XTextBox_LostFocus);
            // this.GotFocus -= new RoutedEventHandler(XTextBox_GotFocus);
            this.GotFocus += new RoutedEventHandler(XTextBox_GotFocus);
            // this.TouchDown -= new EventHandler<TouchEventArgs>(InputTextBox_TouchDown);
            this.TouchDown += new EventHandler<TouchEventArgs>(InputTextBox_TouchDown);
            this.PreviewMouseDown += new MouseButtonEventHandler(XTextBox_PreviewMouseDown);
            this.TextChanged += new TextChangedEventHandler(InputTextBox_TextChanged);
        }

        private void InputTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            mainWindow = Application.Current.MainWindow as MainWindow;
            InputTextBox inputTextBox = (InputTextBox)sender;
            if (!inputTextBox.IsEnabled && inputTextBox.Background == null)
            {
                inputTextBox.Background = new SolidColorBrush(Color.FromRgb(240, 242, 245));
            }
            ValidationCheck();
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidationCheck();
        }

        private void InputTextBox_TouchDown(object? sender, TouchEventArgs e)
        {
            mainWindow?.ShowKeyboardPage(1);
        }

        private Regex RegDefalt = new Regex("^[0-9a-zA-Z._-]+$"); //默认可输入字符 数字英文大小写._-
        private Regex RegEnglish = new Regex("^[0-9a-zA-Z]*$");

        /// <summary>
        /// 失去焦点时检查输入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            initNumber();
            ValidationCheck();
        }

        public void initNumber()
        {
            //小数可设置小数位数 数字是0
            if ((InputTypement.Decimal == InputType || InputTypement.Numeral == InputType) && XAllowNull == false)
            {
                var text = this.Text;
                if (string.IsNullOrEmpty(text))
                {
                    text = "0";
                }
                // 格式化文本
                string formattedValue = Tools.FormatDecimalString(text, InputTypement.Decimal == InputType ? XPrecision : 0);
                // 设置格式化后的值
                this.Text = formattedValue;
                this.CaretIndex = this.Text.Length; // 将光标移动到文本末尾
            }
        }

        public string ToStringNoRounding(double value, int decimalPlaces)
        {
            double scale = Math.Pow(10, decimalPlaces);
            return (((long)(value * scale)) / scale).ToString();
        }

        public void ValidationCheck()
        {
            this.XIsError = false;
            //不允许为空时候
            if (XAllowNull == false && (this.Text == null || this.Text.Trim() == ""))
            {
                this.XIsError = true;
                return;
            }
            if (this.Text != null && this.Text.Trim() != "")
            {
                string _text = this.Text.Trim();
                //存在正则表达式验证的时候。
                if (XRegExp != null && XRegExp != "" && Regex.IsMatch(_text, XRegExp) == false)
                {
                    this.XIsError = true;
                    return;
                }
                if (InputType == InputTypement.English)
                {
                    if (RegEnglish.IsMatch(_text) == false)
                    {
                        this.XIsError = true;
                        return;
                    }
                }
                else if (InputType == InputTypement.Numeral || InputType == InputTypement.Decimal)
                {
                    decimal _dv;
                    try
                    {
                        _dv = Tools.GetDecimalStringValue(_text);
                        if (!Regex.IsMatch(_text, @"^0(\.0{1,6})?$"))
                        {
                            if (_dv == 0)
                            {
                                this.XIsError = true;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        this.XIsError = true;
                        return;
                    }
                    if (_dv < XMin)
                    {
                        this.XIsError = true;
                        return;
                    }
                    if (_dv > XMax) // 默认值decimal.MaxValue最大79228162514264337593543950335
                    {
                        this.XIsError = true;
                        return;
                    }
                    ////小数位数判断
                    if (InputType == InputTypement.Decimal)
                    {
                        if (_text.IndexOf('.') != -1)
                        {
                            string _precision_value = _text.Substring(_text.IndexOf(".") + 1);
                            if (_precision_value != null && _precision_value != "" && _precision_value.Length > XPrecision)
                            {
                                this.XIsError = true;
                                return;
                            }
                        }
                    }
                }
                else
                {
                }
            }
        }

        /// <summary>
        /// 获得焦点时选中文字
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                this.Focus();
                this.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Render);
            mainWindow?.ShowKeyboardPage(1);
        }

        /// <summary>
        /// 鼠标点击时选中文字
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            mainWindow?.ShowKeyboardPage(1);
        }

        /// <summary>
        /// 输入内容检测 默认、英文、数字、小数
        /// </summary>
        /// <creator>marc</creator>
        public enum InputTypement
        {
            /// <summary>
            /// 默认
            /// </summary>
            Default,

            /// <summary>
            /// 英文
            /// </summary>
            English,

            /// <summary>
            /// 数字
            /// </summary>
            Numeral,

            /// <summary>
            /// 数字或小数
            /// </summary>
            Decimal,
        }

        /// <summary>
        /// 输入内容类型
        /// </summary>
        public InputTypement InputType
        {
            get
            {
                return (InputTypement)GetValue(InputTypeProperty);
            }
            set
            {
                SetValue(InputTypeProperty, value);
            }
        }

        /// <summary>
        /// 数字、小数时的最小值
        /// </summary>
        public decimal XMin
        {
            get
            {
                return (decimal)GetValue(XMinProperty);
            }
            set
            {
                SetValue(XMinProperty, value);
            }
        }

        /// <summary>
        /// 数字、小数时的最大值
        /// </summary>
        public decimal XMax
        {
            get
            {
                return (decimal)GetValue(XMaxProperty);
            }
            set
            {
                SetValue(XMaxProperty, value);
            }
        }

        /// <summary>
        /// 小数类型时的小数位数默认4为
        /// </summary>
        public int XPrecision
        {
            get
            {
                return (int)GetValue(XPrecisionProperty);
            }
            set
            {
                SetValue(XPrecisionProperty, value);
            }
        }

        /// <summary>
        /// 公布属性XWmkText（水印文字）
        /// </summary>
        public String XWmkText
        {
            get
            {
                return (String)GetValue(XWmkTextProperty);
            }
            set
            {
                SetValue(XWmkTextProperty, value);
            }
        }

        /// <summary>
        /// 公布属性XIsError（是否字段有误）
        /// </summary>
        public bool XIsError
        {
            get
            {
                return (bool)base.GetValue(XIsErrorProperty);
            }
            set
            {
                base.SetValue(XIsErrorProperty, value);
            }
        }

        /// <summary>
        /// 公布属性XWmkForeground（水印着色）
        /// </summary>
        public Brush XWmkForeground
        {
            get
            {
                return (Brush)base.GetValue(XWmkForegroundProperty);
            }
            set
            {
                base.SetValue(XWmkForegroundProperty, value);
            }
        }

        /// <summary>
        /// 公布属性XAllowNull（是否允许为空）
        /// </summary>
        public bool XAllowNull
        {
            get
            {
                return (bool)base.GetValue(XAllowNullProperty);
            }
            set
            {
                base.SetValue(XAllowNullProperty, value);
            }
        }

        /// <summary>
        /// 公布属性XRegExp（正则表达式）
        /// </summary>
        public string XRegExp
        {
            get
            {
                return (string)base.GetValue(XRegExpProperty);
            }
            set
            {
                base.SetValue(XRegExpProperty, value);
            }
        }

        #endregion 内部方法
    }
}