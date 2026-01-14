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

namespace 精密切割系统.View.Pages.passowrd
{
    /// <summary>
    /// PasswordPage.xaml 的交互逻辑
    /// </summary>
    public partial class PasswordPage : Page
    {
        private MainWindow? _mainWindow;
        private RightPage? _rightPage;
        private OperatePage? _operatePage;
        private UserDefineDataModel _userDefineDataModel;
        private string menuId = "";
        private string urlParams = "";
        private string pageName = "";

        public PasswordPage()
        {
            InitializeComponent();
            _mainWindow = Application.Current.MainWindow as MainWindow;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_mainWindow is not null)
            {
                _rightPage = _mainWindow.rightFrame.Content as RightPage ?? new RightPage();
                _rightPage.PanelAction.Visibility = Visibility.Visible;
                _rightPage.btnBack.Visibility = Visibility.Visible;
                _rightPage.btnBack.BackFlag = true;
                _rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
                _rightPage.btnSure.Visibility = Visibility.Visible;
                _rightPage.btnSure.GlobalRunOperateFlag = true;
                _rightPage.btnSure.SetRightClickedHandler(BtnSure_RightClicked);
                _operatePage = _mainWindow.operateFrame.Content as OperatePage ?? new OperatePage();
                _operatePage.UpdateOperate([]);
            }
            menuId = QueryUtils.GetValueFromQueryParams(this, "menuId");
            string urlParamsTemp = QueryUtils.GetValueFromQueryParams(this, "urlParams");
            if (!string.IsNullOrEmpty(urlParamsTemp))
            {
                urlParams = Uri.UnescapeDataString(urlParamsTemp);
            }
            pageName = QueryUtils.GetValueFromQueryParams(this, "pageName");
            _ = InitUserDefine();
            inputText.Focus();
            _mainWindow?.ShowKeyboardPage(1);
        }

        //初始化数据
        private async Task InitUserDefine()
        {
            //查询用不基础配置信息
            var list = await SqlHelper.TableAsync<UserDefineDataModel>().Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count > 0)
            {
                _userDefineDataModel = list[0];
            }
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            _mainWindow?.NavigateToPage("MainMenu");
        }

        private async void BtnSure_RightClicked(object? sender, bool e)
        {
            if (!string.IsNullOrEmpty(inputText.Password))
            {
                if (_userDefineDataModel.SystemPassword.Equals(inputText.Password))
                {
                    await UpdatePassWordTime();
                    if (!string.IsNullOrEmpty(pageName))
                    {
                        _mainWindow?.NavigateToPage(pageName, urlParams);
                    }
                    else
                    {
                        _mainWindow?.NavigateToPage("MainMenu", $"menuId={menuId}");
                    }
                }
                else
                {
                    MaterialSnack("密码错误", SnackType.ERROR);
                }
            }
        }

        //刷新当前时间戳
        private async Task UpdatePassWordTime()
        {
            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _userDefineDataModel.SystemPasswordTime = currentTimestamp;
            await SqlHelper.UpdateAsync(_userDefineDataModel);
        }

        private void inputText_TouchDown(object sender, TouchEventArgs e)
        {
            _mainWindow?.ShowKeyboardPage(1);
        }

        private void inputText_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _mainWindow?.ShowKeyboardPage(1);
        }
    }
}
