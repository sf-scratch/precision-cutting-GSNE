using Emgu.CV.Dnn;
using HslCommunication.Profinet.OpenProtocol;
using NPOI.HSSF.UserModel;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
            WindowLayout.OperatePageButtons.Add(ButtonParams.BlueButton("导出TXT", "ExportVariant", ExportAsync));
            WindowLayout.OperatePageButtons.Add(ButtonParams.BlueButton("导出EXCEL", "ExportVariant", ExportToExcel));
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

        private async Task ExportAsync()
        {
            try
            {
                int colWidth = 25;
                string exportStr = "\r\n";
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

                string parentPath = System.Environment.CurrentDirectory + "\\ExportData\\";
                if (!Directory.Exists(parentPath))
                {
                    Directory.CreateDirectory(parentPath);
                }
                DateTime now = DateTime.Now;
                string format1 = now.ToString("yyyy_MM_dd_HHmmss");
                string fileName = "ExportData" + format1 + ".txt";
                string filePath = System.IO.Path.Combine(parentPath, fileName);
                using FileStream fs = new(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                using StreamWriter streamWriter = new(fs, Encoding.UTF8);
                await streamWriter.WriteLineAsync(exportStr);
                await streamWriter.FlushAsync();
                MaterialSnack($"已导出到: {filePath}", SnackType.SUCCESS);
            }
            catch (Exception ex)
            {
                MaterialSnack($"导出异常: {ex.ToString()}", SnackType.SUCCESS);
            }
        }

        public void ExportToExcel()
        {
            string parentPath = System.Environment.CurrentDirectory + "\\ExportData\\";
            if (!Directory.Exists(parentPath))
            {
                Directory.CreateDirectory(parentPath);
            }
            DateTime now = DateTime.Now;
            string format1 = now.ToString("yyyy_MM_dd_HHmmss");
            string fileName = "ExportData" + format1 + ".xlsx";
            string filePath = System.IO.Path.Combine(parentPath, fileName);

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

            // 创建工作簿 2007之前用HSSFWorkbook 2007之后用XSSFWorkbook
            IWorkbook? workBook = null;
            workBook = new HSSFWorkbook();
            // 每个工作簿有多个sheet，创建一个sheet
            ISheet sheet = workBook.CreateSheet("切割数据记录");

            // 创建单元格样式 - 居中
            ICellStyle centerStyle = workBook.CreateCellStyle();
            centerStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center; // 水平居中
            centerStyle.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center; // 垂直居中

            // 创建标题行样式（可选：加粗+居中）
            ICellStyle titleStyle = workBook.CreateCellStyle();
            titleStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            titleStyle.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;

            // 创建字体（用于标题加粗）
            IFont titleFont = workBook.CreateFont();
            titleFont.FontHeightInPoints = 11;
            titleFont.IsBold = true;
            titleStyle.SetFont(titleFont);

            // 在工作表中创建标题行
            IRow titleRow = sheet.CreateRow(0);
            // 设置标题行高度
            titleRow.Height = 30 * 20; // 30点高度

            var firstRunLogs = JsonSerializer.Deserialize<List<RunLogsViewModel>>(sortedSelectedItems.First().RecordContent);
            if (firstRunLogs is not null)
            {
                for (int i = 0; i < firstRunLogs.Count; i++)
                {
                    ICell cell = titleRow.CreateCell(i);
                    string value = firstRunLogs[i].title;
                    cell.SetCellValue(value);
                    cell.CellStyle = titleStyle; // 应用标题样式（加粗居中）
                }
            }

            // 创建数据行
            for (int i = 0; i < sortedSelectedItems.Count; i++)
            {
                IRow row = sheet.CreateRow(i + 1);
                row.Height = 25 * 20; // 设置数据行高度

                var runLogs = JsonSerializer.Deserialize<List<RunLogsViewModel>>(sortedSelectedItems[i].RecordContent);
                if (runLogs is not null)
                {
                    for (int j = 0; j < runLogs.Count; j++)
                    {
                        ICell cell = row.CreateCell(j);
                        string value = runLogs[j].content;

                        if (double.TryParse(value, out double numericValue))
                        {
                            cell.SetCellValue(numericValue);
                        }
                        else
                        {
                            cell.SetCellType(CellType.String);
                            cell.SetCellValue(value);
                        }

                        cell.CellStyle = centerStyle; // 应用居中样式
                    }
                }
            }

            // 在自动调整列宽后，检查并设置最小宽度
            for (int i = 0; i < firstRunLogs?.Count; i++)
            {
                sheet.AutoSizeColumn(i);

                // 获取当前列宽
                double currentWidth = sheet.GetColumnWidth(i);

                // 设置最小列宽（例如：15个字符宽度）
                int minWidth = 20 * 256; // 15个字符
                if (currentWidth < minWidth)
                {
                    sheet.SetColumnWidth(i, minWidth);
                }
            }

            using FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            workBook.Write(fs);
            MaterialSnack($"已导出到: {filePath}", SnackType.SUCCESS);
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