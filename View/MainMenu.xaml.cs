using System;
using System.Collections.Generic;
using System.Data.Common;
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
using 精密切割系统.Assets.config.menu;
using 精密切割系统.View.Controls;
using static 精密切割系统.Utils.Tools;
using 精密切割系统.View.page.right;
using MenuItem = 精密切割系统.Assets.config.menu.MenuItem;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using Path = System.IO.Path;
using 精密切割系统.Assets.config;
using 精密切割系统.Helpers;
using static 精密切割系统.Helpers.MaterialSnackUtils;
using 精密切割系统.Utils;
using 精密切割系统.Driver;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.Model.plc;
using 精密切割系统.ViewModel;
using 精密切割系统.database.db.modle;
using System.Reflection.PortableExecutable;
using 精密切割系统.Model.cut;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.View.Pages.F4_BladeMaintenance;
using Prism.Navigation.Regions;

namespace 精密切割系统.View
{
    /// <summary>
    /// MainMenu.xaml 的交互逻辑
    /// </summary>
    public partial class MainMenu : Page
    {

        private MainWindow? mainWindow;
        private static RightPage? rightPage;
        private String CachedMenuKey = "CachedMenuData";
        private UserDefineDataModel userDefineDataModel = null;
        private String menuId = "";
        public MainMenu()
        {
            InitializeComponent();
            mainWindow = Application.Current.MainWindow as MainWindow;
            rightPage = mainWindow.rightFrame.Content as RightPage;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            rightPage = mainWindow.rightFrame.Content as RightPage;
            // OnFrameSourceChanged();
            //获取菜单数据
            MenuItem item = Application.Current.Properties[CachedMenuKey] as MenuItem;
            if (item==null)
            {
                item = MenuData.GetF1Menu();
            }
            menuId = QueryUtils.GetValueFromQueryParams(this, "menuId");
            //输入密码后跳转回来的数据处理
            if (string.IsNullOrEmpty(menuId))
            {
                UpdateMenu(item); ;
            }
            else
            {
                switch (menuId)
                {
                    case "5":
                        UpdateMenu(MenuData.GetF5Menu());
                        break;
                    case "7":
                        UpdateMenu(MenuData.GetF7Menu());
                        break;
                }
            }

            _ = initUserDefine();
        }

        //初始化右侧按键等
        private void InitRightPage(MenuItem item)
        {
            rightPage = mainWindow.rightFrame.Content as RightPage;
            if (item.list[0].Code == 2)
            {
                rightPage.PanelAction.Visibility = Visibility.Collapsed;
                rightPage.ShowTemplate.Visibility = Visibility.Visible;
                rightPage.MachinePanel.Visibility = Visibility.Visible;
            }
            else
            {
                rightPage.PanelAction.Visibility = Visibility.Visible;
                rightPage.btnBack.Visibility = Visibility.Visible;
            }
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            OperatePage operatePage = mainWindow.operateFrame.Content as OperatePage;
            mainWindow.shortcutBottomBtnSel = true;
            mainWindow.shortcutTopBtnSel = false;
            if (operatePage == null)
            {
                // mainWindow.isNavigating = true;
                // mainWindow.operateFrame.Navigate(new Uri("View/Pages/operate/OperatePage.xaml", UriKind.Relative));
                operatePage.SetOperateShowType(0);
            } else
            {
                mainWindow.shortcutBottomBtnSel = true;
                mainWindow.SetOperateBtn(operatePage);
            }
            GlobalParams.currentOperateBeanList = new List<OperateBean>();
            
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            // rightPage.PanelAction.Visibility = Visibility.Collapsed;
            UpdateMenu(MenuData.GetF1Menu());
        }

        //初始化数据
        private async Task initUserDefine()
        {
            //查询用不基础配置信息
            var list = await SqlHelper.TableAsync<UserDefineDataModel>()
                   .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                userDefineDataModel = new UserDefineDataModel();
                await SqlHelper.AddAsync(userDefineDataModel);
            }
            else
            {
                userDefineDataModel = (UserDefineDataModel)list[0];
            }
            //显示设定的数据
            rightPage.machineID.Text = userDefineDataModel.MachineId;
        }


