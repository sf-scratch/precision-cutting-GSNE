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
    /// FADressDataDelConf.xaml 的交互逻辑
    /// </summary>
    public partial class FADressDataDelConf : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;

        private BmSharpenParameterModel? _model;
        private List<BmSharpenParameterModel> list;
        //获取参数
        string IdStr;

        public FADressDataDelConf()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mainWindow = Application.Current.MainWindow as MainWindow;
            rightPage = mainWindow.rightFrame.Content as RightPage;
            operatePage = mainWindow.operateFrame.Content as OperatePage;

            //右侧显示
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnSure.Visibility = Visibility.Visible; //右侧显示 - 确定按钮显示
            rightPage.btnBack.Visibility = Visibility.Visible; //右侧显示 - 返回按钮显示
            rightPage.btnSure.BackFlag = false; //确定按钮不执行返回，执行自己的代理事件。
            rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked);
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            //底部操作按钮
            mainWindow.UpdateOperatePage([], null);
            //获取参数
            IdStr = QueryUtils.GetValueFromQueryParams(this, "Id");
            int Id = 0;
            if (IdStr != null)
            {
                Id = int.Parse(IdStr);
                _ = initData(Id);
            }
            
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
                this.delData();                
            }
            else
            {
                MaterialSnackUtils.MaterialSnack("数据异常", MaterialSnackUtils.SnackType.ERROR);
            }
        }

        private async Task initData(long Id)
        {
            var list = await SqlHelper.TableAsync<BmSharpenParameterModel>()
                            .Where(t => t.Id == Id).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 1)
            {
                _model = list[0];
                initView();
            }
            
        }

        //数据显示
        private void initView()
        {
            tbDelParameterNo.Text = _model.BladeLotID;
        }

        //数据处理
        //返回上一页
        public void backPage()
        {
            //Router.ToMachineMaintenanceMenu();
        }

        //保存数据
        public async void delData()
        {
            if (_model != null)
            {
                _model.BladeLotID = tbDelParameterNo.Text.Trim();
                //参数号不存在了，则已经删除
                int exitsNo = SqlHelper.Table<BmSharpenParameterModel>().Where(t => t.BladeLotID == _model.BladeLotID).Count();
                if (exitsNo < 0)
                {
                    tbDelParameterNo.XIsError = true;
                    return;
                }
                else
                {
                    tbDelParameterNo.XIsError = false;
                }
                if (!string.Equals(_model.BladeLotID,"0"))
                {
                    await SqlHelper.DeleteAsync(_model);
                    MaterialSnackUtils.MaterialSnack("消除参数号成功！", MaterialSnackUtils.SnackType.SUCCESS);
                    mainWindow.NavigateToPage("Pages/F4_BladeMaintenance/BmSharpenParameter");
                }                
            }
            else
            {
                Tools.LogError("4.4.4数据异常！");
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
