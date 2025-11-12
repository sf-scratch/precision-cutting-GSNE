using Emgu.CV.Dnn;
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
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.Pages.common;
using 精密切割系统.View.Pages.F2_ManualOperation;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Controls
{
    /// <summary>
    /// DirectOperate.xaml 的交互逻辑
    /// </summary>
    public partial class DirectOperate : UserControl
    {
        public DirectOperate()
        {
            InitializeComponent();
        }

        public void SetHighBtnStatus(bool isHight)
        {
            if (DataContext is DirectOperateViewModel viewModel)
            {
                viewModel.IsHighSpeed = isHight;
            }
        }

        private async void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is DirectOperate directOperate && directOperate.DataContext is DirectOperateViewModel directOperateViewModel)
            {
                if (e.NewValue is bool isVisibleDirectOperate && isVisibleDirectOperate)
                {
                    directOperateViewModel.StartGetAxisInfo();
                }
                else
                {
                    // 如果不可见则停止获取DirectOperate的数据
                    await directOperateViewModel.StopGetAxisInfoAsync();
                }
            }
        }
    }
}