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
using 精密切割系统.Entities;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Utils;
using 精密切割系统.View.common;
using 精密切割系统.View.Controls;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.operate;
using 精密切割系统.ViewModel;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.View.Pages.F4_BladeMaintenance
{
    /// <summary>
    /// BladeInfo.xaml 的交互逻辑
    /// </summary>
    public partial class BladeInfo : Page
    {
        private MainWindow? mainWindow;
        private RightPage? rightPage;
        public static string? PageName { get; set; } = null;

        public BladeInfo()
        {
            mainWindow = Application.Current.MainWindow as MainWindow;
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            NavigateUtils.ClearOperatePage();
            WindowLayout.RightPageButtons.Clear();
            WindowLayout.RightPageButtons.Add(ButtonParams.Back(Back));
            WindowLayout.OperatePageButtons.Clear();
            WindowLayout.OperatePageButtons.Add(ButtonParams.BlueButton("预切关闭", "/Assets/icon/tab_1/02/tab_27.png", ClosePrecut));
            await InitDataAsync();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            WindowLayout.OperatePageButtons.Clear();
        }

        private async Task InitDataAsync()
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
            var bladeInfo = await SqlHelper.GetOrCreateEntityAsync(() => new BladeInfoEntity());
            toolHolderOuterDiameter.Text = bladeInfo.ToolHolderOuterDiameter;
        }

        private void ClosePrecut()
        {
            SemiAutoCutService.Instance.IsOpenPrecut = false;
            MaterialSnack("关闭预切割！", SnackType.SUCCESS);
        }

        private void Back()
        {
            if (string.IsNullOrEmpty(PageName))
            {
                mainWindow.NavigateToPage("MainMenu");
            }
            else
            {
                ContainerLocator.Container.Resolve<IRegionManager>().RequestNavigate(RegionName.MainRegion, PageName);
            }
        }
    }
}