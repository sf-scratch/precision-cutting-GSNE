using NPOI.SS.Formula.Functions;
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
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.common;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.F4_BladeMaintenance
{
    /// <summary>
    /// FocusMaintenanceConf.xaml 的交互逻辑
    /// </summary>
    public partial class FocusMaintenanceConf : Page
    {
        private MainWindow? _mainWindow;
        private RightPage? _rightPage;
        private CancellationTokenSource? _cts;

        public FocusMaintenanceConf()
        {
            InitializeComponent();
            _mainWindow = Application.Current.MainWindow as MainWindow;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _cts = new CancellationTokenSource();
            if (_mainWindow == null) return;
            _rightPage = _mainWindow.rightFrame.Content as RightPage;
            if (_rightPage == null) return;
            _rightPage.PanelAction.Visibility = Visibility.Visible;
            _rightPage.btnBack.Visibility = Visibility.Visible;
            _rightPage.btnBack.BackFlag = false;
            _rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            _rightPage.btnSure.Visibility = Visibility.Visible;
            _rightPage.btnSure.BackFlag = false;
            _rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked);

            NavigateUtils.ClearOperatePage();
            WindowLayout.OperatePageButtons.Clear();
            WindowLayout.OperatePageButtons.Add(ButtonParams.BlueButton("实行测量", "FocusAuto", GlobalFocusAsync));
            InitData();
            LoadPosition(_cts.Token);
            // 打开镜头盖
            await PlcControl.tagControl.wholeDevice.OpenCameraLensCapAsync();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            WindowLayout.OperatePageButtons.Clear();
        }

        private void InitData()
        {
            focusSetPostion.Text = Appsettings.FocusWorkpiecesClearZ?.ToString(GlobalParams.DecimalStringFormat);
        }

        private void LoadPosition(CancellationToken token)
        {
            _ = Task.Run(async () =>
            {
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(300));
                while (await timer.WaitForNextTickAsync(token))
                {
                    float? currentZ2 = await PlcControl.tagControl.Z2axis.GetCurrentLocationAsync();
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        // 显示实时位置
                        z2AbsolutePosition.Text = currentZ2?.ToString(GlobalParams.DecimalStringFormat);
                    });
                }
            });
        }

        private async Task GlobalFocusAsync()
        {
            MaterialSnack("对焦进行中！", SnackType.WARNING);
            var result = await AutoFocusService.GlobalZeroPointFocusAsync(default, _cts?.Token ?? default);
            if (result.IsSuccess)
            {
                MaterialSnack("对焦完成！", SnackType.SUCCESS, 2);
            }
            else
            {
                MaterialSnack(result.Message, SnackType.WARNING);
            }
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            _cts?.Cancel();
            _mainWindow?.NavigateToPage("MainMenu");
        }

        private async void BtnSure_RightClicked(object? sender, bool e)
        {
            float? currentZ2 = await PlcControl.tagControl.Z2axis.GetCurrentLocationAsync();
            if (currentZ2 == null)
            {
                MaterialSnack("获取当前位置失败，请检测PLC连接状态！", SnackType.SUCCESS);
            }
            else
            {
                focusSetPostion.Text = currentZ2.Value.ToString(GlobalParams.DecimalStringFormat);
                Appsettings.FocusWorkpiecesClearZ = currentZ2.Value;
                Appsettings.FocusClearZ = null;
                MaterialSnack("对焦点位置确认成功！", SnackType.SUCCESS);
            }
        }
    }
}