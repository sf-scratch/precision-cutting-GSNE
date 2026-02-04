using Prism.Navigation.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using 精密切割系统.Model.cut;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.View.Pages.common;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

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

        //public static void NavigateToPage<T>(string pageName, T paramObj)
        //{
        //    //跳转界面
        //    MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;
        //    if (mainWindow == null)
        //    {
        //        MaterialSnack("跳转界面失败", SnackType.WARNING, 0);
        //        return;
        //    }
        //    if (paramObj != null)
        //    {
        //        mainWindow.NavigateToPage(pageName, paramObj);
        //    }
        //    else
        //    {
        //        mainWindow.NavigateToPage(pageName);
        //    }
        //}

        public static bool TryParse<View, Data>(this NavigationEventArgs e, out View view, out Data data)
        {
            if (e is { Content: View v, ExtraData: Data d })
            {
                (view, data) = (v, d);
                return true;
            }
            (view, data) = (default!, default!);
            return false;
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
            operatePage.SetOperateShowType(3);
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