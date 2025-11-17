using Emgu.CV.Dnn;
using NPOI.HSSF.UserModel;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Documents;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.Model.sqlite;
using 精密切割系统.Utils;
using 精密切割系统.View.Dialogs;
using 精密切割系统.ViewModel;
using static Emgu.CV.Dai.OpenVino;
using static Emgu.CV.FaceRecognizerSF;
using static 精密切割系统.View.F7_ElectricSpark.ESAxisDataConf;

namespace 精密切割系统.Helpers
{
    public class CompensationVerificationUtils
    {
        public static void ShowDialog()
        {
            List<double> compensation1X = new List<double>();
            List<double> compensation1Y = new List<double>();
            List<double> compensation2X = new List<double>();
            List<double> compensation2Y = new List<double>();

            // 获取补偿数据模型
            List<PositionCompensationModel> models = CurrentUtils.GetPositionCompensationModels();
            if (models == null || models.Count == 0) { return; }
            PositionCompensationModel? axisModel = models.Find(p => p.AxisType.Equals("Y轴"));
            if (axisModel == null) { return; }
            float[] positionNumbers = axisModel.AxisPosition.Split(",").Select(float.Parse).ToArray();
            float[] compensateNumbers = axisModel.AxisCompensate.Split(",").Select(float.Parse).ToArray();
            if (positionNumbers.Length != compensateNumbers.Length) { return; }
            for (int i = 0; i < positionNumbers.Length; i++)
            {
                compensation1X.Add(positionNumbers[i]);
                compensation1Y.Add(compensateNumbers[i] - positionNumbers[i]);
            }
            compensation1X.Reverse();
            compensation1Y.Reverse();

            List<RunLogsModel> logsModels = SqlHelper.Table<RunLogsModel>().ToList();
            logsModels = logsModels.Where(p => p.Id >= 3319 && p.Id <= 3612).ToList();
            List<List<RunLogsViewModel>> logList = new List<List<RunLogsViewModel>>();
            foreach (RunLogsModel logModel in logsModels)
            {
                List<RunLogsViewModel>? logsList = JsonSerializer.Deserialize<List<RunLogsViewModel>>(logModel.RecordContent);
                if (logsList == null || logsList.Count == 0) { continue; }
                logList.Add(logsList);
            }
            logList.Reverse();
            var result = logList.Select(p =>
            {
                double y = 0, actualY = 0;
                var runLogsModel = p.Where(q => q.title == "Y轴切割位置").FirstOrDefault();
                if (runLogsModel is not null)
                {
                    y = runLogsModel.content.ToFloat();
                }
                var runLogsModel2 = p.Where(q => q.title == "Y轴实际切割位置").FirstOrDefault();
                if (runLogsModel2 is not null)
                {
                    actualY = runLogsModel2.content.ToFloat();
                }
                return (x: y, y: actualY - y);
            });
            foreach (var xy in result)
            {
                compensation2X.Add(xy.x);
                compensation2Y.Add(xy.y);
            }
            PlotWindow plotWindow = new PlotWindow();
            plotWindow.formsPlot1.Plot.Add.SignalXY(compensation1X.ToArray(), compensation1Y.ToArray(), ScottPlot.Color.FromColor(System.Drawing.Color.Red));
            plotWindow.formsPlot1.Plot.Add.SignalXY(compensation2X.ToArray(), compensation2Y.ToArray(), ScottPlot.Color.FromColor(System.Drawing.Color.Blue));
            plotWindow.formsPlot1.Plot.Axes.AutoScale();
            plotWindow.formsPlot1.Refresh();
            plotWindow.ShowDialog();
        }

        public static void ExportToExcel()
        {
            string parentPath = System.Environment.CurrentDirectory + "\\excelData\\EsAxisData\\";
            DirectoryPathExists(parentPath);
            DateTime now = DateTime.Now;
            // 标准日期和时间格式化字符串
            string format1 = now.ToString("yyyy_MM_dd_HHmmss"); // 24小时制
            string fileName = "erExcel" + format1 + ".xlsx";
            string filePath = parentPath + fileName;

            List<RunLogsModel> logsModels = SqlHelper.Table<RunLogsModel>().ToList();
            logsModels = logsModels.Where(p => p.Id >= 3613 && p.Id <= 3937).ToList();
            List<List<RunLogsViewModel>> logList = new List<List<RunLogsViewModel>>();
            foreach (RunLogsModel logModel in logsModels)
            {
                List<RunLogsViewModel>? logsList = JsonSerializer.Deserialize<List<RunLogsViewModel>>(logModel.RecordContent);
                if (logsList == null || logsList.Count == 0) { continue; }
                logList.Add(logsList);
            }

            //【1】.创建工作簿 2007之前用HSSFWorkbook 2007之后用XSSFWorkbook
            IWorkbook? workBook = null;
            // 根据版本号创建不同版本的excel
            workBook = new HSSFWorkbook();
            // 每个工作簿有多个sheet，创建一个sheet
            ISheet sheet = workBook.CreateSheet("sheet1");
            //宽度： SetColumnWidth方法里的第二个参数要乘以256，因为这个参数的单位是1 / 256个字符宽度，所以要乘以256才是一整个字符宽度。
            //高度： .Height 属性后面的值的单位是：1 / 20个点，所以要想得到一个点的话，需要乘以20。
            //sheet.DefaultColumnWidth = 5 * 256 * 256;
            //sheet.DefaultRowHeight = 30 * 20;

            // 在工作表中创建标题行
            IRow titleRow = sheet.CreateRow(0);
            // 放入属性对应的中文名称
            List<RunLogsViewModel> firstRunLogs = logList.First();
            for (int i = 0; i < firstRunLogs.Count; i++)
            {
                ICell cell = titleRow.CreateCell(i);
                string value = firstRunLogs[i].title;
                cell.SetCellValue(value);
            }
            // 创建数据行
            for (int i = 0; i < logList.Count; i++)
            {
                IRow row = sheet.CreateRow(i + 1);

                for (int j = 0; j < logList[i].Count; j++)
                {
                    ICell cell = row.CreateCell(j);
                    if (cell != null)
                    {
                        object? temp = logList[i][j].content;
                        string? data = temp == null ? "" : temp.ToString();
                        cell.SetCellValue(data);
                    }
                }
            }
            for (int i = 0; i < firstRunLogs.Count; i++)
            {
                sheet.SetColumnWidth(i, 25 * 256);//
            }
            using (FileStream fs = File.OpenWrite(filePath))
            {
                workBook.Write(fs);
            }
        }
    }
}