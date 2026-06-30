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
using 精密切割系统.Extensions;
using 精密切割系统.Helpers;
using 精密切割系统.Helpers.GTN;
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
                new AxisOperationModel(AxisType.X, async (a,b) => { if(!b) await GsneMotion.Instance.Axis.StopJogAsync(a.Axis);}){IsChecked = true, AxisSlowSpeed = "0.1", AxisSpeed = "10", RelativeDistance = "5", CurPosition = "0", Unit = "mm", SpeedUnit = "mm/s"},
                new AxisOperationModel(AxisType.Y, async (a,b) => { if(!b) await GsneMotion.Instance.Axis.StopJogAsync(a.Axis);}){AxisSlowSpeed = "0.1", AxisSpeed = "10", RelativeDistance = "5", CurPosition = "0", Unit = "mm", SpeedUnit = "mm/s"},
                new AxisOperationModel(AxisType.Z1, async(a, b) => { if(!b) await GsneMotion.Instance.Axis.StopJogAsync(a.Axis);}){AxisSlowSpeed = "0.1", AxisSpeed = "10", RelativeDistance = "5", CurPosition = "0", Unit = "mm", SpeedUnit = "mm/s"},
                new AxisOperationModel(AxisType.Z2, async(a, b) => { if(! b) await GsneMotion.Instance.Axis.StopJogAsync(a.Axis); }){AxisSlowSpeed = "0.1", AxisSpeed = "10", RelativeDistance = "5", CurPosition = "0", Unit = "mm", SpeedUnit = "mm/s"}]);
            if (GlobalParams.HasTheta)
            {
                AxisOperationList.Add(new AxisOperationModel(AxisType.Theta, async (a, b) => { if (!b) await GsneMotion.Instance.Axis.StopJogAsync(a.Axis); }) { AxisSlowSpeed = "0.1", AxisSpeed = "10", RelativeDistance = "5", CurPosition = "0", IsReady = true, Unit = "deg", SpeedUnit = "deg/s" });
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
                await GsneMotion.Instance.Axis.StartHomingAsync(selectedAxis.Axis);
            }
        }

        private async Task RelaxAxisAsync()
        {
            var selectedAxis = AxisOperationList.FirstOrDefault(a => a.IsChecked);
            if (selectedAxis != null)
            {
                var result = await GsneMotion.Instance.GetAxisStatusAsync(selectedAxis.Axis, AxisStatusBits.MotorEnabled);
                if (result.IsSuccess)
                {
                    if (result.Data)
                    {
                        await GsneMotion.Instance.Axis.AxisOffAsync(selectedAxis.Axis);
                    }
                    else
                    {
                        await GsneMotion.Instance.Axis.AxisOnAsync(selectedAxis.Axis);
                    }
                }
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
                await GsneMotion.Instance.Axis.StartRelativeAsync(selectedAxis.Axis, isPositive ? selectedAxis.RelativeDistance.ToFloat() : -selectedAxis.RelativeDistance.ToFloat(), selectedAxis.AxisSlowSpeed.ToFloat(), _cts.Token);
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
                await GsneMotion.Instance.Axis.StartRelativeAsync(selectedAxis.Axis, isPositive ? selectedAxis.RelativeDistance.ToFloat() : -selectedAxis.RelativeDistance.ToFloat(), selectedAxis.AxisSpeed.ToFloat(), _cts.Token);
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
                await GsneMotion.Instance.Axis.StartJogAsync(selectedAxis.Axis, (isPositive ? 1 : -1) * selectedAxis.AxisSlowSpeed.ToFloat());
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
                await GsneMotion.Instance.Axis.StartJogAsync(selectedAxis.Axis, (isPositive ? 1 : -1) * selectedAxis.AxisSpeed.ToFloat());
            }
        }

        private async Task StopJogAsync()
        {
            var selectedAxis = AxisOperationList.FirstOrDefault(a => a.IsChecked);
            if (selectedAxis != null)
            {
                await GsneMotion.Instance.Axis.StopJogAsync(selectedAxis.Axis);
            }
        }

        private async Task MonitiorAxisState(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var axisPostion = await AutoCutUtils.GetAxisPositionAsync();
                var axisState = await AutoCutUtils.GetAxisStateAsync();
                var xAxis = AxisOperationList.Where(p => p.Axis == AxisType.X).FirstOrDefault();
                var yAxis = AxisOperationList.Where(p => p.Axis == AxisType.Y).FirstOrDefault();
                var zAxis = AxisOperationList.Where(p => p.Axis == AxisType.Z1).FirstOrDefault();
                var z2Axis = AxisOperationList.Where(p => p.Axis == AxisType.Z2).FirstOrDefault();
                var thetaAxis = AxisOperationList.Where(p => p.Axis == AxisType.Theta).FirstOrDefault();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (xAxis is not null)
                    {
                        xAxis.CurPosition = axisPostion.X?.ToString("F3") ?? "N";
                        xAxis.IsReady = axisState.X == true;
                    }
                    if (yAxis is not null)
                    {
                        yAxis.CurPosition = axisPostion.Y?.ToString("F3") ?? "N";
                        yAxis.IsReady = axisState.Y == true;
                    }
                    if (zAxis is not null)
                    {
                        zAxis.CurPosition = axisPostion.Z1?.ToString("F3") ?? "N";
                        zAxis.IsReady = axisState.Z1 == true;
                    }
                    if (z2Axis is not null)
                    {
                        z2Axis.CurPosition = axisPostion.Z2?.ToString("F3") ?? "N";
                        z2Axis.IsReady = axisState.Z2 == true;
                    }
                    if (thetaAxis is not null)
                    {
                        thetaAxis.CurPosition = axisPostion.Theta?.ToString("F3") ?? "N";
                        thetaAxis.IsReady = axisState.Theta == true;
                    }
                });
                await Task.Delay(200);
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