using Prism.Navigation.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using 精密切割系统.Model.cut;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.View.Pages.common;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;
using static 精密切割系统.View.Pages.operate.OperatePage;

namespace 精密切割系统.Helpers
{
    public static class NavigateUtils
    {
        public static void NavigateToPage(string pageName, string? paramsStr = default, bool isNavigateEmpty = true)
        {
            WarmUpHelper.StopWarmUp();
            //跳转界面
            MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null)
            {
                MaterialSnack("跳转界面失败", SnackType.WARNING, 0);
                return;
            }
            mainWindow.NavigateToPage(pageName, paramsStr ?? string.Empty, isNavigateEmpty);
        }

        public static void ToOperateButton(OperateType type = OperateType.PrismOperationMenu)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Keyboard.ClearFocus();
                if (Application.Current.MainWindow is not MainWindow mainWindow)
                {
                    return;
                }
                if (mainWindow.rightFrame.Content is not RightPage rightPage || mainWindow.operateFrame.Content is not OperatePage operatePage)
                {
                    return;
                }
                operatePage.SetOperateShowType(type);
            });
        }

        public static void ClearOperatePage()
        {
            MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null)
            {
                return;
            }
            RightPage? rightPage = mainWindow.rightFrame.Content as RightPage;
            OperatePage? operatePage = mainWindow.operateFrame.Content as OperatePage;
            if (rightPage == null || operatePage == null)
            {
                return;
            }
            operatePage.SetOperateShowType(OperateType.PrismOperationMenu);
            operatePage.UpdateOperate([]);
            rightPage.PanelAction.Visibility = Visibility.Visible;
        }

        public static void ClearMainFrame()
        {
            MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null)
            {
                return;
            }
            mainWindow.mainFrame.Content = null;
        }

        public static void SetWindowIsEnable(bool isEnable)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow == null)
                {
                    return;
                }
                mainWindow.IsEnabled = isEnable;
            });
        }
    }
}