using Emgu.CV.Dnn;
using MaterialDesignThemes.Wpf;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
using 精密切割系统.database.db.modle;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.common;
using 精密切割系统.View.Controls;
using 精密切割系统.View.Dialogs;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;
using static 精密切割系统.Helpers.MaterialSnackUtils;

namespace 精密切割系统.View.Pages.F4_BladeMaintenance
{
    /// <summary>
    /// BMBladeReplacementConf.xaml 的交互逻辑
    /// </summary>
    public partial class BMBladeReplacementConf : Page
    {
        private MainWindow? _mainWindow;
        private RightPage? _rightPage;
        private CancellationTokenSource _cts;

        public BMBladeReplacementConf()
        {
            InitializeComponent();
            _mainWindow = Application.Current.MainWindow as MainWindow;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_mainWindow is null) return;
            _rightPage = _mainWindow.rightFrame.Content as RightPage;
            if (_rightPage is null) return;
            _rightPage.PanelAction.Visibility = Visibility.Visible;
            _rightPage.btnBack.Visibility = Visibility.Visible;
            _rightPage.btnBack.BackFlag = false;
            _rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            _rightPage.btnSure.Visibility = Visibility.Visible;
            _rightPage.btnSure.SetRightClickedHandler(BladeReplaceSure);
            _cts = new CancellationTokenSource();
            NavigateUtils.ClearOperatePage();
            WindowLayout.OperatePageButtons.Add(RightButtonParams.BlueButton("换刀片", "SawBlade", ReplaceBladeAsync));
            WindowLayout.OperatePageButtons.Add(RightButtonParams.BlueButton("换工件", "Square", ReplaceWaferAsync));
            InitData();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
            _cts.Dispose();
            WindowLayout.OperatePageButtons.Clear();
        }

        private async Task ReplaceBladeAsync()
        {
            await using var timeoutToken = TaskUtils.GetTimeoutCancellationToken(TimeSpan.FromSeconds(60), _cts.Token);
            await AutoCutUtils.ReplaceBladeAsync(default, timeoutToken.Token); 
            InitData();
        }

        private async Task ReplaceWaferAsync()
        {
            await using var timeoutToken = TaskUtils.GetTimeoutCancellationToken(TimeSpan.FromSeconds(60), _cts.Token);
            await AutoCutUtils.ReplaceWaferAsync(default, timeoutToken.Token);
        }

        private void InitData()
        {
            bladeOuterDiameter.Text = Appsettings.BladeOuterDiameter?.ToString("F3");
            bladeThickness.Text = Appsettings.BladeThickness?.ToString("F3");
            afterReplaceBladeCutTimes.Text = Appsettings.AfterReplaceBladeCutTimes?.ToString();
            afterReplaceBladeCutLength.Text = (Appsettings.AfterReplaceBladeCutLength / 1000 ?? 0).ToString("F2");
            measureHeightFirst.Text = Appsettings.MeasureHeightFirst?.ToString("F3");
            measureHeightLast.Text = Appsettings.MeasureHeightLast?.ToString("F3");
        }

        private void SaveData()
        {
            Appsettings.BladeOuterDiameter = bladeOuterDiameter.Text.ToFloat();
            Appsettings.BladeThickness = bladeThickness.Text.ToFloat();
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            _mainWindow?.NavigateToPage("MainMenu");
        }

        private void BladeReplaceSure(object? sender, bool e)
        {
            try
            {
                SaveData();
                MaterialSnackUtils.MaterialSnack("换刀成功！", MaterialSnackUtils.SnackType.SUCCESS);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                MaterialSnackUtils.MaterialSnack("保存失败", MaterialSnackUtils.SnackType.ERROR);
                return;
            }
        }
    }
}