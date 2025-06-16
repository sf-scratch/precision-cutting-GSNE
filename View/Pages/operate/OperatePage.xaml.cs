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
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.View.Pages.common;
using 精密切割系统.View.Pages.F4_BladeMaintenance;
using 精密切割系统.ViewModel;
using static 精密切割系统.Helpers.MaterialSnackUtils;

namespace 精密切割系统.View.Pages.operate
{
    /// <summary>
    /// OperatePage.xaml 的交互逻辑
    /// </summary>
    public partial class OperatePage : Page
    {
        static CtViewModel ctViewModel = new CtViewModel();
        public OperatePage()
        {
            InitializeComponent();
            this.DataContext = ctViewModel;
        }

        MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        bool btnRunFlag = false;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            List<KeyboardBtn> list = Tools.GetChildrenOfType<KeyboardBtn>(this);
            Debug.WriteLine("进入了哦！");
            list.ForEach(btn => {
                btn.KeyPressed -= btnClick;
                btn.KeyPressed += btnClick;
            });
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
            if (GlobalParams.onlineFlag)
            {
                Thread _thread = new Thread(ShowOperateBtn);
                _thread.IsBackground = true;
                _thread.Start();
            }
            SetLettersCase(upperFlag);
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
                bool tempPanelStatus = CommonCheck.GetParamsStatus(DeviceKey.panelStatusKey);
                Application.Current.Dispatcher.Invoke(() => {
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

        private bool upperFlag = true;

        public void btnClick(object sender, string key)
        {
            /*if (CommonCheck.CheckGlobalRunStatus())
            {
                return;
            }*/
            // 处理按下事件
            KeyboardBtn btn = (KeyboardBtn)sender;
            //  0 是字母  null 或者是1，则是数字或者字母
            if (btn.BtnType == null)
            {
                CustomKeyPress(btn.BtnValue);
            }
            else if (btn.BtnType.Equals("0"))
            {
                // 0 
                CustomKeyPress(btn.BtnValue);
            }
            else if (btn.BtnType.Equals("2"))
            {
                string sendKey = "";
                if (btn.BtnValue == "Shift")
                {
                    upperFlag = upperFlag ? false : true;
                    SetLettersCase(upperFlag);
                    sendKey = "capslock";
                }
                else if (btn.BtnValue == "Backtab")
                {
                    sendKey = "shift+tab";
                }
                else if (btn.BtnValue == "Home")
                {
                    sendKey = "ctrl+a";
                }
                else if (btn.BtnValue == "Tab")
                {
                    sendKey = "tab";
                }
                else if (btn.BtnValue == ".")
                {
                    sendKey = "dot";
                }
                else if (btn.BtnValue == "Del")
                {
                    sendKey = "del";
                }
                else if (btn.BtnValue == "+")
                {
                    sendKey = "plus";
                }
                else if (btn.BtnValue == "-")
                {
                    sendKey = "minus";
                }
                else if (btn.BtnValue == "Down")
                {
                    mainWindow.ShowKeyboardPage(0);
                }
                if (!string.IsNullOrEmpty(sendKey))
                {
                    CustomKeyPress(sendKey);
                }
            }
            else if (btn.BtnType.Equals("3"))
            {
                string sendKey = "";
                if ("↑".Equals(btn.BtnValue))
                {
                    sendKey = "up";
                }
                else if ("↓".Equals(btn.BtnValue))
                {
                    sendKey = "down";
                }
                else if ("←".Equals(btn.BtnValue))
                {
                    sendKey = "left";
                }
                else if ("→".Equals(btn.BtnValue))
                {
                    sendKey = "right";
                }
                if (btn.BtnValue != "")
                {
                    CustomKeyPress(sendKey);
                }
            }
        }

        private void CustomKeyPress(string key)
        {
            Task.Run(() =>  KeyboardSimulator.SimulateKeyPress(key) );
        }

        // caseType 大小写类型 0 大写 1 小写
        public void SetLettersCase(bool upperFlagValue)
        {
            List<KeyboardBtn> list = Tools.GetChildrenOfType<KeyboardBtn>(this);
            list.ForEach(btn =>
            {
                if ("0".Equals(btn.BtnType))
                {
                    string btnText = btn.BtnValue;
                    if (btnText != null && btnText.Length > 0)
                    {
                        btn.BtnValue = upperFlagValue ? btnText.ToUpper() : btnText.ToLower();
                    }
                }

            });
        }

        public static bool IsBetweenAandZ(char c)
        {
            return c >= 'a' && c <= 'z';
        }

        /***
         * 开关状态显示
         * type true：开；false：关
         * code 当前按钮的编码
         * */
        public static void isSwitchOpen(bool type,int code)
        {
            ctViewModel.UpdateImage(type, code);
        }

        /// <summary>
        /// 设置显示类型
        /// </summary>
        /// <param name="type">0 操作菜单 1 方向操作菜单 2 自定义键盘</param>
        public void SetOperateShowType(int type)
        {
            // 设置可见性
            OperateGrid.Visibility = type == 0 ? Visibility.Visible : Visibility.Collapsed;
            commonDirection.Visibility = type == 1 ? Visibility.Visible : Visibility.Collapsed;
            costomKeyboardGrid.Visibility = type == 2 ? Visibility.Visible : Visibility.Collapsed;
            OperateButtonListBox.Visibility = type == 3 ? Visibility.Visible : Visibility.Collapsed;

            Task.Run(() => {
                Thread.Sleep(500);
                // 延时释放触控设备
                Dispatcher.InvokeAsync(() =>
                {
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
                
            });
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
            if (list.Count==0)
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
            if (bean.Code==-1)
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
        CameraCommon cameraCommon;
        bool spindRunStatus = false;
        private async void OperateButton_OperateClicked(object? sender, OperateBean e)
        {
            if (!string.IsNullOrEmpty(e.PageUrl))
            {
                mainWindow.NavigateToPage(e.PageUrl);
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
                    // CT 真空
                    await VacuumOperateAsync();
                    break;
                case 4:
                    await OperateCameraSecurityDoorAsync();
                    break;
                case 5:
                    // 切割水
                    await CutWaterOperateAsync();
                    break;
                case 6:
                    if (!GlobalParams.onlineFlag)
                    {
                        return;
                    }
                    // 系统初始化
                    await SystemInitOperateAsync();
                    break;
                case 7:
                    if (!GlobalParams.onlineFlag)
                    {
                        return;
                    }
                    // 操作切割安全门
                    await OperateCutSecurityDoorAsync();
                    break;
                case 8:
                    if (!GlobalParams.onlineFlag)
                    {
                        return;
                    }
                    // 推拉门操作
                    await OperateCameraSecurityDoorAsync();
                    break;
                case 9:
                    // 相机吹气
                    await CameraBlowingOperateAsync();
                    break;
                case 10:
                    if (!GlobalParams.onlineFlag)
                    {
                        return;
                    }
                    // 操作工作盘真空
                    await OperateWorkVacuumSwitchAsync();
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

        private void ToBladeHeight(OperateButton operateBtn)
        {
            if (!GlobalParams.onlineFlag)
            {
                mainWindow.NavigateToPage("Pages/F4_BladeMaintenance/BmContactSetupConf");
                return;
            }
            // 测高
            if (CommonCheck.MlignStatusCheck())
            {
                // 新发送PLC进入模式，当模式进入成功后，跳转页面
                // 进入测高模式
                MaterialSnackUtils.MaterialSnack("进入测高模式中...", SnackType.WARNING, 0);
                PlcControl.tagControl.bladeMantance.RunBladeSetup(1);
                PlcControl.tagControl.wholeDevice.SetPanelButtonsStauts(1);
                GlobalParams.globalRunFlag = true;
                operateBtn.resetState = false;
                // 监听状态，如果模式准备完成，则跳转页面
                Task.Run(() =>
                {
                    bool flag = Tools.WaitForValue(DeviceKey.bladeMantanceStatusKey, 1);
                    GlobalParams.globalRunFlag = false;
                    if (flag)
                    {
                        mainWindow.NavigateToPage("Pages/F4_BladeMaintenance/BmContactSetupConf");
                    }
                    else
                    {
                        operateBtn.resetState = true;
                        MaterialSnackUtils.MaterialSnack("进入刀片测高失败！", SnackType.WARNING);
                    }
                });
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
            if (await PlcControl.tagControl.wholeDevice.IsOpenVacuumSwitchAsync())
            {
                await PlcControl.tagControl.wholeDevice.CloseVacuumSwitchAsync();
            }
            else
            {
                await PlcControl.tagControl.wholeDevice.OpenVacuumSwitchAsync();
            }
        }

        /// <summary>
        /// 切割水
        /// </summary>
        private async Task CutWaterOperateAsync()
        {
            if (await PlcControl.tagControl.wholeDevice.IsOpenSpindleCuttingWaterAsync())
            {
                await PlcControl.tagControl.wholeDevice.CloseCuttingWaterAsync();
            }
            else
            {
                await PlcControl.tagControl.wholeDevice.OpenCuttingWaterAsync();
            }
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
            if (await PlcControl.tagControl.wholeDevice.IsSystemInitingAsync())
            {
                MaterialSnackUtils.MaterialSnack("初始化中，请稍后再试！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            if (!await PlcControl.tagControl.wholeDevice.CanSystemInitAsync())
            {
                MaterialSnackUtils.MaterialSnack("初始化未准备好！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            // 退出所有模式
            PlcControl.plc.exitAllModel();
            await PlcControl.tagControl.wholeDevice.SystemInitAsync();
            MaterialSnackUtils.MaterialSnack("系统初始化中...", MaterialSnackUtils.SnackType.SUCCESS, 0);
            try
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(90));
                await PlcControl.tagControl.wholeDevice.WaitSystemInitCompletedAsync(cts.Token);
                GlobalParams.systemInitFlag = true;
                MaterialSnackUtils.MaterialSnack("系统初始化完成！", MaterialSnackUtils.SnackType.SUCCESS);
            }
            catch (OperationCanceledException)
            {
                MaterialSnackUtils.MaterialSnack("系统初始化过程超时，请检查系统状态!", MaterialSnackUtils.SnackType.WARNING, 0);
            }
            catch (Exception ex)
            {
                MaterialSnackUtils.MaterialSnack($"系统初始化时遇到其他错误: {ex.Message}", MaterialSnackUtils.SnackType.WARNING, 0);
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
                Task cameraSecurityDoorTask = PlcControl.tagControl.wholeDevice.CloseCameraSecurityDoorAsync();
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
                await PlcControl.tagControl.wholeDevice.CloseCameraSecurityDoorAsync();
            }
            else
            {
                Task cutSecurityDoorTask = PlcControl.tagControl.wholeDevice.CloseCutSecurityDoorAsync();
                Task cameraSecurityDoorTask = PlcControl.tagControl.wholeDevice.OpenCameraSecurityDoorAsync();
                await Task.WhenAll(cameraSecurityDoorTask, cutSecurityDoorTask);
            }
        }

        /// <summary>
        /// 操作工作盘真空
        /// </summary>
        private async Task OperateWorkVacuumSwitchAsync()
        {
            if (await PlcControl.tagControl.wholeDevice.IsOpenWorkVacuumSwitchAsync())
            {
                await PlcControl.tagControl.wholeDevice.CloseWorkVacuumSwitchAsync();
            }
            else
            {
                await PlcControl.tagControl.wholeDevice.OpenWorkVacuumSwitchAsync();
            }
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


        /// <summary>
        /// 更换工件
        /// </summary>
        private void ReplaceWorkpiece(bool vacuumFlag = false)
        {
            if (btnRunFlag)
            {
                return;
            }
            if (!CommonCheck.AxisReady(false) || CommonCheck.ModelRunCheck())
            {
                return;
            }
            string replacePositionX = "-63";
            string replacePositionY = "-85";
            string replacePositionZ1 = "0";
            string replacePositionZ2 = "0";
            float z1SafePosition = 5;
            float z2SafePosition = 5;
            int timeoutMilliseconds = 30000; // 超时时间为30秒
            Task.Run(async () =>
            {
                btnRunFlag = true;
                GlobalParams.globalRunFlag = true;
                var stopwatch = new Stopwatch();
                // 开始计时
                stopwatch.Start();
                // 移动Z1和Z2到位置
                PlcControl.tagControl.Z1axis.StartAbsolute("10", replacePositionZ1);
                PlcControl.tagControl.Z2axis.StartAbsolute("10", replacePositionZ2);

                // 等待z1和z2轴状态为done
                while (true)
                {
                    Thread.Sleep(2000);
                    bool z1ReadyFlag = Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.z1CurMotionStatusKey));
                    bool z2ReadyFlag = Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.z2CurMotionStatusKey));

                    if (!z1ReadyFlag && !z2ReadyFlag) break;
                    if (stopwatch.ElapsedMilliseconds > timeoutMilliseconds)
                    {
                        MaterialSnackUtils.MaterialSnack("Z1或Z2轴移动超时！", SnackType.ERROR);
                        GlobalParams.globalRunFlag = false;
                        return;
                    }
                }

                // 检查Z1和Z2位置是否安全
                float z1CurrentPosition = Tools.GetFloatStringValue(PlcControl.plc.GetPlcValueString(DeviceKey.z1CurLocationKey));
                float z2CurrentPosition = Tools.GetFloatStringValue(PlcControl.plc.GetPlcValueString(DeviceKey.z2CurLocationKey));
                if (z1CurrentPosition > z1SafePosition || z2CurrentPosition > z2SafePosition)
                {
                    MaterialSnackUtils.MaterialSnack("Z1或Z2不在安全位置，请重试！", SnackType.WARNING);
                    GlobalParams.globalRunFlag = false;
                    return;
                }


                // 移动X和Y轴、theta轴
                PlcControl.tagControl.Xaxis.StartAbsolute("150", replacePositionX);
                PlcControl.tagControl.Yaxis.StartAbsolute("30", replacePositionY);
                CurrentUtils.InitCutCh();
                // 等待X和Y轴状态为done
                while (true)
                {
                    bool xReadyFlag = Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.curMotionStatusKey));
                    bool yReadyFlag = Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.yCurMotionStatusKey));

                    if (!xReadyFlag && !yReadyFlag) break;

                    if (stopwatch.ElapsedMilliseconds > timeoutMilliseconds)
                    {
                        MaterialSnackUtils.MaterialSnack("X或Y轴移动超时！", SnackType.ERROR);
                        GlobalParams.globalRunFlag = false;
                        return;
                    }
                    Thread.Sleep(100);
                }
                // 打开推拉门
                // PlcControl.tagControl.wholeDevice.OperateSecurityDoor2(1);
                if (vacuumFlag)
                {
                    await VacuumOperateAsync();
                }
                // 停止计时
                stopwatch.Stop();
                GlobalParams.globalRunFlag = false;
                btnRunFlag = false;
            });
        }

        // 手动校准页面
        public void ManualAlignment()
        {
            // 切割相关业务 需要检查状态
            if (CommonCheck.CutStatusCheck())
            {
                mainWindow.NavigateToPage("Pages/F2_ManualOperation/MQManualAlignmentConf", "type=1");
            }
        }
        bool stopCutRunFlag = false;
        /// <summary>
        /// 切割停止
        /// </summary>
        private void StopCut()
        {
            if (stopCutRunFlag)
            {
                return;
            }
            stopCutRunFlag = true;
            GlobalParams.globalRunFlag = true;
            CutOperateUtils.buzzerTipFlag = false;
            CutOperateUtils.exitCut();
            Task.Run(() => {
                CutOperateUtils.MonitorCutStatusFalse();
                stopCutRunFlag = false;
            });
        }
        /// <summary>
        /// 自动对焦
        /// </summary>
        private void AutoFocus()
        {
            CommonOperate.GetInstance().AutoFocus(1, mainWindow);
        }

        //private OperateButton getOperateButton(string code)
        //{
        //    for (int i=0;i< OperateGrid.Children.Count;i++)
        //    {
        //        OperateButton bt = OperateGrid.Children[i] as OperateButton;
        //        if (bt.Name.EndsWith("code"))

        //        {
        //            return bt;
        //        }
        //    }
        //    return null;
        //}
    }
}