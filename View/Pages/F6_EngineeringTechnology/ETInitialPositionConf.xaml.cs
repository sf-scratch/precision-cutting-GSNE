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
                MaterialSnack("操作成功！", SnackType.SUCCESS);
                //返回
                // mainWindow.NavigateToPage("MainMenu");
            }
            else
            {
                MaterialSnack("数据异常!", SnackType.ERROR);
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
            inputBladeSetupInitSpeedX.Text = _model.BladeSetupInitSppedX;
            inputBladeSetupInitY.Text = _model.BladeSetupInitY;
            inputBladeSetupInitSpeedY.Text = _model.BladeSetupInitSppedY;
            inputBladeSetupInitZ1.Text = _model.BladeSetupInitZ1;
            inputBladeSetupInitSpeedZ1.Text = _model.BladeSetupInitSppedZ1;
            //inputBladeSetupInitZ2.Text = _model.BladeSetupInitZ2;
            //inputNoContactBladeSetupInitX.Text = _model.NoContactBladeSetupInitX;
            //inputNoContactBladeSetupInitY.Text = _model.NoContactBladeSetupInitY;
            //inputNoContactBladeSetupInitZ1.Text = _model.NoContactBladeSetupInitZ1;
            //inputNoContactBladeSetupInitZ2.Text = _model.NoContactBladeSetupInitZ2;
            inputCutReplaceInitX.Text = _model.CutReplaceInitX;
            inputCutReplaceInitSpeedX.Text = _model.CutReplaceInitSpeedX;
            inputCutReplaceInitY.Text = _model.CutReplaceInitY;
            inputCutReplaceInitSpeedY.Text = _model.CutReplaceInitSpeedY;
            //inputCutReplaceInitZ1.Text = _model.CutReplaceInitZ1;
            inputCutReplaceInitSpeedZ1.Text = _model.CutReplaceInitSpeedZ1;
            //inputCutReplaceInitZ2.Text = _model.CutReplaceInitZ2;
            inputAlignInitX.Text = _model.AlignInitX;
            inputAlignInitSpeedX.Text = _model.AlignInitSpeedX;
            inputAlignInitY.Text = _model.AlignInitY;
            inputAlignInitSpeedY.Text = _model.AlignInitSpeedY;
            //inputAlignInitZ1.Text = _model.AlignInitZ1;
            inputAlignInitSpeedZ1.Text = _model.AlignInitSpeedZ1;
            inputAlignInitTheta.Text = _model.AlignInitTheta;
            inputAlignInitSpeedTheta.Text = _model.AlignInitSpeedTheta;
            //inputAlignInitZ2.Text = _model.AlignInitZ2;
            //inputCutInitX.Text = _model.CutInitX;
            //inputCutInitY.Text = _model.CutInitY;
            //inputCutInitZ1.Text = _model.CutInitZ1;
            //inputCutInitZ2.Text = _model.CutInitZ2;

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
                _model.BladeSetupInitSppedX = inputBladeSetupInitSpeedX.Text;
                _model.BladeSetupInitY = inputBladeSetupInitY.Text;
                _model.BladeSetupInitSppedY = inputBladeSetupInitSpeedY.Text;
                _model.BladeSetupInitZ1 = inputBladeSetupInitZ1.Text;
                _model.BladeSetupInitSppedZ1 = inputBladeSetupInitSpeedZ1.Text;
                //_model.BladeSetupInitZ2 = inputBladeSetupInitZ2.Text;

                //_model.NoContactBladeSetupInitX = inputNoContactBladeSetupInitX.Text;
                //_model.NoContactBladeSetupInitY = inputNoContactBladeSetupInitY.Text;
                //_model.NoContactBladeSetupInitZ1 = inputNoContactBladeSetupInitZ1.Text;
                //_model.NoContactBladeSetupInitZ2 = inputNoContactBladeSetupInitZ2.Text;

                _model.CutReplaceInitX = inputCutReplaceInitX.Text;
                _model.CutReplaceInitSpeedX = inputCutReplaceInitSpeedX.Text;
                _model.CutReplaceInitY = inputCutReplaceInitY.Text;
                _model.CutReplaceInitSpeedY = inputCutReplaceInitSpeedY.Text;
                //_model.CutReplaceInitZ1 = inputCutReplaceInitZ1.Text;
                _model.CutReplaceInitSpeedZ1 = inputCutReplaceInitSpeedZ1.Text;

                _model.AlignInitX = inputAlignInitX.Text;
                _model.AlignInitSpeedX = inputAlignInitSpeedX.Text;
                _model.AlignInitY = inputAlignInitY.Text;
                _model.AlignInitSpeedY = inputAlignInitSpeedY.Text;
                //_model.AlignInitZ1 = inputAlignInitZ1.Text;
                _model.AlignInitSpeedZ1 = inputAlignInitSpeedZ1.Text;
                _model.AlignInitTheta = inputAlignInitTheta.Text;
                _model.AlignInitSpeedTheta = inputAlignInitSpeedTheta.Text;
                //_model.AlignInitZ2 = inputAlignInitZ2.Text;

                //_model.CutInitX = inputCutInitX.Text;
                //_model.CutInitY = inputCutInitY.Text;
                //_model.CutInitZ1 = inputCutInitZ1.Text;
                //_model.CutInitZ2 = inputCutInitZ2.Text;
                //_model.CutReplaceInitZ2 = inputCutReplaceInitZ2.Text;

                await SqlHelper.UpdateAsync(_model);

                // 测高初始位置
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