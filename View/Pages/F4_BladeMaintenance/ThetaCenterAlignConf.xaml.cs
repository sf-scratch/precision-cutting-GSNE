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
using 精密切割系统.Data;
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
        private CancellationTokenSource? _dataShowsCts;
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
        private SemaphoreSlim _thetaCenterAlignSemaphore = new SemaphoreSlim(1, 1);

        public ThetaCenterAlignConf()
        {
            InitializeComponent();
            _mainWindow = Application.Current.MainWindow as MainWindow ?? new MainWindow();
            var list = SqlHelper.Table<ThetaCenterAlignModel>().Where(t => t.Id == 1).ToList();
            ThetaCenterAlignModel model = list.Count > 0 ? list[0] : new ThetaCenterAlignModel();
            _viewModel = MapperConfig.Mapper.Map<ThetaCenterAlignConfViewModel>(model);
            DataContext = _viewModel;
            thetaCenterX.Text = Appsettings.CameraThetaCenterPoint.X.ToString();
            thetaCenterY.Text = Appsettings.CameraThetaCenterPoint.Y.ToString();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _dataShowsCts = new CancellationTokenSource();
            _rightPage = _mainWindow.rightFrame.Content as RightPage ?? new RightPage();
            _rightPage.PanelAction.Visibility = Visibility.Visible;
            _rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            _rightPage.btnBack.Visibility = Visibility.Visible;
            _rightPage.btnBack.BackFlag = false;
            _rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked);
            _rightPage.btnSure.Visibility = Visibility.Visible;
            //_rightPage.btnSave.SetRightClickedHandler(BtnSave_RightClicked);
            //_rightPage.btnSave.Visibility = Visibility.Visible;
            _rightPage.btnCutStart.SetRightClickedHandler(BtnCutStart_RightClicked);
            _rightPage.btnCutStart.Visibility = Visibility.Visible;
            _mainWindow.UpdateOperatePage(OperateData.GetThetaCenterAlignConfOperate(), OperateClickHandler);
            StartGetAxisInfo();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _dataShowsCts?.Cancel();
        }

        public void StartGetAxisInfo()
        {
            if (_dataShowsCts is null || _dataShowsCts.IsCancellationRequested)
            {
                _dataShowsCts = new CancellationTokenSource();
            }
            CancellationToken token = _dataShowsCts.Token;
            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var axisPostion = await AutoCutUtils.GetAxisPositionAsync();
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            currentX.Text = axisPostion.X?.ToString(GlobalParams.DecimalStringFormat);
                            currentY.Text = axisPostion.Y?.ToString(GlobalParams.DecimalStringFormat);
                        });
                    }
                    catch (Exception ex)
                    {
                        Tools.LogError($"StartGetAxisInfo()报警监控异常: {ex.Message}");
                    }
                    await Task.Delay(200);
                }
            });
        }

        private void Stop()
        {
            _stopCts?.Cancel();
            _monitorCts?.Cancel();
        }

        private async void BtnCutStart_RightClicked(object? sender, bool e)
        {
            if (AlarmConfig.Instance.HasActiveErrorAlarm())
            {
                MaterialSnack(AlarmConfig.HasErrorAlarmMessage, SnackType.WARNING);
                return;
            }
            if (Appsettings.BladeOuterDiameter is null)
            {
                MaterialSnack("未设置刀片外径！", SnackType.WARNING);
                return;
            }
            if (Appsettings.MeasureHeightLast is null)
            {
                MaterialSnack("刀具未测高，请先测高！", SnackType.WARNING);
                return;
            }
            if (!await _thetaCenterAlignSemaphore.WaitAsync(TimeSpan.Zero))
            {
                MaterialSnack("theta中心校准运行中，请勿重复点击！", SnackType.WARNING);
                return;
            }
            try
            {
                _stopCts = new CancellationTokenSource();
                _startX = await PlcControl.tagControl.Xaxis.GetCurrentLocationWaitAsync(_stopCts.Token) ?? 0;
                _endX = _startX + _viewModel.WorkSize.ToFloat();
                _startY = await PlcControl.tagControl.Yaxis.GetCurrentLocationWaitAsync(_stopCts.Token) ?? 0;
                _measureHeigthY = Appsettings.MeasureHeightLast.Value;
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
                MaterialSnack("theta中心校准运行中，请勿重复点击！", SnackType.WARNING);
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
                        MaterialSnack("第一次确认交点中...", SnackType.WARNING, 0);
                        _firstIntersection = new PointF(x, y);
                        Appsettings.CameraRelativeBladePosition = new DataPoint<float>(relativePostion.X, y - _startY);
                        await PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(SecondCutThetaDeg, default, _stopCts.Token);
                        _step = ThetaCenterAlignStep.FindSecondIntersection;
                        break;

                    case ThetaCenterAlignStep.FindSecondIntersection:
                        if (_firstIntersection is null)
                        {
                            MaterialSnack("未第一次确认交点！", SnackType.WARNING, 0);
                            return;
                        }
                        MaterialSnack("第二次确认交点中...", SnackType.WARNING, 0);
                        _secondIntersection = new PointF(x, y);
                        await PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(ThirdCutThetaDeg, default, _stopCts.Token);
                        _step = ThetaCenterAlignStep.FindThirdIntersection;
                        break;

                    case ThetaCenterAlignStep.FindThirdIntersection:
                        if (_secondIntersection is null)
                        {
                            MaterialSnack("未第二次确认交点！", SnackType.WARNING, 0);
                            return;
                        }
                        MaterialSnack("第三次确认交点中...", SnackType.WARNING, 0);
                        _thirdIntersection = new PointF(x, y);
                        float distanceY = y - _secondIntersection.Value.Y;
                        await RunCutLineByThetaDegAsync([FirstCutThetaDeg, SecondCutThetaDeg], _startY + (distanceY / 2), _stopCts.Token);
                        _step = ThetaCenterAlignStep.FindCenterIntersection;
                        break;

                    case ThetaCenterAlignStep.FindCenterIntersection:
                        if (_thirdIntersection is null)
                        {
                            MaterialSnack("未第三次确认交点！", SnackType.WARNING, 0);
                            return;
                        }
                        MaterialSnack("确认中心交点中...", SnackType.WARNING, 0);
                        Appsettings.CameraThetaCenterPoint = new DataPoint<float>(x, y);
                        thetaCenterX.Text = x.ToString();
                        thetaCenterY.Text = y.ToString();
                        _step = ThetaCenterAlignStep.Completed;
                        //await PlcControl.tagControl.cutting.RunMotionAsync(x.ToActualX(), y.ToActualY() + 10, _stopCts.Token);
                        //float startY = _measureHeigthY - _viewModel.WorkThickness.ToFloat() - _viewModel.TapeThickness.ToFloat() - 0.2f;
                        //await PlcControl.tagControl.Yaxis.StartAbsoluteAsync(startY, default, _stopCts.Token);
                        //float endY = _measureHeigthY - _viewModel.WorkThickness.ToFloat() - _viewModel.TapeThickness.ToFloat() + 0.1f;
                        //await PlcControl.tagControl.wholeDevice.OpenCuttingWaterAsync();
                        //await PlcControl.tagControl.Yaxis.StartAbsoluteAsync(endY, 0.001f, _stopCts.Token);
                        //await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
                        //await AutoCutUtils.WorkpieceBlowingAsync(default, default, _stopCts.Token);
                        //await PlcControl.tagControl.cutting.RunMotionAsync(x.ToCameraX(), y.ToCameraY(), _stopCts.Token);
                        //_step = ThetaCenterAlignStep.FindRightEndpoint;
                        break;

                    case ThetaCenterAlignStep.FindRightEndpoint:
                        _rightPoint = new PointF(x, y);
                        _step = ThetaCenterAlignStep.FindLeftEndpoint;
                        break;

                    case ThetaCenterAlignStep.FindLeftEndpoint:
                        if (_rightPoint is null)
                        {
                            MaterialSnack("未确认刀痕线段右侧端点！", SnackType.WARNING, 0);
                            return;
                        }
                        _leftPoint = new PointF(x, y);
                        float relativeX = Appsettings.CameraThetaCenterPoint.X - ((_rightPoint.Value.X + _leftPoint.Value.X) / 2);
                        Appsettings.CameraRelativeBladePosition = new DataPoint<float>(relativeX, relativePostion.Y);
                        _step = ThetaCenterAlignStep.Completed;
                        break;

                    default:
                        Save();
                        break;
                }
                NotifyOperation();
                if (_step == ThetaCenterAlignStep.Completed)
                {
                    _firstIntersection = null;
                    _secondIntersection = null;
                    _thirdIntersection = null;
                    _rightPoint = null;
                    _leftPoint = null;
                    _step = ThetaCenterAlignStep.None;
                    thetaCenterParamsGrid.IsEnabled = true;
                    _rightPage.btnCutStart.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MaterialSnack($"确认交点异常：{ex.Message}", SnackType.ERROR);
            }
            finally
            {
                _thetaCenterAlignSemaphore.Release();
            }
        }

        private void NotifyOperation()
        {
            if (_step == ThetaCenterAlignStep.None)
            {
                return;
            }
            if (_step == ThetaCenterAlignStep.Completed)
            {
                MaterialSnack(_step.GetEnumDescription(), SnackType.SUCCESS, 0);
            }
            else
            {
                MaterialSnack(_step.GetEnumDescription(), SnackType.WARNING, 0);
            }
        }

        private async Task RunCutLineByThetaDegAsync(List<float> thetaDegs, float startY, CancellationToken token)
        {
            if (!GlobalParams.OnlineFlag) return;
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
            float depthEntry = _measureHeigthY - _viewModel.WorkThickness.ToFloat() - _viewModel.TapeThickness.ToFloat() - 0.5f;
            try
            {
                foreach (float thetaDeg in thetaDegs)
                {
                    //当前切割次数
                    int? curCutNum = await PlcControl.tagControl.cutting.GetCutNumAsync();
                    if (curCutNum == null)
                    {
                        MaterialSnack("获取当前切割次数失败！", SnackType.WARNING, 0);
                        return;
                    }
                    await PlcControl.tagControl.ThetaAxis.SetAbsoluteSpeedAsync(GlobalParams.ThetaDefaultSpeed);
                    //设置切割参数
                    await PlcControl.tagControl.cutting.SetCutParamsAsync(_viewModel.CutSpeed.ToFloat(), endZ, startZ, _startX, _startX + _viewModel.WorkSize.ToFloat(), startY, "0", thetaDeg, _viewModel.SpindleSpeed.ToInt(), depthEntry);
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
                await AutoCutUtils.WorkpieceBlowingAsync(default, default, true, default, token);
                await PlcControl.tagControl.cutting.RunMotionAsync(((_startX + _endX) / 2).ToCameraX(), startY.ToCameraY(), token);
            }
        }

        private void Save()
        {
            MaterialSnack("参数保存中...", SnackType.SUCCESS);
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
            Appsettings.CameraThetaCenterPoint = new DataPoint<float>(thetaCenterX.Text.ToFloat(), thetaCenterY.Text.ToFloat());
            MaterialSnack("保存成功！", SnackType.SUCCESS);
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            Stop();
            _mainWindow.NavigateToPage("MainMenu");
        }

        private bool _isPositive = true;

        private async void OperateClickHandler(object? sender, int code)
        {
            if (AlarmConfig.Instance.HasActiveErrorAlarm())
            {
                MaterialSnack(AlarmConfig.HasErrorAlarmMessage, SnackType.WARNING);
                return;
            }
            switch (code)
            {
                case 44002:
                    if (_dataShowsCts is null) return;
                    await _thetaCenterAlignSemaphore.ExecuteAsync(async () =>
                    {
                        try
                        {
                            await using var timeoutToken = TaskUtils.GetTimeoutCancellationToken(TimeSpan.FromSeconds(120));
                            CommonResult<float> focusRusult = await AutoFocusService.GlobalFocusAsync(default, timeoutToken.Token);
                            if (!focusRusult.IsSuccess)
                            {
                                MaterialSnack(focusRusult.Message, SnackType.WARNING);
                                return;
                            }
                            await PlcControl.tagControl.Z2axis.StartAbsoluteAsync(focusRusult.Data, default, timeoutToken.Token);
                            MaterialSnack("对焦完成！", SnackType.SUCCESS, 2);
                        }
                        catch (OperationCanceledException)
                        {
                            if (_dataShowsCts.IsCancellationRequested)
                            {
                                MaterialSnack("对焦已取消！", SnackType.WARNING, default);
                            }
                            else
                            {
                                MaterialSnack("对焦超时！", SnackType.WARNING, default);
                            }
                        }
                    }, "对焦");
                    break;

                case 44003:
                    if (_dataShowsCts is null) return;
                    await _thetaCenterAlignSemaphore.ExecuteAsync(async () =>
                    {
                        try
                        {
                            await using var timeoutToken = TaskUtils.GetTimeoutCancellationToken(TimeSpan.FromSeconds(120), _dataShowsCts.Token);
                            await PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(_isPositive ? 90 : 0, default, timeoutToken.Token);
                            _isPositive = !_isPositive;
                        }
                        catch (OperationCanceledException)
                        {
                            MaterialSnack("实时测量已取消！", SnackType.WARNING);
                        }
                    }, "实时测量");
                    break;

                case 44004:
                    if (_dataShowsCts is null) return;
                    await _thetaCenterAlignSemaphore.ExecuteAsync(async () =>
                    {
                        try
                        {
                            await PlcControl.tagControl.cutting.RunMotionAsync(Appsettings.CameraThetaCenterPoint.X, Appsettings.CameraThetaCenterPoint.Y, _dataShowsCts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            MaterialSnack("不切割操作已取消！", SnackType.WARNING);
                        }
                    }, "不切割");
                    break;

                default:
                    break;
            }
        }

        private enum ThetaCenterAlignStep
        {
            [Description("请开始校准")]
            None,

            [Description("请完成第一次确认交点")] // 十字中心点确认
            FindFirstIntersection,

            [Description("请完成第二次确认交点")] // 旋转后的十字中心点确认
            FindSecondIntersection,

            [Description("请完成第三次确认交点")] // 第二个十字中心点确认
            FindThirdIntersection,

            [Description("请确认中心交点")] // 圆盘中心点确认
            FindCenterIntersection,

            [Description("请确认刀痕线段右侧端点")] // 当前刀痕线段右侧端点确认
            FindRightEndpoint,

            [Description("请确认刀痕线段左侧端点")] // 当前刀痕线段左侧端点确认
            FindLeftEndpoint,

            [Description("已完成校准！")]
            Completed
        }
    }
}