using Emgu.CV.Dnn;
using NPOI.SS.Formula.Functions;
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
using 精密切割系统.database.db.modle;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;
using static 精密切割系统.Helpers.MaterialSnackUtils;

namespace 精密切割系统.View.Pages.F4_BladeMaintenance
{
    /// <summary>
    /// BMBladeReplacementConf.xaml 的交互逻辑
    /// </summary>
    public partial class BMBladeReplacementConf : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;
        public BMBladeReplacementConf()
        {
            InitializeComponent();
            mainWindow = Application.Current.MainWindow as MainWindow;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            List<ReplaceBladeModel> list = SqlHelper.Table<ReplaceBladeModel>().ToList();
            ReplaceBladeViewModel viewModel = (ReplaceBladeViewModel)this.DataContext;
            if(list.Count > 0)
            {
                if (list[0].BladeUnit.Equals("mm"))
                {
                    viewModel.IsBladeUnitMm = true;
                }
                else if (list[0].BladeUnit.Equals("inch"))
                {
                    viewModel.IsBladeUnitInch = true;
                }
                viewModel.BladeUnit = list[0].BladeUnit;
                viewModel.BladeLotID = list[0].BladeLotID;
                viewModel.SpecName = list[0].SpecName;
                viewModel.NewOrOld = list[0].NewOrOld;
                viewModel.BladeOutside = list[0].BladeOutside;
                viewModel.BladeThickness = list[0].BladeThickness;
                viewModel.BladeLife = list[0].BladeLife;
                viewModel.BladeLifeM = list[0].BladeLifeM;
                viewModel.ReplaceReason = list[0].ReplaceReason;
                viewModel.BladeType = list[0].BladeType;
                viewModel.HardBladeLength = list[0].HardBladeLength;
                viewModel.SoftBladeHolder = list[0].SoftBladeHolder;
                DataContext = viewModel;
            }else
            {
                DataContext = new ReplaceBladeViewModel();
            }
            rightPage = mainWindow.rightFrame.Content as RightPage;
            operatePage = mainWindow.operateFrame.Content as OperatePage;
            operatePage.UpdateOperate([]);
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            rightPage.btnSure.Visibility = Visibility.Visible;
            rightPage.btnSure.SetRightClickedHandler(BladeReplaceSure);

            if (string.IsNullOrEmpty(viewModel.NewOrOld)) {
                viewModel.NewOrOld = "新";
            }
            if (string.IsNullOrEmpty(viewModel.BladeType))
            {
                viewModel.BladeType = "硬刀";
            }
            if (string.IsNullOrEmpty(viewModel.ReplaceReason))
            {
                viewModel.ReplaceReason = "新刀片装入";
            }
            MaterialSnackUtils.MaterialSnack("进入刀片更换模式成功！", SnackType.WARNING);
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
        private void BtnBack_RightClicked(object? sender, bool e)
        {
            /*GlobalParams.globalRunFlag = true;
            if (!Tools.TrueFlag(PlcControl.plc.GetPlcValueString(DeviceKey.bladeMantanceStatusKey))
                && PlcControl.allAlarm.Count == 0)
            {
                GlobalParams.globalRunFlag = false;
                return;
            }*/
            GlobalParams.globalRunFlag = true;
            PlcControl.tagControl.bladeMantance.RunBladeReplace(0);
            GlobalParams.globalRunFlag = false;
            mainWindow.NavigateToPage("MainMenu");
            /* bool isNavigated = false;
             Tools.AwaitForStatusFinishAsync(DeviceKey.bladeMantanceStatusKey, (bool flag) =>
             {
                 if (!isNavigated)
                 {
                     GlobalParams.globalRunFlag = false;
                     PlcControl.tagControl.bladeMantance.RunBladeReplace(0);
                     mainWindow.NavigateToPage("MainMenu");
                     isNavigated = true;
                 }
             }, timeoutSeconds: 15);*/

        }

        private void BladeReplaceSure(object sender, bool e)
        {
            List<ReplaceBladeModel> list = SqlHelper.Table<ReplaceBladeModel>().ToList();
            ReplaceBladeModel model = new ReplaceBladeModel();
            ReplaceBladeViewModel viewModel = (ReplaceBladeViewModel)this.DataContext;
            if (viewModel.IsBladeUnitInch)
            {
                model.BladeUnit = "inch";
            }else if(viewModel.IsBladeUnitMm)
            {
                model.BladeUnit = "mm";
            }
            model.BladeLotID = viewModel.BladeLotID;
            model.SpecName = viewModel.SpecName;
            model.NewOrOld = viewModel.NewOrOld;
            model.BladeOutside = viewModel.BladeOutside;
            model.BladeThickness = viewModel.BladeThickness;
            model.BladeLife = viewModel.BladeLife;
            model.BladeLifeM = viewModel.BladeLifeM;
            model.ReplaceReason = viewModel.ReplaceReason;
            model.BladeType = viewModel.BladeType;
            model.HardBladeLength = viewModel.HardBladeLength;
            model.SoftBladeHolder = viewModel.SoftBladeHolder;
            if (list.Count > 0)
            {
                // 执行修改
                model.Id = list[0].Id;
                try
                {
                    SqlHelper.Update(model);
                    MaterialSnackUtils.MaterialSnack("换刀成功！", MaterialSnackUtils.SnackType.SUCCESS);
                    
                    // 更换刀片后，清空测高信息
                }
                catch
                {
                    MaterialSnackUtils.MaterialSnack("保存失败", MaterialSnackUtils.SnackType.ERROR);
                }
            }
            else
            {
                // 执行新增
                try
                {
                    int result = SqlHelper.Add(model);
                    MaterialSnackUtils.MaterialSnack("换刀成功！", MaterialSnackUtils.SnackType.SUCCESS);
                }
                catch
                {
                    MaterialSnackUtils.MaterialSnack("保存失败", MaterialSnackUtils.SnackType.ERROR);
                }
            }
            updateData();
            //MaterialSnackUtils.MaterialSnack("换刀成功！", MaterialSnackUtils.SnackType.SUCCESS);
        }

        /// <summary>
        /// 换刀后修改高度数据为0，测量后修改为测量值
        /// </summary>
        public async void updateData()
        {
            GlobalParams.allDressersNum = 0;
            GlobalParams.cutAllNum = 0;
            GlobalParams.cutAllDistance = 0;
            BladeHeightModel _model;
            var list = await SqlHelper.TableAsync<BladeHeightModel>()
                    .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                _model = new BladeHeightModel();
                await SqlHelper.AddAsync(_model);
            }
            else
            {
                _model = list[0];

            }
            _model.BladeHeight = "0";
            //保存数据
            await SqlHelper.UpdateAsync(_model);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            BtnBack_RightClicked(null, false);
        }
    }
}
