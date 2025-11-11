using Emgu.CV.Dnn;
using ScottPlot;
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
using 精密切割系统.Driver;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.logs;
using 精密切割系统.Model.sqlite;
using 精密切割系统.View.page.right;
using 精密切割系统.ViewModel;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.View.Pages.F7_ElectricSpark
{
    /// <summary>
    /// RunLogsPage.xaml 的交互逻辑
    /// </summary>
    public partial class RunLogsPage : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;

        private ObservableCollection<RunLogsModel> runLogsModels { get; set; } = new ObservableCollection<RunLogsModel>();

        private ObservableCollection<RunLogsViewModel> eventDataRows { get; set; } = new ObservableCollection<RunLogsViewModel>();

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
            // 记录日志
            //RunLogsCommon.LogEvent(
            //    LogType.Cut,
            //    new RunLogsViewModel(LogType.Cut, "切割"),
            //    new RunLogsViewModel("刀数", "3"),
            //    new RunLogsViewModel("开始时间", DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss")),
            //    new RunLogsViewModel("结束时间", DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss")),
            //    new RunLogsViewModel("切割速度", "cutSpeed.ToString()"),
            //    new RunLogsViewModel("Z轴开始位置", "endZ.ToString()"),
            //    new RunLogsViewModel("Z轴结束位置", "startZ.ToString()"),
            //    new RunLogsViewModel("X轴开始位置", "startX.ToString()"),
            //    new RunLogsViewModel("X轴结束位置", "endX.ToString()"),
            //    new RunLogsViewModel("Y轴切割位置", "line.StartPoint.Y.ToString()"),
            //    new RunLogsViewModel("theta角度", "(_cutThetaAlignDeg + cutStep.ThetaDeg).ToString()"),
            //    new RunLogsViewModel("主轴转速", "_spindleRev.ToString()"),
            //    new RunLogsViewModel("震动幅度", string.Join(" ", new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }))
            //    );
            List<RunLogsModel> models = SqlHelper.Table<RunLogsModel>().ToList();
            int index = 1; // 外部变量用于跟踪索引
            models.ForEach(model =>
            {
                model.Index = index;
                List<RunLogsViewModel> logsList = JsonSerializer.Deserialize<List<RunLogsViewModel>>(model.RecordContent);
                model.EventData = string.Join(" ", logsList.Where(x => x.title != "事件时间").Select(x => x.content));
                runLogsModels.Add(model);
                index++;
            });
            pre_listView.ItemsSource = runLogsModels.Reverse();
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