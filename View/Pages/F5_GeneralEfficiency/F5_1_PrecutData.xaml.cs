using Emgu.CV.Dnn;
using HslCommunication.Core.IMessage;
using NPOI.OpenXmlFormats.Spreadsheet;
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
using 精密切割系统.Assets.config.menu;
using 精密切割系统.database.db.modle;
using 精密切割系统.Helpers;
using 精密切割系统.Model.sqlite;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages
{
    /// <summary>
    /// F5_1_PrecutData.xaml 的交互逻辑
    /// </summary>
    public partial class F5_1_PrecutData : Page
    {
        public F5_1_PrecutData()
        {
            InitializeComponent();
        }

        public static ObservableCollection<PreCutModel> precutList = new ObservableCollection<PreCutModel>();
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;
        public static PreCutModel currentModel = null;//当前选中项
        private string surePage = null;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {

            surePage = QueryUtils.GetValueFromQueryParams(this, "surePage");
            mainWindow = Application.Current.MainWindow as MainWindow;
            rightPage = mainWindow.rightFrame.Content as RightPage;
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnSure.Visibility = Visibility.Visible;
            rightPage.btnSure.BackFlag = false;
            rightPage.btnSure.SetRightClickedHandler(btnDataDetails);
            rightPage.btnBack.SetRightClickedHandler(back);

            mainWindow.UpdateOperatePage(OperateData.GetTab5100Operate(), OperateClickHandler);

            //数据封装
            //precutList = SqlHelper.Table<PreCutModel>().Where(t => t.IsDelete == "False").ToList();

            updateData();
        }

        private void updateData()
        {
            List<PreCutModel> list = SqlHelper.Table<PreCutModel>().ToList();
            precutList.Clear();
            foreach (var precut in list)
            {
                precutList.Add(precut);
            }

            preListView.ItemsSource = precutList;
            getDefData(list);

        }

        private void preListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentModel = preListView.SelectedItem as PreCutModel;
            if (currentModel!=null)
            {
                precutNo.Text = currentModel.PrecutNo;
            }
        }

        private void back(object sender, bool e)
        {
            mainWindow.NavigateToPage("MainMenu");
            //mainWindow.GotoF5();
        }

        private void btnDataDetails(object sender, bool e)
        {
            string t_precutNo = precutNo.Text;
            if (!string.IsNullOrEmpty(t_precutNo))
            {
                List<PreCutModel> list = SqlHelper.Table<PreCutModel>().Where(t => t.PrecutNo == t_precutNo).ToList();
                if (list.Count == 1)
                {
                    currentModel = list[0];
                    string page = "Pages/F5_GeneralEfficiency/F5_1_1_PrecutDataDetails";
                    if (!string.IsNullOrEmpty(surePage) && surePage == "len") {
                        page = "Pages/F5_GeneralEfficiency/F5_1_2_PrecutDataDetails";
                    }
                    mainWindow.NavigateToPage(page, $"PrecutNo={currentModel.PrecutNo}");
                }
            }
        }

        private void getDefData(List<PreCutModel> list)
        {
            CurrentConfigurationModel model = CurrentUtils.GetCurrentConfiguration();
            if (model.DeviceDataId!=null)
            {
                //查询数据
                var tableList =  SqlHelper.Table<FileTableItemModel>()
                       .Where(t => t.Id == model.DeviceDataId)
                       .ToList();
                if (tableList.Count>0)
                {
                    FileTableItemModel m = tableList[0];
                    for (int i = 0; i < list.Count; i++) {
                        PreCutModel preCutModel = list[i];
                        if (m.PrecutProcessNo.Equals(preCutModel.PrecutNo))
                        {
                            preListView.SelectedIndex = i;
                            //preListView.SelectedItem = preCutModel;
                            break;
                        }
                    }
                   
                }
            }
        }

        private void OperateClickHandler(object sender, int code)
        {
            string t_precutNo = precutNo.Text;
            switch (code)
            {
                case 5100: // 复制
                    if (!string.IsNullOrEmpty(t_precutNo))
                    {
                        List<PreCutModel> list = SqlHelper.Table<PreCutModel>().Where(t => t.PrecutNo == t_precutNo).ToList();
                        if (list.Count == 1)
                        {
                            currentModel = list[0];
                            mainWindow.NavigateToPage("Pages/F5_GeneralEfficiency/F5_1_PrecutDataCopy", $"id={currentModel.Id}");
                        }
                    }
                    else
                    {
                        if (currentModel == null) return;
                        mainWindow.NavigateToPage("Pages/F5_GeneralEfficiency/F5_1_PrecutDataCopy", $"id={currentModel.Id}");
                    }
                    break;
                case 5101:// 删除
                    if (!string.IsNullOrEmpty(t_precutNo))
                    {
                        List<PreCutModel> list = SqlHelper.Table<PreCutModel>().Where(t => t.PrecutNo == t_precutNo).ToList();
                        if (list.Count == 1)
                        {
                            currentModel = list[0];
                            if (currentModel == null || currentModel.PrecutNo.Equals("0") || currentModel.PrecutNo.Equals("1")) return;
                            mainWindow.NavigateToPage("Pages/F5_GeneralEfficiency/F5_1_PrecutDataDelete", $"id={currentModel.Id}");
                        }
                    }
                    else
                    {
                        if (currentModel == null || currentModel.PrecutNo.Equals("0") || currentModel.PrecutNo.Equals("1")) return;
                        mainWindow.NavigateToPage("Pages/F5_GeneralEfficiency/F5_1_PrecutDataDelete", $"id={currentModel.Id}");
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
