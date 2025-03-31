using HslCommunication.Profinet.OpenProtocol;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using 精密切割系统.Model.sqlite;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.F4_BladeMaintenance
{
    /// <summary>
    /// BmSharpenParameter.xaml 的交互逻辑
    /// </summary>
    public partial class BmSharpenParameter : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;

        private BmSharpenParameterModel? _model;
        private List<BmSharpenParameterModel> list;
        //3列数据展示
        ObservableCollection<BmSharpenParameterModel> Col0_99 { get; set; } = new ObservableCollection<BmSharpenParameterModel>();
        public BmSharpenParameter()
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
            rightPage.btnSure.Visibility = Visibility.Visible; //右侧显示 - 确定按钮显示、           
            rightPage.btnBack.Visibility = Visibility.Visible; //右侧显示 - 返回按钮显示
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked); //返回按钮事件 
            rightPage.btnSure.BackFlag = false; //确定按钮不执行返回，执行自己的代理事件。
            rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked); //确定按钮事件
            mainWindow.UpdateOperatePage(OperateData.GetTab4400Operate(), OperatePage_onClicked);
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            GlobalParams.globalRunFlag = true;
            // 退出切割模式
            PlcControl.tagControl.cutting.EnterFullAutoInit(0);
            GlobalParams.globalRunFlag = false;
            mainWindow.NavigateToPage("MainMenu");
        }

        private void OperatePage_onClicked(object? sender, int code)
        {
            this.BtnOnClicked(code);
        }
        private async void BtnOnClicked(int code) {
            //var item = pre_listView.SelectedItem as BmSharpenParameterModel;
            //if (item == null)
            //{
            //    return;
            //}
            _model = null;
            string BladeLotID = labParameterNo.Text.Trim();
            if (!string.IsNullOrEmpty(BladeLotID))
            {
                List<BmSharpenParameterModel> list = await SqlHelper.TableAsync<BmSharpenParameterModel>().Where(t => t.BladeLotID == BladeLotID).ToListAsync();
                if (list.Count == 1)
                {
                    _model = list[0];
                }
            }
            if (_model == null)
            {
                return;
            }
            //拷贝
            if (code == 4400)
            {
                //List<BmSharpenParameterModel> list = await SqlHelper.QueryAsync<BmSharpenParameterModel>("select * from bm_sharpen_parameter order by id desc limit 1");
                //string BladeLotID = "0";
                //if (list.Count > 0) {
                //    BladeLotID = list[0].Id.ToString();
                //}
                //mainWindow.mainFrame.Source = new Uri($"View/Pages/F4_BladeMaintenance/BmSharpenParameterForm.xaml?Id={item.Id}&Flag=copy&BladeLotID={BladeLotID}" , UriKind.Relative);
                //页面 中输入参数号 按enter 确定执行拷贝
                mainWindow.mainFrame.Source = new Uri($"View/Pages/F4_BladeMaintenance/FADressDataCopyConf.xaml?Id={_model.Id}", UriKind.Relative);
            }
            //删除
            if (code == 4401)
            {
                //string message = "Are you sure you want to delete?";
                //string title = "Delete Confirmation";
                //MessageBoxResult result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);

                //if (result == MessageBoxResult.Yes)
                //{
                //    // 执行删除操作 
                //    int delInt = await SqlHelper.DeleteAsync(_model);
                //    if (delInt == 1)
                //    {
                //        Col0_99.RemoveAt(pre_listView.SelectedIndex);
                //    }
                //}
                if (!string.Equals(_model.BladeLotID, "0")) {
                    mainWindow.mainFrame.Source = new Uri($"View/Pages/F4_BladeMaintenance/FADressDataDelConf.xaml?Id={_model.Id}", UriKind.Relative);
                }
            }
        }

        private void BtnSure_RightClicked(object? sender, bool e)
        {
            //var item = pre_listView.SelectedItem as BmSharpenParameterModel;
            //if (item == null)
            //{
            //    return;
            //}
            _ = loadToParameter();
        }

        private async Task loadToParameter()
        {
            string BladeLotID = labParameterNo.Text.Trim();
            if (!string.IsNullOrEmpty(BladeLotID))
            {
                List<BmSharpenParameterModel> list = await SqlHelper.TableAsync<BmSharpenParameterModel>().Where(t => t.BladeLotID == BladeLotID).ToListAsync();
                if (list.Count == 1)
                {
                    _model = list[0];
                    mainWindow.mainFrame.Source = new Uri($"View/Pages/F4_BladeMaintenance/BmSharpenParameterForm.xaml?Id={_model.Id}&Flag=edit&BladeLotID={_model.BladeLotID}", UriKind.Relative);
                }
            }

        }


        private async Task initData()
        {
            Col0_99.Clear();
            list = await SqlHelper.TableAsync<BmSharpenParameterModel>().ToListAsync();
            //数据不存在，则初始化数据
            if (list != null && list.Count > 0)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Col0_99.Add(list[i]);
                }
            }
            else {
                var nowItem = new BmSharpenParameterModel() { Id = 1, BladeLotID = "0" };
                int id = await SqlHelper.AddAsync(nowItem);
                Col0_99.Add(nowItem);
            }
            pre_listView.ItemsSource = Col0_99;//将数据绑定到列表
        }

        private void pre_listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BmSharpenParameterModel itemModel = pre_listView.SelectedItem as BmSharpenParameterModel;
            if (itemModel != null)
            {
                labParameterNo.Text = itemModel.BladeLotID;
            }
            else
            {
                labParameterNo.Text = "";
            }
        }
    }
}
