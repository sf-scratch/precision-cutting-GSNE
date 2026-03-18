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
using 精密切割系统.Behaviors;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.plc;
using 精密切割系统.Model.position;
using 精密切割系统.PubSubEvent;
using 精密切割系统.Utils;
using 精密切割系统.View.common;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.ViewModel
{
    public class F7_2_AxisOperationViewModel : CustomBindableBase, IValidationExceptionHandler
    {
        public bool IsAllValid { get; set; } = true;
        public ObservableCollection<AxisOperationModel> AxisOperationList { get; set; } = new ObservableCollection<AxisOperationModel>();

        private CancellationTokenSource _cts;

        public F7_2_AxisOperationViewModel()
        {
            AxisOperationList.AddRange([
                new AxisOperationModel(PlcControl.tagControl.Xaxis, async (a,b) => { if(!b) await a.AxisObject.StopJogAsync();}){IsChecked = true, AxisSlowSpeed = "0.1", AxisSpeed = "10", RelativeDistance = "5", CurPosition = "0", Unit = "mm", SpeedUnit = "mm/s"},
                new AxisOperationModel(PlcControl.tagControl.Yaxis, async (a,b) => { if(!b) await a.AxisObject.StopJogAsync();}){AxisSlowSpeed = "0.1", AxisSpeed = "10", RelativeDistance = "5", CurPosition = "0", Unit = "mm", SpeedUnit = "mm/s"},
                new AxisOperationModel(PlcControl.tagControl.Z1axis, async(a, b) => { if(!b) await a.AxisObject.StopJogAsync();}){AxisSlowSpeed = "0.1", AxisSpeed = "10", RelativeDistance = "5", CurPosition = "0", Unit = "mm", SpeedUnit = "mm/s"},
                new AxisOperationModel(PlcControl.tagControl.Z2axis, async(a, b) => { if(! b) await a.AxisObject.StopJogAsync(); }){AxisSlowSpeed = "0.1", AxisSpeed = "10", RelativeDistance = "5", CurPosition = "0", Unit = "mm", SpeedUnit = "mm/s"}]);
            if (GlobalParams.HasTheta)
            {
                AxisOperationList.Add(new AxisOperationModel(PlcControl.tagControl.ThetaAxis, async (a, b) => { if (!b) await a.AxisObject.StopJogAsync(); }) { AxisSlowSpeed = "0.1", AxisSpeed = "10", RelativeDistance = "5", CurPosition = "0", IsReady = true, Unit = "deg", SpeedUnit = "deg/s" });
            }
        }

        protected override void InitRightButton()
        {
            RightButtonCollection.Clear();
            RightButtonCollection.Add(ButtonParams.Sure(Sure));
            RightButtonCollection.Add(ButtonParams.Back(Back));
        }

        protected override void InitBottomButton()
        {
            BottomButtonCollection.Clear();
            BottomButtonCollection.Add(ButtonParams.BlueButton("原点", "RotateRight", StartHomingAsync));
            BottomButtonCollection.Add(ButtonParams.BlueButton("低速尺寸", "Minus", () => SlowRelativeMotionAsync(false)));
            BottomButtonCollection.Add(ButtonParams.BlueButton("尺寸", "Minus", () => RelativeMotionAsync(false)));
            BottomButtonCollection.Add(ButtonParams.BlueButton("低速点动", "Minus", null, () => SlowJogAsync(false), StopJogAsync));
            BottomButtonCollection.Add(ButtonParams.BlueButton("点动", "Minus", null, () => JogAsync(false), StopJogAsync));
            BottomButtonCollection.Add(ButtonParams.BlueButton("放松", "HandFrontLeft", RelaxAxisAsync));
            BottomButtonCollection.Add(ButtonParams.BlueButton("低速尺寸", "Plus", () => SlowRelativeMotionAsync(true)));
            BottomButtonCollection.Add(ButtonParams.BlueButton("尺寸", "Plus", () => RelativeMotionAsync(true)));
            BottomButtonCollection.Add(ButtonParams.BlueButton("低速点动", "Plus", null, () => SlowJogAsync(true), StopJogAsync));
            BottomButtonCollection.Add(ButtonParams.BlueButton("点动", "Plus", null, () => JogAsync(true), StopJogAsync));
        }

        private void Sure()
        {
            if (!IsAllValid)
            {
                MaterialSnack("参数异常，请检查参数格式！", SnackType.WARNING, 2);
                return;
            }
            NavigateUtils.ToOperateButton();
        }

        private void Back()
        {
            NavigateUtils.NavigateToPage("MainMenu");
        }

        private async Task StartHomingAsync()
        {
            var selectedAxis = AxisOperationList.FirstOrDefault(a => a.IsChecked);
            if (selectedAxis != null)
            {
                await selectedAxis.AxisObject.StartHomingAsync();
            }
        }

        private async Task RelaxAxisAsync()
        {
            var selectedAxis = AxisOperationList.FirstOrDefault(a => a.IsChecked);
            if (selectedAxis != null)
            {
                await selectedAxis.AxisObject.RelaxAxisAsync();
            }
        }

        private async Task SlowRelativeMotionAsync(bool isPositive)
        {
            if (!IsAllValid)
            {
                MaterialSnack("参数异常，请检查参数格式！", SnackType.WARNING);
                return;
            }
            var selectedAxis = AxisOperationList.FirstOrDefault(a => a.IsChecked);
            if (selectedAxis != null)
            {
                await selectedAxis.AxisObject.StartRelativeAsync(isPositive ? selectedAxis.RelativeDistance.ToFloat() : -selectedAxis.RelativeDistance.ToFloat(), selectedAxis.AxisSlowSpeed.ToFloat(), _cts.Token);
            }
        }

        private async Task RelativeMotionAsync(bool isPositive)
        {
            if (!IsAllValid)
            {
                MaterialSnack("参数异常，请检查参数格式！", SnackType.WARNING);
                return;
            }
            var selectedAxis = AxisOperationList.FirstOrDefault(a => a.IsChecked);
            if (selectedAxis != null)
            {
                await selectedAxis.AxisObject.StartRelativeAsync(isPositive ? selectedAxis.RelativeDistance.ToFloat() : -selectedAxis.RelativeDistance.ToFloat(), selectedAxis.AxisSpeed.ToFloat(), _cts.Token);
            }
        }

        private async Task SlowJogAsync(bool isPositive)
        {
            if (!IsAllValid)
            {
                MaterialSnack("参数异常，请检查参数格式！", SnackType.WARNING);
                return;
            }
            var selectedAxis = AxisOperationList.FirstOrDefault(a => a.IsChecked);
            if (selectedAxis != null)
            {
                SpeedManager.IsHighSpeed = true;
                await selectedAxis.AxisObject.SetJogRelativeSpeedAsync(selectedAxis.AxisSlowSpeed.ToFloat());
                await selectedAxis.AxisObject.StartJogAsync(isPositive ? 0 : 1);
            }
        }

        private async Task JogAsync(bool isPositive)
        {
            if (!IsAllValid)
            {
                MaterialSnack("参数异常，请检查参数格式！", SnackType.WARNING);
                return;
            }
            var selectedAxis = AxisOperationList.FirstOrDefault(a => a.IsChecked);
            if (selectedAxis != null)
            {
                SpeedManager.IsHighSpeed = true;
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
        }

        private async Task SetLowSpeedAsync()
        {
            SpeedManager.IsHighSpeed = false;
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => MonitiorAxisState(_cts.Token));
        }

        public override async void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
            _cts.Cancel();
            await SetLowSpeedAsync();
        }
    }
}