using Newtonsoft.Json;
using NPOI.POIFS.Crypt.Dsig;
using Prism.Events;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.HttpClients;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.cut.Workpieces;
using 精密切割系统.Model.MeasureHeight;
using 精密切割系统.Model.plc;
using 精密切割系统.PubSubEvent;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.Auto;
using 精密切割系统.View.Pages.F4_BladeMaintenance;
using 精密切割系统.View.Pages.operate;


namespace 精密切割系统.View.Pages.F2_ManualOperation
{
    /// <summary>
    /// MQSemiAutomaticCuttingRun.xaml 的交互逻辑
    /// </summary>
    public partial class MQSemiAutomaticCuttingRun : UserControl
    {
        private readonly MainWindow _mainWindow;
        private RightPage _rightPage;

        public MQSemiAutomaticCuttingRun()
        {
            InitializeComponent();
            _mainWindow = Application.Current.MainWindow as MainWindow ?? new MainWindow();
            _mainWindow.UpdateOperatePage([], null);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateDefineDataModel();
        }

        private async void UpdateDefineDataModel()
        {
            UserDefineDataModel userDefineModel = await SqlHelper.GetOrCreateEntityAsync(() => new UserDefineDataModel());
            bool isSpeedChange = "NO".Equals(userDefineModel.SpeedChange);
            bool isHeightChange = "NO".Equals(userDefineModel.HeightChange);
            if (isSpeedChange)//速度变更
            {
                ChangeFeedSpeed1.Visibility = Visibility.Hidden;
                ChangeFeedSpeed2.Visibility = Visibility.Hidden;
                ChangeFeedSpeed3.Visibility = Visibility.Hidden;
            }
            if (isHeightChange)//高度补偿
            {
                HeightChange1.Visibility = Visibility.Hidden;
                HeightChange2.Visibility = Visibility.Hidden;
                HeightChange3.Visibility = Visibility.Hidden;
            }
        }
    }
}