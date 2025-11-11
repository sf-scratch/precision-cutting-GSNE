using ScottPlot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using 精密切割系统.Helpers;
using Colors = ScottPlot.Colors;

namespace 精密切割系统.View.Controls
{
    /// <summary>
    /// CustomPlot.xaml 的交互逻辑
    /// </summary>
    public partial class CustomPlot : UserControl
    {
        public double XMaxValue
        {
            get { return (double)GetValue(XMaxValueProperty); }
            set { SetValue(XMaxValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for XMaxValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty XMaxValueProperty =
            DependencyProperty.Register("XMaxValue", typeof(double), typeof(CustomPlot), new PropertyMetadata(300d));

        public double XInitialMaxValue
        {
            get { return (double)GetValue(XInitialMaxValueProperty); }
            set { SetValue(XInitialMaxValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for XInitialMaxValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty XInitialMaxValueProperty =
            DependencyProperty.Register("XInitialMaxValue", typeof(double), typeof(CustomPlot), new PropertyMetadata(150d));

        public bool ShowLine
        {
            get { return (bool)GetValue(ShowLineProperty); }
            set { SetValue(ShowLineProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowLine.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowLineProperty =
            DependencyProperty.Register("ShowLine", typeof(bool), typeof(CustomPlot), new PropertyMetadata(true));

        private readonly List<double> _dataY = new List<double>();
        private CancellationTokenSource? _cts = null;

        public CustomPlot()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            formsPlot1.Plot.Clear();
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            InitializePlot();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
        }

        private void InitializePlot()
        {
            double xInitialMax = XInitialMaxValue;
            double xMax = XMaxValue;
            bool showLine = ShowLine;
            Thread thread = new Thread(new ThreadStart(() => RefreshChart(xInitialMax, xMax, showLine)));
            thread.Start();
        }

        private void RefreshChart(double initXMax, double xMax, bool showLine)
        {
            ScottPlot.Plottables.Signal? signalPlot = null;
            ScottPlot.Plottables.HorizontalLine? hLine = null;
            ScottPlot.Plottables.HorizontalLine? hLine2 = null;
            CancellationToken token = _cts!.Token;
            double curMaxValue = initXMax;
            double xMaxValue = xMax;
            formsPlot1.Plot.Axes.Bottom.Min = 0;
            formsPlot1.Plot.Axes.Bottom.Max = curMaxValue;
            int dataInterval = 50;
            int refreshInterval = 1000;
            int currentInterval = 0;
            while (!token.IsCancellationRequested)
            {
                var value = PLCValue.SlightVibration;
                _dataY.Add(value);
                currentInterval += dataInterval;
                if (refreshInterval < currentInterval)
                {
                    currentInterval = 0;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (signalPlot is not null)
                            formsPlot1.Plot.Remove(signalPlot);
                        signalPlot = formsPlot1.Plot.Add.Signal(_dataY, 1, ScottPlot.Color.FromColor(System.Drawing.Color.Red));

                        if (showLine)
                        {
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
                        }
                        formsPlot1.Plot.Axes.AutoScaleY();
                        if (curMaxValue < _dataY.Count)
                        {
                            curMaxValue *= 3;
                            if (curMaxValue > xMaxValue)
                            {
                                curMaxValue = xMaxValue;
                                _dataY.RemoveAt(0);
                            }
                            formsPlot1.Plot.Axes.Bottom.Max = curMaxValue;
                        }
                        formsPlot1.Refresh();
                    });
                }
                Thread.Sleep(dataInterval);
            }
        }

        public void Clear()
        {
            _dataY.Clear();
            formsPlot1.Plot.Clear();
            formsPlot1.Refresh();
        }

        public void Dispose()
        {
            _cts?.Cancel();
        }
    }
}