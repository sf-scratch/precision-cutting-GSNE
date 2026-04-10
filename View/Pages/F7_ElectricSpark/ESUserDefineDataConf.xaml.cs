using Emgu.CV.Dnn;
using MathNet.Numerics.RootFinding;
using Org.BouncyCastle.Asn1.Tsp;
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
using 精密切割系统.Extensions;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Utils;
using 精密切割系统.View.common;
using 精密切割系统.View.Controls;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.View.F7_ElectricSpark
{
    /// <summary>
    /// ESUserDefineDataConf.xaml 的交互逻辑
    /// </summary>
    public partial class ESUserDefineDataConf : Page
    {
        private readonly DispatcherTimer _timer;
        private int _clickCount = 0;

        public ESUserDefineDataConf()
        {
            InitializeComponent();
            // 初始化计时器
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500); // 500毫秒内连点5次
            _timer.Tick += Timer_Tick;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            NavigateUtils.ClearOperatePage();
            WindowLayout.RightPageButtons.Clear();
            WindowLayout.RightPageButtons.Add(ButtonParams.Sure(SaveAsync));
            WindowLayout.RightPageButtons.Add(ButtonParams.Back(() => NavigateUtils.NavigateToPage("MainMenu")));
            WindowLayout.OperatePageButtons.Clear();
            WindowLayout.OperatePageButtons.Add(ButtonParams.BlueButton("设置时日", "/Assets/icon/tab_5/tab_04.png", () => NavigateUtils.NavigateToPage("Pages\\F7_ElectricSpark\\ESUserDefineSysTime")));
            WindowLayout.OperatePageButtons.Add(ButtonParams.BlueButton("工作盘真空", "VacuumOutline", PlcControl.tagControl.wholeDevice.TriggerWorkVacuumSwitchAsync));
            WindowLayout.OperatePageButtons.Add(ButtonParams.BlueButton("暖机", "/Assets/icon/menu_2/menu_2_3_white.png", () => { _ = WarmUpHelper.TriggerWarmUpAsync(); }));
            WindowLayout.OperatePageButtons.Add(ButtonParams.BlueButton("精度确认", "AbTesting", PlcControl.tagControl.wholeDevice.TriggerAccuracyConfirmAsync, isOpenFunc: async () => !await PlcControl.tagControl.wholeDevice.IsOpenAccuracyConfirmAsync(), openOrCloseVisibility: Visibility.Visible));

            UserDefineDataModel userDefineData = await SqlHelper.GetOrCreateEntityAsync(() => new UserDefineDataModel());
            UserDefineDataViewModel viewModel = new UserDefineDataViewModel(userDefineData);
            viewModel.AxisToWorkingDiscDistance = Appsettings.AxisToWorkingDiscDistance?.ToString("F3") ?? string.Empty;
            viewModel.AdditionalMargin = Appsettings.AdditionalMargin?.ToString("F3") ?? string.Empty;
            viewModel.HorizontalStraighteningStroke = Appsettings.HorizontalStraighteningStroke?.ToString("F3") ?? string.Empty;
            viewModel.VerticalStraighteningStroke = Appsettings.VerticalStraighteningStroke?.ToString("F3") ?? string.Empty;
            viewModel.SafetyMarginZ1 = Appsettings.SafetyMarginZ1?.ToString("F3") ?? string.Empty;
            DataContext = viewModel;
            this.InitTbNumber();
        }

        private async Task SpindleDirectionSwitchingAsync()
        {
            if (await PlcControl.tagControl.wholeDevice.GetSpindleSpeedAsync() != 0)
            {
                MaterialSnack("主轴完全停止后，再进行主轴方向切换", SnackType.WARNING);
                return;
            }
            await PlcControl.tagControl.wholeDevice.TriggerSpindleDirection();
            MaterialSnack("主轴方向切换成功！", SnackType.SUCCESS);
        }

        private async Task SaveAsync()
        {
            if (this.HasFormError())
            {
                MaterialSnack("数据异常", SnackType.ERROR);
                return;
            }
            UserDefineDataModel originUserDefineData = await SqlHelper.GetOrCreateEntityAsync(() => new UserDefineDataModel());
            UserDefineDataViewModel viewModel = (UserDefineDataViewModel)this.DataContext;
            UserDefineDataModel model = viewModel.Model;
            model.SystemPassword = inputPassword.Password;
            Appsettings.AxisToWorkingDiscDistance = viewModel.AxisToWorkingDiscDistance.ToFloat();
            Appsettings.AdditionalMargin = viewModel.AdditionalMargin.ToFloat();
            Appsettings.HorizontalStraighteningStroke = viewModel.HorizontalStraighteningStroke.ToFloat();
            Appsettings.VerticalStraighteningStroke = viewModel.VerticalStraighteningStroke.ToFloat();
            Appsettings.SafetyMarginZ1 = viewModel.SafetyMarginZ1.ToFloat();
            CameraOperateUtils.DatumLineChangeStepRatio = (int)(viewModel.SingleAdjustmentBaselineLineWidth.ToFloat() / 0.001);
            // 如果密码为空，保持原密码不变
            if (string.IsNullOrEmpty(model.SystemPassword))
            {
                model.SystemPassword = originUserDefineData.SystemPassword;
            }
            try
            {
                // 轴最大速度
                await PlcControl.tagControl.Xaxis.SetMaxSpeedAsync(model.MaxSpeedX.ToFloat());
                await PlcControl.tagControl.Yaxis.SetMaxSpeedAsync(model.MaxSpeedY.ToFloat());
                await SqlHelper.UpdateAsync(model);
                MaterialSnack("保存成功", SnackType.SUCCESS);
            }
            catch (Exception ex)
            {
                MaterialSnack($"保存失败: {ex}", SnackType.ERROR);
            }
            NavigateUtils.ToOperateButton(OperatePage.OperateType.OperationMenu);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            // 超时未完成连点，重置计数
            _timer.Stop();
            _clickCount = 0;
        }

        private void Down()
        {
            _clickCount++;

            // 重置计时器
            _timer.Stop();
            _timer.Start();

            if (_clickCount >= 8)
            {
                _timer.Stop();
                WindowLayout.OperatePageButtons.Clear();
                WindowLayout.OperatePageButtons.Add(ButtonParams.BlueButton("设置时日", "/Assets/icon/tab_5/tab_04.png", () => NavigateUtils.NavigateToPage("Pages\\F7_ElectricSpark\\ESUserDefineSysTime")));
                WindowLayout.OperatePageButtons.Add(ButtonParams.BlueButton("工作盘真空", "VacuumOutline", PlcControl.tagControl.wholeDevice.TriggerWorkVacuumSwitchAsync));
                WindowLayout.OperatePageButtons.Add(ButtonParams.BlueButton("暖机", "/Assets/icon/menu_2/menu_2_3_white.png", () => { _ = WarmUpHelper.TriggerWarmUpAsync(); }));
                WindowLayout.OperatePageButtons.Add(ButtonParams.BlueButton("精度确认", "AbTesting", PlcControl.tagControl.wholeDevice.TriggerAccuracyConfirmAsync, isOpenFunc: PlcControl.tagControl.wholeDevice.IsOpenAccuracyConfirmAsync, openOrCloseVisibility: Visibility.Visible));
                WindowLayout.OperatePageButtons.Add(ButtonParams.BlueButton("主轴方向切换", "Update", SpindleDirectionSwitchingAsync));
                _clickCount = 0; // 重置计数
            }
        }

        private void labUserDefineData_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Down();
        }

        private void labUserDefineData_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Down();
        }

        private void labUserDefineData_TouchDown(object sender, TouchEventArgs e)
        {
            Down();
        }
    }
}