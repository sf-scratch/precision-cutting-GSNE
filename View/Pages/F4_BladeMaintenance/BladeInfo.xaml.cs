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
using 精密切割系统.database.db.modle;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.F4_BladeMaintenance
{
    /// <summary>
    /// BladeInfo.xaml 的交互逻辑
    /// </summary>
    public partial class BladeInfo : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;
        BladeHeightModel _model = null;
        string urlParams = null;
        string pageName = null;
        public BladeInfo()
        {
            mainWindow = Application.Current.MainWindow as MainWindow;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            rightPage = mainWindow.rightFrame.Content as RightPage;
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            rightPage.btnBack.GlobalRunOperateFlag = true;
            string urlParamsTemp = QueryUtils.GetValueFromQueryParams(this, "urlParams");
            if (!string.IsNullOrEmpty(urlParamsTemp))
            {
                urlParams = Uri.UnescapeDataString(urlParamsTemp);
            }
            pageName = QueryUtils.GetValueFromQueryParams(this, "pageName");
            mainWindow.UpdateOperatePage(new List<OperateBean>(), null);
            initData();
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            if (string.IsNullOrEmpty(pageName))
            {
                mainWindow.NavigateToPage("MainMenu");
            } else
            {
                mainWindow.NavigateToPage(pageName, urlParams);
            }
        }

        //初始化数据
        private async Task initData()
        {
            //测高参数的数据
            List<BladeHeightModel> list = await SqlHelper.TableAsync<BladeHeightModel>()
                    .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                _model = new BladeHeightModel();
                _model.Id = 1;
                //await SqlHelper.AddAsync(_model);
            }
            else
            {
                _model = list[0];

            }
            tbChuckTableSize.Text = _model.ChuckTableSize;
            tbTableType.Text = _model.TableType;
            tbChuckTableShape.Text = _model.ChuckTableShape;
            if ("ROUND".Equals(_model.ChuckTableShape))
            {
                showRound.Visibility = Visibility.Visible;
            }
            else
            {
                showRound.Visibility = Visibility.Hidden; //隐藏
            }

            //刀片更换的数据
            ReplaceBladeModel _gh = new ReplaceBladeModel();
            List<ReplaceBladeModel> list_gh = await SqlHelper.TableAsync<ReplaceBladeModel>()
                    .Where(t => t.Id == 1).ToListAsync();
            if (list_gh.Count() == 0)
            {
                _gh = new ReplaceBladeModel();
                _model.Id = 1;
                //await SqlHelper.AddAsync(_model);
            }
            else
            {
                _gh = list_gh[0];
            }
            tbBladeLotID.Text = _gh.BladeLotID;
            tbBladeOutside.Text = _gh.BladeOutside;
            tbSpecName.Text = _gh.SpecName;
            inputTextBox6.Text = _model.BladeHeight;
            // 切割数据
            inputTextBox14.Text = GlobalParams.cutAllDistance + "";
            inputTextBox15.Text = GlobalParams.cutAllNum + "";
            inputTextBox17.Text = GlobalParams.heightCutAllDistance + "";
            inputTextBox16.Text = GlobalParams.heightCutAllNum + "";
            CurrentConfigurationModel currentConfigurationModel = CurrentUtils.GetCurrentConfiguration();
            inputTextBox19.Text = currentConfigurationModel.ClearedCutAllDistance + "";
            inputTextBox18.Text = currentConfigurationModel.ClearedCutAllNum + "";
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
    }
}
