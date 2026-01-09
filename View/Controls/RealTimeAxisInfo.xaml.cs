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
    /// RealTimeAxisInfo.xaml 的交互逻辑
    /// </summary>
    public partial class RealTimeAxisInfo : UserControl
    {
        private CancellationTokenSource _cts;

        public RealTimeAxisInfo()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _cts = new CancellationTokenSource();
            _ = Task.Run(StartLoadPosition);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
        }

        private async Task StartLoadPosition()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var axisPostion = await AutoCutUtils.GetAxisPositionAsync();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        xAbsolutePosition.Text = axisPostion.X?.ToString("F5");
                        yAbsolutePosition.Text = axisPostion.Y?.ToString("F5");
                        zAbsolutePosition.Text = axisPostion.Z1?.ToString("F5");
                        z2AbsolutePosition.Text = axisPostion.Z2?.ToString("F5");
                        thetaAbsolutePosition.Text = axisPostion.Theta?.ToString("F5");
                    });
                }
                catch (Exception) { }
                await Task.Delay(100);
            }
        }
    }
}