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
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;
using 精密切割系统.Utils;
using 精密切割系统.View.common;
using 精密切割系统.View.Controls;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.F6_EngineeringTechnology
{
    /// <summary>
    /// ETInitialPositionConf.xaml 的交互逻辑
    /// </summary>
    public partial class ETInitialPositionConf : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;

        private InitialPositionModel _model;

        public ETInitialPositionConf()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _ = initData();
            mainWindow = Application.Current.MainWindow as MainWindow;
            rightPage = mainWindow.rightFrame.Content as RightPage;

            //右侧显示
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible; //右侧显示 - 返回按钮显示
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            rightPage.btnSure.Visibility = Visibility.Visible; //右侧显示 - 确定按钮显示
            rightPage.btnSure.BackFlag = false;
            rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked); //确定按钮事件
            mainWindow.UpdateOperatePage([], null);
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            mainWindow.NavigateToPage("MainMenu");
        }

        private void BtnSure_RightClicked(object? sender, bool e)
        {
            var success = this.FormSuccess();
            if (success)
            {
                this.saveData();
                MaterialSnackUtils.MaterialSnack("操作成功！", MaterialSnackUtils.SnackType.SUCCESS);
                //返回
                // mainWindow.NavigateToPage("MainMenu");
            }
            else
            {
                MaterialSnackUtils.MaterialSnack("数据异常!", MaterialSnackUtils.SnackType.ERROR);
            }
        }

        //各模式初始位置 (6.6)
        private async Task initData()
        {
            var list = await SqlHelper.TableAsync<InitialPositionModel>().Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                _model = new InitialPositionModel();
                await SqlHelper.AddAsync(_model);
            }
            else
            {
                _model = list[0];
            }
            initView();
        }

        //数据显示
        private void initView()
        {
            inputBladeSetupInitX.Text = _model.BladeSetupInitX;
            inputBladeSetupInitY.Text = _model.BladeSetupInitY;
            inputBladeSetupInitZ1.Text = _model.BladeSetupInitZ1;
            inputBladeSetupInitZ2.Text = _model.BladeSetupInitZ2;
            //inputNoContactBladeSetupInitX.Text = _model.NoContactBladeSetupInitX;
            //inputNoContactBladeSetupInitY.Text = _model.NoContactBladeSetupInitY;
            //inputNoContactBladeSetupInitZ1.Text = _model.NoContactBladeSetupInitZ1;
            //inputNoContactBladeSetupInitZ2.Text = _model.NoContactBladeSetupInitZ2;
            inputAlignInitX.Text = _model.AlignInitX;
            inputAlignInitY.Text = _model.AlignInitY;
            inputAlignInitZ1.Text = _model.AlignInitZ1;
            inputAlignInitZ2.Text = _model.AlignInitZ2;
            //inputCutInitX.Text = _model.CutInitX;
            //inputCutInitY.Text = _model.CutInitY;
            //inputCutInitZ1.Text = _model.CutInitZ1;
            //inputCutInitZ2.Text = _model.CutInitZ2;
            inputCutReplaceInitX.Text = _model.CutReplaceInitX;
            inputCutReplaceInitY.Text = _model.CutReplaceInitY;
            inputCutReplaceInitZ1.Text = _model.CutReplaceInitZ1;
            inputCutReplaceInitZ2.Text = _model.CutReplaceInitZ2;

            //如果是空或者小数位数不足-小数初始化为0
            initTbNumber();
        }

        //数据处理
        //返回上一页
        public void backPage()
        {
            //Router.ToMachineMaintenanceMenu();
        }

        //保存数据
        public async void saveData()
        {
            if (_model != null)
            {
                _model.BladeSetupInitX = inputBladeSetupInitX.Text;
                _model.BladeSetupInitY = inputBladeSetupInitY.Text;
                _model.BladeSetupInitZ1 = inputBladeSetupInitZ1.Text;
                _model.BladeSetupInitZ2 = inputBladeSetupInitZ2.Text;

                //_model.NoContactBladeSetupInitX = inputNoContactBladeSetupInitX.Text;
                //_model.NoContactBladeSetupInitY = inputNoContactBladeSetupInitY.Text;
                //_model.NoContactBladeSetupInitZ1 = inputNoContactBladeSetupInitZ1.Text;
                //_model.NoContactBladeSetupInitZ2 = inputNoContactBladeSetupInitZ2.Text;

                _model.AlignInitX = inputAlignInitX.Text;
                _model.AlignInitY = inputAlignInitY.Text;
                _model.AlignInitZ1 = inputAlignInitZ1.Text;
                _model.AlignInitZ2 = inputAlignInitZ2.Text;

                //_model.CutInitX = inputCutInitX.Text;
                //_model.CutInitY = inputCutInitY.Text;
                //_model.CutInitZ1 = inputCutInitZ1.Text;
                //_model.CutInitZ2 = inputCutInitZ2.Text;

                _model.CutReplaceInitX = inputCutReplaceInitX.Text;
                _model.CutReplaceInitY = inputCutReplaceInitY.Text;
                _model.CutReplaceInitZ1 = inputCutReplaceInitZ1.Text;
                _model.CutReplaceInitZ2 = inputCutReplaceInitZ2.Text;

                await SqlHelper.UpdateAsync(_model);

                // 测高初始位置
                CurrentUtils.InitInitialPositionModel(_model);
            }
            else
            {
                Tools.LogError("6.6数据异常！");
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