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
using 精密切割系统.ViewModel;

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
            GlobalParams.cutStatusInfo = 2;
            // 加载参数
            //string[] query = Uri.UnescapeDataString(NavigationService.CurrentSource.OriginalString).Split("?");
            //if (query.Length == 2 )
            //{
            //    var runViewModel = JsonConvert.DeserializeObject<MQSemiAutomaticCuttingRunViewModel>(query[1]);
            //    if (runViewModel is not null )
            //    {
            //        _viewModel.DeviceDataNo = runViewModel.DeviceDataNo;
            //        _viewModel.DeviceDataId = runViewModel.DeviceDataId;
            //        _viewModel.RunCutLine = runViewModel.RunCutLine;
            //        _viewModel.AllRunCutLine = runViewModel.AllRunCutLine;
            //        _viewModel.ChannelNum = runViewModel.ChannelNum;
            //        _viewModel.BladeHeight = runViewModel.BladeHeight.ToString();
            //        _viewModel.FeedSpeed = runViewModel.FeedSpeed.ToString();
            //        _viewModel.DepthCompensation = runViewModel.DepthCompensation;
            //        _viewModel.ChangeFeedSpeed = runViewModel.ChangeFeedSpeed;
            //        _viewModel.ExpectedProcessingEndTime = runViewModel.ExpectedProcessingEndTime;
            //        _viewModel.AllCutLine = runViewModel.AllCutLine;
            //        _viewModel.AllCutLineLength = runViewModel.AllCutLineLength.ToString();
            //    }
            //}
            UpdateDefineDataModel();
        }

        //根据默认配置控制对应显示和隐藏
        private void UpdateDefineDataModel()
        {
            UserDefineDataModel userDefineModel = CurrentUtils.GetCurrentUserDefineDataModel();
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