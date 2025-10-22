using Newtonsoft.Json.Linq;
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
using 精密切割系统.Data;
using 精密切割系统.database.db.modle;
using 精密切割系统.Extensions;
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Model.sqlite;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.F7_ElectricSpark
{
    /// <summary>
    /// FlangeTrimmingConf.xaml 的交互逻辑
    /// </summary>
    public partial class FlangeTrimmingConf : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;
        private CancellationTokenSource? _cts;
        private CancellationTokenSource _axisPositionCts = new CancellationTokenSource();

        public FlangeTrimmingConf()
        {
            InitializeComponent();
            mainWindow = Application.Current.MainWindow as MainWindow; ;
        }

        private bool positionLoadFlag = true;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            rightPage = mainWindow.rightFrame.Content as RightPage;

            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);

            rightPage.btnSure.Visibility = Visibility.Visible;
            rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked);

            rightPage.btnCutStart.Visibility = Visibility.Visible;
            rightPage.btnCutStart.SetRightClickedHandler(BtnCutStart_RightClicked);

            rightPage.btnCutStop.Visibility = Visibility.Collapsed;
            rightPage.btnCutStop.SetRightClickedHandler(BtnCutPause_RightClicked);

            operatePage = mainWindow.operateFrame.Content as OperatePage;
            operatePage?.SetOperateShowType(1);
            loadAxisPosition();
            loadData();
        }

        private void loadAxisPosition()
        {
            Thread _thread = new Thread(async () =>
            {
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(300));
                try
                {
                    while (await timer.WaitForNextTickAsync(_axisPositionCts.Token))
                    {
                        var axisPostion = await AutoCutUtils.GetAxisPositionAsync();
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            xCenterPosition.Text = MathF.Round(axisPostion.X ?? 0, 3).ToString("F3");
                            yCenterPosition.Text = MathF.Round(axisPostion.Y ?? 0, 3).ToString("F3");
                            zCenterPosition.Text = MathF.Round(axisPostion.Z1 ?? 0, 3).ToString("F3");
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    Tools.LogDebug("法兰修整界面轴位置更新已取消");
                }
            });
            _thread.IsBackground = true;
            _thread.Start();
        }

        private void loadData()
        {
            xCenterPosition.Text = FlangeTrimmingData.Instance.XCenterPosition.ToString();
            yCenterPosition.Text = FlangeTrimmingData.Instance.YCenterPosition.ToString();
            zCenterPosition.Text = FlangeTrimmingData.Instance.ZCenterPosition.ToString();
            yStepDistance.Text = FlangeTrimmingData.Instance.YStepDistance.ToString();
            xAxisTravel.Text = FlangeTrimmingData.Instance.XAxisTravel.ToString();
            cutSpeed.Text = FlangeTrimmingData.Instance.CutSpeed.ToString();
            spindleRev.Text = FlangeTrimmingData.Instance.SpindleRev.ToString();
            repectCount.Text = FlangeTrimmingData.Instance.RepeatCount.ToString();
            sparkFreeStep.Text = FlangeTrimmingData.Instance.SparkFreeStep.ToString();
        }

        private async void BtnCutStart_RightClicked(object? sender, bool e)
        {
            _cts = new CancellationTokenSource();
            MaterialSnackUtils.MaterialSnack("修整中...", MaterialSnackUtils.SnackType.SUCCESS, -1);
            rightPage.btnCutStart.Visibility = Visibility.Collapsed;
            rightPage.btnCutStop.Visibility = Visibility.Visible;
            currentCount.Text = "0";
            try
            {
                await PlcControl.tagControl.bladeMantance.StartContactHeightMeasurement();
                if (hardKnife.IsSelected)
                {
                    await StartFlangeTrimmingHardKnife(_cts.Token);
                }
                {
                    await StartFlangeTrimmingSoftKnife(_cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                MaterialSnackUtils.MaterialSnack("修整已停止!", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            catch (Exception ex)
            {
                MaterialSnackUtils.MaterialSnack($"修整异常：{ex.Message}", MaterialSnackUtils.SnackType.ERROR);
                return;
            }
            finally
            {
                rightPage.btnCutStart.Visibility = Visibility.Visible;
                rightPage.btnCutStop.Visibility = Visibility.Collapsed;
                currentCount.Text = "0";
            }
        }

        private async Task StartFlangeTrimmingSoftKnife(CancellationToken token)
        {
            try
            {
                await PlcControl.tagControl.cutting.FlangeRepairBeginsAsync();
                //// 设置主轴转速
                await PlcControl.tagControl.cutting.SetSpindleSpeedAsync(FlangeTrimmingData.Instance.SpindleRev);
                await PlcControl.tagControl.wholeDevice.StartSpindleAsync();
                await PlcControl.tagControl.cutting.WaitSpindleRevReachAsync(token);
                AxisPosition axisPostion = await AutoCutUtils.GetAxisPositionAsync();
                float curX = axisPostion.X ?? 0;
                float xLeft = curX + FlangeTrimmingData.Instance.XAxisTravel, xRight = curX;
                //await PlcControl.tagControl.cutting.FlangeRepairBeginsAsync();
                //while (!await PlcControl.tagControl.cutting.IsDetectedSparksAsync())
                //{
                //    await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(xLeft, FlangeTrimmingData.Instance.CutSpeed, token);
                //    await PlcControl.tagControl.Yaxis.StartRelativeAsync(-0.5f, 1, token);
                //    await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(xRight, FlangeTrimmingData.Instance.CutSpeed, token);
                //    await PlcControl.tagControl.Yaxis.StartRelativeAsync(0.5f, 1, token);
                //    await PlcControl.tagControl.Yaxis.StartRelativeAsync(FlangeTrimmingData.Instance.YStepDistance, 1, token);
                //}
                //await PlcControl.tagControl.cutting.FlangeRepairEndAsync();
                for (int i = 0; i < FlangeTrimmingData.Instance.RepeatCount; i++)
                {
                    int sparkFreeSteps = FlangeTrimmingData.Instance.SparkFreeStep;
                    while (sparkFreeSteps > 0)
                    {
                        //await PlcControl.tagControl.cutting.FlangeRepairBeginsAsync();
                        await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(xLeft, FlangeTrimmingData.Instance.CutSpeed, token);
                        //if (!await PlcControl.tagControl.cutting.IsDetectedSparksAsync())
                        {
                            sparkFreeSteps--;
                        }
                        //await PlcControl.tagControl.cutting.FlangeRepairEndAsync();
                        await PlcControl.tagControl.Yaxis.StartRelativeAsync(-0.5f, 5, token);
                        await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(xRight, FlangeTrimmingData.Instance.CutSpeed, token);
                        await PlcControl.tagControl.Yaxis.StartRelativeAsync(0.5f, 5, token);
                    }
                    await PlcControl.tagControl.Yaxis.StartRelativeAsync(FlangeTrimmingData.Instance.YStepDistance, 1, token);
                    currentCount.Text = (currentCount.Text.ToInt() + 1).ToString();
                }
            }
            finally
            {
                await PlcControl.tagControl.cutting.FlangeRepairEndAsync();
            }
        }

        private async Task StartFlangeTrimmingHardKnife(CancellationToken token)
        {
            try
            {
                await PlcControl.tagControl.cutting.FlangeRepairBeginsAsync();
                // 设置主轴转速
                await PlcControl.tagControl.cutting.SetSpindleSpeedAsync(FlangeTrimmingData.Instance.SpindleRev);
                await PlcControl.tagControl.wholeDevice.StartSpindleAsync();
                await PlcControl.tagControl.cutting.WaitSpindleRevReachAsync(token);
                AxisPosition axisPostion = await AutoCutUtils.GetAxisPositionAsync();
                float curX = axisPostion.X ?? 0;
                float xLeft = curX + FlangeTrimmingData.Instance.XAxisTravel, xRight = curX;
                bool isLeft = true;
                //while (true)
                //{
                //    await PlcControl.tagControl.cutting.FlangeRepairBeginsAsync();
                //    await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(isLeft ? xLeft : xRight, FlangeTrimmingData.Instance.CutSpeed, token);
                //    isLeft = !isLeft;
                //    if (await PlcControl.tagControl.cutting.IsDetectedSparksAsync())
                //    {
                //        break;
                //    }
                //    await PlcControl.tagControl.cutting.FlangeRepairEndAsync();
                //    await PlcControl.tagControl.Yaxis.StartRelativeAsync(FlangeTrimmingData.Instance.YStepDistance, 1, token);
                //}
                for (int i = 0; i < FlangeTrimmingData.Instance.RepeatCount; i++)
                {
                    int sparkFreeSteps = FlangeTrimmingData.Instance.SparkFreeStep;
                    while (sparkFreeSteps > 0)
                    {
                        await PlcControl.tagControl.Xaxis.StartAbsoluteAsync(isLeft ? xLeft : xRight, FlangeTrimmingData.Instance.CutSpeed, token);
                        isLeft = !isLeft;
                        sparkFreeSteps--;
                    }
                    currentCount.Text = (currentCount.Text.ToInt() + 1).ToString();
                    await PlcControl.tagControl.Yaxis.StartRelativeAsync(FlangeTrimmingData.Instance.YStepDistance, 1, token);
                }
            }
            finally
            {
                await PlcControl.tagControl.cutting.FlangeRepairEndAsync();
            }
        }

        private void BtnCutPause_RightClicked(object? sender, bool e)
        {
            MaterialSnackUtils.MaterialSnack("修整停止中...", MaterialSnackUtils.SnackType.WARNING, -1);
            _cts?.Cancel();
        }

        private void BtnSure_RightClicked(object? sender, bool e)
        {
            if (this.HasFormError())
            {
                MaterialSnackUtils.MaterialSnack("表单填写有误，请检查!", MaterialSnackUtils.SnackType.ERROR);
                return;
            }
            FlangeTrimmingData.Instance.XCenterPosition = xCenterPosition.Text.ToFloat();
            FlangeTrimmingData.Instance.YCenterPosition = yCenterPosition.Text.ToFloat();
            FlangeTrimmingData.Instance.ZCenterPosition = zCenterPosition.Text.ToFloat();
            FlangeTrimmingData.Instance.YStepDistance = yStepDistance.Text.ToFloat();
            FlangeTrimmingData.Instance.XAxisTravel = xAxisTravel.Text.ToFloat();
            FlangeTrimmingData.Instance.CutSpeed = cutSpeed.Text.ToFloat();
            FlangeTrimmingData.Instance.SpindleRev = spindleRev.Text.ToInt();
            FlangeTrimmingData.Instance.RepeatCount = repectCount.Text.ToInt();
            FlangeTrimmingData.Instance.SparkFreeStep = sparkFreeStep.Text.ToInt();
            MaterialSnackUtils.MaterialSnack("法兰修整参数确认完成!", MaterialSnackUtils.SnackType.SUCCESS);
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            positionLoadFlag = false;
            _cts?.Cancel();
            _axisPositionCts.Cancel();
            mainWindow.NavigateToPage("MainMenu");
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            positionLoadFlag = false;
            _cts?.Cancel();
            _axisPositionCts.Cancel();
        }
    }
}