        //动态创建多个菜单
        private void InitMenuView(MenuItem item)
        {
            //是首页判断
            GlobalParams.currentPageIsHome = item.IsHome;
            MenuGrid.Children.Clear();
            menuTitle.Text = item.Title;
            List<MenuBean> list = item.list;
            if (list.Count>4)//两行
            {
                for (int row = 0; row < 2; row++)
                {

                    MenuBean bean;
                    if (row == 0)
                    {
                        for (int col = 0; col < 4; col++) {
                            bean = list[col];
                            addMenuButton(row, col, bean);
                        }
                    }
                    else
                    {
                        for (int col = 0; col < list.Count-4; col++)
                        {
                            bean = list[col + row + 3];
                            addMenuButton(row, col, bean);
                        }
                    }
                }
            }
            else
            {
                for (int col = 0; col < list.Count; col++)
                {
                    addMenuButton(0, col, list[col]);
                }
            }  
        }

        private void addMenuButton(int row, int col,MenuBean bean)
        {
            MenuButton menuButton = new MenuButton(bean);
            menuButton.Width = 302;
            menuButton.Height = 244;
            menuButton.SetValue(Grid.RowProperty, row);
            menuButton.SetValue(Grid.ColumnProperty, col);
            menuButton.MenuClicked += MenuButton_MenuClicked;
            MenuGrid.Children.Add(menuButton);
        }

