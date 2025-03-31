using Microsoft.VisualBasic;
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

namespace 精密切割系统.View.Pages.common
{
    /// <summary>
    /// CustomDialog.xaml 的交互逻辑
    /// </summary>
    public partial class CustomDialog : Window
    {
        public CustomDialog()
        {
            InitializeComponent();
        }

        public CustomDialog(string msg, string title="")
        {
            InitializeComponent();
            Msg = msg;
            CustomTitle = title;
        }

        public string Msg = "";
        public string CustomTitle = "";
        public static bool IsOk = false;

        // 定义一个依赖属性，用于绑定DialogResult
        public static readonly DependencyProperty DialogResultProperty =
            DependencyProperty.Register("DialogResult", typeof(bool?), typeof(CustomDialog), new PropertyMetadata(null));

        // 提供一个属性包装这个依赖属性
        public bool? DialogResult
        {
            get { return (bool?)GetValue(DialogResultProperty); }
            set { SetValue(DialogResultProperty, value); }
        }

        private void btnOK_RightClicked(object sender, RoutedEventArgs e)
        {
            IsOk = true;
            DialogResult = true;
            Close();
        }

        private void btnCancel_RightClicked(object sender, RoutedEventArgs e)
        {
            IsOk = false;
            DialogResult = true;
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lblTitle.Content = CustomTitle;
            tbxMsg.Text = Msg;
        }

        public void ShowCustomDialog()
        {
            // 设置弹窗在屏幕中央
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.ResizeMode = ResizeMode.NoResize;
            this.ShowDialog(); // 显示弹窗
        }
    }
}
