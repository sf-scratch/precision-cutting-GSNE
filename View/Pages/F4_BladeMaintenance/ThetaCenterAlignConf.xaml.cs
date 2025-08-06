using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using Osklib.Interop;
using Prism.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
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
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.Extensions;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.cut.Workpieces;
using 精密切割系统.Model.MeasureHeight;
using 精密切割系统.Model.plc;
using 精密切割系统.Model.sqlite;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.F4_BladeMaintenance
{
    /// <summary>
    /// ThetaCenterAlignConf.xaml 的交互逻辑
    /// </summary>
    public partial class ThetaCenterAlignConf : Page
    {
        private const float FirstCutThetaDeg = 0;
        private const float SecondCutThetaDeg = FirstCutThetaDeg + 10;
        private const float ThirdCutThetaDeg = SecondCutThetaDeg + 20;
        private MainWindow _mainWindow;
        private RightPage _rightPage;
        private OperatePage _operatePage;
        private ThetaCenterAlignConfViewModel _viewModel;
        private CancellationTokenSource? _stopCts;
        private CancellationTokenSource? _monitorCts;
        private ThetaCenterAlignStep _step;
        private PointF? _firstIntersection;
        private float _startX;
        private float _endX;
        private float _startY;

        public ThetaCenterAlignConf()
        {
            InitializeComponent();
            _stopCts = new CancellationTokenSource();
            _mainWindow = Application.Current.MainWindow as MainWindow ?? new MainWindow();
        }

        // 当前状态，0 参数设置 1 切割中 2 切割完成，确认中
        int status = 0;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _rightPage = _mainWindow.rightFrame.Content as RightPage ?? new RightPage();
            _operatePage = _mainWindow.operateFrame.Content as OperatePage ?? new OperatePage();
            _rightPage.PanelAction.Visibility = Visibility.Visible;
            _rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            _rightPage.btnBack.Visibility = Visibility.Visible;
            _rightPage.btnBack.BackFlag = false;
            _rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked);
            _rightPage.btnSave.SetRightClickedHandler(BtnSave_RightClicked);
            _rightPage.btnSave.Visibility = Visibility.Visible;
            _rightPage.btnCutStart.SetRightClickedHandler(BtnCutStart_RightClicked);
            _rightPage.btnCutStart.Visibility = Visibility.Visible;
            _mainWindow.UpdateOperatePage(OperateData.GetThetaCenterAlignConfOperate(), OperateClickHandler);
            var list = SqlHelper.Table<ThetaCenterAlignModel>().Where(t => t.Id == 1).ToList();
            ThetaCenterAlignModel model = list.Count > 0 ? list[0] : new ThetaCenterAlignModel();
            _viewModel = MapperConfig.Mapper.Map<ThetaCenterAlignConfViewModel>(model);
            DataContext = _viewModel;
        }

        private void Stop()
        {
            _stopCts?.Cancel();
            _monitorCts?.Cancel();
        }

        private async void BtnCutStart_RightClicked(object? sender, bool e)
        {
            try
            {
                _startX = await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync() ?? 0;
                _endX = _startX + _viewModel.WorkSize.ToFloat();
                _startY = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync() ?? 0;
                await RunCutLineByThetaDegAsync([FirstCutThetaDeg, SecondCutThetaDeg]);
                thetaCenterParamsGrid.IsEnabled = false;
                _rightPage.btnCutStart.Visibility = Visibility.Collapsed;
                _rightPage.btnSure.Visibility = Visibility.Visible;
                _step = ThetaCenterAlignStep.FindFirstIntersection;
            }
            catch (OperationCanceledException) { }
        }

        private async void BtnSure_RightClicked(object? sender, bool e)
        {
            float x = await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync() ?? 0 - Appsettings.CameraRelativeBladePosition.X;
            float y = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync() ?? 0 - Appsettings.CameraRelativeBladePosition.Y;
            switch ( _step)
            {
                case ThetaCenterAlignStep.FindFirstIntersection:
                    try
                    {
                        _firstIntersection = new PointF(x, y);
                        await RunCutLineByThetaDegAsync([ThirdCutThetaDeg]);
                        _step = ThetaCenterAlignStep.FindSecondIntersection;
                    }
                    catch (OperationCanceledException) { }
                    break;
                case ThetaCenterAlignStep.FindSecondIntersection:
                    if (_firstIntersection is null)
                    {
                        MaterialSnackUtils.MaterialSnack("未确认第一交点！", MaterialSnackUtils.SnackType.WARNING, 0);
                        return;
                    }
                    PointF firstPoint = _firstIntersection.Value;
                    if (x.NearlyEquals(firstPoint.X) && y.NearlyEquals(firstPoint.Y))
                    {
                        MaterialSnackUtils.MaterialSnack("与第一交点相同，请找到第二交点！", MaterialSnackUtils.SnackType.WARNING, 0);
                        return;
                    }
                    var (a1, a2, a3) = ThetaRotationCenterCalculator.GetLineCoefficients(SecondCutThetaDeg, firstPoint.X, firstPoint.Y);
                    List<PointD> firstPoints = ThetaRotationCenterCalculator.FindRotationCenter(0, 1, firstPoint.Y, a1, a2, a3, SecondCutThetaDeg);
                    var (b1, b2, b3) = ThetaRotationCenterCalculator.GetLineCoefficients(ThirdCutThetaDeg, x, y);
                    List<PointD> secondPoints = ThetaRotationCenterCalculator.FindRotationCenter(0, 1, firstPoint.Y, b1, b2, b3, ThirdCutThetaDeg);
                    List<PointD> centers = [.. firstPoints.Intersect(secondPoints)];
                    if (centers.Count == 1)
                    {
                        MaterialSnackUtils.MaterialSnack($"Theta旋转中心点  X: {centers.First().X:F3}  Y: {centers.First().Y:F3}", MaterialSnackUtils.SnackType.WARNING, 0);
                    }
                    _step = ThetaCenterAlignStep.Completed;
                    thetaCenterParamsGrid.IsEnabled = true;
                    break;
                default:
                    NotifyOperation();
                    break;
            }
        }

        private void NotifyOperation()
        {
            MaterialSnackUtils.MaterialSnack(_step.GetEnumDescription(), MaterialSnackUtils.SnackType.WARNING, 0);
        }

        private async Task RunCutLineByThetaDegAsync(List<float> thetaDegs)
        {
            if (_monitorCts is null || _monitorCts.IsCancellationRequested)
            {
                _monitorCts = new CancellationTokenSource();
                _ = AutoCutUtils.MonitoringAlarmAsync(Stop, default, default, _monitorCts.Token);
            }
            await PlcControl.tagControl.bladeMantance.SetSetupParamsAsync(CurrentUtils.GetBladeHeightModel());
            await PlcControl.tagControl.bladeMantance.SetZAxisMaxDistanceAsync(AutoCutUtils.CaculateZAxisMaxDistance(56f));
            CommonResult<float> curHeightResult = await AutoCutUtils.ProcessMeasureHeightAsync(HeightMeasurementMode.Contact, default, default, _stopCts.Token);
            // 开始测高
            if (!curHeightResult.IsSuccess)
            {
                MaterialSnackUtils.MaterialSnack(curHeightResult.Message, MaterialSnackUtils.SnackType.WARNING, 0);
                return;
            }
            //打开切割水
            await PlcControl.tagControl.wholeDevice.OpenCuttingWaterAsync();
            //进入全自动切割模式
            await PlcControl.tagControl.cutting.EnterCuttingModeAsync(_stopCts.Token);
            float endZ = curHeightResult.Data - _viewModel.BladeHeight.ToFloat();
            float startZ = curHeightResult.Data - _viewModel.WorkThickness.ToFloat() - _viewModel.TapeThickness.ToFloat() - GlobalParams.BladeLiftingHeight;
            try
            {
                foreach (float thetaDeg in thetaDegs)
                {
                    //当前切割次数
                    int? curCutNum = await PlcControl.tagControl.cutting.GetCutNumAsync();
                    if (curCutNum == null)
                    {
                        MaterialSnackUtils.MaterialSnack("获取当前切割次数失败！", MaterialSnackUtils.SnackType.WARNING, 0);
                        return;
                    }
                    await PlcControl.tagControl.ThetaAxis.SetAbsoluteSpeedAsync(GlobalParams.ThetaDefaultSpeed);
                    //设置切割参数
                    await PlcControl.tagControl.cutting.SetCutParamsAsync(_viewModel.CutSpeed.ToFloat(), endZ, startZ, _startX, _startX + _viewModel.WorkSize.ToFloat(), _startY, "0", thetaDeg, _viewModel.SpindleSpeed.ToInt());
                    //开始切割信号
                    await PlcControl.tagControl.cutting.StartCutAsync();
                    //等待切割次数变化
                    await PlcControl.tagControl.cutting.WaitCutNumUdatedAsync(curCutNum.Value + 1, _stopCts.Token);
                }
            }
            finally
            {
                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(_stopCts.Token);
                await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
                // 工作盘吹气
                await AutoCutUtils.WorkpieceBlowingAsync(default, _stopCts.Token);
                await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(Appsettings.CameraRelativeBladePosition.X + (_startX + _endX) / 2);
                await PlcControl.tagControl.Yaxis.StartRelativeAsync(Appsettings.CameraRelativeBladePosition.Y);
            }
        }

        private void BtnSave_RightClicked(object? sender, bool e)
        {
            Keyboard.ClearFocus();
            ThetaCenterAlignModel model = MapperConfig.Mapper.Map<ThetaCenterAlignModel>(_viewModel);
            if (model.Id != 1)
            {
                SqlHelper.Add(model);
            }
            else
            {
                SqlHelper.Update(model);
            }
            MaterialSnackUtils.MaterialSnack("保存成功！", MaterialSnackUtils.SnackType.SUCCESS);
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            Stop();
            _mainWindow.NavigateToPage("MainMenu");
        }

        private void OperateClickHandler(object? sender, int code)
        {
            switch (code)
            {
                case 44002:
                    CommonOperate.GetInstance().AutoFocus(2, _mainWindow);
                    break;
                default:
                    break;
            }
            DisposeStatus();
        }

        private void DisposeStatus()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                switch (status)
                {
                    case 0:
                        thetaCenterParamsGrid.Visibility = Visibility.Visible;
                        dimmingGrid.Visibility = Visibility.Collapsed;
                        directionGrid.Visibility = Visibility.Collapsed;
                        centerPanel.Visibility = Visibility.Collapsed;
                        break;
                    case 1:
                        thetaCenterParamsGrid.Visibility = Visibility.Visible;
                        dimmingGrid.Visibility = Visibility.Collapsed;
                        directionGrid.Visibility = Visibility.Collapsed;
                        break;
                    case 2:
                        thetaCenterParamsGrid.Visibility = Visibility.Collapsed;
                        centerPanel.Visibility = Visibility.Visible;
                        dimmingGrid.Visibility = Visibility.Visible;
                        directionGrid.Visibility = Visibility.Visible;
                        break;
                    default:
                        break;
                }
            });
        }

        private enum ThetaCenterAlignStep
        {
            [Description("请开始第一次切割")]
            None,
            [Description("请确认第一个交点")]
            FindFirstIntersection,
            [Description("请确认第二个交点")]
            FindSecondIntersection,
            Completed
        }
    }
}
