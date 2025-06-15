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
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.Assets.config.menu;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Controls
{
    /// <summary>
    /// OperateButton.xaml 的交互逻辑
    /// </summary>
    public partial class OperateButton : UserControl
    {

        private SolidColorBrush LeaveBackground = new SolidColorBrush(Color.FromRgb(23, 124, 250));
        private SolidColorBrush DownBackground = new SolidColorBrush(Color.FromRgb(80, 135, 203));

        
        public OperateButton(OperateBean bean)
        {
            InitializeComponent();
            operateIcon.Source = bean.Icon;
            operateText.Name = "operateTxt" + bean.Code.ToString();
            operateText.Text = bean.Title;  
            updataSwitchState(bean);//开关按钮
            operateBorder.Background = new SolidColorBrush(Color.FromRgb(80, 135, 203));
            operateBorder.Tag = bean;
            if (DevicesUtis.IsTouchSupported())
            {
                operateBorder.TouchDown += OperateBorder_TouchDown;
                operateBorder.TouchLeave += OperateBorder_TouchLeave;
                operateBorder.TouchUp += OperateBorder_TouchUp;
            }
            else
            {
                operateBorder.PreviewMouseDown += OperateBorder_MouseDown;
                operateBorder.PreviewMouseUp += OperateBorder_MouseUp;
            }
        }





        //开关按钮
        private void updataSwitchState(OperateBean bean)
        {
            //先全部默认隐藏一次
            /*operate03.Visibility = Visibility.Collapsed;
            operate04.Visibility = Visibility.Collapsed;
            operate05.Visibility = Visibility.Collapsed;
            operate06.Visibility = Visibility.Collapsed;
            operate07.Visibility = Visibility.Collapsed;
            operate08.Visibility = Visibility.Collapsed;
            operate09.Visibility = Visibility.Collapsed;*/
            //根据条件开放按钮显示
            switch (bean.Code)
            {
                case 3:
                    operate03.Visibility = Visibility.Visible;
                    break;
                case 4:
                    operate04.Visibility = Visibility.Visible;
                    break;
                case 5:
                    operate05.Visibility = Visibility.Visible;
                    break;
                case 6:
                    operate06.Visibility = Visibility.Visible;
                    break;
                case 7:
                    operate07.Visibility = Visibility.Visible;
                    break;
                case 8:
                    operate08.Visibility = Visibility.Visible;
                    break;
                case 9:
                    operate09.Visibility = Visibility.Visible;
                    break;
                case 10:
                    operate010.Visibility = Visibility.Visible;
                    break;
                case 8004:
                    operate8004.Visibility = Visibility.Visible;
                    break;
            }

        }

        private void OperateBorder_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Border operateBorder = sender as Border;
            OperateBean bean = operateBorder.Tag as OperateBean;
            operateBorder.Background = new SolidColorBrush(Color.FromRgb(80, 135, 203));
            onClick(bean);
            OperateonLeave?.Invoke(this, bean);
        }

        private void OperateBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            Border operateBorder = sender as Border;
            OperateBean bean = operateBorder.Tag as OperateBean;
            operateBorder.Background = new SolidColorBrush(Color.FromRgb(80, 135, 203));
            OperateonLeave?.Invoke(this, bean);
        }

        private void OperateBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Border operateBorder = sender as Border;
            OperateBean bean = operateBorder.Tag as OperateBean;
            operateBorder.Background = new SolidColorBrush(Color.FromRgb(23, 124, 250));
            OperateonDown?.Invoke(this, bean);
        }

        private void OperateBorder_TouchLeave(object? sender, TouchEventArgs e)
        {
            Border operateBorder = sender as Border;
            OperateBean bean = operateBorder.Tag as OperateBean;
            if (resetState)
            {
                operateBorder.Background = new SolidColorBrush(Color.FromRgb(80, 135, 203));
            }
            OperateonLeave?.Invoke(this, bean);

        }

        private void OperateBorder_TouchDown(object? sender, TouchEventArgs e)
        {
            Border operateBorder = sender as Border;
            OperateBean bean = operateBorder.Tag as OperateBean;
            if (bean.Code!= 309&& bean.Code != 3004)
            {
                operateBorder.Background = new SolidColorBrush(Color.FromRgb(23, 124, 250));
            }
           
            OperateonDown?.Invoke(this, bean);
        }

        private void OperateBorder_TouchUp(object? sender, TouchEventArgs e)
        {
            Border menuButton = sender as Border;
            OperateBean bean = menuButton.Tag as OperateBean;
            onClick(bean);
            if (OperateonLeave != null)
            {
                OperateonLeave.Invoke(this, bean);
            }
        }

        private void onClick(OperateBean bean)
        {
            OperateClicked?.Invoke(this, bean);
        }
        public event EventHandler<OperateBean> OperateClicked;
        public event EventHandler<OperateBean> OperateonLeave;
        public event EventHandler<OperateBean> OperateonDown;

        public void updateChName(string chName)
        {
            operateText.Text = chName;
        }

        public bool resetState { get; set; } = true;

        public void resetBg()
        {
            operateBorder.Background = new SolidColorBrush(Color.FromRgb(23, 124, 250));
        }
    }
}
