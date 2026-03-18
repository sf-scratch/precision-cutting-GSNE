using OpenCvSharp.XFeatures2D;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.plc;
using 精密切割系统.Model.position;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using static MaterialDesignThemes.Wpf.Theme.ToolBar;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.ViewModel
{
    internal class DebugPageViewModel : CustomBindableBase
    {
        public ObservableCollection<DebugItem> DebugItemList { get; } = new ObservableCollection<DebugItem>();

        private CancellationTokenSource _cts;

        public DebugPageViewModel()
        {
            Add();
            Add();
            Add();
        }

        private void InitRightButton()
        {
            RightButtonCollection.Clear();
            RightButtonCollection.Add(ButtonParams.GreenRightButton("确定", "CogBox", Sure));
            RightButtonCollection.Add(ButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private void InitBottomButton()
        {
            BottomButtonCollection.Clear();
            BottomButtonCollection.Add(ButtonParams.BlueButton("运行", "PlayOutline", Start));
            BottomButtonCollection.Add(ButtonParams.BlueButton("新增", "TableColumnPlusAfter", Add));
            BottomButtonCollection.Add(ButtonParams.BlueButton("删除", "BeakerRemoveOutline", Remove));
        }

        private void Sure()
        {
            NavigateUtils.ToOperateButton();
            MaterialSnack("参数已确定！", SnackType.SUCCESS);
        }

        private void Back()
        {
            NavigateUtils.NavigateToPage("MainMenu");
        }

        private async Task Start()
        {
            if (AlarmConfig.Instance.HasActiveErrorAlarm())
            {
                MaterialSnack(AlarmConfig.HasErrorAlarmMessage, SnackType.WARNING);
                return;
            }
            var debugItems = DebugItemList.Where(a => a.IsChecked).ToList();
            if (debugItems.Count == 0)
            {
                MaterialSnack("没有选中的参数！", SnackType.WARNING);
                return;
            }
            var tempItem = new DebugItem();
            foreach (var item in debugItems)
            {
                if (!item.IsCheckedX && !item.IsCheckedY && !item.IsCheckedZ1 && !item.IsCheckedZ2 && !item.IsCheckedTheta)
                {
                    MaterialSnack($"{item.ItemName} 没有选中任何轴！", SnackType.WARNING);
                    return;
                }
                if (!ValidateFloatParameters(item))
                {
                    MaterialSnack($"{item.ItemName} 数据异常，请检测参数格式！", SnackType.WARNING);
                    return;
                }
                tempItem.IsCheckedX = tempItem.IsCheckedX || item.IsCheckedX;
                tempItem.IsCheckedY = tempItem.IsCheckedY || item.IsCheckedY;
                tempItem.IsCheckedZ1 = tempItem.IsCheckedZ1 || item.IsCheckedZ1;
                tempItem.IsCheckedZ2 = tempItem.IsCheckedZ2 || item.IsCheckedZ2;
                tempItem.IsCheckedTheta = tempItem.IsCheckedTheta || item.IsCheckedTheta;
            }
            MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null)
            {
                MaterialSnack("获取控件异常！", SnackType.WARNING);
                return;
            }
            OperatePage? operatePage = mainWindow.operateFrame.Content as OperatePage;
            if (operatePage == null)
            {
                MaterialSnack("获取控件异常！", SnackType.WARNING);
                return;
            }
            try
            {
                operatePage.IsEnabled = false;
                await PlcControl.tagControl.wholeDevice.OpenDebugModeAsync();
                if (tempItem.IsCheckedX)
                {
                    if (!await PlcControl.tagControl.Xaxis.IsReadyAsync())
                    {
                        MaterialSnack($"{PlcControl.tagControl.Xaxis.axisName} 未准备好！", SnackType.WARNING);
                        return;
                    }
                }
                if (tempItem.IsCheckedY)
                {
                    if (!await PlcControl.tagControl.Yaxis.IsReadyAsync())
                    {
                        MaterialSnack($"{PlcControl.tagControl.Yaxis.axisName} 未准备好！", SnackType.WARNING);
                        return;
                    }
                }
                if (tempItem.IsCheckedZ1)
                {
                    if (!await PlcControl.tagControl.Z1axis.IsReadyAsync())
                    {
                        MaterialSnack($"{PlcControl.tagControl.Z1axis.axisName} 未准备好！", SnackType.WARNING);
                        return;
                    }
                }
                if (tempItem.IsCheckedZ2)
                {
                    if (!await PlcControl.tagControl.Z2axis.IsReadyAsync())
                    {
                        MaterialSnack($"{PlcControl.tagControl.Z2axis.axisName} 未准备好！", SnackType.WARNING);
                        return;
                    }
                }
                if (tempItem.IsCheckedTheta)
                {
                    if (!await PlcControl.tagControl.ThetaAxis.IsReadyAsync())
                    {
                        MaterialSnack($"{PlcControl.tagControl.ThetaAxis.axisName} 未准备好！", SnackType.WARNING);
                        return;
                    }
                }
                foreach (var item in debugItems)
                {
                    MaterialSnack($"{item.ItemName} 执行中...", SnackType.WARNING, 0);
                    List<Task> moveTasks = new List<Task>();
                    if (item.IsCheckedX)
                    {
                        moveTasks.Add(PlcControl.tagControl.Xaxis.StartAbsoluteAsync(item.XPosition.ToFloat(), default, _cts.Token));
                    }
                    if (item.IsCheckedY)
                    {
                        moveTasks.Add(PlcControl.tagControl.Yaxis.StartAbsoluteAsync(item.YPosition.ToFloat(), default, _cts.Token));
                    }
                    if (item.IsCheckedZ1)
                    {
                        moveTasks.Add(PlcControl.tagControl.Z1axis.StartAbsoluteAsync(item.Z1Position.ToFloat(), default, _cts.Token));
                    }
                    if (item.IsCheckedZ2)
                    {
                        moveTasks.Add(PlcControl.tagControl.Z2axis.StartAbsoluteAsync(item.Z2Position.ToFloat(), default, _cts.Token));
                    }
                    if (item.IsCheckedTheta)
                    {
                        moveTasks.Add(PlcControl.tagControl.ThetaAxis.StartAbsoluteAsync(item.ThetaPosition.ToFloat(), default, _cts.Token));
                    }
                    await Task.WhenAll(moveTasks);
                    await Task.Delay(500);
                }
                MaterialSnack("运行完成！", SnackType.SUCCESS);
            }
            catch (OperationCanceledException)
            {
                MaterialSnack("调试操作已取消！", SnackType.INFO);
            }
            catch (Exception ex)
            {
                MaterialSnack($"运行出错：{ex.Message}", SnackType.ERROR);
            }
            finally
            {
                await PlcControl.tagControl.wholeDevice.CloseDebugModeAsync();
                operatePage.IsEnabled = true;
            }
        }

        private static int DebugItemID = 0;

        private void Add()
        {
            if (DebugItemList.Count >= 10)
            {
                MaterialSnack("最多只能添加10个调试参数！", SnackType.WARNING);
                return;
            }
            DebugItemList.Add(new DebugItem { XPosition = "0", YPosition = "0", Z1Position = "0", Z2Position = "0", ThetaPosition = "0", ItemName = $"调试参数 No.{++DebugItemID}" });
        }

        private void Remove()
        {
            var list = DebugItemList.Where(a => !a.IsChecked).ToList();
            DebugItemList.Clear();
            DebugItemList.AddRange(list);
        }

        private bool ValidateFloatParameters(DebugItem item)
        {
            string[] values = { item.XPosition, item.YPosition, item.Z1Position, item.Z2Position, item.ThetaPosition };
            return values.All(value => float.TryParse(value, out _));
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            _cts = new CancellationTokenSource();
            InitRightButton();
            InitBottomButton();
        }

        public override async void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
            _cts.Cancel();
        }
    }
}