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
        private MainWindow? _mainWindow;
        private RightPage? _rightPage;
        private MCCalibrationParametersViewModel ViewModel { get; set; }

        public MCCalibrationParameters()
        {
            InitializeComponent();
            _mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_mainWindow is null) return;
            _rightPage = _mainWindow.rightFrame.Content as RightPage;
            if (_rightPage is null) return;
            _rightPage.PanelAction.Visibility = Visibility.Visible;
            _rightPage.btnBack.Visibility = Visibility.Visible;
            _rightPage.btnBack.BackFlag = false;
            _rightPage.btnBack.SetRightClickedHandler(BackFrom);
            _rightPage.btnSure.SetRightClickedHandler(SureOk);
            _rightPage.btnSure.GlobalRunOperateFlag = true;
            _rightPage.btnBack.GlobalRunOperateFlag = true;
            _rightPage.btnSure.Visibility = Visibility.Visible;
            _mainWindow.UpdateOperatePage([], null);
            ViewModel = new MCCalibrationParametersViewModel(await SqlHelper.GetOrCreateEntityAsync(() => new UserDefineDataModel()));
            this.DataContext = ViewModel;
            long id = long.Parse(QueryUtils.getQuery(this)["id"]);
            FileTableItemModel fileTableItem = await SqlHelper.GetOrCreateEntityAsync(() => new FileTableItemModel(), id);
            ViewModel.HorizontalStraighteningStroke = fileTableItem.HorizontalStraighteningStroke;
            ViewModel.VerticalStraighteningStroke = fileTableItem.VerticalStraighteningStroke;
        }

        private void BackFrom(object? sender, bool v)
        {
            int id = int.Parse(QueryUtils.getQuery(this)["id"]);
            bool lookState = bool.Parse(QueryUtils.getQuery(this)["look"]);
            _mainWindow?.NavigateToPage("Pages/F3_ModelCatalog/MCDeviceDataConf", $"id={id}&look={lookState}");
        }

        private async void SureOk(object? sender, bool v)
        {
            try
            {
                int id = int.Parse(QueryUtils.getQuery(this)["id"]);
                bool lookState = bool.Parse(QueryUtils.getQuery(this)["look"]);
                FileTableItemModel fileTableItem = await SqlHelper.GetOrCreateEntityAsync(() => new FileTableItemModel(), id);
                fileTableItem.HorizontalStraighteningStroke = ViewModel.HorizontalStraighteningStroke;
                fileTableItem.VerticalStraighteningStroke = ViewModel.VerticalStraighteningStroke;
                SqlHelper.Update(fileTableItem);
                SqlHelper.Update(ViewModel.UserDefineDataModel);
                MaterialSnack("保存成功", SnackType.SUCCESS);
            }
            catch
            {
                MaterialSnack("保存失败", SnackType.ERROR);
            }
        }
    }
}