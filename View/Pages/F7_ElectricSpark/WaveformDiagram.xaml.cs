using ScottPlot;
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
using 精密切割系统.Driver;
using 精密切割系统.View.page.right;
using 精密切割系统.ViewModel;
using Colors = ScottPlot.Colors;

namespace 精密切割系统.View.Pages.F7_ElectricSpark
{
    /// <summary>
    /// WaveformDiagram.xaml 的交互逻辑
    /// </summary>
    public partial class WaveformDiagram : Page
    {
        private MainWindow? _mainWindow;
        private RightPage? _rightPage;
        private List<double> _dataY = new List<double>();
        private CancellationTokenSource? _cts = null;

        public WaveformDiagram()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _mainWindow = Application.Current.MainWindow as MainWindow;
            if (_mainWindow == null) return;
            _rightPage = _mainWindow.rightFrame.Content as RightPage;
            if (_rightPage == null) return;
            _rightPage.PanelAction.Visibility = Visibility.Visible;
            _rightPage.btnBack.Visibility = Visibility.Visible;
            _rightPage.btnBack.BackFlag = false;
            _rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            _rightPage.btnClear.Visibility = Visibility.Visible;
            _rightPage.btnClear.BackFlag = false;
            _rightPage.btnClear.SetRightClickedHandler(BtnClear_RightClicked);
            _cts = new CancellationTokenSource();
            InitializePlot();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
        }

        private void InitializePlot()
        {
            Thread thread = new Thread(new ThreadStart(RefreshChart));
            thread.Start();
        }

        private async void RefreshChart()
        {
            ScottPlot.Plottables.Signal? signalPlot = null;
            ScottPlot.Plottables.HorizontalLine? hLine = null;
            ScottPlot.Plottables.HorizontalLine? hLine2 = null;
            CancellationToken token = _cts!.Token;
            double maxValue = 2000;
            double targetMaxValue = 150;
            formsPlot1.Plot.Axes.Bottom.Min = 0;
            formsPlot1.Plot.Axes.Bottom.Max = targetMaxValue;
            while (!token.IsCancellationRequested)
            {
                var result = await PlcControl.plc.ReadDataAsync<short>("DM2000");
                if (result == null) continue;
                var value = result.Value;
                //double value = GetRandomNum(1).First();
                _dataY.Add(value);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (signalPlot is not null)
                        formsPlot1.Plot.Remove(signalPlot);
                    signalPlot = formsPlot1.Plot.Add.Signal(_dataY, 1, ScottPlot.Color.FromColor(System.Drawing.Color.Red));

                    if (hLine is not null) formsPlot1.Plot.Remove(hLine);
                    // 添加水平横线
                    hLine = formsPlot1.Plot.Add.HorizontalLine(_dataY.Min()); // Y=0.5的水平线
                    hLine.LineWidth = 2;
                    hLine.LinePattern = LinePattern.Dotted;
                    hLine.LineColor = Colors.Red;
                    hLine.Text = "Min: " + _dataY.Min();

                    if (hLine2 is not null) formsPlot1.Plot.Remove(hLine2);
                    // 添加水平横线
                    hLine2 = formsPlot1.Plot.Add.HorizontalLine(_dataY.Max()); // Y=0.5的水平线
                    hLine2.LineWidth = 2;
                    hLine2.LinePattern = LinePattern.Dotted;
                    hLine2.LineColor = Colors.Red;
                    hLine2.Text = "Max: " + _dataY.Max();
                    formsPlot1.Plot.Axes.AutoScaleY();
                    if (targetMaxValue < _dataY.Count)
                    {
                        targetMaxValue *= 3;
                        if (targetMaxValue > maxValue)
                        {
                            targetMaxValue = maxValue;
                            _dataY.RemoveAt(0);
                        }
                        formsPlot1.Plot.Axes.Bottom.Max = targetMaxValue;
                    }
                    formsPlot1.Refresh();
                });
                Thread.Sleep(50);
            }
        }

        private void BtnClear_RightClicked(object? sender, bool e)
        {
            _dataY.Clear();
            formsPlot1.Plot.Clear();
            formsPlot1.Refresh();
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            _mainWindow?.NavigateToPage("MainMenu");
        }

        public double[] GetRandomNum(int length)
        {
            double[] getDate = new double[length];
            Random random = new Random(); //创建一个Random实例
            for (int i = 0; i < length; i++)
            {
                getDate[i] = random.Next(1, 100); //使用同一个Random实例生成随机数
            }
            return getDate;
        }
    }
}