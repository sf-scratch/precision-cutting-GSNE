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
using 精密切割系统.Entities;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.logs;
using 精密切割系统.Model.sqlite;
using 精密切割系统.Utils;
using 精密切割系统.View.common;
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
        private ObservableCollection<RunLogsModel> runLogsModels { get; set; } = new ObservableCollection<RunLogsModel>();

        public RunLogsPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            InitData();
            NavigateUtils.ClearOperatePage();
            WindowLayout.RightPageButtons.Clear();
            WindowLayout.RightPageButtons.Add(ButtonParams.Back(Back));
            WindowLayout.OperatePageButtons.Clear();
            WindowLayout.OperatePageButtons.Add(ButtonParams.BlueButton("导出", "ExportVariant", Export));
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            WindowLayout.RightPageButtons.Clear();
            WindowLayout.OperatePageButtons.Clear();
        }

        public void InitData()
        {
            List<RunLogsModel> models = SqlHelper.Table<RunLogsModel>().ToList();
            int index = 1; // 外部变量用于跟踪索引
            models.ForEach(model =>
            {
                model.Index = index;
                List<RunLogsViewModel>? logsList = JsonSerializer.Deserialize<List<RunLogsViewModel>>(model.RecordContent);
                if (logsList != null)
                {
                    model.EventData = string.Join(" ", logsList.Where(x => x.title != "事件时间").Select(x => x.content));
                }
                runLogsModels.Add(model);
                index++;
            });
            pre_listView.ItemsSource = runLogsModels.Reverse();
        }

        private void Export()
        {
            int colWidth = 25;
            string exportStr = "\r\n";

            // 将SelectedItems转换为List便于排序
            var selectedItemsList = pre_listView.SelectedItems.Cast<object>().ToList();

            // 对SelectedItems进行排序，按照每个item中的"刀数"值
            var sortedSelectedItems = selectedItemsList
                .Where(item => item is RunLogsModel)
                .Cast<RunLogsModel>()
                .OrderBy(model =>
                {
                    try
                    {
                        var logsList = JsonSerializer.Deserialize<List<RunLogsViewModel>>(model.RecordContent);
                        var cutCountItem = logsList?.FirstOrDefault(log => log.title == "刀数");
                        if (cutCountItem != null && int.TryParse(cutCountItem.content, out int cutCount))
                        {
                            return cutCount;
                        }
                    }
                    catch
                    {
                        // 解析失败时返回最大值，排在最后
                    }
                    return int.MaxValue;
                })
                .ToList();

            // 遍历排序后的SelectedItems
            foreach (var model in sortedSelectedItems)
            {
                var logsList = JsonSerializer.Deserialize<List<RunLogsViewModel>>(model.RecordContent);
                if (logsList is not null)
                {
                    foreach (var runModel in logsList)
                    {
                        if (runModel.title == "震动幅度" || runModel.title == "结束时间" || runModel.title == "开始时间" || runModel.title == "CUT")
                        {
                            continue;
                        }
                        if (runModel.title == "事件时间")
                        {
                            exportStr += runModel.content.PadRight(colWidth);
                            continue;
                        }
                        exportStr += $"{runModel.title}: {runModel.content}".PadRight(colWidth);
                    }
                    exportStr += "\r\n";
                }
            }

            Tools.CuttingRecord(exportStr);
        }

        private void Back()
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.NavigateToPage("MainMenu");
            }
        }

        private void pre_listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RunLogsModel? model = pre_listView.SelectedItem as RunLogsModel;
            if (model is not null)
            {
                var logsList = JsonSerializer.Deserialize<List<RunLogsViewModel>>(model.RecordContent);
                if (logsList is not null)
                {
                    ObservableCollection<RunLogsViewModel> eventDataRows = new(logsList);
                    EventDataItems.ItemsSource = eventDataRows;
                }
            }
        }
    }
}