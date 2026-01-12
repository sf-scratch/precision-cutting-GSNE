using Emgu.CV.Dnn;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Threading;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.View.page.right;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.F7_ElectricSpark
{
    /// <summary>
    /// ESUserDefineDataConf.xaml 的交互逻辑
    /// </summary>
    public partial class ESUserDefineDataConf : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;

        public ESUserDefineDataConf()
        {
            InitializeComponent();
            mainWindow = Application.Current.MainWindow as MainWindow;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            List<UserDefineDataModel> list = SqlHelper.Table<UserDefineDataModel>().ToList();
            Debug.WriteLine(list.Count);
            if (list.Count > 0)
            {
                UserDefineDataViewModel viewModel = new UserDefineDataViewModel(await SqlHelper.GetOrCreateEntityAsync(() => new UserDefineDataModel()));
                viewModel.MachineId = list[0].MachineId;
                viewModel.SystemPassword = list[0].SystemPassword;
                viewModel.SystemPasswordTime = list[0].SystemPasswordTime;
                viewModel.AfterEdgeDressPos = list[0].AfterEdgeDressPos;
                viewModel.BladeExchangeYPos = list[0].BladeExchangeYPos;
                viewModel.HairlineAdjustLimit = list[0].HairlineAdjustLimit;
                viewModel.BlowTime = list[0].BlowTime;
                viewModel.WorkVacuumCheckTime = list[0].WorkVacuumCheckTime;
                viewModel.WaitTimeUntilEnergySavingMode = list[0].WaitTimeUntilEnergySavingMode;
                viewModel.Language = list[0].Language;
                viewModel.DeviceChangeCutSpeed = list[0].DeviceChangeCutSpeed;
                viewModel.SpeedChange = list[0].SpeedChange;
                viewModel.HeightChange = list[0].HeightChange;
                viewModel.ZAxisCutModel = list[0].ZAxisCutModel;
                viewModel.CutWorkCheckWhenAlignment = list[0].CutWorkCheckWhenAlignment;
                viewModel.ContinueAfterBladeUserLimitError = list[0].ContinueAfterBladeUserLimitError;
                viewModel.ProcessingAfterBladeUserLimitError = list[0].ProcessingAfterBladeUserLimitError;
                viewModel.BBDTiming = list[0].BBDTiming;
                viewModel.StopSpindleByBbd = list[0].StopSpindleByBbd;
                viewModel.HairlineAdjustment = list[0].HairlineAdjustment;
                viewModel.LightingAdjustment = list[0].LightingAdjustment;
                viewModel.BladeReplacementCheck = list[0].BladeReplacementCheck;
                viewModel.ZProcessingDataSelection = list[0].ZProcessingDataSelection;
                viewModel.AlignSelectionWhenSemiAutoCutting = list[0].AlignSelectionWhenSemiAutoCutting;
                viewModel.SpindleCenterPositionOffset = list[0].SpindleCenterPositionOffset;
                viewModel.WaterPumpOnTimer = list[0].WaterPumpOnTimer;
                viewModel.AtomizingNozzlePositionX = list[0].AtomizingNozzlePositionX;
                viewModel.AtomizingNozzlePositionY = list[0].AtomizingNozzlePositionY;
                viewModel.AxisToWorkingDiscDistance = Appsettings.AxisToWorkingDiscDistance?.ToString("F3") ?? string.Empty;
                viewModel.AdditionalMargin = Appsettings.AdditionalMargin?.ToString("F3") ?? string.Empty;
                viewModel.HorizontalStraighteningStroke = Appsettings.HorizontalStraighteningStroke?.ToString("F3") ?? string.Empty;
                viewModel.VerticalStraighteningStroke = Appsettings.VerticalStraighteningStroke?.ToString("F3") ?? string.Empty;
                viewModel.SafetyMarginZ1 = Appsettings.SafetyMarginZ1?.ToString("F3") ?? string.Empty;
                DataContext = viewModel;
            }
            else
            {
                DataContext = new UserDefineDataViewModel(await SqlHelper.GetOrCreateEntityAsync(() => new UserDefineDataModel()));
            }
            rightPage = mainWindow.rightFrame.Content as RightPage;
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            rightPage.btnSure.Visibility = Visibility.Visible;
            rightPage.btnSure.BackFlag = false;
            rightPage.btnSure.SetRightClickedHandler(Save);
            //底部操作按钮
            mainWindow.UpdateOperatePage(OperateData.GetTab53Operate(), OperatePage_onClicked);
            //如果是空或者小数位数不足-小数初始化为0
            initTbNumber();
        }

        public void initTbNumber()
        {
            List<InputTextBox> tbs = Tools.GetChildrenOfType<InputTextBox>(this);
            for (int i = 0; i < tbs.Count; i++)
            {
                tbs[i].initNumber();
            }
        }

        private void OperatePage_onClicked(object? sender, int e)
        {
            _ = this.BtnOnClicked(e);
        }

        private async Task BtnOnClicked(int code)
        {
            //日期设置
            if (code == 5300)
            {
                mainWindow.NavigateToPage("Pages\\F7_ElectricSpark\\ESUserDefineSysTime");
            }
            else if (code == 5301)
            {
                await PlcControl.tagControl.wholeDevice.TriggerWorkVacuumSwitchAsync();
            }
            else if (code == 2407)
            {
                // 暖机
                _ = WarmUpHelper.TriggerWarmUpAsync();
            }
            else if (code == 5302)
            {
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
            }
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            WarmUpHelper.StopWarmUp();
            mainWindow.NavigateToPage("MainMenu");
        }

        private void Save(object? sender, bool e)
        {
            var success = this.FormSuccess();
            if (!success)
            {
                MaterialSnack("数据异常", SnackType.ERROR);
                return;
            }
            List<UserDefineDataModel> list = SqlHelper.Table<UserDefineDataModel>().ToList();
            UserDefineDataViewModel viewModel = (UserDefineDataViewModel)this.DataContext;
            UserDefineDataModel model = new()
            {
                MachineId = viewModel.MachineId,
                SystemPassword = inputPassword.Password,
                SystemPasswordTime = viewModel.SystemPasswordTime,
                AfterEdgeDressPos = viewModel.AfterEdgeDressPos,
                BladeExchangeYPos = viewModel.BladeExchangeYPos,
                HairlineAdjustLimit = viewModel.HairlineAdjustLimit,
                BlowTime = viewModel.BlowTime,
                WorkVacuumCheckTime = viewModel.WorkVacuumCheckTime,
                WaitTimeUntilEnergySavingMode = viewModel.WaitTimeUntilEnergySavingMode,
                Language = viewModel.Language,
                DeviceChangeCutSpeed = viewModel.DeviceChangeCutSpeed,
                SpeedChange = viewModel.SpeedChange,
                HeightChange = viewModel.HeightChange, // 注意这里属性名是HeightCange，不是HeightChange
                ZAxisCutModel = viewModel.ZAxisCutModel,
                CutWorkCheckWhenAlignment = viewModel.CutWorkCheckWhenAlignment,
                ContinueAfterBladeUserLimitError = viewModel.ContinueAfterBladeUserLimitError,
                ProcessingAfterBladeUserLimitError = viewModel.ProcessingAfterBladeUserLimitError,
                BBDTiming = viewModel.BBDTiming,
                StopSpindleByBbd = viewModel.StopSpindleByBbd,
                HairlineAdjustment = viewModel.HairlineAdjustment,
                LightingAdjustment = viewModel.LightingAdjustment,
                BladeReplacementCheck = viewModel.BladeReplacementCheck,
                ZProcessingDataSelection = viewModel.ZProcessingDataSelection, // 注意这里属性名是ZPocessingDataSelection，不是ZProcessingDataSelection
                AlignSelectionWhenSemiAutoCutting = viewModel.AlignSelectionWhenSemiAutoCutting,
                SpindleCenterPositionOffset = viewModel.SpindleCenterPositionOffset,
                WaterPumpOnTimer = viewModel.WaterPumpOnTimer,
                AtomizingNozzlePositionX = viewModel.AtomizingNozzlePositionX,
                AtomizingNozzlePositionY = viewModel.AtomizingNozzlePositionY,
                WarmUpTime = viewModel.WarmUpTime,
                WarmUpEndX = viewModel.WarmUpEndX,
                WarmUpStartX = viewModel.WarmUpStartX,
                WarmUpEndY = viewModel.WarmUpEndY,
                WarmUpStartY = viewModel.WarmUpStartY,
            };
            Appsettings.AxisToWorkingDiscDistance = viewModel.AxisToWorkingDiscDistance.ToFloat();
            Appsettings.AdditionalMargin = viewModel.AdditionalMargin.ToFloat();
            Appsettings.HorizontalStraighteningStroke = viewModel.HorizontalStraighteningStroke.ToFloat();
            Appsettings.VerticalStraighteningStroke = viewModel.VerticalStraighteningStroke.ToFloat();
            Appsettings.SafetyMarginZ1 = viewModel.SafetyMarginZ1.ToFloat();
            if (list.Count > 0)
            {
                UserDefineDataModel originUserDefineData = list[0];
                // 如果密码为空，保持原密码不变
                if (string.IsNullOrEmpty(model.SystemPassword))
                {
                    model.SystemPassword = originUserDefineData.SystemPassword;
                }
                // 执行修改
                model.Id = originUserDefineData.Id;
                try
                {
                    SqlHelper.Update(model);
                    Debug.WriteLine("修改");
                    MaterialSnack("保存成功", SnackType.SUCCESS);
                }
                catch
                {
                    MaterialSnack("保存失败", SnackType.ERROR);
                }
            }
            else
            {
                // 执行新增
                try
                {
                    int result = SqlHelper.Add(model);
                    Debug.WriteLine("新增");
                    MaterialSnack("保存成功", SnackType.SUCCESS);
                }
                catch
                {
                    MaterialSnack("保存失败", SnackType.ERROR);
                }
            }
            Debug.WriteLine(viewModel.Language);
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
                tbs[i].RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
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
    }
}