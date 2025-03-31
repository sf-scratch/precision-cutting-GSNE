using Emgu.CV.Dnn;
using HslCommunication.Secs.Types;
using Microsoft.Win32;
using NPOI.SS.Formula.Functions;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
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
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.Utils;
using 精密切割系统.View.common;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.F3_ModelCatalog;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;
using static OpenCvSharp.ML.DTrees;

namespace 精密切割系统.View.F3_ModelCatalog
{
    /// <summary>
    /// MCDeviceDataListConf.xaml 的交互逻辑
    /// </summary>
    public partial class MCDeviceDataListConf : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private ObservableCollection<FileTableItemModel> ColList { get; set; } = new ObservableCollection<FileTableItemModel>();
        private long DeviceDataId = -1;//默认配置
        private ObservableCollection<FileTableModel> listTree = new ObservableCollection<FileTableModel>();
        private FileTableModel currentFileTable = null;
        public MCDeviceDataListConf()
        {
            InitializeComponent();
            mainWindow = Application.Current.MainWindow as MainWindow;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            rightPage = mainWindow.rightFrame.Content as RightPage;
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnSure.Visibility = Visibility.Visible;
            rightPage.btnSee.Visibility = Visibility.Visible;
            rightPage.btnSure.GlobalRunOperateFlag = true;
            rightPage.btnBack.GlobalRunOperateFlag = true;
            rightPage.btnSee.GlobalRunOperateFlag = true;
            rightPage.btnSure.SetRightClickedHandler(EnterFrom);
            rightPage.btnSee.SetRightClickedHandler(SeeFrom);
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BackFrom);
            mainWindow.UpdateOperatePage(OperateData.GetTab03Operate(), OperatePage_onClicked);
            CutUtils.UpdateGlobalRunFlag(OperateData.GetTab03Operate());
            preListView.ItemsSource = ColList;
            DeviceDataId= CurrentUtils.GetCurrentConfiguration().DeviceDataId;
            _ = initTreeData();
        }



        private void rootTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            currentFileTable = rootTreeView.SelectedItem as FileTableModel;
            if (currentFileTable == null) return;
            DirectoryName.Text = currentFileTable.Name;
            ////查询数据条数
            inputSerialNumber.Text = "";
            ClearSelect();
            currentFileTable.IsSelected = true;
            _ = SelectConfiguration(currentFileTable.Id);
        }

       
        private void preListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FileTableItemModel itemModel = preListView.SelectedItem as FileTableItemModel;
            if (itemModel != null)
            {
                inputSerialNumber.Text = itemModel.DeviceDataNo;
            }
            else
            {
                inputSerialNumber.Text = "";
            }
            
        }

        //初始化数据库数据
        private async Task initTreeData()
        {
            var list = await SqlHelper.TableAsync<FileTableModel>()
                    .Where(t => t.Name == "Root")
                    .Where(t => t.Level == 0)
                    .ToListAsync();
            //查询Root节点是否存在
            if (list.Count() == 0)
            {
                FileTableModel model = new FileTableModel();
                model.Id = 1;
                model.Name = "Root";
                model.Level = 0;
                model.ParentId = 0;
                await SqlHelper.AddAsync(model);
            }
            _ = initItemTreeData();
            _ = selectTreeData();
        }

        //初始化配置数据
        private async Task initItemTreeData()
        {
            var list01 = await SqlHelper.TableAsync<FileTableItemModel>()
                   .Where(t => t.Id == 1)
                   .ToListAsync();

            if (list01.Count() == 0)
            {
                FileTableItemModel model01 = new FileTableItemModel();
                model01.Id = 1;
                model01.DirectoryId = 1;
                model01.DeviceType = 1;
                model01.DeviceDataNo = "000";
                model01.DeviceDataId = "INCH-SAMPLE";
                String sql = "INSERT INTO file_table_item(id) VALUES(1);";
                await SqlHelper.ExecuteAsync(sql);
                await SqlHelper.UpdateAsync(model01);
                //初始化配置CH数据
                _ = initItemChData(model01);

            }

            var list02 = await SqlHelper.TableAsync<FileTableItemModel>()
                    .Where(t => t.Id == 2)
                    .ToListAsync();
            if (list02.Count() == 0)
            {
                FileTableItemModel model02 = new FileTableItemModel();
                model02.Id = 2;
                model02.DirectoryId = 1;
                model02.DeviceType = 2;
                model02.DeviceDataNo = "111";
                model02.DeviceDataId = "MM-SAMPLE";

                String sql = "INSERT INTO file_table_item(id) VALUES(2);";
                await SqlHelper.ExecuteAsync(sql);
                await SqlHelper.UpdateAsync(model02);
                //初始化配置CH数据
                _ = initItemChData(model02);
            }

        }

        //初始化配置CH数据
        private async Task initItemChData(FileTableItemModel model)
        {
            FileTableItemChModel ch1 = new FileTableItemChModel();
            ch1.ItemId = model.Id;
            ch1.ChName = GlobalParams.CH1;
            await SqlHelper.AddAsync(ch1);
            FileTableItemChModel ch2 = new FileTableItemChModel();
            ch2.ItemId = model.Id;
            ch2.ChName = GlobalParams.CH2;
            await SqlHelper.AddAsync(ch2);
            if (model.DeviceType == 1)//第一种类型才有4个CH
            {
                FileTableItemChModel ch3 = new FileTableItemChModel();
                ch3.ItemId = model.Id;
                ch3.ChName = GlobalParams.CH3;
                await SqlHelper.AddAsync(ch3);
                FileTableItemChModel ch4 = new FileTableItemChModel();
                ch4.ItemId = model.Id;
                ch4.ChName = GlobalParams.CH4;
                await SqlHelper.AddAsync(ch4);
            }

        }

        //查询全部数据（包括刷新数据）
        private async Task selectTreeData()
        {
            var list = await SqlHelper.TableAsync<FileTableModel>().ToListAsync();
            //把list数据转成树结构
            listTree.Clear();
            listTree = TreeUtils.recursionMethod(list);
            rootTreeView.ItemsSource = listTree;
            //设置默认数据
            

            DefTreeSelectItem();
        }


        //默认左侧选中项
        private void DefTreeSelectItem()
        {
            if (DeviceDataId == 0 || DeviceDataId == -1) return;
            //查询数据
            List<FileTableItemModel> itemList =  SqlHelper.Table<FileTableItemModel>()
                   .Where(t => t.Id == DeviceDataId)
                   .ToList();
            if (itemList.Count == 0) return;
        
            long DirectoryId = itemList[0].DirectoryId;

            ClearSelect();
            foreach (FileTableModel model in listTree) {
                if (model.Id== DirectoryId)
                {
                    model.IsSelected = true;
                    DirectoryName.Text = model.Name;
                    currentFileTable = model;
                    //右侧默认选中
                    _ =SelectConfiguration(DirectoryId);
                    break;
                }
                else
                {
                    if (model.Children != null && model.Children.Count > 0) {
                        foreach (FileTableModel modelCh in model.Children)
                        {
                            if (modelCh.Id == DirectoryId)
                            {
                                DirectoryName.Text = model.Name;
                                modelCh.IsSelected = true;
                                currentFileTable = modelCh;
                                //右侧默认选中
                                _ = SelectConfiguration(DirectoryId);
                                break;
                            }
                        }
                    }
                    }
            }
           
        }

        private void ClearSelect()
        {
            foreach (FileTableModel model in listTree)
            {
                model.IsSelected = false;
                if (model.Children!=null&& model.Children.Count>0)
                {
                    foreach (FileTableModel modelCh in model.Children)
                    {
                        modelCh.IsSelected = false;
                    }
                }
               
            }
        }


        //----------------------配置操作-----------------------------

        private async Task SelectConfiguration(long directoryId)
        {
            _ = updateTotal();
            var list = await SqlHelper.TableAsync<FileTableItemModel>().Where(t => t.DirectoryId == directoryId).ToListAsync();
            labDataSum.Text = list.Count.ToString();
            ColList.Clear();
            if (list.Count() > 0)
            {
                for (var i = 0; i < list.Count; i++) {
                    FileTableItemModel model = list[i];
                    ColList.Add(model);
                }
            }

            if (DeviceDataId == 0 || DeviceDataId == -1) return;
            var defaultSelectedItem = preListView.Items.Cast<FileTableItemModel>().FirstOrDefault(item => item.Id == DeviceDataId);
            if (defaultSelectedItem != null)
            {
                preListView.SelectedItem = defaultSelectedItem;
            }
        }

        //刷新总条数
        private async Task updateTotal()
        {
            var listTotal = await SqlHelper.TableAsync<FileTableItemModel>().ToListAsync();
            labSumTotal.Text = listTotal.Count.ToString();
        }

        

        //查看
        private void SeeFrom(object sender, bool e) {
            if (!String.IsNullOrEmpty(inputSerialNumber.Text))
            {
                var list = SqlHelper.Table<FileTableItemModel>()
                        .Where(t => t.DirectoryId == currentFileTable.Id)
                        .Where(t => t.DeviceDataNo == inputSerialNumber.Text)
                        .ToList();
                if (list.Count > 0)
                {
                    FileTableItemModel li = list[0];
                    ToDevicePageData(li.Id, true);
                }
            }
            else
            {
                FileTableItemModel itemModel = preListView.SelectedItem as FileTableItemModel;
                if (itemModel != null)
                {
                    ToDevicePageData(itemModel.Id, true);
                }
            }
        }
        private void EnterFrom(object sender, bool e)
        {
            if (!String.IsNullOrEmpty(inputSerialNumber.Text))
            {
                var list = SqlHelper.Table<FileTableItemModel>()
                        .Where(t => t.DirectoryId == currentFileTable.Id)
                        .Where(t => t.DeviceDataNo == inputSerialNumber.Text)
                        .ToList();
                if (list.Count > 0)
                {
                    FileTableItemModel li = list[0];
                    ToDevicePageData(li.Id,false);
                }
            }
            else
            {
                FileTableItemModel itemModel = preListView.SelectedItem as FileTableItemModel;
                if (itemModel!=null)
                {
                    ToDevicePageData(itemModel.Id, false);
                }
            }
        }

        private void BackFrom(object sender, bool e) {
            if (CommonCheck.CutModeCheck())
            {
                switch (GlobalParams.cutStatusInfo)
                {
                    case 0:
                        mainWindow.NavigateToPage("Pages/F2_ManualOperation/MQSemiAutomaticCuttingConf");
                        break;
                    case 1:
                        mainWindow.NavigateToPage("Pages/F2_ManualOperation/MQSemiAutomaticCuttingRun");
                        break;
                    case 2:
                        mainWindow.NavigateToPage("Pages/F2_ManualOperation/MQSemiAutomaticCuttingStop");
                        break;
                    default:
                        mainWindow.NavigateToPage("MainMenu");
                        break;
                }
            } else
            {
                mainWindow.NavigateToPage("MainMenu");
            }
        }
        

        //跳转详情页面
        private void ToDevicePageData(long id, bool look)
        {
            // Uri uri = new Uri($"View/Pages/F3_ModelCatalog/MCDeviceDataConf.xaml?id={id}", UriKind.Relative);
            // mainWindow.mainFrame.Source = uri;
            if (look)
            {
                mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCDeviceDataConf", $"id={id}&look={look}");
            }
            else
            {
                string paramsData = Uri.EscapeDataString($"id={id}&look={look}");
                mainWindow.NavigateToPage("Pages/passowrd/PasswordPage"
                    , $"pageName=Pages/F3_ModelCatalog/MCDeviceDataConf&urlParams={paramsData}");
            }
        }


        private void OperatePage_onClicked(object? sender, int code)
        {
            Uri uri;
            FileTableItemModel itemModel=null;
            List<FileTableItemModel> listItemModel = null;
            switch (code)
            {
                case 308://添加目录
                    var list =  SqlHelper.Table<FileTableModel>()
                    .Where(t => t.Level == 0)
                    .ToList();
                    if (list.Count>0)//主目录还存在
                    {
                    
                        mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCAddDeviceDirectoryConf", $"id={list[0].Id}");
                    }
                    break;
                case 302://拷贝
                    if (!String.IsNullOrEmpty(inputSerialNumber.Text))
                    {
                        listItemModel = SqlHelper.Table<FileTableItemModel>()
                                .Where(t => t.DirectoryId == currentFileTable.Id)
                                .Where(t => t.DeviceDataNo == inputSerialNumber.Text)
                                .ToList();
                        if (listItemModel.Count > 0)
                        {
                             itemModel = listItemModel[0];
                            mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCCopyDeviceDirectoryConf", $"id={itemModel.Id}");
                        }
                    }
                    else
                    {
                         itemModel = preListView.SelectedItem as FileTableItemModel;
                        if (itemModel != null)
                        {
                            mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCCopyDeviceDirectoryConf", $"id={itemModel.Id}");
                        }
                    }
                    break;
                case 303://移动
                    if (!String.IsNullOrEmpty(inputSerialNumber.Text))
                    {
                        listItemModel = SqlHelper.Table<FileTableItemModel>()
                                .Where(t => t.DirectoryId == currentFileTable.Id)
                                .Where(t => t.DeviceDataNo == inputSerialNumber.Text)
                                .ToList();
                        if (listItemModel.Count > 0)
                        {
                           
                            itemModel = listItemModel[0];
                            if (itemModel.Id == 1 || itemModel.Id == 2) return;
                            mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCMoveDeviceDirectoryConf", $"id={itemModel.Id}");
                        }
                    }
                    else
                    {
                        itemModel = preListView.SelectedItem as FileTableItemModel;
                        if (itemModel != null)
                        {
                            if (itemModel.Id == 1 || itemModel.Id == 2) return;
                            mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCMoveDeviceDirectoryConf", $"id={itemModel.Id}");
                        }
                    }
                    break;
                case 304://重命名
                    if (!String.IsNullOrEmpty(inputSerialNumber.Text))
                    {
                        listItemModel = SqlHelper.Table<FileTableItemModel>()
                                .Where(t => t.DirectoryId == currentFileTable.Id)
                                .Where(t => t.DeviceDataNo == inputSerialNumber.Text)
                                .ToList();
                        if (listItemModel.Count > 0)
                        {
                            itemModel = listItemModel[0];
                            if (itemModel.Id == 1 || itemModel.Id == 2) return;
                            mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCRenameDeviceDirectoryConf", $"id={itemModel.Id}");
                        }
                    }
                    else
                    {
                        itemModel = preListView.SelectedItem as FileTableItemModel;
                        if (itemModel != null)
                        {
                            if (itemModel.Id == 1 || itemModel.Id == 2) return;
                            mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCRenameDeviceDirectoryConf", $"id={itemModel.Id}");
                        }
                    }
                    break;
                case 305://删除
                    if (!String.IsNullOrEmpty(inputSerialNumber.Text))
                    {
                        listItemModel = SqlHelper.Table<FileTableItemModel>()
                                .Where(t => t.DirectoryId == currentFileTable.Id)
                                .Where(t => t.DeviceDataNo == inputSerialNumber.Text)
                                .ToList();
                        if (listItemModel.Count > 0)
                        {
                            itemModel = listItemModel[0];
                            if (itemModel.Id == 1 || itemModel.Id == 2) return;
                            mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCDeleteDeviceDirectoryConf", $"id={itemModel.Id}");
                        }
                    }
                    else
                    {
                        itemModel = preListView.SelectedItem as FileTableItemModel;
                        if (itemModel != null)
                        {
                            if (itemModel.Id == 1 || itemModel.Id == 2) return;
                            mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCDeleteDeviceDirectoryConf", $"id={itemModel.Id}");
                        }
                    }
                        break;
                case 306://指定参数
                    itemModel = preListView.SelectedItem as FileTableItemModel;
                    CurrentConfigurationModel currentConfigurationModel = CurrentUtils.GetCurrentConfiguration();
                    currentConfigurationModel.DeviceDataId = itemModel.Id;
                    currentConfigurationModel.ChannelNum = GlobalParams.CH1;
                    CurrentUtils.UpdateCurrentConfiguration(currentConfigurationModel);
                    MaterialSnackUtils.MaterialSnack("保存成功！", MaterialSnackUtils.SnackType.SUCCESS);
                    break;
                case 307://子目录删除
                    FileTableModel selectedItem = rootTreeView.SelectedItem as FileTableModel;
                    if (selectedItem != null)
                    {
                        mainWindow.NavigateToPage("Pages/F3_ModelCatalog/MCDeleteDirectoryConf", $"id={selectedItem.Id}");
                    }
                    break;
                case 309://导入配置
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Filter = "DB文件 |*.db";
                    openFileDialog.Title = "选中db文件";
                    if (openFileDialog.ShowDialog() == true)
                    { 
                        readBackDb(openFileDialog.FileName);
                    }
                    break;
                case 310://导出配置
                    createBackDb();
                    break;
            }
        }

        //创建新的数据备份库
        private void createBackDb()
        {
            long timeStampMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string connstr = System.IO.Path.Combine(desktopPath, $"{timeStampMilliseconds}.db");
            SQLiteConnection db =  new SQLiteConnection(connstr, false);
            db.CreateTable<FileTableModel>();//目录文件夹
            db.CreateTable<FileTableItemModel>();//配置文件
            db.CreateTable<FileTableItemChModel>();//目录库
            //查询原来库中对应信息后加入新库中
            //查询表FileTableModel
            List<FileTableModel> FileTableList =  SqlHelper.Table<FileTableModel>().ToList();
            foreach (FileTableModel model in FileTableList)
            {
                db.Insert(model);
            }
            //查询表FileTableItemModel
            List<FileTableItemModel> FileTableItemList = SqlHelper.Table<FileTableItemModel>().ToList();
            foreach (FileTableItemModel model in FileTableItemList)
            {
                db.Insert(model);
            }
            //查询表FileTableItemChModel
            List<FileTableItemChModel> FileTableItemChModelList = SqlHelper.Table<FileTableItemChModel>().ToList();
            foreach (FileTableItemChModel model in FileTableItemChModelList)
            {
                db.Insert(model);
            }
            db.Close();
            MaterialSnackUtils.MaterialSnack($"导出成功：{connstr}", MaterialSnackUtils.SnackType.SUCCESS);
        }
        //读取数据备份库
        private async void readBackDb(string filePath)
        {

            //删除表
            SqlHelper.getSQLiteConnection().DropTable<FileTableModel>();
            SqlHelper.getSQLiteConnection().DropTable<FileTableItemModel>();
            SqlHelper.getSQLiteConnection().DropTable<FileTableItemChModel>();
            //再重新创建表
            SqlHelper.getSQLiteConnection().CreateTable<FileTableModel>();
            SqlHelper.getSQLiteConnection().CreateTable<FileTableItemModel>();
            SqlHelper.getSQLiteConnection().CreateTable<FileTableItemChModel>();

            //连接备份数据库
            SQLiteConnection db = new SQLiteConnection(filePath, false);
            //读取FileTableModel
            List<FileTableModel> FileTableBackList = db.Table<FileTableModel>().ToList();
            foreach (FileTableModel model in FileTableBackList)
            {
                SqlHelper.Add(model);
            }
            //读取FileTableItemModel
            List<FileTableItemModel> FileTableItemBackList = db.Table<FileTableItemModel>().ToList();
            foreach (FileTableItemModel model in FileTableItemBackList)
            {
                SqlHelper.Add(model);
            }
            //读取FileTableItemChModel
            List<FileTableItemChModel> FileTableItemChBackList = db.Table<FileTableItemChModel>().ToList();
            foreach (FileTableItemChModel model in FileTableItemChBackList)
            {
                SqlHelper.Add(model);
            }
            MaterialSnackUtils.MaterialSnack("导入成功！", MaterialSnackUtils.SnackType.SUCCESS);
            ////刷新数据
            _ = initTreeData();
        }
    }

}
