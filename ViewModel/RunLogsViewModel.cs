using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.Helpers;
using 精密切割系统.Model.sqlite;
using 精密切割系统.View.Controls;
using 精密切割系统.View.Dialogs;

namespace 精密切割系统.ViewModel
{
    internal class RunLogsViewModel : BindableBase
    {
        private DelegateCommand _openDialogCommand;

        public DelegateCommand OpenDialogCommand =>
            _openDialogCommand ?? (_openDialogCommand = new DelegateCommand(ExecuteOpenDialogCommand));

        private void ExecuteOpenDialogCommand()
        {
            if (title != "震动幅度") return;
            var dataY = _content.Split(" ").Select(p => p.ToFloat()).ToList();
            PlotWindow plotWindow = new PlotWindow();
            plotWindow.formsPlot1.Plot.Axes.Bottom.Min = dataY.Min();
            plotWindow.formsPlot1.Plot.Axes.Bottom.Max = dataY.Max();
            plotWindow.formsPlot1.Plot.Add.Signal(dataY, 1, ScottPlot.Color.FromColor(System.Drawing.Color.Red));
            plotWindow.formsPlot1.Plot.Axes.AutoScale();
            plotWindow.formsPlot1.Refresh();
            plotWindow.ShowDialog();
        }

        private string _title;

        public string title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private string _content;

        public string content
        {
            get { return _content; }
            set { SetProperty(ref _content, value); }
        }

        public RunLogsViewModel()
        {
        }

        public RunLogsViewModel(string titleParams, string contentParams)
        {
            this.title = titleParams;
            this.content = contentParams;
        }
    }
}