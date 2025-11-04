using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.position;
using 精密切割系统.PubSubEvent;
using 精密切割系统.View.common;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.ViewModel
{
    public class F7_2_AxisOperationViewModel : CustomBindableBase
    {
        public ObservableCollection<AxisOperationModel> AxisOperationList { get; set; } = new ObservableCollection<AxisOperationModel>();

        private CancellationTokenSource _cts;

        public F7_2_AxisOperationViewModel()
        {
        }

        private void InitRightButton()
        {
            RightButtonCollection.Clear();
            RightButtonCollection.Add(RightButtonParams.GreenRightButton("确定", "CogBox", Sure));
            RightButtonCollection.Add(RightButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private void Sure()
        {
            MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null)
            {
                return;
            }
            RightPage? rightPage = mainWindow.rightFrame.Content as RightPage;
            OperatePage? operatePage = mainWindow.operateFrame.Content as OperatePage;
            if (rightPage == null || operatePage == null)
            {
                return;
            }
            operatePage.SetOperateShowType(3);
        }

        private void Back()
        {
            NavigateUtils.NavigateToPage("MainMenu");
        }

        private void InitBottomButton()
        {
            BottomButtonCollection.Clear();
            BottomButtonCollection.Add(RightButtonParams.BlueButton("慢相对向前", "/Assets/icon/tab_7/01/tab_01.png", () => SlowRelativeMotionAsync(true)));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("慢相对向后", "/Assets/icon/tab_7/01/tab_02.png", () => SlowRelativeMotionAsync(false)));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("相对向前", "/Assets/icon/tab_7/01/tab_03.png", () => RelativeMotionAsync(true)));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("相对向后", "/Assets/icon/tab_7/01/tab_04.png", () => RelativeMotionAsync(false)));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("", "", () => { }, buttonVisibility: System.Windows.Visibility.Hidden));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("慢点动向前", "/Assets/icon/tab_7/01/tab_05.png", null, () => SlowJogAsync(true), StopJogAsync));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("慢点动向后", "/Assets/icon/tab_7/01/tab_06.png", null, () => SlowJogAsync(false), StopJogAsync));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("点动向前", "/Assets/icon/tab_7/01/tab_07.png", null, () => JogAsync(true), StopJogAsync));
            BottomButtonCollection.Add(RightButtonParams.BlueButton("点动向后", "/Assets/icon/tab_7/01/tab_08.png", null, () => JogAsync(false), StopJogAsync));
        }

        private async Task SlowRelativeMotionAsync(bool isPositive)
        {
            var selectedAxis = AxisOperationList.FirstOrDefault(a => a.IsChecked);
            if (selectedAxis != null)
            {
                await selectedAxis.AxisObject.SetAbsoluteSpeedAsync(5);
                await selectedAxis.AxisObject.StartRelativeAsync(isPositive ? selectedAxis.RelativeDistance.ToFloat() : -selectedAxis.RelativeDistance.ToFloat(), selectedAxis.AxisSlowSpeed.ToFloat(), _cts.Token);
            }
        }

        private async Task RelativeMotionAsync(bool isPositive)
        {
            var selectedAxis = AxisOperationList.FirstOrDefault(a => a.IsChecked);
            if (selectedAxis != null)
            {
                await selectedAxis.AxisObject.SetAbsoluteSpeedAsync(selectedAxis.AxisSpeed.ToFloat());
                await selectedAxis.AxisObject.StartRelativeAsync(isPositive ? selectedAxis.RelativeDistance.ToFloat() : -selectedAxis.RelativeDistance.ToFloat(), selectedAxis.AxisSpeed.ToFloat(), _cts.Token);
            }
        }

        private async Task SlowJogAsync(bool isPositive)
        {
            var selectedAxis = AxisOperationList.FirstOrDefault(a => a.IsChecked);
            if (selectedAxis != null)
            {
                await selectedAxis.AxisObject.SetHighSpeedAsync(0);
                await selectedAxis.AxisObject.StartJogAsync(isPositive ? 0 : 1);
            }
        }

        private async Task JogAsync(bool isPositive)
        {
            var selectedAxis = AxisOperationList.FirstOrDefault(a => a.IsChecked);
            if (selectedAxis != null)
            {
                await selectedAxis.AxisObject.SetHighSpeedAsync(1);
                await selectedAxis.AxisObject.SetJogRelativeSpeedAsync(selectedAxis.AxisSpeed.ToFloat());
                await selectedAxis.AxisObject.StartJogAsync(isPositive ? 0 : 1);
            }
        }

        private async Task StopJogAsync()
        {
            var selectedAxis = AxisOperationList.FirstOrDefault(a => a.IsChecked);
            if (selectedAxis != null)
            {
                await selectedAxis.AxisObject.StopJogAsync();
            }
        }

        private async Task MonitiorAxisState(CancellationToken token)
        {
            await Task.Run(async () =>
            {
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(200));
                while (await timer.WaitForNextTickAsync(token))
                {
                    var axisPostion = await AutoCutUtils.GetAxisPositionAsync();
                    var axisState = await AutoCutUtils.GetAxisStateAsync();
                    var xAxis = AxisOperationList.Where(p => p.AxisName == AxisNameType.X).FirstOrDefault();
                    if (xAxis is not null)
                    {
                        xAxis.CurPosition = axisPostion.X?.ToString("F3") ?? "N";
                        xAxis.IsReady = axisState.X == true;
                    }
                    var yAxis = AxisOperationList.Where(p => p.AxisName == AxisNameType.Y).FirstOrDefault();
                    if (yAxis is not null)
                    {
                        yAxis.CurPosition = axisPostion.Y?.ToString("F3") ?? "N";
                        yAxis.IsReady = axisState.Y == true;
                    }
                    var zAxis = AxisOperationList.Where(p => p.AxisName == AxisNameType.Z1).FirstOrDefault();
                    if (zAxis is not null)
                    {
                        zAxis.CurPosition = axisPostion.Z1?.ToString("F3") ?? "N";
                        zAxis.IsReady = axisState.Z1 == true;
                    }
                    var z2Axis = AxisOperationList.Where(p => p.AxisName == AxisNameType.Z2).FirstOrDefault();
                    if (z2Axis is not null)
                    {
                        z2Axis.CurPosition = axisPostion.Z2?.ToString("F3") ?? "N";
                        z2Axis.IsReady = axisState.Z2 == true;
                    }
                    var thetaAxis = AxisOperationList.Where(p => p.AxisName == AxisNameType.Theta).FirstOrDefault();
                    if (thetaAxis is not null)
                    {
                        thetaAxis.CurPosition = axisPostion.Theta?.ToString("F3") ?? "N";
                        thetaAxis.IsReady = axisState.Theta == true;
                    }
                }
            });
        }

        private async Task SetLowSpeedAsync()
        {
            // 设置为低速
            Task x = PlcControl.tagControl.Xaxis.SetHighSpeedAsync(0);
            Task y = PlcControl.tagControl.Yaxis.SetHighSpeedAsync(0);
            Task z1 = PlcControl.tagControl.Z1axis.SetHighSpeedAsync(0);
            Task z2 = PlcControl.tagControl.Z2axis.SetHighSpeedAsync(0);
            Task theta = PlcControl.tagControl.ThetaAxis.SetHighSpeedAsync(0);
            await Task.WhenAll(x, y, z1, z2, theta);
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            _cts = new CancellationTokenSource();
            AxisOperationList.AddRange([
                new AxisOperationModel(PlcControl.tagControl.Xaxis, async (a,b) => { if(!b) await a.AxisObject.StopJogAsync();}){IsChecked = true, AxisSlowSpeed = "0", AxisSpeed = "10", RelativeDistance = "5", CurPosition = "0"},
                new AxisOperationModel(PlcControl.tagControl.Yaxis, async (a,b) => { if(!b) await a.AxisObject.StopJogAsync();}){AxisSlowSpeed = "0", AxisSpeed = "10", RelativeDistance = "5", CurPosition = "0"},
                new AxisOperationModel(PlcControl.tagControl.Z1axis, async(a, b) => { if(!b) await a.AxisObject.StopJogAsync();}){AxisSlowSpeed = "0", AxisSpeed = "10", RelativeDistance = "5", CurPosition = "0"},
                new AxisOperationModel(PlcControl.tagControl.Z2axis, async(a, b) => { if(! b) await a.AxisObject.StopJogAsync(); }){AxisSlowSpeed = "0", AxisSpeed = "10", RelativeDistance = "5", CurPosition = "0"},
                new AxisOperationModel(PlcControl.tagControl.ThetaAxis, async(a, b) => { if(! b) await a.AxisObject.StopJogAsync(); }){AxisSlowSpeed = "0", AxisSpeed = "10", RelativeDistance = "5", CurPosition = "0",IsReady = true},]);
            _ = MonitiorAxisState(_cts.Token);
            InitRightButton();
            InitBottomButton();
        }

        public override async void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
            _cts.Cancel();
            foreach (var axis in AxisOperationList)
            {
                axis.ClearCallback();
            }
            AxisOperationList.Clear();
            await SetLowSpeedAsync();
        }
    }
}