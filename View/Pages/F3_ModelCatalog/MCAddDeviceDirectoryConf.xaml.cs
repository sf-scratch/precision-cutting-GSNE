using NPOI.SS.Formula.Functions;
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
using 精密切割系统.database.db.modle;
using 精密切割系统.Helpers;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using static System.Net.Mime.MediaTypeNames;
using 精密切割系统.ViewModel;
using System.Printing;

namespace 精密切割系统.View.Pages.F3_ModelCatalog
{
    /// <summary>
    /// MCAddDeviceDirectoryConf.xaml 的交互逻辑
    /// </summary>
    public partial class MCAddDeviceDirectoryConf : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        public MCAddDeviceDirectoryConf()
        {
            InitializeComponent();
            mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            rightPage = mainWindow.rightFrame.Content as RightPage;
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnSure.Visibility = Visibility.Visible;
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked);
            rightPage.btnSure.GlobalRunOperateFlag = true;
            rightPage.btnBack.GlobalRunOperateFlag = true;
            mainWindow.UpdateOperatePage([], null);
        }
        private void BtnBack_RightClicked(object? sender, bool e)
        {
            mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCDeviceDataListConf");
        }
        private void BtnSure_RightClicked(object? sender, bool e)
        {
            if (!String.IsNullOrEmpty(inputText.Text))
            {
                //查询文件名是否已存在
                var list = SqlHelper.Table<FileTableModel>().Where(t => t.Name == inputText.Text).ToList();
                if (list.Count > 0)
                {
                    MaterialSnackUtils.MaterialSnack("目录名称已存在，请修改！", MaterialSnackUtils.SnackType.WARNING);
                }
                else
                {
                    int id = int.Parse(QueryUtils.getQuery(this)["id"]);
                    FileTableModel model = new FileTableModel();
                    model.Name = inputText.Text;
                    model.Level = 1;
                    model.ParentId = id;
                    SqlHelper.Add(model);
                    mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCDeviceDataListConf");
                }
            }
            else
            {
                MaterialSnackUtils.MaterialSnack("目录名称不能为空！", MaterialSnackUtils.SnackType.WARNING);
            }
        }
    }
}
