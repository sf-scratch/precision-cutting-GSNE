using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
    /// MCMoveDeviceDataConf.xaml 的交互逻辑
    /// </summary>
    public partial class MCMoveDeviceDirectoryConf : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private ObservableCollection<FileTableModel> commList { get; set; } = new ObservableCollection<FileTableModel>();
        private FileTableItemModel currentModel;//当前配置

        public MCMoveDeviceDirectoryConf()
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
                labDeviceDataNo.Text = tableList[0].DeviceDataNo;
                initFileView(tableList[0].DirectoryId);
            }
        }

        private async Task initFileView(long DirectoryId)
        {
            var fileList = await SqlHelper.TableAsync<FileTableModel>().Where(t => t.Id == DirectoryId).ToListAsync();
            if (fileList.Count > 0)
            {
                labRootFile.Text = fileList[0].Name;
            }

            List<FileTableModel> list = await SqlHelper.TableAsync<FileTableModel>().ToListAsync();
            for (int i = 0; i < list.Count; i++)
            {
                commList.Add(list[i]);
            }
            comboDirectory.ItemsSource = commList;
            comboDirectory.DisplayMemberPath = "Name";
            comboDirectory.SelectedValuePath = "Id";
            comboDirectory.SelectedIndex = 0;
        }

        private void BtnSure_RightClicked(object? sender, bool e)
        {
            //查询文件名是否已存在
            //查询是否有相同的名称
            long fileId = (long)comboDirectory.SelectedValue;
            var list = SqlHelper.Table<FileTableItemModel>()
            .Where(t => t.DirectoryId == fileId)
            .Where(t => t.DeviceDataNo == currentModel.DeviceDataNo)
            .ToList();
            if (list.Count > 0)
            {
                MaterialSnack("该型号参数号已存在，请修改！", SnackType.WARNING);
            }
            else
            {
                currentModel.DirectoryId = fileId;
                int count = SqlHelper.Update(currentModel);
                mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCDeviceDataListConf");
            }
        }
    }
}