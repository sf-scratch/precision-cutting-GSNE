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
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.cut;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.Auto
{
    /// <summary>
    /// AutoCutRuning.xaml 的交互逻辑
    /// </summary>
    public partial class AutoCutRuning : Page
    {
        private NavigationService _navService;
        public AutoCutRuning()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _navService = NavigationService;
            if (_navService != null)
            {
                _navService.Navigated += AutoCutRuning_Navigated;
            }
            NavigateUtils.ClearRightPage();
        }

        private void AutoCutRuning_Navigated(object sender, NavigationEventArgs e)
        {
            if (_navService != null)
            {
                _navService.Navigated -= AutoCutRuning_Navigated;
            }
            if (e.TryParse(out AutoCutPausing autoCutPausing, out AutoCutRuningViewModel autoCutRuningViewModel))
            {
                autoCutPausing.DataContext = new AutoCutPausingViewModel(autoCutRuningViewModel);
            }
        }
    }
}
