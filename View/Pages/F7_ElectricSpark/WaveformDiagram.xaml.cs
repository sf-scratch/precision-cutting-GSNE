using ScottPlot;
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
using 精密切割系统.Driver;
using 精密切割系统.Helpers;
using 精密切割系统.View.page.right;
using 精密切割系统.ViewModel;
using Colors = ScottPlot.Colors;

namespace 精密切割系统.View.Pages.F7_ElectricSpark
{
    /// <summary>
    /// WaveformDiagram.xaml 的交互逻辑
    /// </summary>
    public partial class WaveformDiagram : Page
    {
        private MainWindow? _mainWindow;
        private RightPage? _rightPage;

        public WaveformDiagram()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _mainWindow = Application.Current.MainWindow as MainWindow;
            if (_mainWindow == null) return;
            _rightPage = _mainWindow.rightFrame.Content as RightPage;
            if (_rightPage == null) return;
            _rightPage.PanelAction.Visibility = Visibility.Visible;
            _rightPage.btnBack.Visibility = Visibility.Visible;
            _rightPage.btnBack.BackFlag = false;
            _rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            _rightPage.btnClear.Visibility = Visibility.Visible;
            _rightPage.btnClear.BackFlag = false;
            _rightPage.btnClear.SetRightClickedHandler(BtnClear_RightClicked);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            customPlot.Dispose();
        }

        private void BtnClear_RightClicked(object? sender, bool e)
        {
            customPlot.Clear();
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            _mainWindow?.NavigateToPage("MainMenu");
        }
    }
}