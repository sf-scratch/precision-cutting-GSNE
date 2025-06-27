using System;
using System.Collections.Generic;
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
using 精密切割系统.database.db.modle;
using 精密切割系统.Helpers;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.passowrd
{
    /// <summary>
    /// PasswordPage.xaml 的交互逻辑
    /// </summary>
    public partial class PasswordPage : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;
        private UserDefineDataModel userDefineDataModel = null;
        private String menuId="";
        private String urlParams = "";
        private String pageName = "";
        public PasswordPage()
        {
            InitializeComponent();
            mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            rightPage = mainWindow.rightFrame.Content as RightPage;
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnSure.Visibility = Visibility.Visible;
            rightPage.btnBack.BackFlag = true;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            rightPage.btnSure.GlobalRunOperateFlag = true;
            rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked);
            operatePage = mainWindow.operateFrame.Content as OperatePage;
            operatePage.UpdateOperate([]);
            menuId = QueryUtils.GetValueFromQueryParams(this, "menuId");
            string urlParamsTemp = QueryUtils.GetValueFromQueryParams(this, "urlParams");
            if (!string.IsNullOrEmpty(urlParamsTemp))
            {
                urlParams = Uri.UnescapeDataString(urlParamsTemp);
            }
            pageName = QueryUtils.GetValueFromQueryParams(this, "pageName");
            _ = initUserDefine();
        }

        //初始化数据
        private async Task initUserDefine()
        {
            //查询用不基础配置信息
            var list = await SqlHelper.TableAsync<UserDefineDataModel>()
                   .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() > 0)
            {
                userDefineDataModel = list[0];
            }
        }

        private void BtnBack_RightClicked(object? sender, bool e) 
        {
            mainWindow.NavigateToPage("MainMenu");
        }

        private void BtnSure_RightClicked(object? sender, bool e) {
            if (!string.IsNullOrEmpty(inputText.Password))
            {
                if (userDefineDataModel.SystemPassword.Equals(inputText.Password))
                {
                    _ = updatePassWordTime();
                    if (!string.IsNullOrEmpty(pageName))
                    {
                        mainWindow.NavigateToPage(pageName, urlParams);
                    } else
                    {
                        mainWindow.NavigateToPage("MainMenu", $"menuId={menuId}");
                    }
                }
                else
                {
                    MaterialSnackUtils.MaterialSnack("密码错误", MaterialSnackUtils.SnackType.ERROR);
                }
            }
        }
        //刷新当前时间戳
        private async Task updatePassWordTime()
        {
            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            userDefineDataModel.SystemPasswordTime = currentTimestamp;
            SqlHelper.UpdateAsync(userDefineDataModel);

        }

        private void inputText_TouchDown(object sender, TouchEventArgs e)
        {
            mainWindow.ShowKeyboardPage(1);
        }

        private void inputText_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            mainWindow.ShowKeyboardPage(1);
        }
    }
}
