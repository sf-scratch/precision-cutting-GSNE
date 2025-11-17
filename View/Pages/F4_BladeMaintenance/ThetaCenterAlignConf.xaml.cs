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
using static 精密切割系统.Helpers.MaterialSnackUtils;

namespace 精密切割系统.View.Pages.F4_BladeMaintenance
{
    /// <summary>
    /// ThetaCenterAlignConf.xaml 的交互逻辑
    /// </summary>
    public partial class ThetaCenterAlignConf : Page
    {
        private const float FirstCutThetaDeg = 0;
        private const float SecondCutThetaDeg = 90;
        private const float ThirdCutThetaDeg = 180;
        private MainWindow _mainWindow;
        private RightPage _rightPage;
        private ThetaCenterAlignConfViewModel _viewModel;
        private CancellationTokenSource? _stopCts;
        private CancellationTokenSource? _monitorCts;
        private ThetaCenterAlignStep _step;
        private PointF? _firstIntersection;
        private PointF? _secondIntersection;
        private PointF? _thirdIntersection;
        private PointF? _rightPoint;
        private PointF? _leftPoint;
        private float _startX;
        private float _endX;
        private float _startY;
        private float _measureHeigthY;
        private SemaphoreSlim _thetaCenterAlignSemaphore;

        public ThetaCenterAlignConf()
        {
            InitializeComponent();
            _mainWindow = Application.Current.MainWindow as MainWindow ?? new MainWindow();
            var list = SqlHelper.Table<ThetaCenterAlignModel>().Where(t => t.Id == 1).ToList();
            ThetaCenterAlignModel model = list.Count > 0 ? list[0] : new ThetaCenterAlignModel();
            _viewModel = MapperConfig.Mapper.Map<ThetaCenterAlignConfViewModel>(model);
            DataContext = _viewModel;
            _thetaCenterAlignSemaphore = new SemaphoreSlim(1, 1);
        }

        // 当前状态，0 参数设置 1 切割中 2 切割完成，确认中
        private int status = 0;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _rightPage = _mainWindow.rightFrame.Content as RightPage ?? new RightPage();
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
        }

        private void Stop()
        {
            _stopCts?.Cancel();
            _monitorCts?.Cancel();
        }

