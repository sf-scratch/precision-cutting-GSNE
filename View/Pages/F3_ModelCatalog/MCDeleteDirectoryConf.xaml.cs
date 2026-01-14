using Emgu.CV.Dnn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
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
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;

namespace 精密切割系统.View.Pages.F3_ModelCatalog
{
    /// <summary>
    /// MCDeleteDirectoryConf.xaml 的交互逻辑
    /// </summary>
    public partial class MCDeleteDirectoryConf : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private FileTableModel currentModel;//当前配置
        public MCDeleteDirectoryConf()
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

            _ = initView();
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCDeviceDataListConf");
        }

        private async Task initView()
        {

            int id = int.Parse(QueryUtils.getQuery(this)["id"]);

            //查询数据
            var tableList = await SqlHelper.TableAsync<FileTableModel>()
                   .Where(t => t.Id == id)
                   .ToListAsync();
            if (tableList.Count > 0)
            {
                currentModel = tableList[0];
                inputText.Text = tableList[0].Name;
            }


        }

        private void BtnSure_RightClicked(object? sender, bool e)
        {
            //查询文件名是否已存在
            var list = SqlHelper.Table<FileTableItemModel>().Where(t => t.DirectoryId == currentModel.Id).ToList();
            if (list.Count()== 0) {
                int count = SqlHelper.Delete(currentModel);
                mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCDeviceDataListConf");
            }else {
                MaterialSnack("目录下有配置文件，不允许删除！", SnackType.WARNING);
            }

        }
    }
}
