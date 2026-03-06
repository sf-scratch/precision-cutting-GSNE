using Emgu.CV.Dnn;
using MaterialDesignThemes.Wpf;
using NPOI.SS.Formula.Functions;
using Prism.Events;
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
using 精密切割系统.Entities;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.common;
using 精密切割系统.View.Controls;
using 精密切割系统.View.Dialogs;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

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

        private async void Page_Loaded(object sender, RoutedEventArgs e)
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
            WindowLayout.OperatePageButtons.Clear();
            WindowLayout.OperatePageButtons.Add(ButtonParams.BlueButton("换工件", "Square", ReplaceWaferAsync));
            await InitDataAsync();
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
            await InitDataAsync();
        }

        private async Task ReplaceWaferAsync()
        {
            await using var timeoutToken = TaskUtils.GetTimeoutCancellationToken(TimeSpan.FromSeconds(60), _cts.Token);
            await AutoCutUtils.ReplaceWaferAsync(default, timeoutToken.Token);
        }

        private async Task InitDataAsync()
        {
            bladeOuterDiameter.Text = Appsettings.BladeOuterDiameter?.ToString("F3");
            bladeThickness.Text = Appsettings.BladeThickness?.ToString("F3");
            afterReplaceBladeCutTimes.Text = Appsettings.AfterReplaceBladeCutTimes?.ToString();
            afterReplaceBladeCutLength.Text = (Appsettings.AfterReplaceBladeCutLength / 1000 ?? 0).ToString("F2");
            afterMeasureHeightCutTimes.Text = Appsettings.AfterMeasureHeightCutTimes?.ToString();
            afterMeasureHeightCutLength.Text = (Appsettings.AfterMeasureHeightCutLength / 1000 ?? 0).ToString("F2");
            afterClearDataCutTimes.Text = Appsettings.AfterClearDataCutTimes?.ToString();
            afterClearDataCutLength.Text = (Appsettings.AfterClearDataCutLength / 1000 ?? 0).ToString("F2");
            measureHeightFirst.Text = Appsettings.MeasureHeightFirst?.ToString("F3");
            measureHeightLast.Text = Appsettings.MeasureHeightLast?.ToString("F3");
            var bladeInfo = await SqlHelper.GetOrCreateEntityAsync(() => new BladeInfoEntity());
            toolHolderOuterDiameter.Text = bladeInfo.ToolHolderOuterDiameter;
        }

        private async Task SaveDataAsync()
        {
            Appsettings.BladeOuterDiameter = bladeOuterDiameter.Text.ToFloat();
            Appsettings.BladeThickness = bladeThickness.Text.ToFloat();
            var bladeInfo = await SqlHelper.GetOrCreateEntityAsync(() => new BladeInfoEntity());
            bladeInfo.ToolHolderOuterDiameter = toolHolderOuterDiameter.Text;
            await SqlHelper.UpdateAsync(bladeInfo);
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            _mainWindow?.NavigateToPage("MainMenu");
        }

        private async void BladeReplaceSure(object? sender, bool e)
        {
            if (_mainWindow == null)
            {
                MaterialSnack($"{nameof(_mainWindow)}为空", SnackType.WARNING);
                return;
            }
            if (!GlobalParams.OnlineFlag)
            {
                MaterialSnack("准备更换刀片,轴运动中！", SnackType.WARNING, 0);
                _mainWindow.IsEnabled = false;
                await Task.Delay(500);
                _mainWindow.IsEnabled = true;
                MaterialSnack("请打开切割安全门，更换刀片！", SnackType.SUCCESS, default);
                return;
            }
            var operateParams = await CurrentUtils.GetOperationParametersModelAsync();
            if (GlobalParams.DeviceModel == GlobalParams.Device_321 && operateParams.IsStartPreCuttingAfterChangeBlade)
            {
                SemiAutoCutService.Instance.IsOpenPrecut = true;
                MaterialSnack("换刀成功，预切割已开启！", SnackType.SUCCESS, 3);
            }
            else
            {
                MaterialSnack("换刀成功！", SnackType.SUCCESS);
            }
        }
    }
}