using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;
using static 精密切割系统.Helpers.MaterialSnackUtils;

namespace 精密切割系统.View.Pages.F8_ElectricalDischargeTruing
{
    /// <summary>
    /// ElectricalDischargeTruing.xaml 的交互逻辑
    /// </summary>
    public partial class ElectricalDischargeTruing : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;

        // 运行状态 0 未运行 1 运行中 2 暂停中
        static int _runFlag = 0;
        ElectricalDischargeTruingViewModel model;

        public ElectricalDischargeTruing()
        {
            InitializeComponent();
            
            mainWindow = Application.Current.MainWindow as MainWindow;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            rightPage = mainWindow.rightFrame.Content as RightPage;
            operatePage = mainWindow.operateFrame.Content as OperatePage;
            mainWindow.UpdateOperatePage(OperateData.GetElectricalDischargeTruingOperate(), OperatePage_onClicked);
            // 设置结束修刀可以操作
            if (!GlobalParams.globalRunEnableOperateBtnCodes.Contains(8001))
            {
                GlobalParams.globalRunEnableOperateBtnCodes.Add(8001);
            }

            rightPage.PanelAction.Visibility = Visibility.Visible;

            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.SetRightClickedHandler(BtnElectricalBack_RightClicked);

            rightPage.btnSure.Visibility = Visibility.Visible;
            rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked);

            rightPage.btnElectricalStart.Visibility = Visibility.Visible;
            rightPage.btnElectricalStart.SetRightClickedHandler(BtnElectricalStart_RightClicked);

            rightPage.btnElectricalPause.Visibility = Visibility.Collapsed;
            rightPage.btnElectricalPause.GlobalRunOperateFlag = true;
            rightPage.btnElectricalPause.SetRightClickedHandler(BtnElectricalPause_RightClicked);

            OperatePage.isSwitchOpen(true, 8004);
            PlcControl.tagControl.wholeDevice.SetPanelButtonsStauts(1);
            List<ElectricalDischargeTruingModel> list = SqlHelper.Table<ElectricalDischargeTruingModel>().ToList();
            if (list.Count > 0)
            {
                model = new ElectricalDischargeTruingViewModel();
                Debug.WriteLine(list[0].XInitLocation);
                model.XInitLocation = list[0].XInitLocation;
                model.YBladeFrontLocation = list[0].YBladeFrontLocation;
                model.YBladeBackLocation = list[0].YBladeBackLocation;
                model.ZSetPosition = list[0].ZSetPosition;
                model.BladeAngle = list[0].BladeAngle;
                model.X0BasePosition = list[0].X0BasePosition;
                model.Y0BasePosition = list[0].Y0BasePosition;
                model.Z0BasePosition = list[0].Z0BasePosition;
                model.ZCuttingAmount = list[0].ZCuttingAmount;
                model.RepeatCount = list[0].RepeatCount;
                model.ElectrodePolaritySetting = list[0].ElectrodePolaritySetting;
                model.BladeCorrectionSpeed = list[0].BladeCorrectionSpeed;
                model.SpindleSpeed = list[0].SpindleSpeed;
                model.BladeThickness = list[0].BladeThickness;
                model.ElectrodeThickness = list[0].ElectrodeThickness;
                model.YOffsetAmount = list[0].YOffsetAmount;
                model.YFloatingAmount = list[0].YFloatingAmount;
                model.ZLimitPosition = list[0].ZLimitPosition;
                model.ElectrodeAngle = list[0].ElectrodeAngle;
                model.ClearDressersNum = GlobalParams.clearDressersNum;
                model.AllDressersNum = GlobalParams.allDressersNum;
                Debug.WriteLine("model.XInitLocation===" + model.XInitLocation);
                DataContext = model;
            }
            else
            {
                model = new ElectricalDischargeTruingViewModel();
                DataContext = model;
                ElectricalDischargeTruingModel s_model = new ElectricalDischargeTruingModel
                {
                    XInitLocation = model.XInitLocation,
                    YBladeFrontLocation = model.YBladeFrontLocation,
                    YBladeBackLocation = model.YBladeBackLocation,
                    ZSetPosition = model.ZSetPosition,
                    BladeAngle = model.BladeAngle,
                    X0BasePosition = model.X0BasePosition,
                    Y0BasePosition = model.Y0BasePosition,
                    Z0BasePosition = model.Z0BasePosition,
                    ZCuttingAmount = model.ZCuttingAmount,
                    RepeatCount = model.RepeatCount,
                    ElectrodePolaritySetting = model.ElectrodePolaritySetting,
                    BladeCorrectionSpeed = model.BladeCorrectionSpeed,
                    SpindleSpeed = model.SpindleSpeed,
                    BladeThickness = model.BladeThickness,
                    ElectrodeThickness = model.ElectrodeThickness,
                    YOffsetAmount = model.YOffsetAmount,
                    YFloatingAmount = model.YFloatingAmount,
                    ZLimitPosition = model.ZLimitPosition,
                    ElectrodeAngle = model.ElectrodeAngle
                };
                int result = SqlHelper.Add(s_model);
            }

