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
using 精密切割系统.Helpers;
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

            var operationParameter = CurrentUtils.GetOperationParametersModel();
            if (operationParameter is not null)
            {
                ViewModel.operationParameter = operationParameter;
            }
            else
            {
                await SqlHelper.AddAsync(ViewModel.operationParameter);
            }
            ViewModel.PositiveLimitPositionX = (Appsettings.PositiveLimitPositionX ?? 0).ToString();
            ViewModel.NegativeLimitPositionX = (Appsettings.NegativeLimitPositionX ?? 0).ToString();
            ViewModel.PositiveLimitPositionY = (Appsettings.PositiveLimitPositionY ?? 0).ToString();
            ViewModel.NegativeLimitPositionY = (Appsettings.NegativeLimitPositionY ?? 0).ToString();
            ViewModel.PositiveLimitPositionZ1 = (Appsettings.PositiveLimitPositionZ1 ?? 0).ToString();
            ViewModel.NegativeLimitPositionZ1 = (Appsettings.NegativeLimitPositionZ1 ?? 0).ToString();
            ViewModel.PositiveLimitPositionZ2 = (Appsettings.PositiveLimitPositionZ2 ?? 0).ToString();
            ViewModel.NegativeLimitPositionZ2 = (Appsettings.NegativeLimitPositionZ2 ?? 0).ToString();
            ViewModel.PositiveLimitPositionTheta = (Appsettings.PositiveLimitPositionTheta ?? 0).ToString();
            ViewModel.NegativeLimitPositionTheta = (Appsettings.NegativeLimitPositionTheta ?? 0).ToString();
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
                await SaveData();
                MaterialSnack("操作成功", SnackType.SUCCESS);
                //mainWindow.NavigateToPage("MainMenu");
                //mainWindow.mainFrame.Source = new Uri("View/Pages/F4_BladeMaintenance/BmSharpenParameter.xaml", UriKind.Relative);
            }
            else
            {
                MaterialSnack("数据异常", SnackType.ERROR);
            }
        }

        public async Task SaveData()
        {
            OperationParametersModel operationParameter = ViewModel.operationParameter;
            if (operationParameter != null)
            {
                operationParameter.ZStopAfterSeq = cbxZStopAfterSeq.Text;
                await SqlHelper.UpdateAsync(operationParameter);
                CurrentUtils.InitAxisSpeedIndex(operationParameter);
                // 设置Z轴补偿量
                GlobalParams.zAxisCompNum = ViewModel.zAxisCompNum;
                GlobalParams.zAxisCompValue = Tools.GetFloatStringValue(ViewModel.zAxisCompValue);
                Appsettings.PositiveLimitPositionX = ViewModel.PositiveLimitPositionX.ToFloat();
                Appsettings.NegativeLimitPositionX = ViewModel.NegativeLimitPositionX.ToFloat();
                Appsettings.PositiveLimitPositionY = ViewModel.PositiveLimitPositionY.ToFloat();
                Appsettings.NegativeLimitPositionY = ViewModel.NegativeLimitPositionY.ToFloat();
                Appsettings.PositiveLimitPositionZ1 = ViewModel.PositiveLimitPositionZ1.ToFloat();
                Appsettings.NegativeLimitPositionZ1 = ViewModel.NegativeLimitPositionZ1.ToFloat();
                Appsettings.PositiveLimitPositionZ2 = ViewModel.PositiveLimitPositionZ2.ToFloat();
                Appsettings.NegativeLimitPositionZ2 = ViewModel.NegativeLimitPositionZ2.ToFloat();
                Appsettings.PositiveLimitPositionTheta = ViewModel.PositiveLimitPositionTheta.ToFloat();
                Appsettings.NegativeLimitPositionTheta = ViewModel.NegativeLimitPositionTheta.ToFloat();
                await AutoCutUtils.SetFunctionalParameters();
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