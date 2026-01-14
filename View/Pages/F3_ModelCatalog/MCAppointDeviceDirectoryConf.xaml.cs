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
using 精密切割系统.database.db.modle;
using 精密切割系统.Helpers;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;

namespace 精密切割系统.View.Pages.F3_ModelCatalog
{
    /// <summary>
    /// MCAppointDeviceDirectoryConf.xaml 的交互逻辑
    /// </summary>
    public partial class MCAppointDeviceDirectoryConf : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private FileTableItemModel currentModel;//当前配置
        public MCAppointDeviceDirectoryConf()
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
            var tableList = await SqlHelper.TableAsync<FileTableItemModel>()
                   .Where(t => t.Id == id)
                   .ToListAsync();
            if (tableList.Count > 0)
            {
                currentModel = tableList[0];
                inputText.Text = tableList[0].DeviceDataNo;
            }


        }

        private void BtnSure_RightClicked(object? sender, bool e)
        {
            //查询文件名是否已存在
            CurrentConfigurationModel currentConfigurationModel = CurrentUtils.GetCurrentConfiguration();
            currentConfigurationModel.DeviceDataId = currentModel.Id;
            CurrentUtils.UpdateCurrentConfiguration(currentConfigurationModel);
            mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCDeviceDataListConf");
        }
    }
}
