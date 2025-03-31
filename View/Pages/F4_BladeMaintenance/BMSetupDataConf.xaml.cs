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
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.F4_BladeMaintenance
{
    /// <summary>
    /// BMSetupDataConf.xaml 的交互逻辑
    /// </summary>
    public partial class BMSetupDataConf : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;

        BladeHeightViewModel viewModel = new BladeHeightViewModel();
        public BladeHeightModel _bladeHeightModel;
        private string RePage = null;
        private string RePageId = null;
        public BMSetupDataConf()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RePage = QueryUtils.GetValueFromQueryParams(this, "RePage");
            RePageId = QueryUtils.GetValueFromQueryParams(this, "RePageId");
            mainWindow = Application.Current.MainWindow as MainWindow;
            rightPage = mainWindow.rightFrame.Content as RightPage;
            operatePage = mainWindow.operateFrame.Content as OperatePage;
            //底部操作按钮
            operatePage.UpdateOperate([]);
            //operatePage.SetOnClickedHandler(OperatePage_onClicked);
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            rightPage.btnSure.Visibility = Visibility.Visible;
            rightPage.btnSure.BackFlag = false;
            rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked);

            loadDBData();
        }

        public async void loadDBData()
        {
            List<BladeHeightModel> list = await SqlHelper.TableAsync<BladeHeightModel>().Where(t => t.Id == 1).ToListAsync();
            if (list.Count > 0)
            {
                viewModel._bladeHeightModel = list[0];
                //Debug.WriteLine("list[0].BladeUnit==" + list[0].Unit);
                //if (list[0].Unit.Equals("mm"))
                //{
                //    viewModel.IsBladeUnitMm = true;
                //}
                //else if (list[0].Unit.Equals("inch"))
                //{
                //    viewModel.IsBladeUnitInch = true;
                //}                
                DataContext = viewModel;
            }
            else
            {
                await SqlHelper.AddAsync(viewModel._bladeHeightModel);
                
            }
            DataContext = viewModel;

            viewModel.SetupDefault = "CONTACT";
            viewModel.CallOperatorWhenAutoSetup = "NO";
            viewModel.PrecutAfterNonContactSetup = "NO";
            

        }
        private void BtnBack_RightClicked(object? sender, bool e)
        {
            //GlobalParams.globalRunFlag = true;
            //Tools.AwaitForStatusFinishAsync(DeviceKey.bladeMantanceStatusKey, (bool flag) =>
            //{
            //    PlcControl.tagControl.bladeMantance.RunBladeReplace(0);
            //    mainWindow.NavigateToPage("MainMenu");
            //    GlobalParams.globalRunFlag = false;
            //}, timeoutSeconds: 15);
            if (!string.IsNullOrEmpty(RePage))
            {
                mainWindow.NavigateToPage(RePage, $"id={RePageId}");
                return;
            }
            mainWindow.NavigateToPage("MainMenu");
        }

        private async void BtnSure_RightClicked(object sender, bool e)
        {
            if (string.IsNullOrEmpty(viewModel.ChuckTableSize))
            {
                MaterialSnackUtils.MaterialSnack("请选择工作盘尺寸", MaterialSnackUtils.SnackType.ERROR);
                return;
            }
            if (string.IsNullOrEmpty(viewModel.ChuckTableShape))
            {
                MaterialSnackUtils.MaterialSnack("请选择工作盘形状", MaterialSnackUtils.SnackType.ERROR);
                return;
            }
            if (string.IsNullOrEmpty(viewModel.TableType))
            {
                MaterialSnackUtils.MaterialSnack("请选择工作盘种类", MaterialSnackUtils.SnackType.ERROR);
                return;
            }
            //执行数据库数据保存。
            var success = this.FormSuccess();
            if (success)
            {
                await saveData();
                MaterialSnackUtils.MaterialSnack("操作成功", MaterialSnackUtils.SnackType.SUCCESS);
                PlcControl.tagControl.bladeMantance.SetSetupParams(viewModel._bladeHeightModel);
                //mainWindow.NavigateToPage("MainMenu");
                //mainWindow.mainFrame.Source = new Uri("View/Pages/F4_BladeMaintenance/BmSharpenParameter.xaml", UriKind.Relative);
            }
            else
            {
                MaterialSnackUtils.MaterialSnack("数据异常", MaterialSnackUtils.SnackType.ERROR);
            }
        }

        public async Task saveData()
        {
            _bladeHeightModel = viewModel._bladeHeightModel;
            if (viewModel.IsBladeUnitInch)
            {
                _bladeHeightModel.Unit = "inch";
            }
            else if (viewModel.IsBladeUnitMm)
            {
                _bladeHeightModel.Unit = "mm";
            }
            if (_bladeHeightModel != null)
            {              
                await SqlHelper.UpdateAsync(_bladeHeightModel);
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
