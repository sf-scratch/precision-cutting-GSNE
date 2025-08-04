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
using 精密切割系统.Helpers;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.F3_ModelCatalog
{
    /// <summary>
    /// MCCalibrationParameters.xaml 的交互逻辑
    /// </summary>
    public partial class MCCalibrationParameters : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;
        public MCCalibrationParameters()
        {
            InitializeComponent();
            mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            rightPage = mainWindow.rightFrame.Content as RightPage;
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BackFrom);
            rightPage.btnSure.SetRightClickedHandler(SureOk);
            rightPage.btnSure.GlobalRunOperateFlag = true;
            rightPage.btnBack.GlobalRunOperateFlag = true;
            operatePage = mainWindow.operateFrame.Content as OperatePage;
            rightPage.btnSure.Visibility = Visibility.Visible;
            mainWindow.UpdateOperatePage([], null);
            this.DataContext = new MCCalibrationParametersViewModel();
        }

        private void BackFrom(object sender, bool v)
        {
            int id = int.Parse(QueryUtils.getQuery(this)["id"]);
            bool lookState = bool.Parse(QueryUtils.getQuery(this)["look"]);
            mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCDeviceDataConf", $"id={id}&look={lookState}");
        }

        private async void SureOk(object sender, bool v)
        {
            if (this.DataContext is MCCalibrationParametersViewModel model)
            {
                try
                {
                    SqlHelper.Update(model.UserDefineDataModel);
                    MaterialSnackUtils.MaterialSnack("保存成功", MaterialSnackUtils.SnackType.SUCCESS);
                    int id = int.Parse(QueryUtils.getQuery(this)["id"]);
                    bool lookState = bool.Parse(QueryUtils.getQuery(this)["look"]);
                    mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCDeviceDataConf", $"id={id}&look={lookState}");
                }
                catch
                {
                    MaterialSnackUtils.MaterialSnack("保存失败", MaterialSnackUtils.SnackType.ERROR);
                }
            }
        }
    }
}
