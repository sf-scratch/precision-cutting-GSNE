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
using 精密切割系统.View.Controls;
using 精密切割系统.View.page.right;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.F5_GeneralEfficiency
{
    /// <summary>
    /// F5_1_PrecutDataCopy.xaml 的交互逻辑
    /// </summary>
    public partial class F5_1_PrecutDataCopy : Page
    {
        public F5_1_PrecutDataCopy()
        {
            InitializeComponent();
        }

        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private PreCutModel currentModel;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mainWindow = Application.Current.MainWindow as MainWindow;
            rightPage = mainWindow.rightFrame.Content as RightPage;
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            rightPage.btnSure.Visibility = Visibility.Visible;
            rightPage.btnSure.SetRightClickedHandler(btuCopySure);
            //底部操作按钮
            mainWindow.UpdateOperatePage([], null);
            long id = long.Parse(QueryUtils.GetValueFromQueryParams(this, "id"));
            List<PreCutModel> precutList = SqlHelper.Table<PreCutModel>().Where(t =>t.Id == id).ToList();
            if (precutList.Count>0)
            {
                currentModel = precutList[0];
                tbxSrc.Text = currentModel.PrecutNo;
            }
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            mainWindow.NavigateToPage("Pages/F5_GeneralEfficiency/F5_1_PrecutData");
        }

        private void btuCopySure(object sender, bool e)
        {
            //执行数据库数据保存。
            var success = this.FormSuccess();
            if (!success)
            {
                MaterialSnack("数据异常", SnackType.ERROR);
                return;
            }
            if (string.IsNullOrEmpty(tbxDst.Text)) return;
            List<PreCutModel> precutList = SqlHelper.Table<PreCutModel>().Where(t => t.PrecutNo == tbxDst.Text).ToList();
            if (precutList.Count > 0)
            {
                MaterialSnack("预切割编号已存在，请重新输入！", SnackType.WARNING);
                return;
            }
            currentModel.PrecutNo = tbxDst.Text;
            SqlHelper.Add(currentModel);
            mainWindow.NavigateToPage("Pages/F5_GeneralEfficiency/F5_1_PrecutData");
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
