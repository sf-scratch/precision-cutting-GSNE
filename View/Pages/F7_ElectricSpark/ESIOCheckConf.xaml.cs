using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
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
using 精密切割系统.Model.plc;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.F7_ElectricSpark
{

    

    /// <summary>
    /// ESIOCheckConf.xaml 的交互逻辑
    /// </summary>
    public partial class ESIOCheckConf : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;

        public ObservableCollection<ESIOCheckConfDataModel> DataItems { get; set; }

        public ObservableCollection<ESIOCheckConfDataModel> DataItems2 { get; set; }

        public SolidColorBrush brush3 = new SolidColorBrush(System.Windows.Media.Color.FromRgb(23, 124, 250)); //// 字体颜色 
        private int pageIndexIn = (int)Math.Ceiling((double)IOTags.ioTagsDI.Count / 16);
        private int pageIndexOut = (int)Math.Ceiling((double)IOTags.ioTagsDO.Count / 16);
        private int displayStatus = 0; //0: di and do, 1: di, 2: do
        private int curIndexPage = 0;

        public ESIOCheckConf()
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
            rightPage.btnBack.Visibility = Visibility.Visible; //右侧显示 - 返回按钮显示
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            //rightPage.btnSure.Visibility = Visibility.Visible; //右侧显示 - 确定按钮显示
            //rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked); //确定按钮事件

            SolidColorBrush brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(23, 124, 250)); //// 字体颜色 
            DataItems = new ObservableCollection<ESIOCheckConfDataModel>();
            DataItems2 = new ObservableCollection<ESIOCheckConfDataModel>();
            // 进入IO调试模式
            PlcControl.tagControl.wholeDevice.IoModelSet(1);
            GotoPage();

            // 设置数据上下文
            DataContext = this;

            mainWindow.UpdateOperatePage(OperateData.GetTab7120Operate(), OperateClickHandler);
        }

        private void GotoPage()
        {
            SolidColorBrush brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(23, 124, 250)); //// 字体颜色 
            SolidColorBrush brush2 = new SolidColorBrush(System.Windows.Media.Color.FromRgb(23, 124, 250)); //// 字体颜色 
            if (displayStatus == 0 || displayStatus == 1)
            {
                DataItems.Clear();
                for (int i = curIndexPage * 16; i < IOTags.ioTagsDI.Count; i++)
                {
                    if (DataItems.Count == 16)
                    {
                        break;
                    }
                    string tmpV = "On";
                    if (IOTags.ioTagsDI[i].value=="" || IOTags.ioTagsDI[i].value == "0" || IOTags.ioTagsDI[i].value.ToLower() == "false")
                    {
                        tmpV = "Off";
                    }
                    DataItems.Add(new ESIOCheckConfDataModel { No = IOTags.ioTagsDI[i].addr, WriteNo = IOTags.ioTagsDI[i].writeAddr
                        , Desc = IOTags.ioTagsDI[i].name, Status = tmpV, Brush = brush });
                }
            }
            if (displayStatus == 0 || displayStatus == 2)
            {
                DataItems2.Clear();
                for (int i = curIndexPage * 16; i < IOTags.ioTagsDO.Count; i++)
                {
                    if (DataItems2.Count == 16)
                    {
                        break;
                    }
                    string tmpV = "On";
                    if (IOTags.ioTagsDO[i].value == "" || IOTags.ioTagsDO[i].value == "0" || IOTags.ioTagsDO[i].value.ToLower() == "false")
                    {
                        tmpV = "Off";
                    }
                    DataItems2.Add(new ESIOCheckConfDataModel { No = IOTags.ioTagsDO[i].addr, WriteNo = IOTags.ioTagsDO[i].writeAddr
                        , Desc = IOTags.ioTagsDO[i].name, Status = tmpV, Brush = brush2 });
                }
            }
            if (displayStatus == 1)
            {
                DataItems2.Clear();
            }
            else if (displayStatus == 2)
            {
                DataItems.Clear();
            }
        }

        private void OperateClickHandler(object sender, int code)
        {
            int maxPage = Math.Max(pageIndexIn, pageIndexOut);
            switch (code)
            {
                case 7200: // 上一页
                    if (curIndexPage == 0)
                    {
                        return;
                    }
                    curIndexPage--;
                    GotoPage();
                    break;
                case 7201:// 下一页
                    if (displayStatus == 0 && curIndexPage < (maxPage - 1))
                    {
                        curIndexPage++;
                        GotoPage();
                    }
                    else if (displayStatus == 1 && curIndexPage < (pageIndexIn - 1))
                    {
                        curIndexPage++;
                        GotoPage();
                    }
                    else if (displayStatus == 2 && curIndexPage < (pageIndexOut - 1))
                    {
                        curIndexPage++;
                        GotoPage();
                    }
                    GotoPage();
                    break;
                case 7202:// 首页
                    if (curIndexPage != 0)
                    {
                        curIndexPage = 0;
                        GotoPage();
                    }
                    break;
                case 7203:// 中间页
                    if (displayStatus == 0 && maxPage > 2)
                    {
                        curIndexPage = maxPage / 2;
                        GotoPage();
                    }
                    else if (displayStatus == 1 && pageIndexIn > 2)
                    {
                        curIndexPage = maxPage / 2;
                        GotoPage();
                    }
                    else if (displayStatus == 2 && pageIndexOut > 2)
                    {
                        curIndexPage = maxPage / 2;
                        GotoPage();
                    }
                    break;
                case 7204:// 最后页
                    if (displayStatus == 0 && curIndexPage != (maxPage - 1))
                    {
                        curIndexPage = maxPage - 1;
                        GotoPage();
                    }
                    else if (displayStatus == 1 && curIndexPage != (pageIndexIn - 1))
                    {
                        curIndexPage = pageIndexIn - 1;
                        GotoPage();
                    }
                    else if (displayStatus == 2 && curIndexPage != (pageIndexOut - 1))
                    {
                        curIndexPage = pageIndexOut - 1;
                        GotoPage();
                    }
                    break;
                case 7205:// 输出和输入
                    displayStatus = 0;
                    curIndexPage = 0;
                    GotoPage();
                    break;
                case 7206:// 输入
                    displayStatus = 1;
                    curIndexPage = 0;
                    GotoPage();
                    break;
                case 7207:// 输出
                    displayStatus = 2;
                    curIndexPage = 0;
                    GotoPage();
                    break;
                default:
                    break;
            }
        }

        private void BtnSure_RightClicked(object? sender, bool e)
        {

        }
        private void BtnBack_RightClicked(object? sender, bool e)
        {
            mainWindow.NavigateToPage("MainMenu");
        }
        private void sepBd_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var clickedItem = (sender as Border)?.DataContext as ESIOCheckConfDataModel;
            //MessageBox.Show($"You clicked on: {clickedItem?.No}");             
        }

        private void sepBd_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            var clickedItem = (sender as Border)?.DataContext as ESIOCheckConfDataModel;
            //MessageBox.Show($"第二个 You clicked on: {clickedItem?.No}");
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // 退出IO调试模式
            PlcControl.tagControl.wholeDevice.IoModelSet(0);
        }

        private void Label_TouchDown(object sender, TouchEventArgs e)
        {
            OperateDO(sender);
        }

        private void Label_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            OperateDO(sender);
        }
        
        private void OperateDO(object sender)
        {
            // 确保 sender 是 Label
            if (sender is Label label)
            {
                // 获取绑定的上下文
                var dataContext = label.DataContext;

                // 假设绑定的上下文是一个类，例如 MyItem，其中包含 Status 属性
                if (dataContext is ESIOCheckConfDataModel item)
                {
                    string status = item.Status;
                    PlcControl.plc.WriteData(item.WriteNo, status.Equals("On") ? false : true, Driver.PlcDataType.Bool);
                    item.Status = status.Equals("On") ? "Off" : "On";
                }
            }
        }
    }

    public class ESIOCheckConfDataModel
    {
        //
        public string No { get; set; }
        public string Desc { get; set; }
        public string Status { get; set; }
        public string WriteNo { get; set; }

        public SolidColorBrush Brush { get; set; }
}
}
