using System;
using System.Collections.Generic;
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
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;

namespace 精密切割系统.View.Pages.F4_BladeMaintenance
{
    /// <summary>
    /// BladeInfo.xaml 的交互逻辑
    /// </summary>
    public partial class BladeInfo : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        private OperatePage? operatePage;
        private BladeHeightModel _model = null;
        private string urlParams = null;
        private string pageName = null;

        public BladeInfo()
        {
            mainWindow = Application.Current.MainWindow as MainWindow;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            rightPage = mainWindow.rightFrame.Content as RightPage;
            rightPage.PanelAction.Visibility = Visibility.Visible;
            rightPage.btnBack.Visibility = Visibility.Visible;
            rightPage.btnBack.BackFlag = false;
            rightPage.btnBack.SetRightClickedHandler(BtnBack_RightClicked);
            rightPage.btnBack.GlobalRunOperateFlag = true;
            string urlParamsTemp = QueryUtils.GetValueFromQueryParams(this, "urlParams");
            if (!string.IsNullOrEmpty(urlParamsTemp))
            {
                urlParams = Uri.UnescapeDataString(urlParamsTemp);
            }
            pageName = QueryUtils.GetValueFromQueryParams(this, "pageName");
            mainWindow.UpdateOperatePage(new List<OperateBean>(), null);
            InitData();
        }

        private void InitData()
        {
            bladeOuterDiameter.Text = Appsettings.BladeOuterDiameter?.ToString("F3");
            bladeThickness.Text = Appsettings.BladeThickness?.ToString("F3");
            afterReplaceBladeCutTimes.Text = Appsettings.AfterReplaceBladeCutTimes?.ToString();
            afterReplaceBladeCutLength.Text = (Appsettings.AfterReplaceBladeCutLength / 1000 ?? 0).ToString("F2");
            afterMeasureHeightCutTimes.Text = Appsettings.AfterMeasureHeightCutTimes?.ToString();
            afterMeasureHeightCutLength.Text = (Appsettings.AfterMeasureHeightCutLength / 1000 ?? 0).ToString("F2");
            afterClearDataCutTimes.Text = Appsettings.AfterClearDataCutTimes?.ToString();
            afterClearDataCutLength.Text = (Appsettings.AfterClearDataCutLength / 1000 ?? 0).ToString("F2");
            measureHeightFirst.Text = Appsettings.MeasureHeightFirst?.ToString("F3");
            measureHeightLast.Text = Appsettings.MeasureHeightLast?.ToString("F3");
        }

        private void BtnBack_RightClicked(object? sender, bool e)
        {
            if (string.IsNullOrEmpty(pageName))
            {
                mainWindow.NavigateToPage("MainMenu");
            }
            else
            {
                mainWindow.NavigateToPage(pageName, urlParams);
            }
        }
    }
}