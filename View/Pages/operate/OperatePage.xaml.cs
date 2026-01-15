using NPOI.SS.Formula.Functions;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
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
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.Assets.config.menu;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.View.Pages.common;
using 精密切割系统.View.Pages.F2_ManualOperation;
using 精密切割系统.View.Pages.F4_BladeMaintenance;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.operate
{
    /// <summary>
    /// OperatePage.xaml 的交互逻辑
    /// </summary>
    public partial class OperatePage : Page
    {
        private static CtViewModel ctViewModel = new CtViewModel();

        public OperatePage()
        {
            InitializeComponent();
            this.DataContext = ctViewModel;
        }

        private MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ctViewModel.UpdateImage(false, 1);
            ctViewModel.UpdateImage(false, 2);
            ctViewModel.UpdateImage(false, 3);
            ctViewModel.UpdateImage(false, 4);
            ctViewModel.UpdateImage(false, 5);
            ctViewModel.UpdateImage(false, 6);
            ctViewModel.UpdateImage(false, 7);
            ctViewModel.UpdateImage(false, 8);
            ctViewModel.UpdateImage(false, 9);
            ctViewModel.UpdateImage(false, 10);
            ctViewModel.UpdateImage(false, 8004);
            isSwitchOpen(false, 7);
            isSwitchOpen(false, 8);
            if (GlobalParams.OnlineFlag)
            {
                Thread _thread = new Thread(ShowOperateBtn);
                _thread.IsBackground = true;
                _thread.Start();
            }
        }

        public async void ShowOperateBtn()
        {
            bool cutSecurityDoor = false;
            bool vacuumState = false;
            bool spindleCuttingWater = false;
            bool workpieceBlowingStatus = false;
            bool isOpenWorkVacuumSwitchStatus = false;
            bool systemInitFlagStatus = false;
            bool panelStatus = false;
            bool cameraSecurityDoor = false;
            bool isOpenOpticalFiberSensorBlowing = false;
            bool isOpenOpticalFiberSensorBlowingWater = false;
            bool isRuningSpindle = false;
            bool firstJoin = true;
            while (true)
            {
                bool tempCutSecurityDoor = !await PlcControl.tagControl.wholeDevice.IsOpenCutSecurityDoorAsync();
                bool tempCameraSecurityDoor = !await PlcControl.tagControl.wholeDevice.IsOpenCameraSecurityDoorAsync();
                bool tempVacuumState = await PlcControl.tagControl.wholeDevice.IsOpenVacuumSwitchAsync();
                bool tempSpindleCuttingWater = await PlcControl.tagControl.wholeDevice.IsOpenSpindleCuttingWaterAsync();
                bool tempIsOpenOpticalFiberSensorBlowing = await PlcControl.tagControl.bladeMantance.GetOpticalFiberSensorBlowingAsync();
                bool tempIsOpenOpticalFiberSensorBlowingWater = await PlcControl.tagControl.bladeMantance.GetOpticalFiberSensorBlowingWaterAsync();
                bool tempWorkpieceBlowingStatus = await PlcControl.tagControl.wholeDevice.IsOpenWorkpieceBlowingAsync();
                bool tempSystemInitFlagStatus = await PlcControl.tagControl.wholeDevice.IsCompletedSystemInitAsync();
                bool tempIsOpenWorkVacuumSwitchStatus = await PlcControl.tagControl.wholeDevice.IsOpenWorkVacuumSwitchAsync();
                bool tempIsRuningSpindle = await PlcControl.tagControl.wholeDevice.GetSpindleSpeedAsync() != 0;
                bool tempPanelStatus = CommonCheck.GetParamsStatus(DeviceKey.panelStatusKey);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (tempIsOpenOpticalFiberSensorBlowing != isOpenOpticalFiberSensorBlowing || firstJoin)
                    {
                        isOpenOpticalFiberSensorBlowing = tempIsOpenOpticalFiberSensorBlowing;
                        isSwitchOpen(isOpenOpticalFiberSensorBlowing, 1);
                    }
                    if (tempIsOpenOpticalFiberSensorBlowingWater != isOpenOpticalFiberSensorBlowingWater || firstJoin)
                    {
                        isOpenOpticalFiberSensorBlowingWater = tempIsOpenOpticalFiberSensorBlowingWater;
                        isSwitchOpen(isOpenOpticalFiberSensorBlowingWater, 2);
                    }
                    if (tempVacuumState != vacuumState || firstJoin)
                    {
                        vacuumState = tempVacuumState;
                        isSwitchOpen(tempVacuumState, 3);
                    }
                    if (cameraSecurityDoor != tempCameraSecurityDoor || firstJoin)
                    {
                        cameraSecurityDoor = tempCameraSecurityDoor;
                        isSwitchOpen(cameraSecurityDoor, 4);
                    }
                    if (tempSpindleCuttingWater != spindleCuttingWater || firstJoin)
                    {
                        spindleCuttingWater = tempSpindleCuttingWater;
                        isSwitchOpen(tempSpindleCuttingWater, 5);
                    }
                    if (tempSystemInitFlagStatus != systemInitFlagStatus || firstJoin)
                    {
                        systemInitFlagStatus = tempSystemInitFlagStatus;
                        GlobalParams.systemInitFlag = systemInitFlagStatus;
                        isSwitchOpen(tempSystemInitFlagStatus, 6);
                    }
                    if (tempCutSecurityDoor != cutSecurityDoor || firstJoin)
                    {
                        cutSecurityDoor = tempCutSecurityDoor;
                        isSwitchOpen(cutSecurityDoor, 7);
                    }
                    if (tempIsRuningSpindle != isRuningSpindle || firstJoin)
                    {
                        isRuningSpindle = tempIsRuningSpindle;
                        isSwitchOpen(isRuningSpindle, 8);
                    }
                    if (tempWorkpieceBlowingStatus != workpieceBlowingStatus || firstJoin)
                    {
                        workpieceBlowingStatus = tempWorkpieceBlowingStatus;
                        isSwitchOpen(tempWorkpieceBlowingStatus, 9);
                    }
                    if (tempIsOpenWorkVacuumSwitchStatus != isOpenWorkVacuumSwitchStatus || firstJoin)
                    {
                        isOpenWorkVacuumSwitchStatus = tempIsOpenWorkVacuumSwitchStatus;
                        isSwitchOpen(isOpenWorkVacuumSwitchStatus, 10);
                    }
                    if (tempPanelStatus != panelStatus || firstJoin)
                    {
                        panelStatus = tempPanelStatus;
                        isSwitchOpen(tempPanelStatus, 8004);
                    }
                    if (firstJoin)
                    {
                        firstJoin = false;
                    }
                });
                Thread.Sleep(500);
            }
        }

        /***
         * 开关状态显示
         * type true：开；false：关
         * code 当前按钮的编码
         * */

        public static void isSwitchOpen(bool type, int code)
        {
            ctViewModel.UpdateImage(type, code);
        }

        /// <summary>
        /// 设置显示类型
        /// </summary>
        /// <param name="type">0 操作菜单 1 方向操作菜单 2 自定义键盘</param>
        public void SetOperateShowType(int type)
        {
            // 延时释放触控设备
            Dispatcher.InvokeAsync(() =>
            {
                // 设置可见性
                OperateGrid.Visibility = type == 0 ? Visibility.Visible : Visibility.Collapsed;
                commonDirection.Visibility = type == 1 ? Visibility.Visible : Visibility.Collapsed;
                costomKeyboardGrid.Visibility = type == 2 ? Visibility.Visible : Visibility.Collapsed;
                OperateButtonListBox.Visibility = type == 3 ? Visibility.Visible : Visibility.Collapsed;
                // 设置触控响应
                OperateGrid.IsHitTestVisible = type == 0;
                commonDirection.IsHitTestVisible = type == 1;
                costomKeyboardGrid.IsHitTestVisible = type == 2;
                OperateButtonListBox.IsHitTestVisible = type == 3;
                // 设置 ZIndex
                Panel.SetZIndex(OperateGrid, type == 0 ? 1 : 0);
                Panel.SetZIndex(commonDirection, type == 1 ? 1 : 0);
                Panel.SetZIndex(costomKeyboardGrid, type == 2 ? 1 : 0);
                Panel.SetZIndex(OperateButtonListBox, type == 3 ? 1 : 0);
            }, System.Windows.Threading.DispatcherPriority.Background);
            //Task.Run(() =>
            //{
            //    Thread.Sleep(500);
            //    // 延时释放触控设备
            //    Dispatcher.InvokeAsync(() =>
            //    {
            //        // 设置触控响应
            //        OperateGrid.IsHitTestVisible = type == 0;
            //        commonDirection.IsHitTestVisible = type == 1;
            //        costomKeyboardGrid.IsHitTestVisible = type == 2;
            //        OperateButtonListBox.IsHitTestVisible = type == 3;

            //        // 设置 ZIndex
            //        Panel.SetZIndex(OperateGrid, type == 0 ? 1 : 0);
            //        Panel.SetZIndex(commonDirection, type == 1 ? 1 : 0);
            //        Panel.SetZIndex(costomKeyboardGrid, type == 2 ? 1 : 0);
            //        Panel.SetZIndex(OperateButtonListBox, type == 3 ? 1 : 0);
            //    }, System.Windows.Threading.DispatcherPriority.Background);
            //});
        }

        //动态创建多个菜单
        public void UpdateOperate(List<OperateBean> list)
        {
            var tempList = list.Where(bean => bean.Code == 6).ToList();
            if (tempList.Count == 0)
            {
                // 记录当前页面菜单
                GlobalParams.currentOperateBeanList = list;
                // 改变下面选中样式
                mainWindow.SetShortcutBtnStatus(false, false);
            }

            OperateGrid.Children.Clear();
            if (list.Count == 0)
            {
                return;
            }
            if (list.Count > 5) //两行
            {
                for (int row = 0; row < 2; row++)
                {
                    OperateBean bean;
                    if (row == 0)
                    {
                        for (int col = 0; col < 5; col++)
                        {
                            bean = list[col];
                            addOperateButton(row, col, bean);
                        }
                    }
                    else
                    {
                        for (int col = 0; col < list.Count - 5; col++)
                        {
                            bean = list[col + row + 4];
                            addOperateButton(row, col, bean);
                        }
                    }
                }
            }
            else
            {
                for (int col = 0; col < list.Count; col++)
                {
                    addOperateButton(0, col, list[col]);
                }
            }
        }

        private void addOperateButton(int row, int col, OperateBean bean)
        {
            //-1为占位空
            if (bean.Code == -1)
            {
                Label lbl = new Label();
                lbl.SetValue(Grid.RowProperty, row);
                lbl.SetValue(Grid.ColumnProperty, col);
                OperateGrid.Children.Add(lbl);
            }
            else
            {
                OperateButton operateButton = new OperateButton(bean);
                operateButton.Width = 268;
                operateButton.Height = 100;
                operateButton.SetValue(Grid.RowProperty, row);
                operateButton.SetValue(Grid.ColumnProperty, col);
                operateButton.OperateClicked += null;
                operateButton.OperateClicked += OperateButton_OperateClicked;

                operateButton.OperateonLeave += null;
                operateButton.OperateonLeave += OperateButton_OperateonLeave;
                operateButton.OperateonDown += null;
                operateButton.OperateonDown += OperateButton_OperateonDown;
                OperateGrid.Children.Add(operateButton);
            }
        }

        private void OperateButton_OperateonDown(object? sender, OperateBean e)
        {
            onDown?.Invoke(sender, e.Code);
        }

        private void OperateButton_OperateonLeave(object? sender, OperateBean e)
        {
            onLeave?.Invoke(sender, e.Code);
        }

        private async void OperateButton_OperateClicked(object? sender, OperateBean e)
        {
            if (!string.IsNullOrEmpty(e.PageUrl))
            {
                if (mainWindow.mainRegion.Content is not MQSemiAutomaticCuttingRun && mainWindow.mainRegion.Content is not MQSemiAutomaticCuttingStop)
                {
                    mainWindow.NavigateToPage(e.PageUrl);
                }
                else
                {
                    MaterialSnack("半自动切割运行/暂停中，无法切换页面！", SnackType.WARNING);
                }
                return;
            }

            //按钮点击事件
            switch (e.Code)
            {
                case 1:
                    await OpticalFiberSensorBlowingAsync();
                    break;

                case 2:
                    await OpticalFiberSensorBlowingWaterAsync();
                    break;

                case 3:
                    if (!GlobalParams.OnlineFlag)
                    {
                        return;
                    }
                    if (SemiAutoCutService.Instance.IsRuning)
                    {
                        MaterialSnack("半自动切割运行中，无法操作CT真空！", SnackType.WARNING);
                        return;
                    }
                    var operationParameter = await CurrentUtils.GetOperationParametersModelAsync();
                    if (!operationParameter.IsAutoShutOffWaterWhenCuttingCompleted && operationParameter.IsAutoShutOffWaterWhenCloseVacuum)
                    {
                        await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
                    }
                    if (SemiAutoCutService.Instance.HasNotTakenOutWorkpiecesAfterCuttingCompleted)
                    {
                        await AutoCutUtils.ReplaceWaferAsync(default, TaskUtils.GetTimeoutCancellationToken(TimeSpan.FromSeconds(120)).Token);
                    }
                    // CT 真空
                    await VacuumOperateAsync();
                    break;

                case 4:
                    await OperateCameraSecurityDoorAsync();
                    break;

                case 5:
                    if (!GlobalParams.OnlineFlag)
                    {
                        return;
                    }
                    if (SemiAutoCutService.Instance.IsRuning)
                    {
                        MaterialSnack("半自动切割运行中，无法操作切割水！", SnackType.WARNING);
                        return;
                    }
                    // 切割水
                    await CutWaterOperateAsync();
                    break;

                case 6:
                    if (!GlobalParams.OnlineFlag)
                    {
                        return;
                    }
                    if (SemiAutoCutService.Instance.IsRuning)
                    {
                        MaterialSnack("半自动切割运行中，无法初始化系统！", SnackType.WARNING);
                        return;
                    }
                    // 系统初始化
                    await SystemInitOperateAsync();
                    break;

                case 7:
                    if (!GlobalParams.OnlineFlag)
                    {
                        return;
                    }
                    if (SemiAutoCutService.Instance.IsRuning)
                    {
                        MaterialSnack("半自动切割运行中，无法操作相机镜头盖！", SnackType.WARNING);
                        return;
                    }
                    // 操作相机镜头盖
                    await OperateCutSecurityDoorAsync();
                    break;

                case 8:
                    if (!GlobalParams.OnlineFlag)
                    {
                        return;
                    }
                    if (SemiAutoCutService.Instance.IsRuning)
                    {
                        MaterialSnack("半自动切割运行中，无法操作主轴！", SnackType.WARNING);
                        return;
                    }
                    if (AlarmConfig.Instance.HasSpindleCoolingWaterAlarm())
                    {
                        MaterialSnack("主轴冷却水报警中，无法操作主轴！", SnackType.WARNING);
                        return;
                    }
                    await SpindleManuallyRunAsync();
                    break;

                case 9:
                    if (!GlobalParams.OnlineFlag)
                    {
                        return;
                    }
                    if (SemiAutoCutService.Instance.IsRuning)
                    {
                        MaterialSnack("半自动切割运行中，无法操作相机吹气！", SnackType.WARNING);
                        return;
                    }
                    // 相机吹气
                    await CameraBlowingOperateAsync();
                    break;

                case 10:
                    if (!GlobalParams.OnlineFlag)
                    {
                        return;
                    }
                    // 操作工作盘真空
                    await OperateWorkVacuumSwitchAsync();
                    break;

                case 11:
                    if (AlarmConfig.Instance.HasActiveErrorAlarm())
                    {
                        MaterialSnack("存在未处理报警，无法更换工件！", SnackType.WARNING);
                        return;
                    }
                    if (SemiAutoCutService.Instance.IsRuning)
                    {
                        MaterialSnack("半自动切割运行中，无法更换工件！", SnackType.WARNING);
                        return;
                    }
                    TimeoutToken timeoutToken = TaskUtils.GetTimeoutCancellationToken(TimeSpan.FromSeconds(120));
                    await AutoCutUtils.ReplaceWaferAsync(default, timeoutToken.Token);
                    break;

                case 5302:
                    // 弹出确认对话框
                    MessageBoxResult result = MessageBox.Show("确定要关机吗？", "关机确认", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        ProcessStartInfo psi = new ProcessStartInfo("shutdown", "/s /t 0")
                        {
                            UseShellExecute = true,
                            Verb = "runas" // 以管理员权限运行
                        };
                        Process.Start(psi);
                    }
                    break;

                default:
                    onClicked?.Invoke(this, e.Code);
                    break;
            }
        }

        // 设置事件处理器的方法
        public void SetOnClickedHandler(EventHandler<int> handler, EventHandler<int> leaveHandler, EventHandler<int> downHandler = null)
        {
            // 添加新的处理器
            onClicked = null;
            onClicked += handler;
            onLeave = null;
            onLeave += leaveHandler;
            onDown = null;
            onDown += downHandler;
        }

        //操作代理
        public event EventHandler<int> onClicked;

        public event EventHandler<int> onDown;

        public event EventHandler<int> onLeave;

        // CT 真空
        private async Task VacuumOperateAsync()
        {
            await PlcControl.tagControl.wholeDevice.TriggerVacuumSwitchAsync();
        }

        /// <summary>
        /// 切割水
        /// </summary>
        private async Task CutWaterOperateAsync()
        {
            await PlcControl.tagControl.wholeDevice.TriggerCuttingWaterAsync();
        }

        /// <summary>
        /// 光纤传感器吹气
        /// </summary>
        private async Task OpticalFiberSensorBlowingAsync()
        {
            if (await PlcControl.tagControl.bladeMantance.GetOpticalFiberSensorBlowingAsync())
            {
                await PlcControl.tagControl.bladeMantance.CloseOpticalFiberSensorBlowingAsync();
            }
            else
            {
                await PlcControl.tagControl.bladeMantance.OpenOpticalFiberSensorBlowingAsync();
            }
        }

        /// <summary>
        /// 光纤传感器吹水
        /// </summary>
        private async Task OpticalFiberSensorBlowingWaterAsync()
        {
            if (await PlcControl.tagControl.bladeMantance.GetOpticalFiberSensorBlowingWaterAsync())
            {
                await PlcControl.tagControl.bladeMantance.CloseOpticalFiberSensorBlowingWaterAsync();
            }
            else
            {
                await PlcControl.tagControl.bladeMantance.OpenOpticalFiberSensorBlowingWaterAsync();
            }
        }

        /// <summary>
        /// 系统初始化
        /// </summary>
        private async Task SystemInitOperateAsync()
        {
            await PlcControl.tagControl.wholeDevice.AlarmResetAsync();
            await PlcControl.tagControl.wholeDevice.AlarmResetAsync();
            await PlcControl.tagControl.wholeDevice.AlarmResetAsync();
            if (await PlcControl.tagControl.wholeDevice.IsSystemInitingAsync())
            {
                MaterialSnack("初始化中，请等待初始化完成！", SnackType.WARNING);
                return;
            }
            if (!await PlcControl.tagControl.wholeDevice.CanSystemInitAsync())
            {
                MaterialSnack("初始化未准备好！", SnackType.WARNING);
                return;
            }
            // 退出所有模式
            PlcControl.plc.exitAllModel();
            await PlcControl.tagControl.wholeDevice.SystemInitAsync();
            MaterialSnack("系统初始化中...", SnackType.SUCCESS, 0);
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
                await PlcControl.tagControl.wholeDevice.WaitSystemInitCompletedAsync(cts.Token);
                GlobalParams.systemInitFlag = true;
                MaterialSnack("系统初始化完成！", SnackType.SUCCESS);
            }
            catch (OperationCanceledException)
            {
                MaterialSnack("系统初始化过程超时，请检查系统状态!", SnackType.WARNING, 0);
            }
            catch (Exception ex)
            {
                MaterialSnack($"系统初始化时遇到其他错误: {ex.Message}", SnackType.WARNING, 0);
            }
        }

        /// <summary>
        /// 操作切割安全门
        /// </summary>
        private async Task OperateCutSecurityDoorAsync()
        {
            if (await PlcControl.tagControl.wholeDevice.GetCutSecurityDoorAddressAsync())
            {
                await PlcControl.tagControl.wholeDevice.CloseCutSecurityDoorAsync();
            }
            else
            {
                Task cameraSecurityDoorTask = PlcControl.tagControl.wholeDevice.LockCameraSecurityDoorAsync();
                Task cutSecurityDoorTask = PlcControl.tagControl.wholeDevice.OpenCutSecurityDoorAsync();
                await Task.WhenAll(cameraSecurityDoorTask, cutSecurityDoorTask);
            }
        }

        /// <summary>
        /// 操作相机安全门
        /// </summary>
        private async Task OperateCameraSecurityDoorAsync()
        {
            if (await PlcControl.tagControl.wholeDevice.GetCameraSecurityDoorAddressAsync())
            {
                await PlcControl.tagControl.wholeDevice.LockCameraSecurityDoorAsync();
            }
            else
            {
                Task cutSecurityDoorTask = PlcControl.tagControl.wholeDevice.CloseCutSecurityDoorAsync();
                Task cameraSecurityDoorTask = PlcControl.tagControl.wholeDevice.UnlockCameraSecurityDoorAsync();
                await Task.WhenAll(cameraSecurityDoorTask, cutSecurityDoorTask);
            }
        }

        /// <summary>
        /// 操作工作盘真空
        /// </summary>
        private async Task OperateWorkVacuumSwitchAsync()
        {
            await PlcControl.tagControl.wholeDevice.TriggerWorkVacuumSwitchAsync();
        }

        /// <summary>
        /// 相机吹气
        /// </summary>
        private async Task CameraBlowingOperateAsync()
        {
            if (await PlcControl.tagControl.wholeDevice.IsOpenWorkpieceBlowingAsync())
            {
                await PlcControl.tagControl.wholeDevice.CloseWorkpieceBlowingAsync();
            }
            else
            {
                await PlcControl.tagControl.wholeDevice.OpenWorkpieceBlowingAsync();
            }
        }

        private SemaphoreSlim _spindleSemaphore = new SemaphoreSlim(1, 1);

        private async Task SpindleManuallyRunAsync()
        {
            if (!await _spindleSemaphore.WaitAsync(TimeSpan.Zero))
            {
                MaterialSnack("主轴加减速中，请勿重复点击！", SnackType.WARNING);
                return;
            }
            try
            {
                if (await PlcControl.tagControl.wholeDevice.GetSpindleSpeedAsync() == 0)
                {
                    await PlcControl.tagControl.wholeDevice.StartSpindleAsync();
                }
                else
                {
                    await PlcControl.tagControl.wholeDevice.StopSpindleAsync();
                    await PlcControl.tagControl.wholeDevice.WaitSpindleSpeedToZeroAsync();
                }
            }
            finally
            {
                _spindleSemaphore.Release();
            }
        }
    }
}