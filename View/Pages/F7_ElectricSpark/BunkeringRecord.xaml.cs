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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using 精密切割系统.Model.sqlite;
using 精密切割系统.View.page.right;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.F7_ElectricSpark
{
    /// <summary>
    /// BunkeringRecord.xaml 的交互逻辑
    /// </summary>
    public partial class BunkeringRecord : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;

        ObservableCollection<BunkeringRecordModel> bunkeringRecordModels { get; set; } = new ObservableCollection<BunkeringRecordModel>();
        public BunkeringRecord()
        {
            mainWindow = Application.Current.MainWindow as MainWindow;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            rightPage = mainWindow.rightFrame.Content as RightPage;
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            InitData();
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            mainWindow.NavigateToPage("MainMenu");
        }

        private void InitData()
        {
            List<BunkeringRecordModel> models = SqlHelper.Table<BunkeringRecordModel>().ToList();
            int index = 1; // 外部变量用于跟踪索引
            models.ForEach(model => {
                model.BunkeringIndex = index;
                bunkeringRecordModels.Add(model);
                index++;
            });
            pre_listView.ItemsSource = bunkeringRecordModels;
        }
    }
}