            //如果是空或者小数位数不足-小数初始化为0
            initTbNumber();
            MaterialSnack("进入电火花修刀模式成功！", SnackType.WARNING);
            // 初始化参数
            _runFlag = 0;
        }

        private void BtnElectricalBack_RightClicked(object? sender, bool e)
        {
            GlobalParams.globalRunFlag = true;
            // 退出修刀模式
            PlcControl.tagControl.sparkRepairKnife.EnterElectrical(0);
            PlcControl.tagControl.wholeDevice.SetPanelButtonsStauts(0);
            GlobalParams.globalRunFlag = false;
            mainWindow.NavigateToPage("MainMenu");
        }
        /// <summary>
        /// 暂停修刀
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnElectricalPause_RightClicked(object? sender, bool e)
        {
            string status = PlcControl.plc.GetPlcValueString(DeviceKey.electricalStatusKey);
            if (stopRunFlag)
            {
                return;
            }
            if (_runFlag != 1 && Tools.TrueFlag(status))
            {
                return;
            }
            _runFlag = 2;
            // 暂停修刀
            PlcControl.tagControl.sparkRepairKnife.ToggleKnifeSharpening(1);
            MaterialSnack("正在暂停修刀...", SnackType.WARNING, 0);
            GlobalParams.globalRunFlag = true;
            // 监听状态，如果已停止，则改变状态和提示
            Task.Run(() =>
            {
                Tools.WaitForValue(DeviceKey.electricalStatusKey, 0);
                MaterialSnack("暂停中...", SnackType.WARNING, 0);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    rightPage.btnElectricalStart.Visibility = Visibility.Visible;
                    rightPage.btnElectricalStart.GlobalRunOperateFlag = true;
                    rightPage.btnElectricalPause.Visibility = Visibility.Collapsed;
                    // 设置输入框状态为可输入
                    SetInputTextBoxEnable(true);
                });
                GlobalParams.globalRunFlag = false;
                startRunFlag = false;
                stopRunFlag = false;
            });
        }
        bool startRunFlag = false;
        private void BtnElectricalStart_RightClicked(object? sender, bool e)
        {
            // 判断是否已点击过
            if (stopRunFlag || startRunFlag)
            {
                MaterialSnack("重复点击！", SnackType.WARNING);
                return;
            }
            startRunFlag = true;
            btnStart();
        }
        bool stopRunFlag = false;
        private void OperatePage_onClicked(object? sender, int code)
        {
            switch (code)
            {
                case 8001:
                    if (_runFlag == 0 && (stopRunFlag || !startRunFlag) )
                    {
                        return;
                    }
                    stopRunFlag = true;
                    // 结束修刀
                    PlcControl.tagControl.sparkRepairKnife.ToggleKnifeSharpening(2);
                    MaterialSnack("正在停止修刀...", SnackType.WARNING, 0);
                    Task.Run(() =>
                    {
                        Tools.WaitForValue(DeviceKey.electricalStatusKey, 0);
                        MaterialSnack("停止修刀！", SnackType.WARNING);
                        Finish();
                    });
                    break;
                case 8002:
                    if (_runFlag != 0)
                    {
                        return;
                    }
                    // 确认Y轴前端位置
                    if (model == null)
                    {
                        return;
                    }
                    // 获取Y轴当前位置
                    string yFrontLocation = PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey);
                    model.YBladeFrontLocation = Tools.FormatDecimalString(yFrontLocation, 4);
                    // 移动Z轴到初始位置
                    PlcControl.tagControl.Z1axis.StartAbsolute( "10", model.ZSetPosition);
                    save();
                    break;
                case 8003:
                    if (_runFlag != 0)
                    {
                        return;
                    }
                    // 确认Y轴前端位置
                    if (model == null)
                    {
                        return;
                    }
                    // 确认Y轴后端位置
                    // 获取Y轴当前位置
                    string yBackLocation = PlcControl.plc.GetPlcValueString(DeviceKey.yCurLocationKey);
                    model.YBladeBackLocation = Tools.FormatDecimalString(yBackLocation, 4);
                    Debug.WriteLine("model.YBladeBackLocation:" + model.YBladeBackLocation);
                    // 移动Z轴到初始位置
                    PlcControl.tagControl.Z1axis.StartAbsolute( "10", model.ZSetPosition);

                    save();

                    break;
                case 8004:
                    if (_runFlag != 0)
                    {
                        return;
                    }
                    // 启用 禁用面板按钮
                    bool panelStatus = CommonCheck.GetParamsStatus(DeviceKey.panelStatusKey);
                    PlcControl.tagControl.wholeDevice.SetPanelButtonsStauts(panelStatus ? 0 : 1);
                    OperatePage.isSwitchOpen(!panelStatus, 8004);
                    break;
                case 8005:
                    if (_runFlag != 0)
                    {
                        return;
                    }
                    GlobalParams.clearDressersNum = 0;
                    model.ClearDressersNum = 0;
                    MaterialSnack("清零成功！", SnackType.SUCCESS);
                    break;
                case 8006:
                    if (_runFlag != 0)
                    {
                        return;
                    }
                    string zCurrentLocation = PlcControl.plc.GetPlcValueString(DeviceKey.z1CurLocationKey);
                    model.Z0BasePosition = Tools.FormatDecimalString(zCurrentLocation, 4);
                    // 移动Z轴刀初始位置
                    PlcControl.tagControl.Z1axis.StartAbsolute("10", model.ZSetPosition);
                    save();
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// 检查异常状态 急停后，要全部重新标定一次
        /// </summary>
        /// <returns></returns>
        private void CheckError()
        {
            Thread thread = new Thread(() =>
            {
                while (_runFlag == 0)
                {
                    if (AlarmConfig.Instance.HasActiveErrorAlarm())
                    {
                        Finish();
                    }
                    Thread.Sleep(100);
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
        private void BtnSure_RightClicked(object sender, bool e)
        {
            //执行数据库数据保存。
            var success = this.FormSuccess();
            if (success)
            {
                save();
            }
            else
            {
                MaterialSnack("数据异常", SnackType.ERROR);
            }
        }
        int tipsFlag = 0;
        private void btnStart()
        {
            // 如果有异常，则不能进行操作
            if (AlarmConfig.Instance.HasActiveErrorAlarm())
            {
                GlobalParams.globalRunFlag = false;
                startRunFlag = false;
                return;
            }

            //执行数据合法行验证
            var isError = this.FormError();
            if (isError)
            {
                MaterialSnack("数据异常", SnackType.ERROR);
                GlobalParams.globalRunFlag = false;
                startRunFlag = false;
                return;
            }
            // 判断是否是运行中
            string status = PlcControl.plc.GetPlcValueString(DeviceKey.electricalStatusKey);
            if (_runFlag == 1 || Tools.TrueFlag(status))
            {
                GlobalParams.globalRunFlag = false;
                startRunFlag = false;
                return;
            }            
            // 判断状态是否准备好
            if (!CommonCheck.TruingStatusCheck())
            {
                GlobalParams.globalRunFlag = false;
                startRunFlag = false;
                return;
            }
            if (_runFlag == 2)
            {
                _runFlag = 1;
                rightPage.btnElectricalStart.Visibility = Visibility.Collapsed;
                rightPage.btnElectricalPause.Visibility = Visibility.Visible;
                // 重新开始
                PlcControl.tagControl.sparkRepairKnife.ToggleKnifeSharpening(0);
                PlcControl.tagControl.wholeDevice.SetPanelButtonsStauts(0);
                GlobalParams.globalRunFlag = true;
                SetInputTextBoxEnable(false);
                MaterialSnack("修刀中...", SnackType.WARNING, 0);
                return;
            }
            // 校验参数
            if (model == null)
            {
                GlobalParams.globalRunFlag = false;
                startRunFlag = false;
                return;
            }
            if (model.YBladeFrontLocation == null) { return; }
            if (model.YBladeBackLocation == null) { return; }
            if (model.BladeAngle == null) { return; }
            if (model.X0BasePosition == null) { return; }
            if (model.Y0BasePosition == null) { return; }
            if (model.Z0BasePosition == null) { return; }
            if (model.ZCuttingAmount == null) { return; }
            if (model.RepeatCount == 0) { return; }
            if (model.BladeCorrectionSpeed == null) { return; }
            if (model.SpindleSpeed == null) { return; }
            if (model.BladeThickness == null) { return; }
            if (model.ElectrodeThickness == null) { return; }
            if (model.YOffsetAmount == null) { return; }
            if (model.YFloatingAmount == null) { return; }
            if (model.ZLimitPosition == null) { return; }
            if (model.ElectrodeAngle == null) { return; }

            // 提示确认位置
            if (tipsFlag == 0)
            {
                tipsFlag = 1;
                GlobalParams.globalRunFlag = false;
                startRunFlag = false;
                MaterialSnack("请确认Y轴前端位置、Y轴后端位置、Z0轴基准位置是否正确，再次点击开始修刀按钮将进行修刀操作。", SnackType.WARNING);
                return;
            }
            tipsFlag = 0;
            // 计算A点开始和结束位置
            // 获取y轴前后的位置
            double yBladeFrontLocation = double.Parse(model.YBladeFrontLocation);
            double yBladeBackLocation = double.Parse(model.YBladeBackLocation);
            // 获取Y轴偏移量
            double YOffsetAmount = double.Parse(model.YOffsetAmount);
            // 获取Y轴两边的值
            double y0 = yBladeFrontLocation + YOffsetAmount;
            double y1 = yBladeBackLocation - YOffsetAmount;

            // 获取Z轴初始位置
            double ZSetPosition = double.Parse(model.ZSetPosition);
            // 获取刀片角度
            double bladeAngle = (180 - double.Parse(model.BladeAngle)) / 2;
            // 获取Z0的位置 41.321 - 5 

            // 根据y0+z轴初始位置和角度，计算z0坐标
            double[] A = { y0, ZSetPosition };
            double[] B = { y1, ZSetPosition };
            // z0的位置
            double[] endZ1 = TriangleAngles.CalculatePointC(A, B, bladeAngle);
            double z0 = 41.321;
            // z轴结束位置-z轴开始位置
            double zRunDistance = endZ1[1] - ZSetPosition;
            // 轴开始位置 = z0 + zRunDistance + 修刀浮动量  129.641  127.857
            double tempValue = z0 + zRunDistance + double.Parse(model.YFloatingAmount);
            // 结束位置 = ZSetPosition + (z0的位置-zSetPosition) * 2
            double endZPostion = ZSetPosition + (zRunDistance * 2);
            // 另外一中算法
            // double tempEndZPostion = TriangleAngles.CalculateBXCoordinate(ZSetPosition, y1, y0, double.Parse(model.BladeAngle) / 2);
            // 新的算法：Z轴开始和结束位置 = Z0BasePosition - zRunDistance 结束位置 = Z0BasePosition + zRunDistance
            double tempZ0BasePosition = double.Parse(model.Z0BasePosition);
            double newZStartPosition = tempZ0BasePosition - zRunDistance;
            double newZEndPosition = tempZ0BasePosition + zRunDistance;

            // 由于实际修出来角度大了，所以手动减少y0和y1的距离
            y0 += -0.070;
            y1 += 0.070;
            // 设置修刀参数 131.9917 129.9117  40.959   132.0000  131.970  129.9000 129.870
            PlcControl.tagControl.sparkRepairKnife.SetParams(newZStartPosition, newZEndPosition, y0, y1, model.X0BasePosition
                , model.ZCuttingAmount, model.RepeatCount, model.BladeCorrectionSpeed, model.SpindleSpeed, model.ZLimitPosition);

            /*PlcControl.tagControl.sparkRepairKnife.SetParams(ZSetPosition, endZPostion, y0, y1, model.X0BasePosition
                , model.ZCuttingAmount, model.RepeatCount, model.BladeCorrectionSpeed, model.SpindleSpeed, model.ZLimitPosition);*/
            // 启动修刀
            PlcControl.tagControl.sparkRepairKnife.ToggleKnifeSharpening(0);
            GlobalParams.globalRunFlag = true;
            PlcControl.tagControl.wholeDevice.SetPanelButtonsStauts(0);
            rightPage.btnElectricalStart.Visibility = Visibility.Collapsed;
            rightPage.btnElectricalPause.Visibility = Visibility.Visible;
            SetInputTextBoxEnable(false);
            CheckError();
            MaterialSnack("修刀中...", SnackType.WARNING, 0);
            _runFlag = 1;
            Thread _thread = new Thread(new ThreadStart(checkStatus));
            _thread.IsBackground = true;
            _thread.Start();
        }
        /// <summary>
        /// 检查修刀进度
        /// </summary>
        public void checkStatus()
        {
            String repeatCount = model.RepeatCount.ToString();
            String currentCountStr = PlcControl.plc.GetPlcValueString(DeviceKey.currentCountKey);
            while (!repeatCount.Equals(currentCountStr) && _runFlag != 0)
            {
                if (AlarmConfig.Instance.HasActiveErrorAlarm()) 
                {
                    _runFlag = 0;
                    break;
                }
                currentCountStr = PlcControl.plc.GetPlcValueString(DeviceKey.currentCountKey);
                if (currentCountStr == null)
                {
                    currentCountStr = model.RepeatCount.ToString();
                    MaterialSnack("获取当前刀数异常！", SnackType.ERROR);
                }
                else
                {
                    int currentCountNum = int.Parse(currentCountStr);
                    if (currentCountNum != model.CurrentRepairNum)
                    {
                        GlobalParams.allDressersNum += 1;
                        GlobalParams.clearDressersNum += 1;
                        model.CurrentRepairNum = currentCountNum;
                        model.ClearDressersNum = GlobalParams.clearDressersNum;
                        model.AllDressersNum = GlobalParams.allDressersNum;
                    }
                    
                }
                Thread.Sleep(100);
            }
            Finish();

        }
        
        public void Finish()
        {
            startRunFlag = false;
            stopRunFlag = false;
            _runFlag = 0;
            SetInputTextBoxEnable(true);
            // 自动计算z0下次开始位置
            if (model.CurrentRepairNum > 0)
            {
                double zAmount = double.Parse(model.ZCuttingAmount);
                // 如果大于0 则根据修刀次数 * 每次下降量
                double sumDistance = model.CurrentRepairNum * zAmount;
                
                model.Z0BasePosition = Tools.FormatDecimalString((Tools.GetDoubleStringValue(model.Z0BasePosition) + sumDistance) + "", 3);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    save(false);
                });
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                rightPage.btnElectricalStart.Visibility = Visibility.Visible;
                rightPage.btnElectricalPause.Visibility = Visibility.Collapsed;
            });
            GlobalParams.globalRunFlag = false;
            PlcControl.tagControl.wholeDevice.SetPanelButtonsStauts(1);
            MaterialSnack("修刀完成！！", SnackType.SUCCESS);
        }
        private void save(bool showTips = true)
        {
            ElectricalDischargeTruingViewModel viewModel = (ElectricalDischargeTruingViewModel)this.DataContext; // 获取当前窗口的DataContext
            List<ElectricalDischargeTruingModel> list = SqlHelper.Table<ElectricalDischargeTruingModel>().ToList();
            ElectricalDischargeTruingModel model = new ElectricalDischargeTruingModel
            {
                XInitLocation = viewModel.XInitLocation,
                YBladeFrontLocation = viewModel.YBladeFrontLocation,
                YBladeBackLocation = viewModel.YBladeBackLocation,
                ZSetPosition = viewModel.ZSetPosition,
                BladeAngle = viewModel.BladeAngle,
                X0BasePosition = viewModel.X0BasePosition,
                Y0BasePosition = viewModel.Y0BasePosition,
                Z0BasePosition = viewModel.Z0BasePosition,
                ZCuttingAmount = viewModel.ZCuttingAmount,
                RepeatCount = viewModel.RepeatCount,
                ElectrodePolaritySetting = viewModel.ElectrodePolaritySetting,
                BladeCorrectionSpeed = viewModel.BladeCorrectionSpeed,
                SpindleSpeed = viewModel.SpindleSpeed,
                BladeThickness = viewModel.BladeThickness,
                ElectrodeThickness = viewModel.ElectrodeThickness,
                YOffsetAmount = viewModel.YOffsetAmount,
                YFloatingAmount = viewModel.YFloatingAmount,
                ZLimitPosition = viewModel.ZLimitPosition,
                ElectrodeAngle = viewModel.ElectrodeAngle
            };
            Debug.WriteLine(viewModel.BladeCorrectionSpeed);
            // 保存之前校验是否已有数据，已有就做修改操作
            if (list.Count > 0)
            {
                model.Id = list[0].Id;
                try
                {
                    int result = SqlHelper.Update(model);
                    if (showTips)
                    {
                        MaterialSnack("保存成功", SnackType.SUCCESS);
                    }
                    Debug.WriteLine("修改==" + result);
                }
                catch
                {
                    if (showTips)
                    {
                        MaterialSnack("保存失败", SnackType.ERROR);
                    }
                }
            }
            else
            {
                try
                {
                    int result = SqlHelper.Add(model);
                    if (showTips)
                    {
                        MaterialSnack("保存成功", SnackType.SUCCESS);
                    }
                    Debug.WriteLine("新增==" + result);
                }
                catch
                {
                    if (showTips)
                    {
                        MaterialSnack("保存失败", SnackType.ERROR);
                    }
                }
            }
        }
        /// <summary>
        /// 设置单元格状态
        /// </summary>
        /// <param name="status"></param>
        public void SetInputTextBoxEnable(bool status)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                List<InputTextBox> inputTextBoxes = Tools.GetChildrenOfType<InputTextBox>(this);
                inputTextBoxes.ForEach(inputTextBox =>
                {
                    if (!inputTextBox.Name.Equals("currentRepairNumInput") && !inputTextBox.Name.Equals("repeatCountInput")
                        && !inputTextBox.Name.Equals("allDressersNumInput") && !inputTextBox.Name.Equals("clearDressersNumInput"))
                    {
                        inputTextBox.IsEnabled = status;
                    }
                });
            });
        }

        public void initTbNumber()
        {
            List<InputTextBox> tbs = Tools.GetChildrenOfType<InputTextBox>(this);
            for (int i = 0; i < tbs.Count; i++)
            {
                tbs[i].initNumber();
            }
        }

        /// <summary>
        /// 表单内容是否错误  false是正常 true是出错了
        /// </summary>
        /// <returns>false表示没有错误，true表示出错了</returns>
        public bool FormError()
        {
            bool result = false;
            List<InputTextBox> tbs = Tools.GetChildrenOfType<InputTextBox>(this);
            for (int i = 0; i < tbs.Count; i++)
            {
                bool isError = tbs[i].XIsError;
                if (isError)
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// 表单内容验证通过  false是不通过 true是通过
        /// </summary>
        /// <returns>false是不通过 true是通过</returns>
        public bool FormSuccess()
        {
            return !FormError();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            BtnElectricalBack_RightClicked(null, false);
        }
    }
}