        private void MenuButton_MenuClicked(object? sender, MenuBean bean)
        {
            Tools.LogInfo("当点事件：" + bean.Code + ":" + bean.Title);
            if (bean.Type == 2)
            {
                mainWindow.NavigateToPage(bean.PageUrl);
                return;
            }
            switch (bean.Code)
            {
                case 1:
                    {
                        rightPage.PanelAction.Visibility = Visibility.Collapsed;
                        rightPage.btnBack.Visibility = Visibility.Collapsed;
                        break;
                    }
                case 2:
                    {
                        rightPage.PanelAction.Visibility = Visibility.Visible;
                        rightPage.btnBack.Visibility = Visibility.Visible;
                        UpdateMenu(MenuData.GetF2Menu());
                        break;
                    }
                case 4:
                    {
                        rightPage.PanelAction.Visibility = Visibility.Visible;
                        rightPage.btnBack.Visibility = Visibility.Visible;
                        UpdateMenu(MenuData.GetF4Menu());
                        break;
                    }
                case 5:
                    {
                        if (havePassWord()) {
                            mainWindow.NavigateToPage("Pages/passowrd/PasswordPage", $"menuId={bean.Code}");
                        }
                        else
                        {
                            rightPage.PanelAction.Visibility = Visibility.Visible;
                            rightPage.btnBack.Visibility = Visibility.Visible;
                            UpdateMenu(MenuData.GetF5Menu());
                        }
                        break;
                    }
                //case 6:
                //    {
                //        rightPage.PanelAction.Visibility = Visibility.Visible;
                //        rightPage.btnBack.Visibility = Visibility.Visible;
                //        UpdateMenu(MenuData.GetF6Menu());
                //        break;
                //    }
                case 7:
                    {
                        if (havePassWord())
                        {
                            mainWindow.NavigateToPage("Pages/passowrd/PasswordPage", $"menuId={bean.Code}");
                        }
                        else
                        {
                            rightPage.PanelAction.Visibility = Visibility.Visible;
                            rightPage.btnBack.Visibility = Visibility.Visible;
                            UpdateMenu(MenuData.GetF7Menu());
                        }
                        
                        break;
                    }
                case 8:
                    {
                        if (!GlobalParams.onlineFlag)
                        {
                            mainWindow.NavigateToPage(bean.PageUrl);
                            break;
                        }
                        // 电火花修刀
                        if (CommonCheck.TruingStatusCheck())
                        {
                            // 新发送PLC进入模式，当模式进入成功后，跳转页面
                            MenuButton menu = sender as MenuButton;
                            menu.resetState = false;
                            MaterialSnackUtils.MaterialSnack("进入电火花修刀模式中...", SnackType.WARNING, 0);
                            PlcControl.tagControl.sparkRepairKnife.EnterElectrical(1);
                            GlobalParams.globalRunFlag = true;
                            // 监听状态，如果模式准备完成，则跳转页面
                            Task.Run(() =>
                            {
                                bool flag = Tools.WaitForValue(DeviceKey.sharpenStatusKey, 1);
                                GlobalParams.globalRunFlag = false;
                                if (flag)
                                {
                                    mainWindow.NavigateToPage(bean.PageUrl);
                                }
                                else
                                {   
                                    menu.resetState = true;
                                    MaterialSnackUtils.MaterialSnack("进入电火花修刀模式失败！", SnackType.WARNING, 0);
                                    // 进入失败，退出模式
                                    PlcControl.tagControl.sparkRepairKnife.EnterElectrical(0);
                                }
                            });
                        }
                        break;
                    }
                case 202:
                    mainWindow.NavigateToPage(bean.PageUrl);
                    break;
                case 401:
                    ContainerLocator.Container.Resolve<IRegionManager>().RequestNavigate(RegionName.MainRegion, nameof(BladeReplacementConfiguration));
                    break;
                case 439:
                    ContainerLocator.Container.Resolve<IRegionManager>().RequestNavigate(RegionName.MainRegion, nameof(AutoCutSelectConfig));
                    break;
                case 402:
                    if (!GlobalParams.onlineFlag)
                    {
                        mainWindow.NavigateToPage(bean.PageUrl);
                        break;
                    }
                    // 测高
                    if (CommonCheck.MlignStatusCheck())
                    {
                        // 新发送PLC进入模式，当模式进入成功后，跳转页面
                        // 进入测高模式
                        MaterialSnackUtils.MaterialSnack("进入测高模式中...", SnackType.WARNING, 0);
                        MenuButton menu = sender as MenuButton;
                        menu.resetState = false;
                        PlcControl.tagControl.bladeMantance.RunBladeSetup(1);
                        PlcControl.tagControl.wholeDevice.SetPanelButtonsStauts(1);
                        GlobalParams.globalRunFlag = true;
                        // 监听状态，如果模式准备完成，则跳转页面
                        Task.Run(() =>
                        {
                            bool flag = Tools.WaitForValue(DeviceKey.bladeMantanceStatusKey, 1);
                            GlobalParams.globalRunFlag = false;
                            if (flag)
                            {
                                mainWindow.NavigateToPage(bean.PageUrl);
                            }
                            else
                            {
                                menu.resetState = true;
                                PlcControl.tagControl.bladeMantance.RunBladeSetup(0);
                                PlcControl.tagControl.wholeDevice.SetPanelButtonsStauts(0);
                                MaterialSnackUtils.MaterialSnack("进入刀片测高失败！", SnackType.WARNING, 0);
                            }
                        });
                    }
                    break;
                case 203:
                case 706:
                case 440:
                case 204:
                case 607:
                    mainWindow.NavigateToPage(bean.PageUrl);
                    break;
                case 709:
                    PlcControl.tagControl.flange.JoinTrimming(1);
                    mainWindow.NavigateToPage(bean.PageUrl);
                    break;
                default:

                    break;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.NavigateToPage("F3TypeCatalog");
            //MainWindow.mainFrame.Source = new Uri("View/F3TypeCatalog.xaml", UriKind.Relative);
        }

        private void Camera_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.NavigateToPage("camera/Camera");
        }


        //刷新菜单数据
        public void UpdateMenu(MenuItem menuItem)
        {
            //初始化右侧按键等
            InitRightPage(menuItem);
            Application.Current.Properties[CachedMenuKey] = menuItem;
            InitMenuView(menuItem);
        }

        //是否需要密码
        private Boolean havePassWord() {
            if (!string.IsNullOrEmpty(userDefineDataModel.SystemPassword))
            {
                //查询录入的密码时间戳
                /*if (userDefineDataModel.SystemPasswordTime == 0)
                {
                    return true;
                }
                long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                //判断当前录入时间是否间隔2小时
                if (currentTimestamp - userDefineDataModel.SystemPasswordTime > 1000 * 60 * 60 * 2)
                {
                    return true;
                }
                return false;*/
                return true;
            }
            return false;
        }
       
    }



   
}
