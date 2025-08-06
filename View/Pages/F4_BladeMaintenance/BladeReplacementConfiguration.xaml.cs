using Prism.Events;
using System;
using System.Collections.Generic;
using System.Data;
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
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.plc;
using 精密切割系统.PubSubEvent;
using 精密切割系统.Utils;
using 精密切割系统.View.common;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.F4_BladeMaintenance
{
    /// <summary>
    /// BladeReplacementConfiguration.xaml 的交互逻辑
    /// </summary>
    public partial class BladeReplacementConfiguration : UserControl
    {
        private readonly IEventAggregator _eventAggregator;
        private MainWindow? _mainWindow;

        public BladeReplacementConfiguration(IEventAggregator eventAggregator)
        {
            InitializeComponent();
            _eventAggregator = eventAggregator;
            _mainWindow = Application.Current.MainWindow as MainWindow;
            this.Loaded += BladeReplacementConfiguration_Loaded;
            this.Unloaded += BladeReplacementConfiguration_Unloaded;
        }

        private void LunguFocus(string target)
        {
            if (target == "lunguTextBox")
                lunguTextBox.Focus();
        }

        private void BladeReplacementConfiguration_Loaded(object sender, RoutedEventArgs e)
        {
            _eventAggregator.GetEvent<SetFocusEvent>().Subscribe(LunguFocus, ThreadOption.UIThread);
        }

        private void BladeReplacementConfiguration_Unloaded(object sender, RoutedEventArgs e)
        {
            _eventAggregator.GetEvent<SetFocusEvent>().Unsubscribe(LunguFocus);
        }

        private void lunguTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _mainWindow?.ShowKeyboardPage(1);
        }

        private void lunguTextBox_TouchDown(object sender, TouchEventArgs e)
        {
            _mainWindow?.ShowKeyboardPage(1);
        }
    }
}
