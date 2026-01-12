using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using 精密切割系统.Helpers;
using 精密切割系统.Utils;
using 精密切割系统.View.Controls;
using 精密切割系统.View.page.right;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.F5_GeneralEfficiency
{
    /// <summary>
    /// F5_1_2_PrecutDataDetails.xaml 的交互逻辑
    /// </summary>
    public partial class F5_1_2_PrecutDataDetails : Page
    {
        public F5_1_2_PrecutDataDetails()
        {
            InitializeComponent();
        }

        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private F5_1_1_PrecutDataViewModel model;
        private List<PreCutModel> precutList;
        public static int idx = 0;
        public static string PrecutNo = "";
        private PreCutModel _model = null;
        private string RePage = null;
        private string RePageUrl = null;
        private string RePageId = null;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            model = new F5_1_1_PrecutDataViewModel();
            loadDBData();
        }

        public void loadDBData()
        {

            PrecutNo = QueryUtils.GetValueFromQueryParams(this, "PrecutNo");
            RePage = QueryUtils.GetValueFromQueryParams(this, "RePage");
            RePageUrl = QueryUtils.GetValueFromQueryParams(this, "RePageUrl");
            RePageId = QueryUtils.GetValueFromQueryParams(this, "RePageId");
            //其他页面传过来的ID
            if (!string.IsNullOrEmpty(PrecutNo))
            {
                precutList = SqlHelper.Table<PreCutModel>().Where(t => t.PrecutNo.Equals(PrecutNo)).ToList();
            }
            else
            {
                precutList = SqlHelper.Table<PreCutModel>().ToList();
            }

            loadItem();

            mainWindow = Application.Current.MainWindow as MainWindow;
            rightPage = mainWindow.rightFrame.Content as RightPage;
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnSure.Visibility = Visibility.Visible;
            rightPage.btnSure.BackFlag = false;
            rightPage.btnSure.SetRightClickedHandler(btnSave);
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(back);

            mainWindow.UpdateOperatePage(OperateData.GetTab5120Operate(), OperateClickHandler);
        }

        private void back(object sender, bool e)
        {
            if (!string.IsNullOrEmpty(RePage))
            {
                mainWindow.NavigateToPage(RePage, $"id={RePageId}" + (string.IsNullOrEmpty(RePageUrl) ? "" : $"&url={RePageUrl}"));
            }
            else
            {
                mainWindow.NavigateToPage("Pages/F5_GeneralEfficiency/F5_1_PrecutData", $"surePage=len");
            }
        }

        private void OperateClickHandler(object sender, int code)
        {
            switch (code)
            {
                case 5120:
                    // 位置清零
                    string[] s = Enumerable.Repeat("0.000", 30).ToArray();
                    string[] n = Enumerable.Repeat("0.000", 30).ToArray();
                    _model.FeedSpd = string.Join(",", s);
                    _model.FeedDistance = string.Join(",", n);
                    for (int i = 0; i < model.PrecutTable.Count; i++)
                    {
                        model.PrecutTable[i].Speed = "0.000";
                        model.PrecutTable[i].Len = "0.000";
                    }
                    model.precutParameter = _model;
                    DataContext = model;
                    lvwPrecut2.ItemsSource = model.PrecutTable;
                    break;
                case 5121: //去页面刀数控制
                    mainWindow.NavigateToPage("Pages/F5_GeneralEfficiency/F5_1_1_PrecutDataDetails", $"RePage={RePage}&PrecutNo={_model.PrecutNo}&RePageId={RePageId}&RePageUrl={RePageUrl}");
                    break;
                default:
                    break;
            }
        }

        private void loadItem()
        {
            if (precutList.Count == 0) return;
            _model = precutList[0];
            model.Id = _model.Id;
            model.PrecutNo = _model.PrecutNo;
            model.PrecutID = _model.PrecutID;
            model.UsedBladeNo = _model.UsedBladeNo;
            model.NewBladeNo = _model.NewBladeNo;
            model.PrecutDecrease = _model.PrecutDecrease;
            model.WorkThickness = _model.WorkThickness;
            model.OfLines = _model.OfLines;
            model.FeedSpd = _model.FeedSpd;
            model.FeedDistance = _model.FeedDistance;

            ObservableCollection<PreCutTableClass> pct = new ObservableCollection<PreCutTableClass>();
            List<string> LinesSpeed = model.FeedSpd.Split(",").ToList();
            List<string> LinesLen = model.FeedDistance.Split(",").ToList(); //长度
            for (int i = 0; i < LinesSpeed.Count; i++)
            {
                PreCutTableClass pTmp = new PreCutTableClass();
                pTmp.Speed = LinesSpeed[i];
                pTmp.Len = LinesLen[i];
                pTmp.Index = (i + 1).ToString();
                pct.Add(pTmp);
            }
            model.PrecutTable = pct;
            this.DataContext = model;

            //如果是空或者小数位数不足-小数初始化为0
            initTbNumber();

            // lvwPrecut2.ItemsSource = model.PrecutTable;
        }

        private void btnSave(object sender, bool e)
        {
            //执行数据库数据保存。
            var success = this.FormSuccess();
            if (!success)
            {
                MaterialSnack("数据异常", SnackType.ERROR);
                return;
            }

            var LinesLen = model.PrecutTable
                        .Select(table => table.Len)
                        .ToList();
            var LinesSpeed = model.PrecutTable
                        .Select(table => table.Speed)
                        .ToList();  // 转换为 List
            model.precutParameter.FeedSpd = string.Join(",", LinesSpeed);
            model.precutParameter.FeedDistance = string.Join(",", LinesLen);
            // viewModel.PrecutTable = (ObservableCollection<PreCutTableClass>)lvwPrecut2.ItemsSource;
            // viewModel.precutParameter.PrecutID = cbbItems.SelectedItem.ToString();
            int result = SqlHelper.Update(model.precutParameter);
            MaterialSnack("保存成功！", SnackType.SUCCESS);
            // mainWindow.NavigateToPage("Pages/F5_GeneralEfficiency/F5_1_PrecutData");
        }


        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            model = (F5_1_1_PrecutDataViewModel)this.DataContext; // 获取当前窗口的DataContext
            model.PrecutTable = (ObservableCollection<PreCutTableClass>)lvwPrecut2.ItemsSource;
            model.PrecutID = model.precutParameter.Id.ToString();
            SqlHelper.Add(model.precutParameter);
            precutList = SqlHelper.Table<PreCutModel>().ToList();
            //cbbItems.Items.Add(model.PrecutID);
            loadItem();
            mainWindow.NavigateToPage("Pages/F5_GeneralEfficiency/F5_1_PrecutData");
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            model = (F5_1_1_PrecutDataViewModel)this.DataContext; // 获取当前窗口的DataContext
            /*int IndexNow = cbbItems.SelectedIndex;
            if (precutList.Count > 1)
            {
                cbbItems.SelectedIndex = 0;
                SqlHelper.Delete(precutList[IndexNow]);
                cbbItems.Items.RemoveAt(IndexNow);
                precutList.RemoveAt(IndexNow);
                
            }*/
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
