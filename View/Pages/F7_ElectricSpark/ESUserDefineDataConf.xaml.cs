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
using 精密切割系统.Extensions;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
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
            UserDefineDataModel userDefineData = await SqlHelper.GetOrCreateEntityAsync(() => new UserDefineDataModel());
            UserDefineDataViewModel viewModel = new UserDefineDataViewModel(userDefineData);
            viewModel.AxisToWorkingDiscDistance = Appsettings.AxisToWorkingDiscDistance?.ToString("F3") ?? string.Empty;
            viewModel.AdditionalMargin = Appsettings.AdditionalMargin?.ToString("F3") ?? string.Empty;
            viewModel.HorizontalStraighteningStroke = Appsettings.HorizontalStraighteningStroke?.ToString("F3") ?? string.Empty;
            viewModel.VerticalStraighteningStroke = Appsettings.VerticalStraighteningStroke?.ToString("F3") ?? string.Empty;
            viewModel.SafetyMarginZ1 = Appsettings.SafetyMarginZ1?.ToString("F3") ?? string.Empty;
            DataContext = viewModel;
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
            else if (code == 2408)
            {
                if (await PlcControl.tagControl.wholeDevice.GetSpindleSpeedAsync() != 0)
                {
                    MaterialSnack("主轴完全停止后，再进行主轴方向切换", SnackType.WARNING);
                    return;
                }
                await PlcControl.tagControl.wholeDevice.TriggerSpindleDirection();
                MaterialSnack("主轴方向切换成功！", SnackType.SUCCESS);
            }
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            mainWindow.NavigateToPage("MainMenu");
        }

        private async void Save(object? sender, bool e)
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
    }
}