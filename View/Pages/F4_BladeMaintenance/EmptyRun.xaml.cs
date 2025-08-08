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
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.Helpers;
using 精密切割系统.Model.sqlite;
using 精密切割系统.View.page.right;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.F4_BladeMaintenance
{
    /// <summary>
    /// EmptyRun.xaml 的交互逻辑
    /// </summary>
    public partial class EmptyRun : Page
    {
        private MainWindow _mainWindow;
        private RightPage _rightPage;

        public EmptyRun()
        {
            _mainWindow = Application.Current.MainWindow as MainWindow ?? new MainWindow();
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _rightPage = _mainWindow.rightFrame.Content as RightPage ?? new RightPage();
            _rightPage.PanelAction.Visibility = Visibility.Visible;
            _rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            _rightPage.btnBack.Visibility = Visibility.Visible;
            _rightPage.btnBack.BackFlag = false;
            //_rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked);
            //_rightPage.btnSure.Visibility = Visibility.Visible;
            //_rightPage.btnCutStart.SetRightClickedHandler(BtnCutStart_RightClicked);
            //_rightPage.btnCutStart.Visibility = Visibility.Visible;
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            _mainWindow.NavigateToPage("MainMenu");
        }
    }
}
