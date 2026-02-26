using MaterialDesignThemes.Wpf;
using NPOI.SS.Formula.Functions;
using ScottPlot;
using ScottPlot.WPF;
using System;
using System.Collections.Generic;
using System.Globalization;
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
using 精密切割系统.Driver;
using 精密切割系统.Entities;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.View.common;
using 精密切割系统.View.Dialogs;
using 精密切割系统.View.page.right;
using 精密切割系统.ViewModel;
using static NPOI.HSSF.Util.HSSFColor;
using Colors = ScottPlot.Colors;

namespace 精密切割系统.View.Pages.F7_ElectricSpark
{
    /// <summary>
    /// WaveformDiagram.xaml 的交互逻辑
    /// </summary>
    public partial class WaveformDiagram : Page
    {
        private string _dateFormat = "yyyy-MM-dd HH:mm:ss";

        public WaveformDiagram()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            StartDateTextBlock.Text = DateTime.Today.ToString(_dateFormat);
            EndDateTextBlock.Text = DateTime.Now.ToString(_dateFormat);
            NavigateUtils.ClearOperatePage();
            WindowLayout.RightPageButtons.Clear();
            WindowLayout.RightPageButtons.Add(ButtonParams.GreenRightButton("清除", "/Assets/icon/tab_1/02/tab_27.png", Clear));
            WindowLayout.RightPageButtons.Add(ButtonParams.Back(Back));
            WindowLayout.OperatePageButtons.Clear();
            WindowLayout.OperatePageButtons.Add(ButtonParams.BlueButton("实时震动", "Vibrate", RealTimeVibration));
            List<TemperatureSensorEntity> temperatureSensors = await SqlHelper.TableAsync<TemperatureSensorEntity>().ToListAsync();
            foreach (var sensor in temperatureSensors)
            {
                WindowLayout.OperatePageButtons.Add(ButtonParams.BlueButton(sensor.SensorName, "TemperatureCelsius", () => Temperature(sensor.Id)));
            }
        }

        private void RealTimeVibration()
        {
            customPlot.Visibility = Visibility.Visible;
            wpfPlot.Visibility = Visibility.Collapsed;
        }

        private async Task Temperature(long sensorId)
        {
            customPlot.Visibility = Visibility.Collapsed;
            wpfPlot.Visibility = Visibility.Visible;
            DateTime startTime = DateTime.ParseExact(StartDateTextBlock.Text, _dateFormat, CultureInfo.InvariantCulture);
            DateTime endTime = DateTime.ParseExact(EndDateTextBlock.Text, _dateFormat, CultureInfo.InvariantCulture);
            var temperatureLogs = await SqlHelper.TableAsync<TemperatureLogEntity>()
                .Where(log => log.SensorId == sensorId && log.CreatedAt >= startTime && log.CreatedAt < endTime)
                .OrderBy(log => log.CreatedAt)
                .ToListAsync();

            List<DateTime> times = temperatureLogs.Select(log => log.CreatedAt).ToList();
            List<double> temperatures = temperatureLogs.Select(log => (double)log.Temperature).ToList();
            wpfPlot.Plot.Title("(Temperature Variation Chart)");
            wpfPlot.Plot.Add.Scatter(times.ToArray(), temperatures.ToArray());
            wpfPlot.Plot.Axes.DateTimeTicksBottom();
            wpfPlot.Refresh();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            customPlot.Dispose();
            WindowLayout.RightPageButtons.Clear();
            WindowLayout.OperatePageButtons.Clear();
        }

        private void Clear()
        {
            customPlot.Clear();
            wpfPlot.Plot.Clear();
            wpfPlot.Refresh();
        }

        private void Back()
        {
            MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.NavigateToPage("MainMenu");
        }

        private async void StartTimeButton_Click(object sender, RoutedEventArgs e)
        {
            SelectDateTimeDialog dateTimeDialog = new SelectDateTimeDialog();
            dateTimeDialog.SelectedDate = DateTime.ParseExact(StartDateTextBlock.Text, _dateFormat, CultureInfo.InvariantCulture);
            var res = await DialogHost.Show(dateTimeDialog);
            if (res is DateTime dateTime && dateTime != default)
            {
                StartDateTextBlock.Text = dateTime.ToString(_dateFormat);
                Clear();
            }
        }

        private async void EndTimeButton_Click(object sender, RoutedEventArgs e)
        {
            SelectDateTimeDialog dateTimeDialog = new SelectDateTimeDialog();
            dateTimeDialog.SelectedDate = DateTime.ParseExact(EndDateTextBlock.Text, _dateFormat, CultureInfo.InvariantCulture);
            var res = await DialogHost.Show(dateTimeDialog);
            if (res is DateTime dateTime && dateTime != default)
            {
                EndDateTextBlock.Text = dateTime.ToString(_dateFormat);
                Clear();
            }
        }
    }
}