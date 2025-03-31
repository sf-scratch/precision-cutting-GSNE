using Emgu.CV.Dnn;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    /// RunLogsPage.xaml 的交互逻辑
    /// </summary>
    public partial class RunLogsPage : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        ObservableCollection<RunLogsModel> runLogsModels { get; set; } = new ObservableCollection<RunLogsModel>();
        ObservableCollection<RunLogsViewModel> eventDataRows { get; set; } = new ObservableCollection<RunLogsViewModel>();
        public RunLogsPage()
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
        public void InitData()
        {
            List<RunLogsModel> models = SqlHelper.Table<RunLogsModel>().ToList();
            int index = 1; // 外部变量用于跟踪索引
            models.ForEach(model => {
                model.Index = index;
                List<RunLogsViewModel> logsList = JsonSerializer.Deserialize<List<RunLogsViewModel>>(model.RecordContent);
                model.EventData = string.Join(" ", logsList.Where(x => x.title != "事件时间").Select(x => x.content));
                runLogsModels.Add(model);
                index++;
            });
            pre_listView.ItemsSource = runLogsModels;
            
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            mainWindow.NavigateToPage("MainMenu");
        }

        private void pre_listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            eventDataRows = new ObservableCollection<RunLogsViewModel>();
            RunLogsModel model = pre_listView.SelectedItem as RunLogsModel;
            var logsList = JsonSerializer.Deserialize<List<RunLogsViewModel>>(model.RecordContent);
            eventDataRows = new ObservableCollection<RunLogsViewModel>(logsList);
            EventDataItems.ItemsSource = eventDataRows;
        }
    }
}
