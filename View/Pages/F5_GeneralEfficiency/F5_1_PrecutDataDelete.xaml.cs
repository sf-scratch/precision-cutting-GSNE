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
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.View.Pages.F5_GeneralEfficiency
{
    /// <summary>
    /// F5_1_PrecutDataDelete.xaml 的交互逻辑
    /// </summary>
    public partial class F5_1_PrecutDataDelete : Page
    {
        public F5_1_PrecutDataDelete()
        {
            InitializeComponent();
        }

        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private PreCutModel currentModel;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            long id = long.Parse(QueryUtils.GetValueFromQueryParams(this, "id"));
            List<PreCutModel> precutList = SqlHelper.Table<PreCutModel>().Where(t => t.Id == id).ToList();
            if (precutList.Count > 0)
            {
                currentModel = precutList[0];
                tbxSrc.Text = currentModel.PrecutNo;
            }

            mainWindow = Application.Current.MainWindow as MainWindow;
            rightPage = mainWindow.rightFrame.Content as RightPage;
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnSure.Visibility = Visibility.Visible;
            rightPage.btnSure.SetRightClickedHandler(btnDataDetails);
            rightPage.btnBack.SetRightClickedHandler(back);
            //底部操作按钮
            mainWindow.UpdateOperatePage([], null);
        }

        private void back(object sender, bool e)
        {
            mainWindow.NavigateToPage("Pages/F5_GeneralEfficiency/F5_1_PrecutData");
        }

        private void btnDataDetails(object sender, bool e)
        {
            if (currentModel == null) return;
            SqlHelper.Delete(currentModel);
            mainWindow.NavigateToPage("Pages/F5_GeneralEfficiency/F5_1_PrecutData");
        }
    }
}
