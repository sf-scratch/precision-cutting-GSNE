using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using 精密切割系统.Assets.config.buttom;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.Helpers;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.plc;
using 精密切割系统.Utils;
using 精密切割系统.View.page.right;
using 精密切割系统.View.Pages.common;

namespace 精密切割系统.View.Pages.F2_ManualOperation
{
    /// <summary>
    /// MQSemiAutomaticCuttingStop.xaml 的交互逻辑
    /// </summary>
    public partial class MQSemiAutomaticCuttingStop : UserControl
    {
        public MQSemiAutomaticCuttingStop()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateDefineDataModel();
        }

        //根据默认配置控制对应显示和隐藏
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