        private async void BtnCutStart_RightClicked(object? sender, bool e)
        {
            if (Appsettings.BladeOuterDiameter is null)
            {
                MaterialSnack("未设置刀片外径！", SnackType.WARNING);
                return;
            }
            if (!await _thetaCenterAlignSemaphore.WaitAsync(TimeSpan.Zero))
            {
                MaterialSnackUtils.MaterialSnack("theta中心校准运行中，请勿重复点击！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            try
            {
                _stopCts = new CancellationTokenSource();
                _startX = await PlcControl.tagControl.Xaxis.GetCurrentLocationWaitAsync(_stopCts.Token) ?? 0;
                _endX = _startX + _viewModel.WorkSize.ToFloat();
                _startY = await PlcControl.tagControl.Yaxis.GetCurrentLocationWaitAsync(_stopCts.Token) ?? 0;
                CommonResult<float> curHeightResult = await AutoCutUtils.ProcessCombineMeasureHeightAsync(default, _stopCts.Token);
                // 开始测高
                if (!curHeightResult.IsSuccess)
                {
                    MaterialSnackUtils.MaterialSnack(curHeightResult.Message, MaterialSnackUtils.SnackType.WARNING, 0);
                    return;
                }
                _measureHeigthY = curHeightResult.Data;
                await RunCutLineByThetaDegAsync([FirstCutThetaDeg, SecondCutThetaDeg], _startY, _stopCts.Token);
                thetaCenterParamsGrid.IsEnabled = false;
                _rightPage.btnCutStart.Visibility = Visibility.Collapsed;
                _rightPage.btnSure.Visibility = Visibility.Visible;
                _step = ThetaCenterAlignStep.FindFirstIntersection;
                await PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(FirstCutThetaDeg, default, _stopCts.Token);
                NotifyOperation();
            }
            catch (OperationCanceledException) { }
            finally
            {
                _thetaCenterAlignSemaphore.Release();
            }
        }

        private async void BtnSure_RightClicked(object? sender, bool e)
        {
            if (!await _thetaCenterAlignSemaphore.WaitAsync(TimeSpan.Zero))
            {
                MaterialSnackUtils.MaterialSnack("theta中心校准运行中，请勿重复点击！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            try
            {
                if (_stopCts is null || _stopCts.IsCancellationRequested)
                {
                    _stopCts = new CancellationTokenSource();
                }
                DataPoint<float> relativePostion = Appsettings.CameraRelativeBladePosition;
                float x = await PlcControl.tagControl.Xaxis.GetCurrentLocationAsync() ?? 0;
                float y = await PlcControl.tagControl.Yaxis.GetCurrentLocationAsync() ?? 0;
                switch (_step)
                {
                    case ThetaCenterAlignStep.FindFirstIntersection:
                        _firstIntersection = new PointF(x, y);
                        _step = ThetaCenterAlignStep.FindSecondIntersection;
                        Appsettings.CameraRelativeBladePosition = new DataPoint<float>(relativePostion.X, y - _startY);
                        await PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(SecondCutThetaDeg, default, _stopCts.Token);
                        break;

                    case ThetaCenterAlignStep.FindSecondIntersection:
                        if (_firstIntersection is null)
                        {
                            MaterialSnackUtils.MaterialSnack("未第一次确认交点！", MaterialSnackUtils.SnackType.WARNING, 0);
                            return;
                        }
                        _secondIntersection = new PointF(x, y);
                        _step = ThetaCenterAlignStep.FindThirdIntersection;
                        await PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(ThirdCutThetaDeg, default, _stopCts.Token);
                        break;

                    case ThetaCenterAlignStep.FindThirdIntersection:
                        if (_secondIntersection is null)
                        {
                            MaterialSnackUtils.MaterialSnack("未第二次确认交点！", MaterialSnackUtils.SnackType.WARNING, 0);
                            return;
                        }
                        _thirdIntersection = new PointF(x, y);
                        float distanceY = y - _secondIntersection.Value.Y;
                        await RunCutLineByThetaDegAsync([FirstCutThetaDeg, SecondCutThetaDeg], _startY + (distanceY / 2), _stopCts.Token);
                        _step = ThetaCenterAlignStep.FindCenterIntersection;
                        break;

                    case ThetaCenterAlignStep.FindCenterIntersection:
                        if (_thirdIntersection is null)
                        {
                            MaterialSnackUtils.MaterialSnack("未第三次确认交点！", MaterialSnackUtils.SnackType.WARNING, 0);
                            return;
                        }
                        Appsettings.CameraThetaCenterPoint = new DataPoint<float>(x, y);
                        await PlcControl.tagControl.cutting.RunMotionAsync(x.ToActualX(), y.ToActualY() + 10, _stopCts.Token);
                        float startY = _measureHeigthY - _viewModel.WorkThickness.ToFloat() - _viewModel.TapeThickness.ToFloat() - 0.2f;
                        await PlcControl.tagControl.Yaxis.StartAbsoluteAsync(startY, default, _stopCts.Token);
                        float endY = _measureHeigthY - _viewModel.WorkThickness.ToFloat() - _viewModel.TapeThickness.ToFloat() + 0.1f;
                        await PlcControl.tagControl.wholeDevice.OpenCuttingWaterAsync();
                        await PlcControl.tagControl.Yaxis.StartAbsoluteAsync(endY, 0.001f, _stopCts.Token);
                        await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
                        await AutoCutUtils.WorkpieceBlowingAsync(default, default, _stopCts.Token);
                        await PlcControl.tagControl.cutting.RunMotionAsync(x.ToCameraX(), y.ToCameraY(), _stopCts.Token);
                        _step = ThetaCenterAlignStep.FindRightEndpoint;
                        break;

                    case ThetaCenterAlignStep.FindRightEndpoint:
                        _rightPoint = new PointF(x, y);
                        _step = ThetaCenterAlignStep.FindLeftEndpoint;
                        break;

                    case ThetaCenterAlignStep.FindLeftEndpoint:
                        if (_rightPoint is null)
                        {
                            MaterialSnackUtils.MaterialSnack("未确认刀痕线段右侧端点！", MaterialSnackUtils.SnackType.WARNING, 0);
                            return;
                        }
                        _leftPoint = new PointF(x, y);
                        float relativeX = Appsettings.CameraThetaCenterPoint.X - ((_rightPoint.Value.X + _leftPoint.Value.X) / 2);
                        Appsettings.CameraRelativeBladePosition = new DataPoint<float>(relativeX, relativePostion.Y);
                        _step = ThetaCenterAlignStep.Completed;
                        break;

                    default:
                        break;
                }
                if (_step == ThetaCenterAlignStep.Completed)
                {
                    _step = ThetaCenterAlignStep.Completed;
                    thetaCenterParamsGrid.IsEnabled = true;
                }
                NotifyOperation();
            }
            finally
            {
                _thetaCenterAlignSemaphore.Release();
            }
        }

        private void NotifyOperation()
        {
            MaterialSnackUtils.MaterialSnack(_step.GetEnumDescription(), MaterialSnackUtils.SnackType.WARNING, 0);
        }

        private async Task RunCutLineByThetaDegAsync(List<float> thetaDegs, float startY, CancellationToken token)
        {
            if (_monitorCts is null || _monitorCts.IsCancellationRequested)
            {
                _monitorCts = new CancellationTokenSource();
                _ = AutoCutUtils.MonitoringAlarmAsync(Stop, AlarmConfig.Instance.HasAutoRunUnexpectedAlarms, default, _monitorCts.Token);
            }
            //打开切割水
            await PlcControl.tagControl.wholeDevice.OpenCuttingWaterAsync();
            //进入全自动切割模式
            await PlcControl.tagControl.cutting.EnterCuttingModeAsync(token);
            float endZ = _measureHeigthY - _viewModel.BladeHeight.ToFloat();
            float startZ = _measureHeigthY - _viewModel.WorkThickness.ToFloat() - _viewModel.TapeThickness.ToFloat() - GlobalParams.BladeLiftingHeight;
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
                    await PlcControl.tagControl.cutting.SetCutParamsAsync(_viewModel.CutSpeed.ToFloat(), endZ, startZ, _startX, _startX + _viewModel.WorkSize.ToFloat(), startY, "0", thetaDeg, _viewModel.SpindleSpeed.ToInt());
                    //开始切割信号
                    await PlcControl.tagControl.cutting.StartCutAsync();
                    //等待切割次数变化
                    await PlcControl.tagControl.cutting.WaitCutNumUdatedAsync(curCutNum.Value + 1, token);
                }
            }
            finally
            {
                await PlcControl.tagControl.cutting.ExitCuttingModeAsync(token);
                await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
                // 工作盘吹气
                await AutoCutUtils.WorkpieceBlowingAsync(default, default, token);
                await PlcControl.tagControl.cutting.RunMotionAsync(((_startX + _endX) / 2).ToCameraX(), startY.ToCameraY(), token);
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

        private async void OperateClickHandler(object? sender, int code)
        {
            switch (code)
            {
                case 44002:
                    await AutoCutUtils.AutoFocusAsync();
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
            [Description("请开始校准")]
            None,

            [Description("请完成第一次确认交点")]
            FindFirstIntersection,

            [Description("请完成第二次确认交点")]
            FindSecondIntersection,

            [Description("请完成第三次确认交点")]
            FindThirdIntersection,

            [Description("请确认中心交点")]
            FindCenterIntersection,

            [Description("请刀痕线段右侧端点")]
            FindRightEndpoint,

            [Description("请刀痕线段左侧端点")]
            FindLeftEndpoint,

            [Description("已完成校准！")]
            Completed
        }
    }
}