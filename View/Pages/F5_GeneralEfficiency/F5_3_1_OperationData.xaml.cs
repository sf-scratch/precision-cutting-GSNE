using System;
using System.Collections.Generic;
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
using 精密切割系统.database.db.modle;
using 精密切割系统.Entities;
using 精密切割系统.Helpers;
using 精密切割系统.Helpers.GTN;
using 精密切割系统.Utils;
using 精密切割系统.View.common;
using 精密切割系统.View.Controls;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages
{
    /// <summary>
    /// F5_3_1_FunctionData.xaml 的交互逻辑
    /// </summary>
    public partial class F5_3_1_FunctionData : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;

        private F5_3_1_OperationDataViewModel ViewModel { get; set; } = new F5_3_1_OperationDataViewModel();

        public F5_3_1_FunctionData()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            loadDBData();
        }

        public async void loadDBData()
        {
            mainWindow = Application.Current.MainWindow as MainWindow;
            rightPage = mainWindow.rightFrame.Content as RightPage;
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            rightPage.btnSure.Visibility = Visibility.Visible;
            rightPage.btnSure.BackFlag = false;
            rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked);

            var operationParameter = await CurrentUtils.GetOperationParametersModelAsync();
            ViewModel.operationParameter = operationParameter;
            var axisX = await SqlHelper.GetOrCreateEntityAsync(() => new AxisSettingEntity(), (long)AxisType.X);
            var axisY = await SqlHelper.GetOrCreateEntityAsync(() => new AxisSettingEntity(), (long)AxisType.Y);
            var axisZ1 = await SqlHelper.GetOrCreateEntityAsync(() => new AxisSettingEntity(), (long)AxisType.Z1);
            var axisZ2 = await SqlHelper.GetOrCreateEntityAsync(() => new AxisSettingEntity(), (long)AxisType.Z2);
            var axisTheta = await SqlHelper.GetOrCreateEntityAsync(() => new AxisSettingEntity(), (long)AxisType.Theta);
            ViewModel.PositiveLimitPositionX = axisX.PositiveSoftLimit;
            ViewModel.NegativeLimitPositionX = axisX.NegativeSoftLimit;
            ViewModel.PositiveLimitPositionY = axisY.PositiveSoftLimit;
            ViewModel.NegativeLimitPositionY = axisY.NegativeSoftLimit;
            ViewModel.PositiveLimitPositionZ1 = axisZ1.PositiveSoftLimit;
            ViewModel.NegativeLimitPositionZ1 = axisZ1.NegativeSoftLimit;
            ViewModel.PositiveLimitPositionZ2 = axisZ2.PositiveSoftLimit;
            ViewModel.NegativeLimitPositionZ2 = axisZ2.NegativeSoftLimit;
            ViewModel.PositiveLimitPositionTheta = axisTheta.PositiveSoftLimit;
            ViewModel.NegativeLimitPositionTheta = axisTheta.NegativeSoftLimit;
            DataContext = ViewModel;

            //如果是空或者小数位数不足-小数初始化为0
            initTbNumber();
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            mainWindow.NavigateToPage("MainMenu");
        }

        private async void BtnSure_RightClicked(object? sender, bool e)
        {
            //执行数据库数据保存。
            var success = this.FormSuccess();
            if (success)
            {
                MaterialSnack("操作中。。。", SnackType.WARNING);
                await SaveDataAsync();
                MaterialSnack("操作成功", SnackType.SUCCESS);
                //mainWindow.NavigateToPage("MainMenu");
                //mainWindow.mainFrame.Source = new Uri("View/Pages/F4_BladeMaintenance/BmSharpenParameter.xaml", UriKind.Relative);
            }
            else
            {
                MaterialSnack("数据异常", SnackType.ERROR);
            }
        }

        public async Task SaveDataAsync()
        {
            OperationParametersModel operationParameter = ViewModel.operationParameter;
            if (operationParameter != null)
            {
                operationParameter.ZStopAfterSeq = cbxZStopAfterSeq.Text;
                await SqlHelper.UpdateAsync(operationParameter);
                await CurrentUtils.InitAxisSpeedIndexAsync(operationParameter);
                // 设置Z轴补偿量
                GlobalParams.zAxisCompNum = ViewModel.zAxisCompNum;
                GlobalParams.zAxisCompValue = Tools.GetFloatStringValue(ViewModel.zAxisCompValue);
                var axisX = await SqlHelper.GetOrCreateEntityAsync(() => new AxisSettingEntity(), (long)AxisType.X);
                var axisY = await SqlHelper.GetOrCreateEntityAsync(() => new AxisSettingEntity(), (long)AxisType.Y);
                var axisZ1 = await SqlHelper.GetOrCreateEntityAsync(() => new AxisSettingEntity(), (long)AxisType.Z1);
                var axisZ2 = await SqlHelper.GetOrCreateEntityAsync(() => new AxisSettingEntity(), (long)AxisType.Z2);
                var axisTheta = await SqlHelper.GetOrCreateEntityAsync(() => new AxisSettingEntity(), (long)AxisType.Theta);
                axisX.PositiveSoftLimit = ViewModel.PositiveLimitPositionX;
                axisX.NegativeSoftLimit = ViewModel.NegativeLimitPositionX;
                axisY.PositiveSoftLimit = ViewModel.PositiveLimitPositionY;
                axisY.NegativeSoftLimit = ViewModel.NegativeLimitPositionY;
                axisZ1.PositiveSoftLimit = ViewModel.PositiveLimitPositionZ1;
                axisZ1.NegativeSoftLimit = ViewModel.NegativeLimitPositionZ1;
                axisZ2.PositiveSoftLimit = ViewModel.PositiveLimitPositionZ2;
                axisZ2.NegativeSoftLimit = ViewModel.NegativeLimitPositionZ2;
                axisTheta.PositiveSoftLimit = ViewModel.PositiveLimitPositionTheta;
                axisTheta.NegativeSoftLimit = ViewModel.NegativeLimitPositionTheta;
                await SqlHelper.UpdateAsync(axisX);
                await SqlHelper.UpdateAsync(axisY);
                await SqlHelper.UpdateAsync(axisZ1);
                await SqlHelper.UpdateAsync(axisZ2);
                await SqlHelper.UpdateAsync(axisTheta);
                await GsneMotion.Instance.Axis.SetAllAxisSoftLimit();
                //await AutoCutUtils.SetFunctionalParameters();
            }
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
                InputTextBox tb = tbs[i];
                tb.ValidationCheck();
                bool isError = tb.XIsError;
                if (isError)
                {
                    result = true;
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