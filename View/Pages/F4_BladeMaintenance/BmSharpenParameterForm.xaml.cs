using NPOI.SS.UserModel;
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
using System.Windows.Threading;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;
using 精密切割系统.Model.sqlite;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.F4_BladeMaintenance
{
    /// <summary>
    /// BmSharpenParameterForm.xaml 的交互逻辑
    /// </summary>
    public partial class BmSharpenParameterForm : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;

        private BmSharpenParameterModel? _model;
        private List<BmSharpenParameterModel> list;
        // 切割方向 0 前切 1 后切
        public static int cutDirection = -1;
        public BmSharpenParameterForm()
        {
            InitializeComponent();            
        }
        //获取参数
        string IdStr;
        string Flag;
        string BladeLotID;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mainWindow = Application.Current.MainWindow as MainWindow;
            rightPage = mainWindow.rightFrame.Content as RightPage;

            //右侧显示
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnSure.Visibility = Visibility.Visible; //右侧显示 - 确定按钮显示
            rightPage.btnBack.Visibility = Visibility.Visible; //右侧显示 - 返回按钮显示
            rightPage.btnSure.BackFlag = false; //确定按钮不执行返回，执行自己的代理事件。
            rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked); 
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            rightPage.btnCutStart.Visibility = Visibility.Visible;
            rightPage.btnCutStart.BackFlag = false;
            rightPage.btnCutStart.SetRightClickedHandler(BtnCutStart_RightClicked);
            // 向后切
            rightPage.btnCutBackward.Visibility = Visibility.Visible;
            rightPage.btnCutBackward.BackFlag = false;
            rightPage.btnCutBackward.SetRightClickedHandler(BtnCutBackward_RightClicked);
            // 向前切
            rightPage.btnCutFront.Visibility = Visibility.Visible;
            rightPage.btnCutFront.BackFlag = false;
            rightPage.btnCutFront.SetRightClickedHandler(BtnCutFront_RightClicked);

            //底部操作按钮
            mainWindow.UpdateOperatePage(OperateData.GetTab4401Operate(), OperatePage_onClicked);
            //获取参数
            IdStr = QueryUtils.GetValueFromQueryParams(this, "Id");
            Flag = QueryUtils.GetValueFromQueryParams(this, "Flag");
            BladeLotID = QueryUtils.GetValueFromQueryParams(this, "BladeLotID");            
            int Id = 0;
            if (IdStr != null) {
                Id = int.Parse(IdStr);
            }            
            _ = initData(Id, Flag, BladeLotID);
        }

        private void BtnCutFront_RightClicked(object? sender, bool e)
        {
            // 向前切
            tbCoCutDirection.Text = "向前切";
            cutDirection = 0;
        }

        private void BtnCutBackward_RightClicked(object? sender, bool e)
        {
            // 向后切
            tbCoCutDirection.Text = "向后切";
            cutDirection = 1;
        }

        private void BtnCutStart_RightClicked(object? sender, bool e)
        {
            // 检查切割条件
            if (!CommonCheck.CutStatusCheck())
            {
                return;
            }
            if (cutDirection == -1)
            {
                MaterialSnackUtils.MaterialSnack("请设置切割方向！", MaterialSnackUtils.SnackType.WARNING);
                return;
            }
            mainWindow.NavigateToPage("Pages/F4_BladeMaintenance/BmSharpenParameterRun", "Id=" + IdStr + "&Flag=" + Flag + "&BladeLotID=" + BladeLotID + "&cutDirection=" + cutDirection);
        }

        private void OperatePage_onClicked(object? sender, int e)
        {
            _ = this.BtnOnClicked(e);
        }

        //返回到列表页面
        private void BtnBack_RightClicked(object? sender, bool e)
        {
            mainWindow.NavigateToPage("Pages/F4_BladeMaintenance/BmSharpenParameter");
        }

        private void BtnSure_RightClicked(object? sender, bool e)
        {
            //执行数据库数据保存。
            var success = this.FormSuccess();
            if (success)
            {
                saveData();
                //mainWindow.NavigateToPage("Pages/F4_BladeMaintenance/BmSharpenParameter");
                //mainWindow.mainFrame.Source = new Uri("View/Pages/F4_BladeMaintenance/BmSharpenParameter.xaml", UriKind.Relative);
            }
            else
            {
                MaterialSnackUtils.MaterialSnack("数据异常", MaterialSnackUtils.SnackType.ERROR);
            }
        }
        private async Task BtnOnClicked(int code)
        {
            //位置清零
            if (code == 44010)
            {
                //rbMm.IsChecked = true;
                //rbInch.IsChecked = false;
                //tbRotateSpeed.Text = "0.000";
                //cmbCutMethod.SelectedIndex = 0;
                //tbCutThickness.Text = "0.000";
                //tbCutHeight.Text = "0.000";
                //cbIfCorrectHeight.IsChecked = false;
                //tbCoCutNum.Text = "0";
                //tbCoXDistance.Text = "0.000";
                //tbCoYDistance.Text = "0.000";
                //tbCoJiaoHeight.Text = "0.000";
                //tbCoCutSize.Text = "0.000";
                //tbCoOffsetX.Text = "0.000";
                //tbCoCutDirection.Text = "---";

                jdMoCutOneSpeed.Text = "0.000";
                jdMoCutOneNo.Text = "0";
                jdMoCutTwoSpeed.Text = "0.000";
                jdMoCutTwoNo.Text = "0";
                jdMoCutThreeSpeed.Text = "0.000";
                jdMoCutThreeNo.Text = "0";
                jdMoCutFourSpeed.Text = "0.000";
                jdMoCutFourNo.Text = "0";
                jdMoCutFiveSpeed.Text = "0.000";
                jdMoCutFiveNo.Text = "0";
                jdMoCutSixSpeed.Text = "0.000";
                jdMoCutSixNo.Text = "0";
                jdMoCutSevenSpeed.Text = "0.000";
                jdMoCutSevenNo.Text = "0";
                jdMoCutEightSpeed.Text = "0.000";
                jdMoCutEightNo.Text = "0";
                jdMoCutNineSpeed.Text = "0.000";
                jdMoCutNineNo.Text = "0";
                jdMoCutTenSpeed.Text = "0.000";
                jdMoCutTenNo.Text = "0";
            }
            //手动校准
            if (code == 44011)
            {
                mainWindow.NavigateToPage("Pages/F2_ManualOperation/MQManualAlignmentConf", "type=4");
            }
            if (code == 2023)
            {
                mainWindow.NavigateToPage("Pages/F2_ManualOperation/MQManualAlignmentConf", "type=2&Id=" + IdStr + "&Flag=" + Flag + "&BladeLotID=" + BladeLotID);
            }
        }
            //校准参数（6.5）
        private async Task initData(long Id,string Flag,string BladeLotID)
        {
            var list = await SqlHelper.TableAsync<BmSharpenParameterModel>()
                            .Where(t => t.Id == Id).ToListAsync(); 
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {               
                _model = new BmSharpenParameterModel();
                _model.BladeLotID = "0";                
            }
            else 
            {
                _model = list[0];
                if (Flag != null && Flag == "copy") {                    
                    _model.Id = 0;
                    _model.BladeLotID = BladeLotID;
                }
            }
            initView();
        }

        //数据显示
        private void initView()
        {
            cutDirection = -1;
            lbBladeLotID.Content = _model.BladeLotID;
            rbMm.IsChecked = true;
            //if (_model.Unit == "inch")
            //{
            //    rbMm.IsChecked = true;
            //    rbInch.IsChecked = false;
            //}
            //else 
            //{
            //    rbMm.IsChecked = false;
            //    rbInch.IsChecked = true;
            //}
            tbRotateSpeed.Text = _model.RotateSpeed;
            if (_model.CutMethod == "A")
            {
                cmbCutMethod.SelectedIndex = 0;
            }
            else if (_model.CutMethod == "B")
            {
                cmbCutMethod.SelectedIndex = 1;
            }
            else 
            {
                cmbCutMethod.SelectedIndex = 0;
            }
            tbCutThickness.Text = _model.CutThickness;
            tbCutHeight.Text = _model.CutHeight + "";
            cbIfCorrectHeight.IsChecked = _model.IfCorrectHeight=="1";
            tbCoCutNum.Text = _model.CoCutNum + "";
            tbCoXDistance.Text = _model.CoXDistance + "";
            tbCoYDistance.Text = _model.CoYDistance + "";
            tbCoJiaoHeight.Text = _model.CoJiaoHeight + "";
            tbCoCutSize.Text = _model.CoCutSize + "";
            tbCoOffsetX.Text = _model.CoOffsetX + "";
            tbCoCutDirection.Text = "---";

            jdMoCutOneSpeed.Text = _model.MoCutOneSpeed;
            jdMoCutOneNo.Text = _model.MoCutOneNo;
            jdMoCutTwoSpeed.Text = _model.MoCutTwoSpeed;
            jdMoCutTwoNo.Text = _model.MoCutTwoNo;
            jdMoCutThreeSpeed.Text = _model.MoCutThreeSpeed;
            jdMoCutThreeNo.Text = _model.MoCutThreeNo;
            jdMoCutFourSpeed.Text = _model.MoCutFourSpeed;
            jdMoCutFourNo.Text = _model.MoCutFourNo;
            jdMoCutFiveSpeed.Text = _model.MoCutFiveSpeed;
            jdMoCutFiveNo.Text = _model.MoCutFiveNo;
            jdMoCutSixSpeed.Text = _model.MoCutSixSpeed;
            jdMoCutSixNo.Text = _model.MoCutSixNo;
            jdMoCutSevenSpeed.Text = _model.MoCutSevenSpeed;
            jdMoCutSevenNo.Text = _model.MoCutSevenNo;
            jdMoCutEightSpeed.Text = _model.MoCutEightSpeed;
            jdMoCutEightNo.Text = _model.MoCutEightNo;
            jdMoCutNineSpeed.Text = _model.MoCutNineSpeed;
            jdMoCutNineNo.Text = _model.MoCutNineNo;
            jdMoCutTenSpeed.Text = _model.MoCutTenSpeed;
            jdMoCutTenNo.Text = _model.MoCutTenNo;
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
                _model.BladeLotID = lbBladeLotID.Content.ToString() ;
                //if (rbMm.IsChecked == true)
                //{
                //    _model.Unit = "mm";
                //}
                //else
                //{
                //    _model.Unit = "inch";
                //}
                _model.Unit = "mm";
                _model.RotateSpeed  = tbRotateSpeed.Text;
                _model.CutMethod = cmbCutMethod.Text;
                _model.CutThickness = tbCutThickness.Text;
                _model.CutHeight = float.Parse(tbCutHeight.Text);
                _model.IfCorrectHeight = cbIfCorrectHeight.IsChecked==true?"1":"0";
                _model.CoCutNum = int.Parse(tbCoCutNum.Text);
                _model.CoXDistance = float.Parse(tbCoXDistance.Text);
                _model.CoYDistance = float.Parse(tbCoYDistance.Text);
                _model.CoJiaoHeight = float.Parse(tbCoJiaoHeight.Text);
                _model.CoCutSize = float.Parse(tbCoCutSize.Text);
                _model.CoOffsetX = float.Parse(tbCoOffsetX.Text);
                _model.CoCutDirection = tbCoCutDirection.Text;
                _model.MoCutOneSpeed = jdMoCutOneSpeed.Text;
                _model.MoCutOneNo = jdMoCutOneNo.Text;
                _model.MoCutTwoSpeed = jdMoCutTwoSpeed.Text;
                _model.MoCutTwoNo = jdMoCutTwoNo.Text;
                _model.MoCutThreeSpeed = jdMoCutThreeSpeed.Text;
                _model.MoCutThreeNo = jdMoCutThreeNo.Text;
                _model.MoCutFourSpeed = jdMoCutFourSpeed.Text;
                _model.MoCutFourNo = jdMoCutFourNo.Text;
                _model.MoCutFiveSpeed = jdMoCutFiveSpeed.Text;
                _model.MoCutFiveNo = jdMoCutFiveNo.Text;
                _model.MoCutSixSpeed = jdMoCutSixSpeed.Text;
                _model.MoCutSixNo = jdMoCutSixNo.Text;
                _model.MoCutSevenSpeed = jdMoCutSevenSpeed.Text;
                _model.MoCutSevenNo = jdMoCutSevenNo.Text;
                _model.MoCutEightSpeed = jdMoCutEightSpeed.Text;
                _model.MoCutEightNo = jdMoCutEightNo.Text;
                _model.MoCutNineSpeed = jdMoCutNineSpeed.Text;
                _model.MoCutNineNo = jdMoCutNineNo.Text;
                _model.MoCutTenSpeed = jdMoCutTenSpeed.Text;
                _model.MoCutTenNo = jdMoCutTenNo.Text;
                if (_model.Id > 0)
                {
                    await SqlHelper.UpdateAsync(_model);
                }
                else {
                    var Id = await SqlHelper.AddAsync(_model);                  
                }
                MaterialSnackUtils.MaterialSnack("保存成功！", MaterialSnackUtils.SnackType.SUCCESS);
            }
            else
            {
                Tools.LogError("6.5数据异常！");
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
