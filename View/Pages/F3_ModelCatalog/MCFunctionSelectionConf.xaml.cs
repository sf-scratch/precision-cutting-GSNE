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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.Helpers;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;

namespace 精密切割系统.View.Pages.F3_ModelCatalog
{
    /// <summary>
    /// MCFunctionSelectionConf.xaml 的交互逻辑
    /// </summary>
    public partial class MCFunctionSelectionConf : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;
        private FunctionSelectionModel _model;

        public MCFunctionSelectionConf()
        {
            InitializeComponent();
            mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        }

        private void Label_Loaded(object sender, RoutedEventArgs e)
        {
            rightPage = mainWindow.rightFrame.Content as RightPage;
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BackFrom);
            rightPage.btnSure.SetRightClickedHandler(SureOk);
            rightPage.btnSure.GlobalRunOperateFlag = true;
            rightPage.btnBack.GlobalRunOperateFlag = true;
            operatePage = mainWindow.operateFrame.Content as OperatePage;
            rightPage.btnSure.Visibility = Visibility.Visible;
            mainWindow.UpdateOperatePage([],null);
            initView();
        }

        private async void initView()
        {
            //获取相关数据
            var list = await SqlHelper.TableAsync<FunctionSelectionModel>()
                        .Where(t => t.Id == 1).ToListAsync();
            if (list.Count>0)
            {
                _model = list[0];
                dpsdCheckbox.IsChecked = _model.DepthStepsFunction;
                loopCheckbox.IsChecked = _model.LoopFunction;
            }
        }

        private void BackFrom(object sender, bool v)
        {
            int id = int.Parse(QueryUtils.getQuery(this)["id"]);
            bool lookState = bool.Parse(QueryUtils.getQuery(this)["look"]);
            mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCDeviceDataConf", $"id={id}&look={lookState}");
        }
        private async void SureOk(object sender, bool v)
        {
            _model.DepthStepsFunction = dpsdCheckbox.IsChecked==true;
            _model.LoopFunction = loopCheckbox.IsChecked == true;
            await SqlHelper.UpdateAsync(_model);
            int id = int.Parse(QueryUtils.getQuery(this)["id"]);
            bool lookState = bool.Parse(QueryUtils.getQuery(this)["look"]);
            mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCDeviceDataConf", $"id={id}&look={lookState}");
        }
    }
}
